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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     Replaces IIS request filtering and the folder-level Web.config authorization rules
    ///     of the Web Forms site, now that static files are served from the content root:
    ///     <list type="bullet">
    ///         <item>configuration files, binaries and the database scripts are never served;</item>
    ///         <item>/Resource (deny users="?") and /Backups require a signed-in user.</item>
    ///     </list>
    /// </summary>
    public sealed class ContentProtectionMiddleware
    {
        private static readonly string[] BlockedPrefixes = { "/appsettings.json", "/appsettings.Development.json", "/bin", "/obj", "/db", "/Resource/Configs", "/Properties", "/Designs" };
        private static readonly string[] BlockedExtensions = { ".config", ".cs", ".csproj", ".dll", ".pdb", ".snk" };
        private static readonly string[] AuthenticatedPrefixes = { "/Resource", "/Backups" };

        private readonly RequestDelegate next;

        public ContentProtectionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            PathString path = context.Request.Path;

            foreach (string prefix in BlockedPrefixes)
            {
                if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }
            }

            string value = path.Value ?? string.Empty;

            // Build output of the module projects that live below the site (Modules/*/bin, obj).
            if (value.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            foreach (string extension in BlockedExtensions)
            {
                if (value.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }
            }

            foreach (string prefix in AuthenticatedPrefixes)
            {
                if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase) &&
                    context.User?.Identity?.IsAuthenticated != true)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
            }

            return this.next(context);
        }
    }
}
