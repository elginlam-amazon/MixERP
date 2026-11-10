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

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MixERP.Net.i18n
{
    /// <summary>
    ///     Maps the route formerly served by the <c>JavascriptResources</c>
    ///     <c>IHttpHandler</c> registered in the FrontEnd's <c>Web.config</c>
    ///     (<c>path="Resources.js"</c>, <c>verb="GET,HEAD"</c>).
    /// </summary>
    public static class ResourcesEndpointExtensions
    {
        /// <summary>
        ///     Maps <c>GET</c> and <c>HEAD /Resources.js</c> — the same verb set the
        ///     Web.config handler declared (<c>verb="GET,HEAD"</c>). Call from
        ///     <c>Program.cs</c>:
        ///     <code>
        ///     app.MapResourcesEndpoint();
        ///     </code>
        /// </summary>
        public static IEndpointRouteBuilder MapResourcesEndpoint(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            endpoints.MapMethods("/Resources.js", new[] { "GET", "HEAD" },
                (HttpContext context, ResourcesScriptBuilder builder) =>
            {
                string culture = context.Request.Query["culture"].ToString();
                string script = builder.Build(culture);

                // The original handler declared the response publicly cacheable for 24
                // hours and emitted an ETag; both are observable behaviour, so they are
                // preserved here with the equivalent response headers.
                context.Response.Headers.CacheControl = "public, max-age=86400";
                context.Response.Headers.ETag = ComputeETag(script);

                return Results.Content(script, "text/javascript", Encoding.UTF8);
            });

            return endpoints;
        }

        private static string ComputeETag(string script)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(script));

            return string.Concat("\"", Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture), "\"");
        }
    }
}
