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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Framework.WebServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Maps the legacy "*.asmx" script services and "*.ashx" generic handlers.
    ///     ASMX is not available on .NET, so the ASP.NET AJAX JSON contract used by the MixERP
    ///     JavaScript is served here: POST (or GET) "~/{path}.asmx/{Method}" with a JSON object of
    ///     named arguments returns {"d": result}; failures return HTTP 500 with {"Message": ...}.
    /// </summary>
    public static class LegacyEndpoints
    {
        private static readonly Regex ClassDirective = new Regex("Class\\s*=\\s*\"(?<class>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly JsonSerializerSettings ResponseSettings = new JsonSerializerSettings
        {
            // ASP.NET AJAX date format ("\/Date(ms)\/") expected by the client scripts.
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
        };

        public static void MapLegacyEndpoints(this IEndpointRouteBuilder endpoints, IWebHostEnvironment environment)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (KeyValuePair<string, string> directive in FindDirectives(environment.ContentRootPath, "*.asmx"))
            {
                MapScriptService(endpoints, directive.Key, ResolveType(assemblies, directive.Value), environment);
            }

            foreach (KeyValuePair<string, string> directive in FindDirectives(environment.ContentRootPath, "*.ashx"))
            {
                Type handlerType = ResolveType(assemblies, directive.Value);

                if (handlerType != null)
                {
                    endpoints.MapHttpHandler(directive.Key, handlerType).RequireSystemWebAdapterSession();
                }
            }
        }

        /// <summary>
        ///     Maps a script service class to a virtual path (used for services registered by handler configuration).
        /// </summary>
        public static void MapScriptService<TService>(this IEndpointRouteBuilder endpoints, string path,
            IWebHostEnvironment environment) where TService : WebService
        {
            MapScriptService(endpoints, path, typeof (TService), environment);
        }

        private static void MapScriptService(IEndpointRouteBuilder endpoints, string path, Type serviceType,
            IWebHostEnvironment environment)
        {
            if (serviceType == null)
            {
                return;
            }

            endpoints.MapMethods(path + "/{method}", new[] {HttpMethods.Get, HttpMethods.Post},
                context => InvokeAsync(context, serviceType, environment))
                .RequireSystemWebAdapterSession();
        }

        private static IEnumerable<KeyValuePair<string, string>> FindDirectives(string root, string pattern)
        {
            foreach (string file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');

                if (relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Match match = ClassDirective.Match(File.ReadAllText(file));

                if (match.Success)
                {
                    yield return new KeyValuePair<string, string>("/" + relative, match.Groups["class"].Value.Trim());
                }
            }
        }

        private static Type ResolveType(IEnumerable<Assembly> assemblies, string typeName)
        {
            Type type = assemblies.Select(a => a.GetType(typeName, false)).FirstOrDefault(t => t != null);

            if (type == null)
            {
                Log.Warning("Could not resolve the legacy handler type {Type}.", typeName);
            }

            return type;
        }

        private static async Task InvokeAsync(HttpContext context, Type serviceType, IWebHostEnvironment environment)
        {
            string methodName = (string) context.Request.RouteValues["method"];

            try
            {
                MethodInfo method = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) &&
                                         m.GetCustomAttribute<WebMethodAttribute>() != null);

                if (method == null)
                {
                    throw new InvalidOperationException("Unknown web service method " + methodName + ".");
                }

                JObject arguments = await ReadArgumentsAsync(context.Request);
                object[] parameters = method.GetParameters().Select(p => GetArgument(arguments, p)).ToArray();

                object service = Activator.CreateInstance(serviceType);
                object result;

                try
                {
                    result = method.Invoke(service, parameters);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new {d = result}, ResponseSettings));
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred in web service {Service}.{Method}. {Exception}.", serviceType.Name, methodName, ex);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";

                object error = environment.IsDevelopment()
                    ? (object) new {ex.Message, ex.StackTrace, ExceptionType = ex.GetType().FullName}
                    : new {ex.Message};

                await context.Response.WriteAsync(JsonConvert.SerializeObject(error));
            }
        }

        private static async Task<JObject> ReadArgumentsAsync(HttpRequest request)
        {
            if (HttpMethods.IsGet(request.Method))
            {
                JObject query = new JObject();

                foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item in request.Query)
                {
                    // ASP.NET AJAX encodes GET arguments as JSON literals; fall back to plain strings.
                    string value = item.Value.ToString();
                    JToken token;

                    try
                    {
                        token = JToken.Parse(value);
                    }
                    catch (JsonReaderException)
                    {
                        token = new JValue(value);
                    }

                    query[item.Key] = token;
                }

                return query;
            }

            using (StreamReader reader = new StreamReader(request.Body))
            {
                string body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    return new JObject();
                }

                return JObject.Parse(body);
            }
        }

        private static object GetArgument(JObject arguments, ParameterInfo parameter)
        {
            JToken token = arguments.GetValue(parameter.Name, StringComparison.OrdinalIgnoreCase);

            if (token == null || token.Type == JTokenType.Null)
            {
                if (parameter.HasDefaultValue)
                {
                    return parameter.DefaultValue;
                }

                return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }

            return token.ToObject(parameter.ParameterType);
        }
    }
}
