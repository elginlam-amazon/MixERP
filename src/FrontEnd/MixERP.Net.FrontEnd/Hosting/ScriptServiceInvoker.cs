/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Web.Script.Services;
using MixERP.Net.Web.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.FrontEnd.Hosting
{
    /// <summary>
    ///     Executes script service calls with the ASP.NET AJAX (ASMX [ScriptService]) wire format which the MixERP
    ///     client scripts use: POST {service}.asmx/{method} with a JSON object of parameters, answered with {"d": result};
    ///     failures are answered with HTTP 500 and {"Message": ..., "ExceptionType": ..., "StackTrace": ...}.
    /// </summary>
    internal static class ScriptServiceInvoker
    {
        private const string Extension = ".asmx/";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            //JavaScriptSerializer compatible dates: "\/Date(ticks)\/".
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static bool TryParse(string path, out string servicePath, out string methodName)
        {
            servicePath = null;
            methodName = null;

            int index = path.IndexOf(Extension, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                return false;
            }

            servicePath = path.Substring(0, index + Extension.Length - 1);
            methodName = path.Substring(index + Extension.Length).Trim('/');
            return !string.IsNullOrWhiteSpace(methodName) && !methodName.Contains('/');
        }

        public static async Task InvokeAsync(HttpContext context, Type serviceType, string methodName)
        {
            MethodInfo method = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) && m.GetCustomAttribute<WebMethodAttribute>() != null);

            if (method == null)
            {
                await WriteErrorAsync(context, new InvalidOperationException("Unknown web method " + methodName + ".")).ConfigureAwait(false);
                return;
            }

            object result;

            try
            {
                object[] arguments = await BindArgumentsAsync(context, method).ConfigureAwait(false);
                object service = Activator.CreateInstance(serviceType);

                try
                {
                    result = method.Invoke(service, arguments);
                }
                finally
                {
                    (service as IDisposable)?.Dispose();
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Log.Warning("Script service {Service}.{Method} failed. {Exception}.", serviceType.FullName, methodName, ex.InnerException);
                await WriteErrorAsync(context, ex.InnerException).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is JsonException || ex is InvalidOperationException || ex is FormatException)
            {
                await WriteErrorAsync(context, ex).ConfigureAwait(false);
                return;
            }

            ScriptMethodAttribute scriptMethod = method.GetCustomAttribute<ScriptMethodAttribute>();

            if (scriptMethod != null && scriptMethod.ResponseFormat == ResponseFormat.Xml && method.ReturnType != typeof(void))
            {
                context.Response.ContentType = "text/xml; charset=utf-8";
                XmlSerializer serializer = new XmlSerializer(method.ReturnType);

                using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    serializer.Serialize(writer, result);
                    await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8).ConfigureAwait(false);
                }

                return;
            }

            context.Response.ContentType = "application/json; charset=utf-8";
            JObject envelope = new JObject {["d"] = result == null ? JValue.CreateNull() : JToken.FromObject(result, JsonSerializer.Create(SerializerSettings))};
            await context.Response.WriteAsync(envelope.ToString(Formatting.None), Encoding.UTF8).ConfigureAwait(false);
        }

        private static async Task<object[]> BindArgumentsAsync(HttpContext context, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            JObject values = new JObject();

            if (context.Request.ContentType != null && context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.EnableBuffering();
                context.Request.Body.Position = 0;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8, false, 4096, true))
                {
                    string body = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        values = JObject.Parse(body);
                    }
                }

                context.Request.Body.Position = 0;
            }
            else
            {
                foreach (var pair in context.Request.Query)
                {
                    values[pair.Key] = pair.Value.ToString();
                }

                if (context.Request.HasFormContentType)
                {
                    IFormCollection form = await context.Request.ReadFormAsync().ConfigureAwait(false);

                    foreach (var pair in form)
                    {
                        values[pair.Key] = pair.Value.ToString();
                    }
                }
            }

            JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                JToken token = values.GetValue(parameter.Name, StringComparison.OrdinalIgnoreCase);

                if (token == null)
                {
                    if (parameter.HasDefaultValue)
                    {
                        arguments[i] = parameter.DefaultValue;
                        continue;
                    }

                    throw new InvalidOperationException("Invalid web service call, missing value for parameter: '" + parameter.Name + "'.");
                }

                arguments[i] = token.Type == JTokenType.Null ? null : token.ToObject(parameter.ParameterType, serializer);
            }

            return arguments;
        }

        private static async Task WriteErrorAsync(HttpContext context, Exception exception)
        {
            bool showDetails = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Headers["jsonerror"] = "true";

            JObject error = new JObject
            {
                ["Message"] = exception.Message,
                ["StackTrace"] = showDetails ? exception.StackTrace : string.Empty,
                ["ExceptionType"] = exception.GetType().FullName
            };

            await context.Response.WriteAsync(error.ToString(Formatting.None), Encoding.UTF8).ConfigureAwait(false);
        }
    }
}
