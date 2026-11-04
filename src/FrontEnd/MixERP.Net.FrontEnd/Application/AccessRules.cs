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
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     The folder level authorization rules of the former web.config files, plus protection for the
    ///     files that now live in the content root (which also serves the static files).
    /// </summary>
    internal static class AccessRules
    {
        // <deny users="*" /> in db/web.config, and folders that IIS never served.
        private static readonly PathString[] Forbidden =
        {
            new PathString("/db"),
            new PathString("/bin"),
            new PathString("/obj"),
            new PathString("/Properties")
        };

        // <deny users="?" /> in Modules/web.config and Resource/web.config.
        // Backups (database backup downloads) did not have a rule; they now require a signed in user as well.
        private static readonly PathString[] AuthenticatedOnly =
        {
            new PathString("/Modules"),
            new PathString("/Resource"),
            new PathString("/Backups")
        };

        internal static IApplicationBuilder UseFolderAuthorization(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                PathString path = context.Request.Path;

                if (Forbidden.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)) || IsRootSettingsFile(path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                bool authenticated = context.User != null && context.User.Identity != null &&
                                     context.User.Identity.IsAuthenticated;

                if (!authenticated && AuthenticatedOnly.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
                {
                    await context.ChallengeAsync();
                    return;
                }

                await next(context);
            });
        }

        // appsettings*.json, *.runtimeconfig.json etc. in the content root must never be served as static files.
        private static bool IsRootSettingsFile(PathString path)
        {
            string value = path.Value ?? string.Empty;

            return value.LastIndexOf('/') == 0 && value.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
