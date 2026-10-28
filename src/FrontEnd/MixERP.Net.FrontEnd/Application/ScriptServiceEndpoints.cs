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
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using MixERP.Net.Framework.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    /// Hosts the former ASMX script services on ASP.NET Core while keeping their wire contract:
    /// "{service}.asmx/{method}" with a JSON body returns {"d": result} (ASP.NET AJAX format), and
    /// form/query-string calls (the ASMX HttpPost/HttpGet protocols) return the result as XML.
    /// </summary>
    internal static class ScriptServiceEndpoints
    {
        private const string XmlNamespace = "http://tempuri.org/";
        private static readonly Regex ClassAttribute = new Regex("<%@\\s*Web(?:Service|Handler)\\b[^%]*?\\bClass\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            // JavaScriptSerializer compatible dates: "\/Date(ticks)\/".
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        internal static void MapScriptServices(this IEndpointRouteBuilder endpoints, string contentRoot, IDictionary<string, Type> additionalServices)
        {
            Dictionary<string, Type> services = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in EnumerateMarkupFiles(contentRoot, "*.asmx"))
            {
                Type type = ResolveMarkupClass(file);
                string route = "/" + Path.GetRelativePath(contentRoot, file).Replace('\\', '/');

                if (type == null || !typeof(WebService).IsAssignableFrom(type))
                {
                    Log.Warning("Script service {Route} was not mapped because its class could not be resolved.", route);
                    continue;
                }

                services[route] = type;
            }

            foreach (KeyValuePair<string, Type> service in additionalServices)
            {
                services[service.Key] = service.Value;
            }

            foreach (KeyValuePair<string, Type> service in services)
            {
                Type type = service.Value;
                endpoints.Map(service.Key + "/{method}", context => InvokeAsync(context, type)).RequireSystemWebAdapterSession();
            }
        }

        internal static IEnumerable<string> EnumerateMarkupFiles(string contentRoot, string pattern)
        {
            string bin = Path.Combine(contentRoot, "bin") + Path.DirectorySeparatorChar;
            string obj = Path.Combine(contentRoot, "obj") + Path.DirectorySeparatorChar;

            return Directory.EnumerateFiles(contentRoot, pattern, SearchOption.AllDirectories)
                .Where(file => !file.StartsWith(bin, StringComparison.Ordinal) && !file.StartsWith(obj, StringComparison.Ordinal))
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        }

        /// <summary>Reads the Class="..." attribute of a WebService/WebHandler directive.</summary>
        internal static Type ResolveMarkupClass(string file)
        {
            Match match = ClassAttribute.Match(File.ReadAllText(file));
            return match.Success ? ModuleLoader.FindType(match.Groups[1].Value.Trim()) : null;
        }

        private static async Task InvokeAsync(HttpContext context, Type serviceType)
        {
            string methodName = Convert.ToString(context.Request.RouteValues["method"], CultureInfo.InvariantCulture);
            MethodInfo method = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.Ordinal) && m.GetCustomAttribute<WebMethodAttribute>() != null);

            bool isJson = context.Request.ContentType != null && context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

            if (method == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            ScriptMethodAttribute scriptMethod = method.GetCustomAttribute<ScriptMethodAttribute>();

            if (isJson && serviceType.GetCustomAttribute<ScriptServiceAttribute>() == null)
            {
                await WriteErrorAsync(context, new InvalidOperationException("Only web services with a [ScriptService] attribute can be called from script."));
                return;
            }

            if (isJson && HttpMethods.IsGet(context.Request.Method) && (scriptMethod == null || !scriptMethod.UseHttpGet))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            object result;

            try
            {
                object[] arguments = isJson ? await BindJsonAsync(context, method) : await BindFormAsync(context, method);
                object instance = Activator.CreateInstance(serviceType);
                result = method.Invoke(instance, arguments);
            }
            catch (Exception ex)
            {
                Exception error = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                Log.Warning("Script service call {Service}.{Method} failed. {Exception}", serviceType.FullName, methodName, error);
                await WriteErrorAsync(context, error);
                return;
            }

            bool xmlResponse = !isJson || (scriptMethod != null && scriptMethod.ResponseFormat == ResponseFormat.Xml);

            if (xmlResponse)
            {
                await WriteXmlAsync(context, method.ReturnType, result);
                return;
            }

            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(new { d = result }, JsonSettings), Encoding.UTF8);
        }

        private static async Task<object[]> BindJsonAsync(HttpContext context, MethodInfo method)
        {
            string body;

            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            JObject values = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            JsonSerializer serializer = JsonSerializer.Create(JsonSettings);

            return method.GetParameters().Select(parameter =>
            {
                JToken token = values.GetValue(parameter.Name, StringComparison.OrdinalIgnoreCase);

                if (token == null)
                {
                    return GetMissingValue(parameter);
                }

                return token.Type == JTokenType.Null ? null : token.ToObject(parameter.ParameterType, serializer);
            }).ToArray();
        }

        private static async Task<object[]> BindFormAsync(HttpContext context, MethodInfo method)
        {
            IFormCollection form = context.Request.HasFormContentType ? await context.Request.ReadFormAsync() : null;

            return method.GetParameters().Select(parameter =>
            {
                StringValues value = StringValues.Empty;

                if (form == null || !form.TryGetValue(parameter.Name, out value))
                {
                    context.Request.Query.TryGetValue(parameter.Name, out value);
                }

                if (StringValues.IsNullOrEmpty(value))
                {
                    return parameter.ParameterType == typeof(string) && value.Count > 0 ? string.Empty : GetMissingValue(parameter);
                }

                return TypeDescriptor.GetConverter(parameter.ParameterType).ConvertFromInvariantString(value.ToString());
            }).ToArray();
        }

        private static object GetMissingValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Invalid web service call, missing value for parameter: '{0}'.", parameter.Name));
        }

        private static async Task WriteXmlAsync(HttpContext context, Type returnType, object result)
        {
            context.Response.ContentType = "text/xml; charset=utf-8";

            if (returnType == typeof(void))
            {
                return;
            }

            XmlSerializer serializer = new XmlSerializer(returnType, XmlNamespace);
            StringBuilder xml = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(xml, new XmlWriterSettings { Encoding = Encoding.UTF8 }))
            {
                serializer.Serialize(writer, result);
            }

            await context.Response.WriteAsync(xml.ToString(), Encoding.UTF8);
        }

        private static async Task WriteErrorAsync(HttpContext context, Exception error)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.Headers["jsonerror"] = "true";

            string json = JsonConvert.SerializeObject(new
            {
                Message = error.Message,
                StackTrace = string.Empty,
                ExceptionType = error.GetType().FullName
            });

            await context.Response.WriteAsync(json, Encoding.UTF8);
        }
    }
}
