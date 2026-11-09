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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     The application root is also the web root (pages, scripts and configuration files live side by side).
    ///     IIS request filtering used to hide the server-side files; Kestrel has no such defaults, so the
    ///     same hidden segments and denied extensions are rejected here before Web Forms sees the request.
    /// </summary>
    internal static class RequestFiltering
    {
        private static readonly HashSet<string> HiddenSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "App_Code", "App_Data", "App_GlobalResources", "App_LocalResources", "App_WebReferences", "App_Browsers", "Properties"
        };

        private static readonly HashSet<string> DeniedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // IIS requestFiltering defaults (applicationHost.config).
            ".asa", ".asax", ".ascx", ".master", ".skin", ".browser", ".sitemap", ".config", ".cs", ".csproj", ".vb", ".vbproj",
            ".webinfo", ".licx", ".resx", ".resources", ".mdb", ".mdf", ".ldf", ".exclude", ".refresh", ".compiled", ".rules",
            // Files IIS never served because they had no MIME mapping.
            ".snk", ".sql", ".pdb", ".dll", ".exe", ".tt", ".bat", ".psess", ".userprefs", ".licenseheader", ".vsdoc", ".pubxml"
        };

        private static readonly HashSet<string> DeniedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "appsettings.json", "appsettings.Development.json", "web.config", "packages.config"
        };

        internal static IApplicationBuilder UseRequestFiltering(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (IsDenied(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next(context);
            });
        }

        private static bool IsDenied(PathString path)
        {
            if (!path.HasValue)
            {
                return false;
            }

            string[] segments = path.Value.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(segment => HiddenSegments.Contains(segment)))
            {
                return true;
            }

            string fileName = segments.LastOrDefault() ?? string.Empty;

            return DeniedFiles.Contains(fileName) || DeniedExtensions.Contains(Path.GetExtension(fileName));
        }
    }
}
