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
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.JSInterop;

namespace MixERP.Net.Common.Helpers
{
    public static class JSUtility
    {
        /// <summary>
        ///     Registers an external javascript reference with the browser.
        ///     <para>
        ///         SIGNATURE CHANGE: <c>Page</c> → <see cref="IJSRuntime" />. WebForms served
        ///         embedded scripts through <c>ClientScript.GetWebResourceUrl</c>, which no longer
        ///         exists; on modern .NET a script embedded in a razor class library is served from
        ///         the static web asset path <c>/_content/&lt;assembly&gt;/&lt;resource&gt;</c>, which
        ///         is built here from <paramref name="type" />'s assembly and
        ///         <paramref name="resourceName" />. The script element is then injected once per
        ///         <paramref name="key" /> by
        ///         <see cref="PageUtility.RegisterJavascript(string, string, IJSRuntime, bool)" />,
        ///         preserving the original <c>addScriptTags: false</c> call.
        ///     </para>
        /// </summary>
        public static void AddJSReference(IJSRuntime page, string resourceName, string key, Type type)
        {
            if (page != null)
            {
                string script = "<script type='text/javascript' src='" + GetWebResourceUrl(type, resourceName) + "'></script>";
                PageUtility.RegisterJavascript(key, script, page, false);
            }
        }

        /// <summary>
        ///     The route prefix the host must map in order to serve the scripts this library
        ///     references. See the note on <see cref="GetWebResourceUrl" />.
        /// </summary>
        public const string WebResourceRoutePrefix = "/_mixerp/script";

        /// <summary>
        ///     The url of a script shipped as an EMBEDDED RESOURCE of <paramref name="type" />'s
        ///     assembly — the modern replacement for <c>ClientScriptManager.GetWebResourceUrl</c>.
        ///     <para>
        ///         The scripts remain <c>EmbeddedResource</c>s, so they are served through a single
        ///         host endpoint backed by <see cref="GetEmbeddedScript" /> rather than through
        ///         Razor-Class-Library static web assets (which these projects do not produce).
        ///         The host MUST map that endpoint once during startup composition:
        ///     </para>
        ///     <code>
        ///     app.MapGet(JSUtility.WebResourceRoutePrefix + "/{assemblyName}/{resourceName}",
        ///         (string assemblyName, string resourceName) =>
        ///         {
        ///             Assembly assembly = Assembly.Load(assemblyName);
        ///             string script = JSUtility.GetEmbeddedScript(resourceName, assembly);
        ///             return Results.Text(script, "application/javascript");
        ///         });
        ///     </code>
        ///     <para>
        ///         The resource name is passed through VERBATIM (only url-escaped). It is NOT
        ///         rewritten into a path: an embedded resource name cannot be told apart from a
        ///         filename containing dots, so rewriting dots to separators corrupted names such
        ///         as "jquery.qrcode.js", "JsBarcode.CODE128.js" and "ReportEngine.Barcode.js"
        ///         into urls that silently 404.
        ///     </para>
        /// </summary>
        public static string GetWebResourceUrl(Type type, string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return string.Empty;
            }

            string assemblyName = type == null ? string.Empty : type.Assembly.GetName().Name;

            return WebResourceRoutePrefix + "/" + Uri.EscapeDataString(assemblyName) + "/" +
                   Uri.EscapeDataString(resourceName);
        }

        public static string GetEmbeddedScript(string embeddedScriptName, Assembly assembly)
        {
            if (assembly == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(embeddedScriptName))
            {
                return string.Empty;
            }

            Stream stream = assembly.GetManifestResourceStream(embeddedScriptName);

            if (stream == null)
            {
                return string.Empty;
            }

            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetVar(string name, object value, bool isString = true)
        {
            string script = "var {0}={1};";

            if (isString)
            {
                script = "var {0}='{1}';";
            }

            if (value == null)
            {
                script = string.Format(CultureInfo.InvariantCulture, script, name, string.Empty);
            }
            else
            {
                script = string.Format(CultureInfo.InvariantCulture, script, name, value.ToString().Replace("'", @"\'"));
            }

            return script;
        }
    }
}