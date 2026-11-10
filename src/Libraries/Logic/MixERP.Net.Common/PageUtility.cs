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
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Framework;
using Serilog;

namespace MixERP.Net.Common
{
    /// <summary>
    ///     Request helpers. The Web Forms version took a <c>System.Web.UI.Page</c>; the
    ///     ASP.NET Core version works with the current <see cref="HttpContext" />.
    /// </summary>
    public static class PageUtility
    {
        private const string InvalidPasswordAttemptsKey = "InvalidPasswordAttempts";

        private static readonly HttpClient HeadClient = new HttpClient();

        private static string GetUserHostAddress(HttpContext context)
        {
            return context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        }

        private static string GetBrowser(HttpContext context)
        {
            if (context == null)
            {
                return string.Empty;
            }

            return context.Request.Headers.UserAgent.ToString();
        }

        /// <summary>
        ///     Redirects to the access denied page when the number of invalid sign-in attempts in
        ///     this session reaches the configured <c>MaxInvalidPasswordAttempts</c>.
        /// </summary>
        /// <returns>True when access was denied and the response was redirected.</returns>
        public static bool CheckInvalidAttempts(HttpContext context)
        {
            if (context == null)
            {
                return false;
            }

            int triedAttempts = InvalidPasswordAttempts(context.Session);
            int allowedAttemps = Conversion.TryCastInteger(WebContext.GetAppSetting("MaxInvalidPasswordAttempts"));

            if (triedAttempts > 0)
            {
                Log.Warning("{Count} of {Allowed} allowed invalid sign-in attempts from {Host}/{IP} using {Browser}.", triedAttempts, allowedAttemps, GetUserHostAddress(context), GetUserIpAddress(context), GetBrowser(context));
            }

            if (triedAttempts >= allowedAttemps)
            {
                Log.Error("Disallowed access to {Host}/{IP} using {Browser}.", GetUserHostAddress(context), GetUserIpAddress(context), GetBrowser(context));
                context.Response.Redirect(WebContext.ResolveUrl("~/Static/AccessIsDenied.html"));
                return true;
            }

            return false;
        }

        public static string MapPath(string path)
        {
            if (Path.IsPathRooted(path) && !path.StartsWith("/", StringComparison.Ordinal))
            {
                return PathHelper.NormalizePhysicalPath(path);
            }

            return WebContext.MapPath(path);
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

            if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                url = prefix + "://" + url;
            }

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url))
                using (HttpResponseMessage response = HeadClient.Send(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }
                }
            }
            catch (HttpRequestException)
            {
                url = string.Empty;
            }
            catch (UriFormatException)
            {
                url = string.Empty;
            }
            catch (InvalidOperationException)
            {
                url = string.Empty;
            }

            return url;
        }

        public static string GetCurrentDomainName()
        {
            HttpContext context = WebContext.Current;

            if (context == null)
            {
                return string.Empty;
            }

            string url = context.Request.Scheme + "://" + context.Request.Host.Host;

            if (context.Request.Host.Port.HasValue && context.Request.Host.Port.Value != 80)
            {
                url += ":" + context.Request.Host.Port.Value.ToString(CultureInfo.InvariantCulture);
            }

            return url;
        }

        public static string GetCurrentPageUrl(HttpContext context)
        {
            if (context == null)
            {
                return string.Empty;
            }

            return context.Request.PathBase + context.Request.Path;
        }

        public static string GetUserIpAddress()
        {
            return GetUserIpAddress(WebContext.Current);
        }

        public static string GetUserIpAddress(HttpContext context)
        {
            if (context == null)
            {
                return string.Empty;
            }

            string ip = context.Request.Headers["X-Forwarded-For"].ToString();

            if (!string.IsNullOrEmpty(ip))
            {
                string[] ipRange = ip.Split(',');
                ip = ipRange[0];
            }
            else
            {
                ip = GetUserHostAddress(context);
            }

            return ip.Trim();
        }

        public static int InvalidPasswordAttempts(ISession session, int increment = 0)
        {
            if (session == null)
            {
                return 0;
            }

            int retVal = (session.GetInt32(InvalidPasswordAttemptsKey) ?? 0) + increment;
            session.SetInt32(InvalidPasswordAttemptsKey, retVal);

            if (increment > 0)
            {
                HttpContext context = WebContext.Current;
                Log.Warning("{Count} Invalid attempt to sign in from {Host}/{IP} using {Browser}.", retVal, GetUserHostAddress(context), GetUserIpAddress(context), GetBrowser(context));
            }

            return retVal;
        }

        public static bool IsLocalUrl(Uri url, HttpContext context)
        {
            if (context == null || url == null)
            {
                return false;
            }

            try
            {
                Uri current = new Uri(context.Request.GetDisplayUrl());
                Uri requested = new Uri(current, url);

                if (requested.Host == current.Host)
                {
                    return true;
                }
            }
            catch (UriFormatException)
            {
                //
            }

            return false;
        }

        public static bool IsLocalhost(HttpContext context)
        {
            if (context == null)
            {
                return false;
            }

            IPAddress remote = context.Connection.RemoteIpAddress;

            if (remote == null)
            {
                return false;
            }

            return IPAddress.IsLoopback(remote) || remote.Equals(context.Connection.LocalIpAddress);
        }

        public static void RefreshPage(HttpContext context)
        {
            if (context != null)
            {
                context.Response.Redirect(GetCurrentPageUrl(context));
            }
        }

        /// <summary>
        ///     Wraps a script in a script block. Replaces ScriptManager.RegisterStartupScript:
        ///     the caller renders the block together with its markup.
        /// </summary>
        public static string GetScriptBlock(string javaScript)
        {
            if (string.IsNullOrWhiteSpace(javaScript))
            {
                return string.Empty;
            }

            return "<script type='text/javascript'>" + javaScript + "</script>";
        }

        public static string ResolveAbsoluteUrl(HttpContext context, string relativeUrl)
        {
            if (context != null)
            {
                return context.Request.Scheme + "://" + context.Request.Host + WebContext.ResolveUrl(relativeUrl);
            }

            return relativeUrl;
        }

        public static string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return string.Empty;
            }

            return WebContext.ResolveUrl(relativeUrl);
        }
    }
}
