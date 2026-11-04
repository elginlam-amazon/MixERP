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
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.Framework.WebServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Serves the former ASMX script services on ASP.NET Core.
    ///     Each "*.asmx" file keeps its URL; "POST /path/Service.asmx/Method" with a JSON body binds the JSON
    ///     properties to the method parameters by name and answers "{ "d": result }", which is the
    ///     ASP.NET AJAX contract the existing JavaScript (getAjax, $.ajax) relies on. Form posts and
    ///     query strings are also accepted and answered with XML, as the ASMX HttpPost/HttpGet protocols did.
    /// </summary>
    internal static class ScriptServiceEndpoints
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = {new ListItemConverter()}
        };

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);

        internal static void MapScriptServices(this IEndpointRouteBuilder endpoints,
            IEnumerable<KeyValuePair<string, Type>> services)
        {
            foreach (KeyValuePair<string, Type> service in services)
            {
                Type type = service.Value;

                if (!typeof(WebService).IsAssignableFrom(type))
                {
                    Log.Warning("{Type} declared by {Path} is not a web service.", type, service.Key);
                    continue;
                }

                Dictionary<string, MethodInfo> methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<WebMethodAttribute>() != null)
                    .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

                endpoints.MapMethods(service.Key + "/{method}", new[] {"GET", "POST"},
                    context => InvokeAsync(context, type, methods))
                    .RequireSystemWebAdapterSession()
                    .WithDisplayName("Script service " + service.Key);
            }
        }

        private static async Task InvokeAsync(HttpContext context, Type type, IDictionary<string, MethodInfo> methods)
        {
            bool isJson = context.Request.ContentType != null &&
                          context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

            try
            {
                string methodName = Convert.ToString(context.Request.RouteValues["method"], CultureInfo.InvariantCulture);
                MethodInfo method;

                if (!methods.TryGetValue(methodName, out method))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "Unknown web method {0}.", methodName));
                }

                object[] arguments = await GetArgumentsAsync(context, method, isJson);
                object instance = Activator.CreateInstance(type);
                object result;

                try
                {
                    result = method.Invoke(instance, arguments);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
                finally
                {
                    IDisposable disposable = instance as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                if (isJson)
                {
                    await WriteJsonAsync(context, new JObject {{"d", result == null ? JValue.CreateNull() : JToken.FromObject(result, Serializer)}});
                    return;
                }

                await WriteXmlAsync(context, method.ReturnType, result);
            }
            catch (Exception ex)
            {
                Log.Warning("Script service {Path} failed. {Exception}", context.Request.Path, ex);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (isJson)
                {
                    // Same shape as the ASMX JSON fault (customErrors were off for the services folders).
                    context.Response.Headers["jsonerror"] = "true";
                    await WriteJsonAsync(context, new JObject
                    {
                        {"Message", ex.Message},
                        {"StackTrace", ex.StackTrace},
                        {"ExceptionType", ex.GetType().FullName}
                    });
                    return;
                }

                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.Message);
            }
        }

        private static async Task<object[]> GetArgumentsAsync(HttpContext context, MethodInfo method, bool isJson)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];

            if (isJson)
            {
                string body;

                using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }

                JObject values = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);

                for (int i = 0; i < parameters.Length; i++)
                {
                    JToken token = values.GetValue(parameters[i].Name, StringComparison.OrdinalIgnoreCase);

                    if (token == null)
                    {
                        throw MissingParameter(parameters[i]);
                    }

                    arguments[i] = ConvertToken(token, parameters[i].ParameterType);
                }

                return arguments;
            }

            IFormCollection form = context.Request.HasFormContentType ? await context.Request.ReadFormAsync() : null;

            for (int i = 0; i < parameters.Length; i++)
            {
                string name = parameters[i].Name;
                string value = null;

                if (form != null && form.ContainsKey(name))
                {
                    value = form[name];
                }
                else if (context.Request.Query.ContainsKey(name))
                {
                    value = context.Request.Query[name];
                }

                if (value == null)
                {
                    throw MissingParameter(parameters[i]);
                }

                arguments[i] = ConvertString(value, parameters[i].ParameterType);
            }

            return arguments;
        }

        private static Exception MissingParameter(ParameterInfo parameter)
        {
            return new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                "Invalid web service call, missing value for parameter: '{0}'.", parameter.Name));
        }

        private static object ConvertToken(JToken token, Type type)
        {
            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                return type.IsValueType && Nullable.GetUnderlyingType(type) == null ? Activator.CreateInstance(type) : null;
            }

            // JavaScriptSerializer converted JSON strings to the parameter type with its TypeConverter
            // using the invariant culture (dates, numbers and enums are posted as strings by the scripts).
            if (token.Type == JTokenType.String && type != typeof(string))
            {
                return ConvertString(token.Value<string>(), type);
            }

            return token.ToObject(type, Serializer);
        }

        private static object ConvertString(string value, Type type)
        {
            if (type == typeof(string))
            {
                return value;
            }

            Type underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null && string.IsNullOrEmpty(value))
            {
                return null;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(underlying ?? type);

            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromInvariantString(value);
            }

            return JsonConvert.DeserializeObject(value, type, SerializerSettings);
        }

        private static async Task WriteJsonAsync(HttpContext context, JObject value)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(value.ToString(Formatting.None));
        }

        private static async Task WriteXmlAsync(HttpContext context, Type returnType, object result)
        {
            context.Response.ContentType = "text/xml; charset=utf-8";

            if (returnType == typeof(void))
            {
                return;
            }

            XmlSerializer serializer = new XmlSerializer(result == null ? returnType : result.GetType(), "http://tempuri.org/");

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(writer, result);
                await context.Response.WriteAsync(writer.ToString());
            }
        }

        /// <summary>
        ///     Writes ListItem as JavaScriptSerializer did for the properties the scripts read
        ///     (Text, Value, Selected, Enabled), without walking the attribute and style collections.
        /// </summary>
        private sealed class ListItemConverter : JsonConverter<ListItem>
        {
            public override bool CanRead
            {
                get { return false; }
            }

            public override ListItem ReadJson(JsonReader reader, Type objectType, ListItem existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, ListItem value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Attributes");
                writer.WriteStartObject();
                writer.WriteEndObject();
                writer.WritePropertyName("Enabled");
                writer.WriteValue(value.Enabled);
                writer.WritePropertyName("Selected");
                writer.WriteValue(value.Selected);
                writer.WritePropertyName("Text");
                writer.WriteValue(value.Text);
                writer.WritePropertyName("Value");
                writer.WriteValue(value.Value);
                writer.WriteEndObject();
            }
        }
    }
}
