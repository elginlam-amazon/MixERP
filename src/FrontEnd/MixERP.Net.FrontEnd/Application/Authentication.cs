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
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     ASP.NET Core cookie authentication replacement for System.Web.Security.FormsAuthentication.
    ///     The authenticated user name is the global login id, exactly as the forms ticket used to carry.
    /// </summary>
    public static class Authentication
    {
        public const string LoginUrl = "~/SignIn.aspx";
        public const string DefaultUrl = "~/Dashboard/Index.aspx";

        /// <summary>
        ///     Lifetime of the authentication ticket issued at sign in (was the FormsAuthenticationTicket expiration).
        /// </summary>
        public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(30);

        public static void SignIn(long signInId, bool rememberMe)
        {
            Microsoft.AspNetCore.Http.HttpContext context = System.Web.HttpContext.Current;

            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, signInId.ToString(CultureInfo.InvariantCulture)));

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(TicketLifetime)
            };

            context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties)
                .GetAwaiter().GetResult();
        }

        public static void SignOut()
        {
            Microsoft.AspNetCore.Http.HttpContext context = System.Web.HttpContext.Current;

            if (context == null)
            {
                return;
            }

            context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
        }
    }
}
