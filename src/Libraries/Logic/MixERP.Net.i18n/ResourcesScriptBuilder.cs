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

using System.Text;
using System.Text.Encodings.Web;

namespace MixERP.Net.i18n
{
    /// <summary>
    ///     Builds the client-side <c>var Resources = { ... }</c> JavaScript object served at
    ///     <c>GET /Resources.js</c>. This is the host-agnostic replacement for the
    ///     <c>System.Web.IHttpHandler</c> implementation's script generation; the HTTP
    ///     concerns live in <see cref="ResourcesEndpointExtensions" />.
    /// </summary>
    public class ResourcesScriptBuilder
    {
        /// <summary>
        ///     Produces the localization script for the supplied culture. The structure and
        ///     ordering are identical to the original handler's output; the escaping of
        ///     non-ASCII characters differs (JavaScriptEncoder emits \uXXXX escapes where
        ///     HttpUtility passed some characters through), so the emitted JavaScript
        ///     string VALUES are equivalent rather than byte-for-byte identical.
        /// </summary>
        public string Build(string culture)
        {
            StringBuilder script = new StringBuilder();
            script.Append("var Resources = {");

            IEnumerable<LocalizedResource> resources = DbResources.GetLocalizationTable(culture);

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

                    // JavaScriptEncoder is the modern replacement for
                    // System.Web.HttpUtility.JavaScriptStringEncode: it escapes quotes,
                    // backslashes, control characters and non-ASCII text so the emitted
                    // string literal stays valid JavaScript.
                    script.Append(JavaScriptEncoder.Default.Encode(localized ?? string.Empty));

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
