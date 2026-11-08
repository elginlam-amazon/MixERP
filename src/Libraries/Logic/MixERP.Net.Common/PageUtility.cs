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

using MixERP.Net.i18n.Resources;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Common.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace MixERP.Net.Common
{
    public static class PageUtility
    {
        /// <summary>
        ///     Emits a <c>&lt;meta name="..." content="..." /&gt;</c> element and appends it to the
        ///     supplied head-markup accumulator, returning the element markup.
        ///     <para>
        ///         SIGNATURE CHANGE: the first parameter was a <c>System.Web.UI.Page</c> whose
        ///         <c>Header.Controls</c> collection was mutated. Blazor has no server-side header
        ///         control tree: a component renders head metadata through
        ///         <c>&lt;HeadContent&gt;@((MarkupString)PageUtility.AddMeta(head, name, content))&lt;/HeadContent&gt;</c>.
        ///         Passing null for <paramref name="head" /> simply returns the markup.
        ///     </para>
        /// </summary>
        public static string AddMeta(StringBuilder head, string name, string content)
        {
            string meta = "<meta name=\"" + System.Net.WebUtility.HtmlEncode(name) +
                          "\" content=\"" + System.Net.WebUtility.HtmlEncode(content) + "\" />";

            if (head != null)
            {
                head.Append(meta);
            }

            return meta;
        }

        private static string GetUserHostAddress()
        {
            string host = string.Empty;

            HttpContext context = MixERPConfiguration.HttpContext;

            if (context != null)
            {
                string remote = context.Connection.RemoteIpAddress == null
                    ? null
                    : context.Connection.RemoteIpAddress.ToString();

                if (remote != null)
                {
                    host = remote;
                }
            }

            return host;
        }

        /// <summary>
        ///     Modern .NET has no browser capabilities database (".browser" files were removed),
        ///     so the raw User-Agent header is used for diagnostics instead.
        /// </summary>
        private static string GetUserAgent()
        {
            HttpContext context = MixERPConfiguration.HttpContext;

            if (context != null)
            {
                return context.Request.Headers.UserAgent.ToString();
            }

            return string.Empty;
        }

        public static void CheckInvalidAttempts(NavigationManager page)
        {
            if (page != null)
            {
                int triedAttempts = InvalidPasswordAttempts(SessionHelper.GetSession());
                int allowedAttemps = Conversion.TryCastInteger(MixERPConfiguration.GetAppSetting("MaxInvalidPasswordAttempts"));

                if (triedAttempts > 0)
                {
                    Log.Warning("{Count} of {Allowed} allowed invalid sign-in attempts from {Host}/{IP} using {Browser}.", triedAttempts, allowedAttemps, GetUserHostAddress(), GetUserIpAddress(), GetUserAgent());
                }

                if (triedAttempts >= allowedAttemps)
                {
                    Log.Error("Disallowed access to {Host}/{IP} using {Browser}.", GetUserHostAddress(), GetUserIpAddress(), GetUserAgent());
                    page.NavigateTo(ResolveUrl("~/Static/AccessIsDenied.html"), true);
                }
            }
        }

        public static string MapPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return PathHelper.NormalizePhysicalPath(path);
            }

            return MixERPConfiguration.MapPath(path);
        }


        /// <summary>
        ///     Check if the input is a valid url.
        /// </summary>
        /// <param name="url">Url to validate.</param>
        /// <returns>
        ///     Returns input if it's a valid url. If the input is not a valid url, returns empty string.
        /// </returns>
        public static string CleanUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            const string prefix = "http";

            if (url.Substring(0, prefix.Length) != prefix)
            {
                url = prefix + "://" + url;
            }

            try
            {
                //Preserves the HEAD-instead-of-GET behaviour of the original WebClient override.
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url))
                {
                    //CleanUrl is a synchronous frozen API, therefore the asynchronous
                    //HttpClient call is bridged here. A single static HttpClient instance is
                    //reused to avoid socket exhaustion.
                    using (HttpResponseMessage response = HttpClientInstance.Send(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            url = string.Empty;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                url = string.Empty;
            }
            catch (OperationCanceledException)
            {
                //HttpClient signals the request timeout with TaskCanceledException /
                //OperationCanceledException. The original WebClient.DownloadString raised
                //WebException for a timeout and this method returned string.Empty, so the
                //timeout must NOT escape CleanUrl.
                url = string.Empty;
            }
            catch (TimeoutException)
            {
                url = string.Empty;
            }
            catch (NotSupportedException)
            {
                //HttpClient.Send throws NotSupportedException for a handler without
                //synchronous support.
                url = string.Empty;
            }
            catch (InvalidOperationException)
            {
                url = string.Empty;
            }
            catch (UriFormatException)
            {
                url = string.Empty;
            }

            return url;
        }

        /// <summary>
        ///     Searches a component/element hierarchy for the descendant carrying the supplied
        ///     DOM id, returning it or null.
        ///     <para>
        ///         SIGNATURE CHANGE: the WebForms <c>Control</c> tree does not exist in Blazor, so
        ///         the hierarchy is now expressed through <see cref="IMixERPControl" /> — a component
        ///         (or markup builder) exposes its own DOM id and its children. The traversal
        ///         semantics (depth-first, self first, first match wins, null-safe) are unchanged.
        ///     </para>
        /// </summary>
        public static IMixERPControl FindControlIterative(IMixERPControl root, string id)
        {
            if (root == null)
            {
                return null;
            }

            if (root.Id == id)
            {
                return root;
            }

            if (root.Children == null)
            {
                return null;
            }

            foreach (IMixERPControl c in root.Children)
            {
                IMixERPControl t = FindControlIterative(c, id);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        public static string GetCurrentDomainName()
        {
            HttpContext context = MixERPConfiguration.HttpContext;

            if (context == null)
            {
                return string.Empty;
            }

            string url = context.Request.Scheme + "://" + context.Request.Host.Host;

            int port = context.Request.Host.Port ??
                       (string.Equals(context.Request.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80);

            if (port != 80)
            {
                url += ":" + port.ToString(CultureInfo.InvariantCulture);
            }

            return url;
        }

        /// <summary>
        ///     The absolute path (no scheme, host or query) of the page currently being served.
        ///     <para>SIGNATURE CHANGE: <c>Page</c> → <see cref="NavigationManager" />.</para>
        /// </summary>
        public static string GetCurrentPageUrl(NavigationManager p)
        {
            if (p == null)
            {
                return string.Empty;
            }

            return new Uri(p.Uri).AbsolutePath;
        }

        public static string GetUserIpAddress()
        {
            HttpContext context = MixERPConfiguration.HttpContext;

            if (context != null)
            {
                string ip = context.Request.Headers["X-Forwarded-For"].ToString();

                if (!string.IsNullOrEmpty(ip))
                {
                    string[] ipRange = ip.Split(',');
                    ip = ipRange[0];
                }
                else
                {
                    ip = context.Connection.RemoteIpAddress == null
                        ? null
                        : context.Connection.RemoteIpAddress.ToString();
                }

                if (ip == null)
                {
                    return string.Empty;
                }

                return ip.Trim();
            }

            return string.Empty;
        }

        public static int InvalidPasswordAttempts(ISession session, int increment = 0)
        {
            if (session == null)
            {
                return 0;
            }

            int retVal = 0;

            int? current = session.GetInt32("InvalidPasswordAttempts");

            if (current == null)
            {
                retVal = retVal + increment;
                session.SetInt32("InvalidPasswordAttempts", retVal);
            }
            else
            {
                retVal = Conversion.TryCastInteger(current.Value) + increment;
                session.SetInt32("InvalidPasswordAttempts", retVal);
            }


            if (increment > 0)
            {
                Log.Warning("{Count} Invalid attempt to sign in from {Host}/{IP} using {Browser}.", retVal, GetUserHostAddress(), GetUserIpAddress(), GetUserAgent());
            }

            return retVal;
        }

        /// <summary>
        ///     True when <paramref name="url" />, resolved against the current request, points at
        ///     the same host the current request was served from.
        ///     <para>SIGNATURE CHANGE: <c>Page</c> → <see cref="NavigationManager" />.</para>
        /// </summary>
        public static bool IsLocalUrl(Uri url, NavigationManager page)
        {
            if (page == null)
            {
                return false;
            }

            try
            {
                Uri current = new Uri(page.Uri);
                Uri requested = new Uri(current, url);

                if (requested.Host == current.Host)
                {
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                //
            }
            catch (UriFormatException)
            {
                //
            }

            return false;
        }

        /// <summary>
        ///     True when the current request was made from the local machine — the replacement for
        ///     the removed <c>HttpRequest.IsLocal</c>: the remote address is a loopback address, or
        ///     it is identical to the local address of the connection.
        ///     <para>SIGNATURE CHANGE: <c>Page</c> → <see cref="NavigationManager" />.</para>
        /// </summary>
        public static bool IsLocalhost(NavigationManager page)
        {
            if (page == null)
            {
                return false;
            }

            HttpContext context = MixERPConfiguration.HttpContext;

            if (context == null)
            {
                //No ambient request: fall back to the host of the current navigation.
                try
                {
                    return string.Equals(new Uri(page.Uri).Host, "localhost", StringComparison.OrdinalIgnoreCase);
                }
                catch (UriFormatException)
                {
                    return false;
                }
            }

            System.Net.IPAddress remote = context.Connection.RemoteIpAddress;

            if (remote == null)
            {
                //IIS/Kestrel report no remote address for in-process local calls.
                return true;
            }

            if (System.Net.IPAddress.IsLoopback(remote))
            {
                return true;
            }

            System.Net.IPAddress local = context.Connection.LocalIpAddress;

            return local != null && remote.Equals(local);
        }

        /// <summary>
        ///     Reloads the current page, preserving the original "redirect to my own absolute path"
        ///     behaviour (query string dropped).
        ///     <para>SIGNATURE CHANGE: <c>Page</c> → <see cref="NavigationManager" />.</para>
        /// </summary>
        public static void RefreshPage(NavigationManager page)
        {
            if (page != null)
            {
                page.NavigateTo(new Uri(page.Uri).AbsolutePath, true);
            }
        }

        /// <summary>
        ///     Runs a startup script in the browser through JS interop.
        ///     <para>
        ///         SIGNATURE CHANGE: <c>Page</c> → <see cref="IJSRuntime" />. The WebForms
        ///         <c>ScriptManager.RegisterStartupScript</c> buffered the script into the response;
        ///         Blazor Server has no response buffer and must not emit <c>&lt;script&gt;</c> into a
        ///         component, so the script is executed over the circuit instead.
        ///     </para>
        ///     <para>
        ///         <paramref name="addScriptTags" /> kept its meaning: when true the caller passed
        ///         bare JavaScript that needed wrapping in <c>&lt;script&gt;</c> tags, so it is
        ///         evaluated directly; when false the caller already supplied a complete
        ///         <c>&lt;script ...&gt;</c> element (an external reference, see
        ///         <see cref="Helpers.JSUtility.AddJSReference" />), so the element is appended to
        ///         <c>document.head</c> — once per <paramref name="key" />, which reproduces the
        ///         old de-duplication by registration key.
        ///     </para>
        /// </summary>
        public static void RegisterJavascript(string key, string javaScript, IJSRuntime page, bool addScriptTags)
        {
            if (page == null)
            {
                throw new InvalidOperationException(Warnings.CouldNotRegisterJavascript);
            }

            if (string.IsNullOrWhiteSpace(javaScript))
            {
                return;
            }

            //addScriptTags preserves the original ClientScriptManager semantics: true means the
            //caller supplied bare javascript to execute, false means it supplied a complete
            //<script ...></script> element to inject (de-duplicated by key, exactly as WebForms
            //de-duplicated by registration key).
            string function = addScriptTags ? "runScript" : "appendScript";

            //RegisterJavascript is a void, synchronous, frozen API, so the interop call is
            //fire-and-forget over the circuit with failures logged rather than thrown (the
            //original could not fail at this point either). The module is imported instead of
            //calling "eval" so that a Content-Security-Policy without 'unsafe-eval' does not
            //silently disable every script registration in the application.
            Task task = InvokeInteropAsync(page, function, key, javaScript);

            task.ContinueWith(t => Log.Warning(t.Exception, "Could not register the javascript {Key}.", key),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        ///     The url of this library's interop module, shipped as a static web asset of
        ///     MixERP.Net.Common. The host must call <c>app.UseStaticFiles()</c> for it to resolve.
        /// </summary>
        public const string InteropModulePath = "./_content/MixERP.Net.Common/js/mixerp-interop.js";

        private static async Task InvokeInteropAsync(IJSRuntime runtime, string function, string key, string script)
        {
            IJSObjectReference module =
                await runtime.InvokeAsync<IJSObjectReference>("import", InteropModulePath).ConfigureAwait(false);

            await module.InvokeVoidAsync(function, key, script).ConfigureAwait(false);
        }

        /// <summary>
        ///     Encodes a .NET string as a JavaScript string literal.
        /// </summary>

        /// <summary>
        ///     Turns an application-relative url into an absolute one, by prefixing the scheme and
        ///     authority of the current request.
        ///     <para>SIGNATURE CHANGE: <c>Page</c> → <see cref="NavigationManager" />.</para>
        /// </summary>
        public static string ResolveAbsoluteUrl(NavigationManager page, string relativeUrl)
        {
            if (page != null)
            {
                return new Uri(page.Uri).GetLeftPart(UriPartial.Authority) + ResolveUrl(relativeUrl);
            }
            return relativeUrl;
        }

        public static string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return string.Empty;
            }

            string url = relativeUrl.Trim();

            if (!url.StartsWith("~", StringComparison.Ordinal))
            {
                return relativeUrl;
            }

            //"~/x" is application-root relative: resolve it against the request path base.
            url = url.Substring(1);

            if (!url.StartsWith("/", StringComparison.Ordinal))
            {
                url = "/" + url;
            }

            HttpContext context = MixERPConfiguration.HttpContext;

            string pathBase = string.Empty;

            if (context != null && context.Request.PathBase.HasValue)
            {
                pathBase = context.Request.PathBase.Value.TrimEnd('/');
            }

            return pathBase + url;
        }

        /// <summary>
        ///     Single shared HttpClient instance; creating one per call exhausts sockets.
        /// </summary>
        //A process-lifetime HttpClient avoids socket exhaustion, but a bare instance never
        //refreshes DNS. PooledConnectionLifetime recycles connections so a long-lived host
        //picks up DNS changes, and an explicit Timeout keeps CleanUrl from hanging on the
        //100 second default.
        private static readonly HttpClient HttpClientInstance = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}