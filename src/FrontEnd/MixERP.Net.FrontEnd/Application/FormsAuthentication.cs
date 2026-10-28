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
    /// Cookie authentication helpers replacing System.Web.Security.FormsAuthentication
    /// (configured in Program.cs with the former forms settings: login url, 60 minute sliding expiration).
    /// The authenticated identity name is still the global login id.
    /// </summary>
    internal static class FormsAuthentication
    {
        internal const string LoginUrl = "/SignIn.aspx";
        internal const string DefaultUrl = "/Dashboard/Index.aspx";

        internal static void SignIn(System.Web.HttpResponse response, long signInId, bool rememberMe)
        {
            Microsoft.AspNetCore.Http.HttpResponse coreResponse = response;

            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, signInId.ToString(CultureInfo.InvariantCulture)));

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            coreResponse.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties).GetAwaiter().GetResult();
        }

        internal static void SignOut()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;

            if (context == null)
            {
                return;
            }

            Microsoft.AspNetCore.Http.HttpContext coreContext = context;
            coreContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
        }
    }
}
