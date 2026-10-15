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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Hubs;
using MixERP.Net.i18n;
using MixERP.Net.WebControls.AttachmentFactory;
using MixERP.Net.WebControls.AttachmentFactory.Handlers;

namespace MixERP.Net.FrontEnd
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // The legacy IIS layout is kept: pages, scripts and styles live in the content root (not wwwroot).
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                WebRootPath = "."
            });

            // MixERP.Net.Core.Modules.* assemblies are built into this application's output directory and are
            // only discovered at runtime (they reference this project), exactly like the former ~/bin layout.
            LoadModuleAssemblies();

            long maxRequestBodySize = builder.Configuration.GetValue<long>("MixERP:MaxRequestBodySize", 1073741824);
            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodySize);
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxRequestBodySize);

            int timeoutMinutes = builder.Configuration.GetValue<int>("MixERP:SessionTimeoutMinutes", 60);

            builder.Services.AddMemoryCache();
            builder.Services.Configure<InProcSessionOptions>(options => options.TimeoutMinutes = timeoutMinutes);

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/SignIn.aspx";
                    options.ReturnUrlParameter = "ReturnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(timeoutMinutes);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddSystemWebAdapters()
                .AddHttpApplication<Global>()
                .AddRouting(Routes.RegisterRoutes)
                .AddWebForms()
                .AddDynamicPages();

            // Registered after AddSystemWebAdapters so it is the session manager that is resolved (InProc semantics).
            builder.Services.AddSingleton<ISessionManager, InProcSessionManager>();

            builder.Services.AddSignalR();

            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Site/RuntimeError.aspx");
                app.UseHsts();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = CreateContentTypeProvider()
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseLegacyAuthorizationRules();
            app.UseSystemWebAdapters();

            app.MapHttpHandler<FileUploadHanlder>("/FileUploadHanlder.ashx").RequireSystemWebAdapterSession();
            app.MapHttpHandler<ResourcesHandler>("/Resources.js");
            app.MapScriptService<UploadService>("/FileUploadHanlder.asmx", app.Environment);
            app.MapLegacyEndpoints(app.Environment);

            app.MapMixERPHubs(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => (a.GetName().Name ?? string.Empty).StartsWith("MixERP.Net.", StringComparison.Ordinal)));

            app.MapHttpHandlers();

            app.Run();
        }

        /// <summary>
        ///     Static file types served from the content root. Source, configuration and key files are never served
        ///     (IIS request filtering used to block them).
        /// </summary>
        private static FileExtensionContentTypeProvider CreateContentTypeProvider()
        {
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider();

            foreach (string extension in new[] {".config", ".cs", ".csproj", ".snk", ".sql", ".asax", ".pubxml", ".user", ".dll", ".pdb"})
            {
                provider.Mappings.Remove(extension);
            }

            // Database backups are downloadable (was the "*.backup" StaticFileHandler mapping).
            provider.Mappings[".backup"] = "application/octet-stream";

            return provider;
        }

        /// <summary>
        ///     Location based authorization formerly declared in the nested web.config files.
        /// </summary>
        private static void UseLegacyAuthorizationRules(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                PathString path = context.Request.Path;

                // ~/db: <deny users="*" />
                if (path.StartsWithSegments("/db", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                // ~/Modules and ~/Resource: <deny users="?" />
                bool requiresSignIn = path.StartsWithSegments("/Modules", StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWithSegments("/Resource", StringComparison.OrdinalIgnoreCase);

                if (requiresSignIn && context.User.Identity?.IsAuthenticated != true)
                {
                    context.Response.Redirect("/SignIn.aspx?ReturnUrl=" + Uri.EscapeDataString(path + context.Request.QueryString));
                    return;
                }

                await next();
            });
        }

        private static void LoadModuleAssemblies()
        {
            string[] loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToArray();

            foreach (string file in Directory.EnumerateFiles(AppContext.BaseDirectory, "MixERP.Net.Core.Modules.*.dll"))
            {
                if (!loaded.Contains(Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    Assembly.LoadFrom(file);
                }
            }
        }
    }
}
