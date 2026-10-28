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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Hubs;
using MixERP.Net.WebControls.AttachmentFactory;
using MixERP.Net.WebControls.AttachmentFactory.Handlers;
using Serilog;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    /// ASP.NET Core host (replaces the IIS/System.Web pipeline configured in Web.config and Global.asax).
    /// Web Forms runs on CoreWebForms over the System.Web adapters; Global (HttpApplication) still receives
    /// Application_Start/Application_Error through AddHttpApplication.
    /// </summary>
    public static class Program
    {
        // Former <httpRuntime maxRequestLength="1048576"/> (KB) / <requestLimits maxAllowedContentLength="1073741824"/>.
        private const long MaxRequestBodySize = 1073741824;

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            IReadOnlyList<Assembly> modules = ModuleLoader.LoadModules();

            builder.Services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = MaxRequestBodySize);
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxRequestBodySize);

            // Former <authentication mode="Forms"><forms loginUrl="/SignIn.aspx" timeout="60" slidingExpiration="true"/>.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = FormsAuthentication.LoginUrl;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                });
            builder.Services.AddAuthorization();

            // Former <sessionState mode="InProc" cookieless="UseCookies" timeout="60"/>.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSignalR();

            builder.Services.AddSystemWebAdapters()
                .AddHttpApplication<Global>()
                .AddWrappedAspNetCoreSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(60);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                })
                .AddJsonSessionSerializer(options =>
                {
                    options.RegisterKey<string>("Challenge");
                    options.RegisterKey<string>("UserName");
                    options.RegisterKey<bool>("FirstStepsPending");
                    options.RegisterKey<int>("InvalidPasswordAttempts");
                })
                .AddHttpHandlers()
                .AddRouting(Routes.RegisterRoutes)
                .AddWebForms();

            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            // The site's static assets live next to the Web Forms markup (content root), as they did under IIS.
            // Only asset types are served so binaries, configuration and data files are never exposed.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = app.Environment.ContentRootFileProvider,
                ContentTypeProvider = GetStaticContentTypeProvider(),
                ServeUnknownFileTypes = false
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseSystemWebAdapters();

            string contentRoot = app.Environment.ContentRootPath;

            // Former <system.webServer><handlers> registrations.
            app.MapHttpHandler<FileUploadHanlder>("/FileUploadHanlder.ashx");
            app.MapHttpHandler<MixERP.Net.i18n.ResourcesHandler>("/Resources.js");
            MapMarkupHandlers(app, contentRoot);

            app.MapScriptServices(contentRoot, new Dictionary<string, Type>
            {
                { "/FileUploadHanlder.asmx", typeof(UploadService) }
            });

            // SignalR 2 (OWIN app.MapSignalR) hubs, including the hubs contributed by the modules.
            app.MapHub<BaseHub>("/signalr/baseHub");
            app.MapHub<UpdateHub>("/signalr/updateHub");
            MapModuleHubs(app, modules);

            app.MapHttpHandlers();

            try
            {
                app.Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static FileExtensionContentTypeProvider GetStaticContentTypeProvider()
        {
            FileExtensionContentTypeProvider defaults = new FileExtensionContentTypeProvider();
            string[] allowed = { ".css", ".js", ".map", ".html", ".htm", ".xml", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot", ".otf", ".swf" };

            Dictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string extension in allowed)
            {
                string contentType;

                if (defaults.TryGetContentType("file" + extension, out contentType))
                {
                    mappings[extension] = contentType;
                }
            }

            // Former <staticContent><mimeMap fileExtension=".backup"/> (database backups downloaded from BackOffice).
            mappings[".backup"] = "application/octet-stream";

            return new FileExtensionContentTypeProvider(mappings);
        }

        private static void MapMarkupHandlers(IEndpointRouteBuilder endpoints, string contentRoot)
        {
            foreach (string file in ScriptServiceEndpoints.EnumerateMarkupFiles(contentRoot, "*.ashx"))
            {
                Type type = ScriptServiceEndpoints.ResolveMarkupClass(file);
                string route = "/" + Path.GetRelativePath(contentRoot, file).Replace('\\', '/');

                if (type == null || !typeof(System.Web.IHttpHandler).IsAssignableFrom(type))
                {
                    Log.Warning("Handler {Route} was not mapped because its class could not be resolved.", route);
                    continue;
                }

                endpoints.MapHttpHandler(route, type);
            }
        }

        private static void MapModuleHubs(IEndpointRouteBuilder endpoints, IEnumerable<Assembly> modules)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.GetParameters().Length == 2);

            foreach (Type hub in modules.SelectMany(ModuleLoader.GetTypes).Where(t => !t.IsAbstract && typeof(Hub).IsAssignableFrom(t)))
            {
                string name = char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                mapHub.MakeGenericMethod(hub).Invoke(null, new object[] { endpoints, "/signalr/" + name });
            }
        }
    }
}
