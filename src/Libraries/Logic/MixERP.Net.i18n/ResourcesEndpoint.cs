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

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MixERP.Net.i18n
{
    /// <summary>
    /// The JavaScript resource bundle endpoint. Replaces the former
    /// <c>ResourcesHandler : IHttpHandler</c>, which was registered in the legacy Web.config as
    /// the <c>JavascriptResources</c> handler for <c>GET,HEAD Resources.js</c>.
    /// </summary>
    public static class ResourcesEndpoint
    {
        private static readonly JavaScriptEncoder ScriptEncoder =
            JavaScriptEncoder.Create(UnicodeRanges.All);

        /// <summary>
        /// Maps <c>GET</c> and <c>HEAD</c> <c>Resources.js</c>, the exact path and verbs the
        /// legacy handler was registered for. Call this from the host's <c>Program.cs</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapResourcesEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            endpoints.MapMethods("/Resources.js", new[] { HttpMethods.Get, HttpMethods.Head },
                async (HttpContext context) =>
                {
                    string culture = context.Request.Query["culture"];
                    string script = await GetScriptAsync(culture, context.RequestAborted);

                    // Equivalent of SetCacheability(Public) + SetMaxAge(24h).
                    context.Response.Headers.CacheControl = "public, max-age=86400";

                    // The legacy handler derived Last-Modified / ETag from file dependencies,
                    // which has no ASP.NET Core equivalent. The generated script is the only
                    // thing that can change, so the validator is derived from its content.
                    context.Response.Headers.ETag = ComputeETag(script);

                    return Results.Text(script, "text/javascript", Encoding.UTF8);
                });

            return endpoints;
        }

        private static string ComputeETag(string script)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(script));
            return "\"" + Convert.ToHexString(hash).ToLowerInvariant() + "\"";
        }

        private static async Task<string> GetScriptAsync(string culture, CancellationToken cancellationToken)
        {
            StringBuilder script = new StringBuilder();
            script.Append("var Resources = {");

            IEnumerable<LocalizedResource> resources =
                await DbResources.GetLocalizationTableAsync(culture, cancellationToken);

            var resourceClassGroup = resources.GroupBy(r => r.ResourceClass)
                                .Select(group => group.ToList())
                                .ToList();

            foreach (var resourceClass in resourceClassGroup)
            {
                int i = 0;

                foreach (var resource in resourceClass)
                {
                    if (i == 0)
                    {
                        script.Append(resource.ResourceClass + ": {");
                    }

                    script.Append(resource.Key + ": function(){ return \"");
                    string localized = resource.Translated;

                    if (string.IsNullOrWhiteSpace(localized))
                    {
                        localized = resource.Original;
                    }

                    // HttpUtility.JavaScriptStringEncode escaped only the characters that are
                    // unsafe inside a JS string literal and left other non-ASCII glyphs as-is.
                    // Encoding the full Unicode range reproduces that output byte for byte.
                    script.Append(ScriptEncoder.Encode(localized));

                    script.Append("\";");
                    script.Append("},");
                    i++;
                }

                script.Append("},");
            }

            script.Append("};");

            return script.ToString();
        }
    }
}
