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
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.FrontEnd.Application;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     ASP.NET Core host. The Web Forms application itself (Global.asax, Web.config, pages,
    ///     ASMX services and handlers) keeps running unchanged on WebFormsForCore; this class
    ///     replaces IIS and the OWIN startup that used to map the SignalR hubs.
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();
            builder.Services.AddAuthentication(FormsTicketAuthenticationHandler.SchemeName)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, FormsTicketAuthenticationHandler>(
                    FormsTicketAuthenticationHandler.SchemeName, null);

            WebApplication app = builder.Build();

            app.UseRequestFiltering();

            // SignalR hubs are ASP.NET Core endpoints; everything else falls through to Web Forms.
            app.UseRouting();
            app.UseAuthentication();
#pragma warning disable ASP0014 // Deliberate: the hub endpoints must run before the catch-all Web Forms middleware below.
            app.UseEndpoints(endpoints => MapHubs(endpoints));
#pragma warning restore ASP0014

            // The site relies on extensionless page routes (see Application\Routes.cs) and on the
            // Web.config authorization rules for static content, so every remaining request goes to Web Forms.
            // Integrated mode matches the IIS integrated pipeline the site was configured for (system.webServer handlers and modules).
            app.UseWebForms(options => options.UseIntegratedMode().HandleAllRequestsWithWebForms());

            app.Run();
        }

        /// <summary>
        ///     SignalR 2 discovered every Hub in the bin folder, including the hubs of the modules
        ///     (which reference this assembly, so they cannot be referenced back). The same discovery
        ///     is done here and each hub is mapped to /hubs/{hubName}, the URL used by Scripts/mixerp/signalr-hubs.js.
        /// </summary>
        private static void MapHubs(IEndpointRouteBuilder endpoints)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.GetParameters().Length == 2);

            string binDirectory = AppContext.BaseDirectory;

            foreach (string file in Directory.GetFiles(binDirectory, "MixERP.Net.*.dll"))
            {
                Assembly assembly = Assembly.Load(AssemblyName.GetAssemblyName(file));

                foreach (Type hub in GetLoadableTypes(assembly).Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t)))
                {
                    string hubName = char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                    mapHub.MakeGenericMethod(hub).Invoke(null, new object[] {endpoints, "/hubs/" + hubName});
                }
            }
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
