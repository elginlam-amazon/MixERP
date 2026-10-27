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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.i18n;
using MixERP.Net.WebControls.AttachmentFactory;
using MixERP.Net.WebControls.AttachmentFactory.Handlers;
using Serilog;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     ASP.NET Core host for MixERP (replaces IIS/System.Web hosting, Web.config handler registrations and the
    ///     OWIN SignalR startup). Web Forms pages and controls run on CoreWebForms + System.Web adapters.
    /// </summary>
    public static class Program
    {
        // Equivalent of <forms loginUrl="/SignIn.aspx" timeout="60" slidingExpiration="true" />.
        internal const string LoginUrl = "/SignIn.aspx";

        private static readonly Regex HandlerDirective = new Regex(@"<%@\s*WebHandler\b[^%]*\bClass\s*=\s*""(?<class>[^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Folders of the content root that are never served (db/web.config denied all users; build output and project files).
        private static readonly string[] BlockedFolders = {"/bin", "/obj", "/db", "/Properties"};

        // Former <authorization><deny users="?" /></authorization> folders (Modules/web.config, Resource/web.config),
        // plus the database backup folder that exposes downloadable backups.
        private static readonly string[] AuthenticatedFolders = {"/Modules", "/Resource", "/Backups"};

        public static void Main(string[] args)
        {
            // Npgsql 6+ maps timestamptz to UTC-only DateTime values; MixERP was written against Npgsql 2.x semantics.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            IReadOnlyList<Assembly> modules = ModuleLoader.LoadModules();
            IReadOnlyList<Assembly> assemblies = ModuleLoader.GetApplicationAssemblies(modules);

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // <httpRuntime maxRequestLength="1048576" /> and <requestLimits maxAllowedContentLength="1073741824" />.
            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1073741824);
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 1073741824);

            ISystemWebAdapterBuilder systemWeb = builder.Services.AddSystemWebAdapters()
                .AddJsonSessionSerializer(options => SessionKeys.Register(options, assemblies))
                .AddSessionSerializer()
                .AddWrappedAspNetCoreSession()
                .AddHttpApplication<Global>()
                .AddRouting()
                .AddHttpHandlers();

            systemWeb.AddWebForms()
                .AddScriptManager()
                .AddDynamicPages();

            builder.Services.AddSingleton<Microsoft.AspNetCore.SystemWebAdapters.SessionState.Serialization.ISessionKeySerializer, ExceptionSessionKeySerializer>();

            RegisterHttpHandlers(systemWeb, builder.Environment.ContentRootPath, assemblies);

            // <sessionState mode="InProc" cookieless="UseCookies" timeout="60" />.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // <authentication mode="Forms"> replaced by cookie authentication with the same login url and timeout.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = LoginUrl;
                    options.ReturnUrlParameter = "ReturnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR();

            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                PathString path = context.Request.Path;

                if (BlockedFolders.Any(folder => path.StartsWithSegments(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (AuthenticatedFolders.Any(folder => path.StartsWithSegments(folder, StringComparison.OrdinalIgnoreCase)) &&
                    context.User.Identity?.IsAuthenticated != true)
                {
                    await context.ChallengeAsync().ConfigureAwait(false);
                    return;
                }

                await next().ConfigureAwait(false);
            });

            FileExtensionContentTypeProvider contentTypes = new FileExtensionContentTypeProvider();
            contentTypes.Mappings[".backup"] = "application/octet-stream";

            // Server-side sources and configuration files under the content root must never be served as static files.
            foreach (string extension in new[] {".aspx", ".ascx", ".asmx", ".ashx", ".asax", ".master", ".cs", ".config", ".sql", ".snk", ".csproj"})
            {
                contentTypes.Mappings.Remove(extension);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(app.Environment.ContentRootPath),
                ContentTypeProvider = contentTypes
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseSession();
            app.UseSystemWebAdapters();

            app.MapScriptServices(assemblies, new Dictionary<string, Type>
            {
                {"/FileUploadHanlder.asmx", typeof(UploadService)}
            });

            MapHubs(app, assemblies);

            app.MapHttpHandlers();
            app.MapScriptManager();

            Log.Information("MixERP is starting.");
            app.Run();
        }

        private static void RegisterHttpHandlers(ISystemWebAdapterBuilder systemWeb, string contentRoot, IReadOnlyList<Assembly> assemblies)
        {
            // Former Web.config <handlers> registrations.
            systemWeb.AddHttpHandler<FileUploadHanlder>("/FileUploadHanlder.ashx");
            systemWeb.AddHttpHandler<ResourcesHandler>("/Resources.js");

            // *.ashx generic handlers declared with <%@ WebHandler Class="..." %>.
            MethodInfo addHttpHandler = typeof(HandlerServicesExtensions).GetMethod(nameof(HandlerServicesExtensions.AddHttpHandler));

            foreach (string file in Directory.EnumerateFiles(contentRoot, "*.ashx", SearchOption.AllDirectories))
            {
                Match match = HandlerDirective.Match(File.ReadAllText(file));

                if (!match.Success)
                {
                    continue;
                }

                Type handlerType = ModuleLoader.FindType(assemblies, match.Groups["class"].Value.Trim());

                if (handlerType == null)
                {
                    continue;
                }

                string route = "/" + Path.GetRelativePath(contentRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                addHttpHandler.MakeGenericMethod(handlerType).Invoke(null, new object[] {systemWeb, route});
            }
        }

        /// <summary>
        ///     SignalR 2 discovered every hub of every loaded assembly under "/signalr"; ASP.NET Core SignalR maps each
        ///     hub explicitly. Hubs are exposed at "/signalr/{hubName}" (camel-cased type name, the SignalR 2 hub name).
        /// </summary>
        private static void MapHubs(WebApplication app, IEnumerable<Assembly> assemblies)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.GetParameters().Length == 2);

            IEnumerable<Type> hubs = assemblies.SelectMany(ModuleLoader.GetTypes)
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Hub).IsAssignableFrom(t))
                .Distinct();

            foreach (Type hub in hubs)
            {
                string hubName = char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                mapHub.MakeGenericMethod(hub).Invoke(null, new object[] {app, "/signalr/" + hubName});
            }
        }
    }
}
