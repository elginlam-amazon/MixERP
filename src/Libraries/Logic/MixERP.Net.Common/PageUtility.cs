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
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.Common
{
    public static class PageUtility
    {
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

        public static string MapPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return PathHelper.NormalizePhysicalPath(path);
            }

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(basePath, path.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar));
        }

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

            using (MyClient client = new MyClient())
            {
                client.HeadOnly = true;
                try
                {
                    client.DownloadString(url);
                }
                catch (WebException)
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

        public static string GetUserIpAddress()
        {
            if (HttpContext.Current == null)
            {
                return string.Empty;
            }

            string ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ip))
            {
                string[] ipRange = ip.Split(',');
                ip = ipRange[0];
            }
            else
            {
                ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ip?.Trim() ?? string.Empty;
        }

        public static int InvalidPasswordAttempts(System.Web.SessionState.HttpSessionState session, int increment = 0)
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
                Log.Warning("{Count} Invalid attempt to sign in from {Host}/{IP}.", retVal, GetUserHostAddress(), GetUserIpAddress());
            }

            return retVal;
        }

        public static bool IsLocalhost()
        {
            if (HttpContext.Current == null)
            {
                return false;
            }

            return HttpContext.Current.Request.IsLocal;
        }

        public static bool IsLocalhost(dynamic page) => IsLocalhost();

        public static void CheckInvalidAttempts(dynamic page) { }

        public static string ResolveUrl(string relativeUrl)
        {
            return relativeUrl ?? string.Empty;
        }

        public static string GetCurrentPageUrl(dynamic page = null)
        {
            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.Url?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        public static void AddMeta(dynamic page, string name, string content) { }

        public static void RegisterJavascript(string key, string script, dynamic page, bool addScriptTags = false) { }
#pragma warning disable SYSLIB0014

        private class MyClient : WebClient
        {
            public bool HeadOnly { private get; set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest req = base.GetWebRequest(address);

                if (req != null && (this.HeadOnly && req.Method == "GET"))
                {
                    req.Method = "HEAD";
                }
                return req;
            }
        }
    }
}
