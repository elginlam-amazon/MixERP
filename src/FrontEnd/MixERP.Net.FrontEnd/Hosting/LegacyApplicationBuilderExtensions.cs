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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MixERP.Net.FrontEnd.Hosting
{
    /// <summary>
    ///     Middleware replacing IIS/Web.config features: static content, location based authorization and error handling.
    /// </summary>
    internal static class LegacyApplicationBuilderExtensions
    {
        /// <summary>
        ///     Folders which formerly denied anonymous users (Modules/web.config and Resource/web.config).
        /// </summary>
        private static readonly string[] AuthenticatedOnlyPaths = {"/Modules", "/Resource"};

        /// <summary>
        ///     Extensions served as static content from the application folder. Other files (configuration, source,
        ///     database scripts, JSON settings ...) are never served.
        /// </summary>
        private static readonly Dictionary<string, string> StaticContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".js", "application/javascript"},
            {".css", "text/css"},
            {".map", "application/json"},
            {".html", "text/html"},
            {".htm", "text/html"},
            {".txt", "text/plain"},
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".gif", "image/gif"},
            {".bmp", "image/bmp"},
            {".svg", "image/svg+xml"},
            {".ico", "image/x-icon"},
            {".woff", "font/woff"},
            {".woff2", "font/woff2"},
            {".ttf", "font/ttf"},
            {".otf", "font/otf"},
            {".eot", "application/vnd.ms-fontobject"},
            {".pdf", "application/pdf"},
            {".csv", "text/csv"},
            {".xls", "application/vnd.ms-excel"},
            {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".zip", "application/zip"},
            {".backup", "application/octet-stream"}
        };

        public static void UseLegacyExceptionHandler(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                return;
            }

            app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("An unexpected error occurred. The error was logged.").ConfigureAwait(false);
            }));
        }

        public static void UseAnonymousAccessRules(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                PathString path = context.Request.Path;
                bool restricted = AuthenticatedOnlyPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

                if (restricted && context.User?.Identity?.IsAuthenticated != true)
                {
                    Log.Verbose("Anonymous access to {Path} was challenged.", path);
                    await context.ChallengeAsync().ConfigureAwait(false);
                    return;
                }

                await next().ConfigureAwait(false);
            });
        }

        public static void UseLegacyStaticFiles(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StaticContentTypes, StringComparer.OrdinalIgnoreCase));
            PhysicalFileProvider files = new PhysicalFileProvider(environment.ContentRootPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = provider,
                ServeUnknownFileTypes = false
            });
        }

        internal static bool IsStaticContent(string path)
        {
            return StaticContentTypes.ContainsKey(Path.GetExtension(path ?? string.Empty));
        }
    }
}
