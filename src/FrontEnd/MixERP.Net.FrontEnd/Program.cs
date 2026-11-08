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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Entities.Office;
using MixERP.Net.Framework;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Hubs;
using MixERP.Net.i18n;
using MixERP.Net.ReportManager;
using Serilog;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     Application entry point and host configuration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class replaces <c>Global.asax</c> (<c>Global.asax.WebForms.cs</c>) and
    ///         <c>Web.config</c>. Modern .NET has no <c>HttpApplication</c>, so
    ///         <c>Application_Start</c> becomes straight line startup code before
    ///         <c>app.Run()</c> and <c>Application_Error</c> becomes the exception handler
    ///         middleware. Everything that used to live in <c>Web.config</c> — the
    ///         <c>appSettings</c> section, session state, forms authentication, globalization,
    ///         the request size limits, the extra MIME map and the HTTP handlers — is now read
    ///         from <c>appsettings.json</c> and configured here. See knowledge base docs 11 and
    ///         26.
    ///     </para>
    ///     <para>
    ///         The Web Forms pages themselves (<c>.aspx</c>, <c>.ascx</c>, <c>.master</c>) and
    ///         the ASMX services have no equivalent on modern .NET and are not served by this
    ///         host; their re-authoring backlog is tracked in <c>summary.md</c>.
    ///     </para>
    /// </remarks>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // The former <appSettings> section now lives under "AppSettings" in appsettings.json.
            // Registering the provider here gives every library the same single read path without
            // threading IConfiguration through them. See MixERP.Net.Framework.AppSettings.
            IConfiguration configuration = builder.Configuration;
            AppSettings.UseProvider(key => configuration["AppSettings:" + key]);

            ConfigureServices(builder);

            WebApplication app = builder.Build();

            Configure(app);

            await StartApplication();

            await app.RunAsync();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            IConfiguration configuration = builder.Configuration;

            // <httpRuntime maxRequestLength="1048576" /> and
            // <requestLimits maxAllowedContentLength="1073741824" /> are both expressed as the
            // Kestrel body size limit; Kestrel counts bytes where httpRuntime counted kilobytes.
            long maxRequestBody = configuration.GetValue("RequestLimits:MaxRequestBodyBytes", 1073741824L);
            builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = maxRequestBody; });

            // <sessionState mode="InProc" cookieless="UseCookies" timeout="60" />. The System.Web
            // adapters expose the ASP.NET Core session through HttpContext.Current.Session.
            int sessionTimeout = configuration.GetValue("SessionState:TimeoutMinutes", 60);

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddSystemWebAdapters().AddWrappedAspNetCoreSession();

            // <authentication mode="Forms"> with a login page, a sliding 60 minute ticket and a
            // default landing page maps onto cookie authentication.
            int authTimeout = configuration.GetValue("Authentication:TimeoutMinutes", 60);

            builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies
                    .CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = configuration["Authentication:LoginPath"] ?? "/SignIn.aspx";
                    options.ReturnUrlParameter = "ReturnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(authTimeout);
                    options.SlidingExpiration =
                        configuration.GetValue("Authentication:SlidingExpiration", true);
                });

            builder.Services.AddAuthorization();
            builder.Services.AddSignalR();
            builder.Services.AddHttpContextAccessor();
        }

        private static void Configure(WebApplication app)
        {
            // Application_Error forwarded the last error to ApplicationError.Handle. The exception
            // handler middleware is the modern equivalent; Server.GetLastError() becomes the
            // IExceptionHandlerFeature.
            app.UseExceptionHandler(errorApp => errorApp.Run(context =>
            {
                IExceptionHandlerFeature feature = context.Features.Get<IExceptionHandlerFeature>();

                try
                {
                    ApplicationError.Handle(feature?.Error);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception occurred while handling an application error. {Exception}.", ex);
                }

                return Task.CompletedTask;
            }));

            // <globalization culture="auto" uiCulture="auto" /> is request localization driven by
            // the Accept-Language header.
            if (IsAutoCulture(app.Configuration["Globalization:Culture"]) ||
                IsAutoCulture(app.Configuration["Globalization:UICulture"]))
            {
                app.UseRequestLocalization(options => { options.ApplyCurrentCultureToResponseHeaders = true; });
            }

            // <staticContent><mimeMap fileExtension=".backup" mimeType="application/octet-stream" />
            FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".backup"] = "application/octet-stream";

            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypeProvider });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();
            app.UseSystemWebAdapters();

            // <handlers><add name="Resources" path="Resources.js" type="MixERP.Net.i18n.ResourcesHandler" />
            app.MapHttpHandler<ResourcesHandler>("/Resources.js");

            // ASP.NET SignalR 2 exposed every hub on the single /signalr endpoint mapped by
            // app.MapSignalR(); ASP.NET Core SignalR routes each hub separately. See knowledge
            // base doc 32.
            app.MapHub<BaseHub>("/hubs/base");
            app.MapHub<UpdateHub>("/hubs/update");

            MapModuleHubs(app);
        }

        /// <summary>
        ///     Maps the hubs that live in the module assemblies.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The module assemblies reference this one, so this host cannot reference them back
        ///         and cannot name <c>DayOperationHub</c> or <c>DbHub</c> in a
        ///         <c>MapHub&lt;T&gt;</c> call. SignalR 2 had the same constraint and solved it by
        ///         scanning the loaded assemblies from <c>MapSignalR()</c>; the equivalent here is to
        ///         probe the module assemblies next to the host and map each hub they contain.
        ///     </para>
        ///     <para>
        ///         The modules used to build straight into the web application's <c>bin</c>
        ///         directory. Deployment has to keep placing them beside the host for this probe to
        ///         find them; see <c>summary.md</c>.
        ///     </para>
        /// </remarks>
        private static void MapModuleHubs(WebApplication app)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "MapHub" &&
                                     m.IsGenericMethodDefinition &&
                                     m.GetGenericArguments().Length == 1 &&
                                     m.GetParameters().Length == 2);

            if (mapHub == null)
            {
                Log.Error("Could not locate IEndpointRouteBuilder.MapHub<T>; module hubs were not mapped.");
                return;
            }

            foreach (Type hub in GetModuleHubTypes())
            {
                string route = "/hubs/" + TrimHubSuffix(hub.Name).ToLowerInvariant();

                try
                {
                    mapHub.MakeGenericMethod(hub).Invoke(null, new object[] { app, route });
                    Log.Information("Mapped hub {Hub} to {Route}.", hub.FullName, route);
                }
                catch (TargetInvocationException ex)
                {
                    Log.Error("Could not map hub {Hub} to {Route}. {Exception}.", hub.FullName, route, ex);
                }
            }
        }

        private static IEnumerable<Type> GetModuleHubTypes()
        {
            List<Type> hubs = new List<Type>();
            string directory = AppContext.BaseDirectory;

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return hubs;
            }

            foreach (string path in Directory.EnumerateFiles(directory, "MixERP.Net.Core.Modules.*.dll"))
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.LoadFrom(path);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
                catch (FileLoadException ex)
                {
                    Log.Warning("Could not load module assembly {Path}. {Exception}.", path, ex);
                    continue;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    //A module whose types cannot all be loaded still contributes the ones that could.
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                hubs.AddRange(types.Where(IsMappableHub));
            }

            return hubs;
        }

        private static bool IsMappableHub(Type type)
        {
            return type != null &&
                   type.IsPublic &&
                   !type.IsAbstract &&
                   !type.IsGenericTypeDefinition &&
                   typeof(Hub).IsAssignableFrom(type);
        }

        private static string TrimHubSuffix(string name)
        {
            const string suffix = "Hub";

            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - suffix.Length);
            }

            return name;
        }

        private static bool IsAutoCulture(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Equals("auto", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     The former <c>Application_Start</c> body.
        /// </summary>
        /// <remarks>
        ///     <c>Routes.RegisterRoutes</c> is gone: it mapped friendly URLs onto Web Forms pages
        ///     with <c>MapPageRoute</c>, which has no counterpart once the pages themselves are
        ///     re-authored. The routes it declared are recorded in <c>summary.md</c> so they can be
        ///     restored as endpoints.
        /// </remarks>
        private static async Task StartApplication()
        {
            LogManager.IntializeLogger();

            await UpdateManager.CheckForUpdates();

            GlobalLogin.CreateTable();
            Repository.DownloadAndInstallReports();
        }
    }
}
