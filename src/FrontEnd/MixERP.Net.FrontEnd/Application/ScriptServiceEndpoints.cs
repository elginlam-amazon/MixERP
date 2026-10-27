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
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using MixERP.Net.Framework.WebServices;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Hosts the MixERP script services (the former ASMX "*.asmx/{method}" endpoints) on ASP.NET Core,
    ///     keeping the wire contract used by the client scripts:
    ///     JSON requests receive <c>{"d": result}</c> (and <c>{"Message": ...}</c> with HTTP 500 on errors),
    ///     other requests (form posts / query strings) receive the XML-serialized result.
    /// </summary>
    public static class ScriptServiceEndpoints
    {
        // ASMX limit configured in Web.config (system.web.extensions/scripting/webServices/jsonSerialization).
        private const int MaxJsonLength = 5000000;

        private static readonly Regex ClassDirective = new Regex(@"<%@\s*WebService\b[^%]*\bClass\s*=\s*""(?<class>[^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void MapScriptServices(this WebApplication app, IReadOnlyList<Assembly> assemblies,
            IDictionary<string, Type> additionalServices)
        {
            Dictionary<string, Type> services = DiscoverServices(app.Environment.ContentRootPath, assemblies);

            foreach (KeyValuePair<string, Type> service in additionalServices)
            {
                services[service.Key] = service.Value;
            }

            foreach (KeyValuePair<string, Type> service in services)
            {
                foreach (MethodInfo method in GetWebMethods(service.Value))
                {
                    MapWebMethod(app, service.Key + "/" + method.Name, service.Value, method);
                }
            }
        }

        private static Dictionary<string, Type> DiscoverServices(string contentRoot, IReadOnlyList<Assembly> assemblies)
        {
            Dictionary<string, Type> services = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in Directory.EnumerateFiles(contentRoot, "*.asmx", SearchOption.AllDirectories))
            {
                Match match = ClassDirective.Match(File.ReadAllText(file));

                if (!match.Success)
                {
                    continue;
                }

                string className = match.Groups["class"].Value.Trim();
                Type type = ModuleLoader.FindType(assemblies, className);

                if (type == null)
                {
                    Log.Warning("The script service {Service} declared in {File} could not be found.", className, file);
                    continue;
                }

                string virtualPath = "/" + Path.GetRelativePath(contentRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                services[virtualPath] = type;
            }

            return services;
        }

        private static IEnumerable<MethodInfo> GetWebMethods(Type serviceType)
        {
            return serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<WebMethodAttribute>() != null);
        }

        private static void MapWebMethod(WebApplication app, string route, Type serviceType, MethodInfo method)
        {
            WebMethodAttribute webMethod = method.GetCustomAttribute<WebMethodAttribute>();

            IEndpointConventionBuilder endpoint = app.MapMethods(route, new[] {HttpMethods.Get, HttpMethods.Post},
                    context => InvokeAsync(context, serviceType, method))
                .PreBufferRequestStream()
                .BufferResponseStream();

            if (webMethod != null && webMethod.EnableSession)
            {
                endpoint.RequireSystemWebAdapterSession();
            }
        }

        private static async Task InvokeAsync(HttpContext context, Type serviceType, MethodInfo method)
        {
            bool isJson = IsJsonRequest(context.Request);
            ScriptMethodAttribute scriptMethod = method.GetCustomAttribute<ScriptMethodAttribute>();
            bool respondWithXml = !isJson || (scriptMethod != null && scriptMethod.ResponseFormat == ResponseFormat.Xml);

            try
            {
                object[] arguments = isJson
                    ? await GetJsonArgumentsAsync(context.Request, method).ConfigureAwait(false)
                    : await GetFormArgumentsAsync(context.Request, method).ConfigureAwait(false);

                object result;
                object service = Activator.CreateInstance(serviceType);

                try
                {
                    result = method.Invoke(service, arguments);
                }
                finally
                {
                    (service as IDisposable)?.Dispose();
                }

                if (respondWithXml)
                {
                    await WriteXmlAsync(context, serviceType, method.ReturnType, result).ConfigureAwait(false);
                    return;
                }

                await WriteJsonAsync(context, StatusCodes.Status200OK, new Dictionary<string, object> {{"d", result}}).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Exception error = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;

                Log.Warning("Script service call {Path} failed. {Exception}.", context.Request.Path, error);

                if (!isJson)
                {
                    throw;
                }

                Dictionary<string, object> payload = new Dictionary<string, object>
                {
                    {"Message", error.Message},
                    {"ExceptionType", error.GetType().FullName}
                };

                if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                {
                    payload.Add("StackTrace", error.StackTrace);
                }

                context.Response.Headers["jsonerror"] = "true";
                await WriteJsonAsync(context, StatusCodes.Status500InternalServerError, payload).ConfigureAwait(false);
            }
        }

        private static bool IsJsonRequest(HttpRequest request)
        {
            return request.ContentType != null &&
                   request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            return new JavaScriptSerializer {MaxJsonLength = MaxJsonLength};
        }

        private static async Task<object[]> GetJsonArgumentsAsync(HttpRequest request, MethodInfo method)
        {
            string body;

            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            JavaScriptSerializer serializer = CreateSerializer();
            IDictionary<string, object> values = string.IsNullOrWhiteSpace(body)
                ? new Dictionary<string, object>()
                : serializer.Deserialize<IDictionary<string, object>>(body) ?? new Dictionary<string, object>();

            Dictionary<string, object> parameters = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);

            return method.GetParameters().Select(parameter =>
            {
                if (!parameters.TryGetValue(parameter.Name, out object value))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "Invalid web service call, missing value for parameter: '{0}'.", parameter.Name));
                }

                return serializer.ConvertToType(value, parameter.ParameterType);
            }).ToArray();
        }

        private static async Task<object[]> GetFormArgumentsAsync(HttpRequest request, MethodInfo method)
        {
            IFormCollection form = request.HasFormContentType
                ? await request.ReadFormAsync().ConfigureAwait(false)
                : null;

            return method.GetParameters().Select(parameter =>
            {
                StringValues value = form != null && form.ContainsKey(parameter.Name)
                    ? form[parameter.Name]
                    : request.Query[parameter.Name];

                if (value.Count == 0)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                        "Missing parameter: {0}.", parameter.Name));
                }

                return ConvertFromString(value.ToString(), parameter.ParameterType);
            }).ToArray();
        }

        private static object ConvertFromString(string value, Type type)
        {
            if (type == typeof(string))
            {
                return value;
            }

            return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);
        }

        private static async Task WriteJsonAsync(HttpContext context, int statusCode, object value)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(CreateSerializer().Serialize(value), Encoding.UTF8).ConfigureAwait(false);
        }

        private static async Task WriteXmlAsync(HttpContext context, Type serviceType, Type returnType, object result)
        {
            context.Response.ContentType = "text/xml; charset=utf-8";

            if (returnType == typeof(void))
            {
                return;
            }

            WebServiceAttribute webService = serviceType.GetCustomAttribute<WebServiceAttribute>();
            string xmlNamespace = webService?.Namespace ?? WebServiceAttribute.DefaultNamespace;

            XmlSerializer serializer = new XmlSerializer(returnType, xmlNamespace);

            using (StringWriter writer = new Utf8StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings {Indent = false}))
                {
                    serializer.Serialize(xmlWriter, result);
                }

                await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8).ConfigureAwait(false);
            }
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public Utf8StringWriter() : base(CultureInfo.InvariantCulture)
            {
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
