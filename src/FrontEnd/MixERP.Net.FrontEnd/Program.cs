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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SystemWebAdapters;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Hosting;
using MixERP.Net.FrontEnd.Hubs;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     Application entry point (replaces Global.asax, the OWIN startup and the Web.config handler/module registrations).
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     Maximum request size (formerly httpRuntime maxRequestLength / requestLimits maxAllowedContentLength).
        /// </summary>
        private const long MaxRequestBodySize = 1073741824;

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = MaxRequestBodySize);
            builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MaxRequestBodySize);

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpContextAccessor();

            //System.Web compatibility (HttpContext.Current, Session, Server.MapPath, Request.Form ...).
            builder.Services.AddSystemWebAdapters();
            builder.Services.AddSingleton<ISessionManager, InProcSessionManager>();

            //Forms authentication (loginUrl="/SignIn.aspx", timeout=60, slidingExpiration) is replaced by cookie authentication.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/SignIn.aspx";
                    options.ReturnUrlParameter = "ReturnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddSignalR();

            WebApplication app = builder.Build();

            ModuleLoader.LoadModules(app.Environment.ContentRootPath);
            LegacyApplication.Start();

            app.UseLegacyExceptionHandler();
            app.UseAuthentication();
            app.UseAnonymousAccessRules();
            app.UseLegacyStaticFiles(app.Environment);
            app.UseRouting();
            app.UseAuthorization();
            app.UseSystemWebAdapters();

            app.MapHub<BaseHub>("/signalr/baseHub");
            app.MapHub<UpdateHub>("/signalr/updateHub");
            ModuleLoader.MapModuleHubs(app);

            app.MapLegacyApplication();

            app.Run();
        }
    }
}
