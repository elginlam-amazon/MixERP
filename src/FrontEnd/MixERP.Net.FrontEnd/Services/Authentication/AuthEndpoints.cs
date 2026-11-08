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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     The NON-CIRCUIT seam for everything that has to write or delete the authentication cookie.
    ///     <para>
    ///         In Blazor Server <c>HttpContext</c> is null over the SignalR circuit, so
    ///         <c>SignInAsync</c> / <c>SignOutAsync</c> / cookie deletion CANNOT run from an interactive
    ///         component. Interactive code navigates (force-load) to one of these mapped endpoints
    ///         instead, which run on a real HTTP request and then redirect.
    ///     </para>
    /// </summary>
    public static class AuthEndpoints
    {
        /// <summary>
        ///     Performs the full <c>MixERPWebpage.RequestLoginPage()</c> behaviour (sign out, log the
        ///     sign-off, clear every cookie, redirect to <c>/SignIn?ReturnUrl=...</c>).
        ///     Interactive code reaches it through <see cref="GetRequestLoginUrl" />.
        /// </summary>
        public const string RequestLoginRoute = "/_auth/request-login";

        /// <summary>
        ///     Sign-out only, then redirect to the sign-in page. This is what
        ///     <c>Site/Account/SignOut.razor</c> (Specialist 8, task 96) must force-load, because its own
        ///     component code cannot sign the cookie out.
        /// </summary>
        public const string SignOutRoute = "/_auth/signout";

        public static IEndpointRouteBuilder MapMixErpAuthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            //GET and POST: the legacy sign-off was reachable by a plain link (SignOut.aspx) and the
            //forced log-off path was a server-side redirect, so both verbs must work.
            endpoints.MapMethods(RequestLoginRoute, new[] { "GET", "POST" }, async (HttpContext context) =>
            {
                string returnUrl = context.Request.Query[MixERPAuthentication.ReturnUrlParameter];

                await MixERPAuthentication.RequestLoginPageAsync(context, returnUrl).ConfigureAwait(false);

                if (!context.Response.HasStarted && context.Response.StatusCode != StatusCodes.Status302Found)
                {
                    //Already on the login url: the original did nothing at all beyond signing out, so
                    //the user is left on the sign-in page.
                    context.Response.Redirect(MixERPAuthentication.LoginPath);
                }
            }).AllowAnonymous();

            endpoints.MapMethods(SignOutRoute, new[] { "GET", "POST" }, async (HttpContext context) =>
            {
                //Legacy Site/Account/SignOut.aspx: Session.Remove("UserName"), FormsAuthentication.SignOut(),
                //Response.Redirect("~/SignIn.aspx").
                SessionHelper.RemoveSessionKey("UserName");

                await MixERPAuthentication.SignOutAsync(context).ConfigureAwait(false);

                context.Response.Redirect(MixERPAuthentication.LoginPath);
            }).AllowAnonymous();

            return endpoints;
        }

        /// <summary>
        ///     The url interactive code force-loads to request the login page for
        ///     <paramref name="currentAbsolutePath" />.
        /// </summary>
        public static string GetRequestLoginUrl(string currentAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(currentAbsolutePath))
            {
                return RequestLoginRoute;
            }

            return RequestLoginRoute + "?" + MixERPAuthentication.ReturnUrlParameter + "=" +
                   System.Uri.EscapeDataString(currentAbsolutePath);
        }
    }
}
