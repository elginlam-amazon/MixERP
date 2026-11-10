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
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Issues and clears the sign-in cookie. Replaces the FormsAuthenticationTicket code of
    ///     the Web Forms MixERPWebpage: the identity name is still the global login id, which
    ///     is what AppUsers reads back, and the ticket still lasts 30 minutes (sliding).
    /// </summary>
    public static class AuthenticationTicket
    {
        public static void SignIn(HttpContext context, long signInId, bool rememberMe)
        {
            if (context == null)
            {
                return;
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, signInId.ToString(CultureInfo.InvariantCulture))
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties)
                .GetAwaiter().GetResult();
        }

        public static void SignOut(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
            context.Session.Clear();
        }
    }
}
