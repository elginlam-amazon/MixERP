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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using MixERP.Net.ApplicationState.Cache;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     Replaces <c>System.Web.Security.FormsAuthentication</c> and the static
    ///     <c>MixERPWebpage.SetAuthenticationTicket</c> / <c>MixERPWebpage.RequestLoginPage</c> members
    ///     (checklist tasks 49, 50, 51).
    ///     <para>
    ///         BLAZOR SERVER SEAM: every member of this class writes or deletes the authentication
    ///         cookie, which is only possible while a real <see cref="HttpContext" /> response is still
    ///         open — i.e. on a mapped HTTP endpoint or during the initial static render. It can NEVER
    ///         be called from interactive (SignalR) component code; interactive code navigates to the
    ///         endpoints mapped by <see cref="AuthEndpoints" /> instead.
    ///     </para>
    /// </summary>
    public static class MixERPAuthentication
    {
        /// <summary>
        ///     Former <c>&lt;authentication mode="Forms" loginUrl="~/SignIn.aspx" /&gt;</c>.
        /// </summary>
        public const string LoginPath = "/SignIn";

        /// <summary>
        ///     Former <c>&lt;forms defaultUrl="~/Dashboard/Index.aspx" /&gt;</c>. The default url was never
        ///     a cookie option: it was the url FormsAuthentication redirected an authenticated user to, so
        ///     it stays page behaviour (the SignIn page uses this constant).
        /// </summary>
        public const string DefaultUrl = "/Dashboard/Index";

        /// <summary>
        ///     Former <c>FormsAuthentication</c> <c>ReturnUrl</c> query string parameter name.
        /// </summary>
        public const string ReturnUrlParameter = "ReturnUrl";

        /// <summary>
        ///     Former <c>&lt;forms timeout="60" slidingExpiration="true" /&gt;</c>.
        /// </summary>
        public static readonly TimeSpan CookieExpiry = TimeSpan.FromMinutes(60);

        /// <summary>
        ///     The ticket lifetime of the original <c>SetAuthenticationTicket</c>:
        ///     <c>DateTime.Now.AddMinutes(30)</c>. This is DELIBERATELY different from
        ///     <see cref="CookieExpiry" /> — the legacy code issued a 30 minute ticket under a scheme
        ///     configured for 60 minutes, and that discrepancy is preserved as-is.
        /// </summary>
        public static readonly TimeSpan TicketExpiry = TimeSpan.FromMinutes(30);

        /// <summary>
        ///     Builds the signed-in principal. The subject claim (<see cref="ClaimTypes.Name" />, which is
        ///     what <c>User.Identity.Name</c> returns) carries the <c>globalLoginId</c> AS A STRING,
        ///     exactly as the <c>FormsAuthenticationTicket</c> name did: every consumer
        ///     (<c>UpdateHub</c>, <c>AppUsers</c>) reads <c>User.Identity.Name</c> and casts it to long.
        /// </summary>
        public static ClaimsPrincipal CreatePrincipal(long globalLoginId)
        {
            string subject = globalLoginId.ToString(CultureInfo.InvariantCulture);

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, subject),
                //NameIdentifier carries the same value so claim-based consumers do not have to know
                //that the subject is stored in the name claim.
                new Claim(ClaimTypes.NameIdentifier, subject)
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.SetAuthenticationTicket(HttpResponse, long, bool)</c>
        ///     (checklist task 50). A 30 minute ticket, persistent only when <paramref name="rememberMe" />
        ///     is true (the legacy ticket's <c>isPersistent</c> argument).
        ///     Returns silently when there is no response to write to, as the original did for a null
        ///     response.
        /// </summary>
        public static async Task SetAuthenticationTicketAsync(HttpContext context, long signInId, bool rememberMe)
        {
            if (context == null)
            {
                //Original: "if (response == null) return;".
                Log.Warning("The authentication ticket of {SignInId} was not written because there is no open HTTP response.", signInId);
                return;
            }

            DateTimeOffset issued = DateTimeOffset.Now;

            AuthenticationProperties properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                IssuedUtc = issued,
                ExpiresUtc = issued.Add(TicketExpiry),
                //AllowRefresh false keeps the 30 minute ticket from being slid past its own expiry by
                //the scheme's 60 minute sliding window, which is what the explicit
                //FormsAuthenticationTicket expiration did.
                AllowRefresh = false
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                CreatePrincipal(signInId), properties).ConfigureAwait(false);
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.RequestLoginPage()</c> (checklist task 51), for the HTTP side of
        ///     the seam: sign out, log the sign-off, clear every request cookie and redirect to the login
        ///     page with the supplied return url — but only when the current path is not already the
        ///     login url.
        /// </summary>
        /// <param name="context">The open HTTP context (never null here — this runs on an endpoint).</param>
        /// <param name="currentPage">
        ///     The absolute path of the page that asked for the login page, i.e. the legacy
        ///     <c>HttpContext.Current.Request.Url.AbsolutePath</c>.
        /// </param>
        public static async Task RequestLoginPageAsync(HttpContext context, string currentPage)
        {
            if (context == null)
            {
                return;
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

            Log.Information("User {UserName} was signed off.", GetCurrentUserName());

            Log.Debug("Clearing Http Cookies.");

            //The original removed every cookie from the REQUEST collection (which only affects the
            //rest of that request). The faithful modern equivalent is to delete each of them on the
            //response so the next request does not send them back either.
            foreach (string cookie in context.Request.Cookies.Keys)
            {
                context.Response.Cookies.Delete(cookie);
                Log.Verbose("Cleared cookie: {Cookie}.", cookie);
            }

            if (string.IsNullOrWhiteSpace(currentPage))
            {
                currentPage = context.Request.Path.HasValue ? context.Request.Path.Value : string.Empty;
            }

            Log.Verbose("Current page is {CurrentPage}.", currentPage);

            if (!IsLoginUrl(currentPage))
            {
                context.Response.Redirect(GetLoginUrl(currentPage));
                Log.Information("Redirected to login page.");
            }
        }

        /// <summary>
        ///     Sign-out only (no return url, no login redirect) — the
        ///     <c>FormsAuthentication.SignOut()</c> of <c>Site/Account/SignOut.aspx</c>.
        /// </summary>
        public static async Task SignOutAsync(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            Log.Information("User {UserName} was signed off.", GetCurrentUserName());
        }

        /// <summary>
        ///     <c>/SignIn?ReturnUrl=&lt;absolute path of the current page&gt;</c>.
        /// </summary>
        public static string GetLoginUrl(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return LoginPath;
            }

            return LoginPath + "?" + ReturnUrlParameter + "=" + Uri.EscapeDataString(returnUrl);
        }

        /// <summary>
        ///     True when the supplied absolute path already IS the login page — the
        ///     <c>this.currentPage != loginUrl</c> guard of the original. The legacy route table mapped
        ///     both <c>""</c> and <c>SignIn.aspx</c> to the sign-in page, so the root path counts too.
        /// </summary>
        public static bool IsLoginUrl(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return true;
            }

            string path = absolutePath.TrimEnd('/');

            if (path.Length == 0)
            {
                return true;
            }

            return string.Equals(path, LoginPath, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentUserName()
        {
            //AppUsers.GetCurrent() is resilient, but it reads the ambient login; a signed-off or
            //never-signed-in caller must not turn a log statement into an exception.
            try
            {
                MetaLogin login = AppUsers.GetCurrent();

                if (login != null && login.View != null)
                {
                    return login.View.UserName;
                }
            }
            catch (Exception ex)
            {
                Log.Verbose("Could not resolve the current user name while signing off. {Exception}.", ex);
            }

            return string.Empty;
        }
    }
}
