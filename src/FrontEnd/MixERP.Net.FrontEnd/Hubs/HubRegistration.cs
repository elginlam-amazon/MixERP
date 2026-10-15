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
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace MixERP.Net.FrontEnd.Hubs
{
    /// <summary>
    ///     Maps the MixERP SignalR hubs and serves "/signalr/hubs", the hub proxy script that the classic SignalR
    ///     generated. The script describes the hubs (name, URL and server methods) for
    ///     ~/Scripts/mixerp/signalr-hubs.js, which recreates the $.connection API on the ASP.NET Core SignalR client.
    /// </summary>
    public static class HubRegistration
    {
        private const string BasePath = "/signalr/";

        public static void MapMixERPHubs(this IEndpointRouteBuilder endpoints, IEnumerable<Assembly> assemblies)
        {
            MethodInfo mapHub = typeof (HubEndpointRouteBuilderExtensions).GetMethods()
                .First(m => m.Name == "MapHub" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);

            List<object> hubs = new List<object>();

            IEnumerable<Type> hubTypes = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => !t.IsAbstract && typeof (MixERPHub).IsAssignableFrom(t));

            foreach (Type hubType in hubTypes)
            {
                // Classic SignalR proxy naming: camelCased hub type name ($.connection.dbHub).
                string name = char.ToLowerInvariant(hubType.Name[0]) + hubType.Name.Substring(1);
                string url = BasePath + name;

                mapHub.MakeGenericMethod(hubType).Invoke(null, new object[] {endpoints, url});

                string[] methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .Select(m => char.ToLowerInvariant(m.Name[0]) + m.Name.Substring(1))
                    .Distinct()
                    .ToArray();

                hubs.Add(new {name, url, methods});
            }

            string script = "window.mixerpHubs = " + JsonConvert.SerializeObject(hubs) + ";";

            endpoints.MapGet(BasePath + "hubs", async context =>
            {
                context.Response.ContentType = "application/javascript; charset=utf-8";
                await context.Response.WriteAsync(script, Encoding.UTF8);
            });
        }
    }
}
