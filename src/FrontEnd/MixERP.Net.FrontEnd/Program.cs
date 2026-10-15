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
using System.Runtime.Loader;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    /// ASP.NET Core host. Replaces IIS/System.Web hosting and the OWIN startup (Hubs/Startup.cs, app.MapSignalR()).
    /// The Web Forms application itself (Global.asax, Web.config, pages, user controls, ASMX services and handlers)
    /// keeps running unchanged on WebFormsForCore.
    /// </summary>
    public static class Program
    {
        /// <summary>SignalR 2 served every hub below /signalr; each ASP.NET Core hub is mapped to /signalr/{hubName}.</summary>
        internal const string SignalRBasePath = "/signalr";

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();

            WebApplication app = builder.Build();

            app.Use(BridgeFormsAuthenticationForSignalR);

            app.UseRouting();
            MapHubs(app);
            app.UseEndpoints(_ => { });

            // Like IIS with runAllManagedModulesForAllRequests="true": every remaining request (pages, routes from
            // Application/Routes.cs, handlers, static files and their <location> authorization rules) goes through
            // the Web Forms pipeline configured in Web.config.
            app.UseWebForms(options => options.HandleAllRequestsWithWebForms());

            app.Run();
        }

        /// <summary>
        /// SignalR 2 ran inside the System.Web pipeline, so hubs saw the Forms Authentication user in
        /// Context.User. ASP.NET Core SignalR requests bypass that pipeline, so the Forms Authentication
        /// ticket is validated here and exposed as the request user for hub connections only.
        /// </summary>
        private static Task BridgeFormsAuthenticationForSignalR(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.StartsWithSegments(SignalRBasePath) &&
                context.Request.Cookies.TryGetValue(FormsAuthentication.FormsCookieName, out string cookie) &&
                !string.IsNullOrWhiteSpace(cookie))
            {
                try
                {
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie);

                    if (ticket != null && !ticket.Expired)
                    {
                        context.User = new GenericPrincipal(new FormsIdentity(ticket), Array.Empty<string>());
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is System.Web.HttpException || ex is System.Security.Cryptography.CryptographicException)
                {
                    Log.Warning("Invalid forms authentication ticket on a SignalR request. {Exception}", ex);
                }
            }

            return next();
        }

        /// <summary>
        /// SignalR 2 discovered hubs in every assembly of the site's bin folder (the module hubs live in the
        /// module assemblies, which this project does not reference). Do the same and map each hub.
        /// </summary>
        private static void MapHubs(IEndpointRouteBuilder endpoints)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) &&
                             m.IsGenericMethodDefinition && m.GetParameters().Length == 2);

            foreach (Type hub in DiscoverHubs())
            {
                string name = char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                mapHub.MakeGenericMethod(hub).Invoke(null, new object[] { endpoints, SignalRBasePath + "/" + name });
            }
        }

        private static IEnumerable<Type> DiscoverHubs()
        {
            string bin = AppContext.BaseDirectory;

            // Module assemblies (and their *.Data dependencies) are deployed to bin but are not part of this
            // application's dependency manifest, so resolve them from bin by name.
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string candidate = Path.Combine(bin, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };

            foreach (string file in Directory.EnumerateFiles(bin, "MixERP.Net.*.dll"))
            {
                Assembly assembly;

                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(file);
                    assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName))
                               ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types.Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t)))
                {
                    yield return type;
                }
            }
        }
    }
}
