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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SystemWebAdapters;

namespace MixERP.Net.FrontEnd.Base
{
    /// <summary>
    ///     Forms authentication replacement built on ASP.NET Core cookie authentication. The authenticated user name is
    ///     the global login (sign in) id, exactly like the former FormsAuthenticationTicket.
    /// </summary>
    public static class FormsAuthentication
    {
        /// <summary>
        ///     The sign in page (formerly authentication/forms/@loginUrl in Web.config).
        /// </summary>
        public const string LoginUrl = "/SignIn.aspx";

        private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(30);

        [System.CLSCompliant(false)]
        public static void SignIn(System.Web.HttpContext context, long signInId, bool rememberMe)
        {
            if (context == null)
            {
                return;
            }

            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, signInId.ToString(CultureInfo.InvariantCulture)));

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(TicketLifetime)
            };

            context.AsAspNetCore().SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties)
                .GetAwaiter().GetResult();
        }

        public static void SignOut()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;

            if (context == null)
            {
                return;
            }

            context.AsAspNetCore().SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
        }

        public static void RedirectToLoginPage(string returnUrl)
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;

            if (context == null)
            {
                return;
            }

            string url = LoginUrl;

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                url += "?ReturnUrl=" + Uri.EscapeDataString(returnUrl);
            }

            context.Response.Redirect(url, false);
        }
    }
}
