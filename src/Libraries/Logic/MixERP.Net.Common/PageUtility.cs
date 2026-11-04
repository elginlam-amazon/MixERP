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

using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.SessionState;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.Common
{
    public static partial class PageUtility
    {
        /// <summary>
        ///     Shared client used by <see cref="CleanUrl" /> to probe a url. HttpClient is meant to be
        ///     long lived: a new instance per call leaks sockets.
        /// </summary>
        private static readonly HttpClient HeadClient = new HttpClient();

        private static string GetUserHostAddress()
        {
            string host = string.Empty;

            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Request.UserHostAddress != null)
                {
                    host = HttpContext.Current.Request.UserHostAddress;
                }
            }

            return host;
        }

        /// <summary>
        ///     Returns the requesting user agent.
        /// </summary>
        /// <remarks>
        ///     The Web Forms implementation logged <c>HttpBrowserCapabilities.Browsers</c>. Browser
        ///     capability sniffing was removed from ASP.NET Core, so the raw user agent string is
        ///     logged instead. See knowledge base doc 25.
        /// </remarks>
        private static string GetBrowser()
        {
            if (HttpContext.Current != null)
            {
                string userAgent = HttpContext.Current.Request.UserAgent;

                if (!string.IsNullOrWhiteSpace(userAgent))
                {
                    return userAgent;
                }
            }

            return string.Empty;
        }

        public static string MapPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return PathHelper.NormalizePhysicalPath(path);
            }

            return HostingEnvironment.MapPath(path);
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

            //The Web Forms implementation used a WebClient subclass which rewrote the verb to HEAD.
            //WebClient is obsolete (SYSLIB0014); HttpClient issues a HEAD request directly.
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url))
            {
                try
                {
                    using (HttpResponseMessage response = HeadClient.Send(request))
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }
                catch (HttpRequestException)
                {
                    url = string.Empty;
                }
                catch (InvalidOperationException)
                {
                    //The url was not a well formed absolute uri.
                    url = string.Empty;
                }
                catch (UriFormatException)
                {
                    url = string.Empty;
                }

                return url;
            }
        }

        public static string GetCurrentDomainName()
        {
            if (HttpContext.Current == null)
            {
                return string.Empty;
            }

            string url = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Host;

            if (HttpContext.Current.Request.Url.Port != 80)
            {
                url += ":" + HttpContext.Current.Request.Url.Port.ToString(CultureInfo.InvariantCulture);
            }

            return url;
        }

        /// <summary>
        ///     Returns the IP address of the requesting client.
        /// </summary>
        /// <remarks>
        ///     The Web Forms implementation read the <c>HTTP_X_FORWARDED_FOR</c> and <c>REMOTE_ADDR</c>
        ///     server variables off the current <c>Page</c>. ASP.NET Core does not expose IIS server
        ///     variables, so the forwarded header and <c>UserHostAddress</c> are used instead.
        /// </remarks>
        public static string GetUserIpAddress()
        {
            HttpContext context = HttpContext.Current;

            if (context == null)
            {
                return string.Empty;
            }

            string ip = context.Request.Headers["X-Forwarded-For"];

            if (!string.IsNullOrEmpty(ip))
            {
                string[] ipRange = ip.Split(',');
                ip = ipRange[0];
            }
            else
            {
                ip = context.Request.UserHostAddress;
            }

            if (string.IsNullOrEmpty(ip))
            {
                return string.Empty;
            }

            return ip.Trim();
        }

        [CLSCompliant(false)]
        public static int InvalidPasswordAttempts(HttpSessionState session, int increment = 0)
        {
            if (session == null)
            {
                return 0;
            }

            int retVal = 0;

            if (session["InvalidPasswordAttempts"] == null)
            {
                retVal = retVal + increment;
                session.Add("InvalidPasswordAttempts", retVal);
            }
            else
            {
                retVal = Conversion.TryCastInteger(session["InvalidPasswordAttempts"]) + increment;
                session["InvalidPasswordAttempts"] = retVal;
            }


            if (increment > 0)
            {
                Log.Warning("{Count} Invalid attempt to sign in from {Host}/{IP} using {Browser}.", retVal, GetUserHostAddress(), GetUserIpAddress(), GetBrowser());
            }

            return retVal;
        }

        /// <summary>
        ///     Resolves an application relative ("~/") url against the current application path.
        /// </summary>
        /// <remarks>
        ///     The Web Forms implementation delegated to <c>Page.ResolveUrl</c>. There is no
        ///     <c>Page</c> on modern .NET, so the application path is read off the current request.
        /// </remarks>
        public static string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return string.Empty;
            }

            if (!relativeUrl.StartsWith("~", StringComparison.Ordinal))
            {
                return relativeUrl;
            }

            string applicationPath = "/";

            HttpContext context = HttpContext.Current;

            if (context != null && !string.IsNullOrWhiteSpace(context.Request.ApplicationPath))
            {
                applicationPath = context.Request.ApplicationPath;
            }

            return (applicationPath.TrimEnd('/') + "/" + relativeUrl.TrimStart('~').TrimStart('/', '\\')).Replace("\\", "/");
        }

    }
}
