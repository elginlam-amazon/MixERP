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

// Web Forms only part of PageUtility. Every member here takes or produces a
// System.Web.UI type (Page, Control, HtmlMeta, ScriptManager), none of which was ported to
// modern .NET. The file is kept as source for the per-page Razor re-authoring tracked in
// summary.md and is excluded from the net10.0 compile. See knowledge base doc 25.

using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Serilog;
using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace MixERP.Net.Common
{
    public static partial class PageUtility
    {
        public static void AddMeta(Page page, string name, string content)
        {
            using (HtmlMeta meta = new HtmlMeta())
            {
                meta.Name = name;
                meta.Content = content;
                page.Header.Controls.Add(meta);
            }
        }

        public static void CheckInvalidAttempts(Page page)
        {
            if (page != null)
            {
                int triedAttempts = InvalidPasswordAttempts(page.Session);
                int allowedAttemps = Conversion.TryCastInteger(AppSettings.Get("MaxInvalidPasswordAttempts"));

                if (triedAttempts > 0)
                {
                    Log.Warning("{Count} of {Allowed} allowed invalid sign-in attempts from {Host}/{IP} using {Browser}.", triedAttempts, allowedAttemps, GetUserHostAddress(), GetUserIpAddress(), GetBrowser());
                }

                if (triedAttempts >= allowedAttemps)
                {
                    Log.Error("Disallowed access to {Host}/{IP} using {Browser}.", GetUserHostAddress(), GetUserIpAddress(), GetBrowser());
                    page.Response.Redirect("~/Static/AccessIsDenied.html");
                }
            }
        }

        public static Control FindControlIterative(Control root, string id)
        {
            if (root == null)
            {
                return null;
            }

            if (root.ID == id)
            {
                return root;
            }
            foreach (Control c in root.Controls)
            {
                Control t = FindControlIterative(c, id);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        public static string GetCurrentPageUrl(Page p)
        {
            if (p == null)
            {
                return string.Empty;
            }

            return p.Request.Url.AbsolutePath;
        }

        public static bool IsLocalUrl(Uri url, Page page)
        {
            if (page == null)
            {
                return false;
            }

            try
            {
                Uri requested = new Uri(page.Request.Url, url);

                if (requested.Host == page.Request.Url.Host)
                {
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                //
            }

            return false;
        }

        public static bool IsLocalhost(Page page)
        {
            if (page == null)
            {
                return false;
            }

            return page.Request.IsLocal;
        }

        public static void RefreshPage(Page page)
        {
            if (page != null)
            {
                page.Response.Redirect(page.Request.Url.AbsolutePath);
            }
        }

        public static void RegisterJavascript(string key, string javaScript, Page page, bool addScriptTags)
        {
            if (page == null)
            {
                page = HttpContext.Current.Handler as Page;
            }

            if (page == null)
            {
                throw new InvalidOperationException(Warnings.CouldNotRegisterJavascript);
            }

            ScriptManager.RegisterStartupScript(page, typeof (Page), key, javaScript, addScriptTags);
        }

        public static string ResolveAbsoluteUrl(Page page, string relativeUrl)
        {
            if (page != null)
            {
                return page.Request.Url.GetLeftPart(UriPartial.Authority) + page.ResolveUrl(relativeUrl);
            }
            return relativeUrl;
        }
    }
}
