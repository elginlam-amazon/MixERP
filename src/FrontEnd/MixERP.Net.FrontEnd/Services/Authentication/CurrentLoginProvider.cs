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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Framework.Security;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     Circuit-safe replacement for the ambient <c>AppUsers.GetCurrent()</c> call that every
    ///     legacy authorization check made.
    ///     <para>
    ///         WHY THIS EXISTS: <c>AppUsers.GetCurrent()</c> resolves the signed-in user through
    ///         <c>AppState.CurrentUserName</c>, which reads <c>IHttpContextAccessor.HttpContext.User</c>.
    ///         In Blazor Server <c>HttpContext</c> is NULL for everything that happens over the SignalR
    ///         circuit, so inside interactive component code <c>GetCurrent()</c> silently returns an
    ///         EMPTY login (<c>LoginId == 0</c>, <c>IsAdmin == false</c>). Used as-is, every ported
    ///         authorization rule would either deny everyone or, worse, read a blank
    ///         <c>AllowTransactionPosting</c>/<c>IsAdmin</c> and behave unpredictably.
    ///     </para>
    ///     <para>
    ///         This scoped service therefore resolves the <c>globalLoginId</c> from the circuit's
    ///         <see cref="AuthenticationStateProvider" /> (which IS available over the circuit, because
    ///         the authentication state is captured when the circuit is created) and feeds it to the
    ///         explicit <c>AppUsers.GetCurrent(globalLoginId)</c> overload, which needs no
    ///         <c>HttpContext</c>. On a plain HTTP request (endpoints, static render) it falls back to
    ///         <c>HttpContext.User</c>, which is the legacy path.
    ///     </para>
    ///     <para>
    ///         The remote IP address is captured ONCE here, during the initial request, because
    ///         <c>HttpContext.Connection.RemoteIpAddress</c> (legacy
    ///         <c>ServerVariables["REMOTE_ADDR"]</c>) is unavailable at interactive time.
    ///     </para>
    /// </summary>
    public sealed class CurrentLoginProvider : MixERP.Net.Framework.Security.ICurrentLogin
    {
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private readonly IHttpContextAccessor httpContextAccessor;
        private string remoteIpAddress;

        public CurrentLoginProvider(AuthenticationStateProvider authenticationStateProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this.authenticationStateProvider = authenticationStateProvider;
            this.httpContextAccessor = httpContextAccessor;

            //Captured eagerly: a scoped service of a Blazor circuit is first constructed while the
            //initial HTTP request is still open, which is the only moment the remote address is
            //readable.
            this.CaptureRequestInfo();
        }

        /// <summary>
        ///     The remote address of the request that created this scope. Replaces
        ///     <c>Request.ServerVariables["REMOTE_ADDR"]</c>. Falls back to the signed-in user's
        ///     recorded IP address (<c>office.sign_in_view.ip_address</c>) when it was not captured.
        /// </summary>
        public string RemoteIpAddress
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.remoteIpAddress))
                {
                    return this.remoteIpAddress;
                }

                MetaLogin login = this.GetLogin();

                return login != null && login.View != null ? login.View.IpAddress : string.Empty;
            }
        }

        /// <summary>
        ///     True when the ambient principal is authenticated — the legacy
        ///     <c>Request.IsAuthenticated</c>.
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            ClaimsPrincipal user = await this.GetUserAsync().ConfigureAwait(false);

            return user != null && user.Identity != null && user.Identity.IsAuthenticated;
        }

        /// <summary>
        ///     The <c>globalLoginId</c> carried by the subject claim, or 0.
        /// </summary>
        public async Task<long> GetGlobalLoginIdAsync()
        {
            ClaimsPrincipal user = await this.GetUserAsync().ConfigureAwait(false);

            return GetGlobalLoginId(user);
        }

        /// <summary>
        ///     The circuit-safe equivalent of <c>AppUsers.GetCurrent()</c>. Never returns null
        ///     (the <c>AppUsers</c> overload guarantees a non-null login with a non-null
        ///     <c>View</c>), exactly like the legacy call.
        /// </summary>
        public async Task<MetaLogin> GetCurrentAsync()
        {
            long globalLoginId = await this.GetGlobalLoginIdAsync().ConfigureAwait(false);

            return AppUsers.GetCurrent(globalLoginId);
        }

        /// <summary>
        ///     The <see cref="MixERP.Net.Framework.Security.ICurrentLogin" /> projection of
        ///     <see cref="GetCurrentAsync" />: the same login, expressed in primitives, for the
        ///     libraries and Razor Class Libraries that cannot reference
        ///     <c>MixERP.Net.ApplicationState</c>'s <c>MetaLogin</c> (and therefore cannot reference
        ///     this host at all).
        ///     <para>
        ///         Never returns null. Each nullable column is projected with EXACTLY the conversion
        ///         the legacy call sites applied to it (<c>.ToInt()</c> / <c>.ToBool()</c>), so a
        ///         consumer reading <c>LoginInfo.OfficeId</c> gets the same value
        ///         <c>AppUsers.GetCurrent().View.OfficeId.ToInt()</c> produced.
        ///     </para>
        /// </summary>
        public async Task<LoginInfo> GetLoginAsync()
        {
            MetaLogin login = await this.GetCurrentAsync().ConfigureAwait(false);

            return Project(login);
        }

        /// <summary>
        ///     <c>MetaLogin</c> -> <c>LoginInfo</c>. The one place the projection is defined, so the
        ///     interface view and the host view cannot drift.
        /// </summary>
        public static LoginInfo Project(MetaLogin login)
        {
            if (login == null)
            {
                return LoginInfo.Empty;
            }

            LoginView view = login.View;

            if (view == null)
            {
                return new LoginInfo
                {
                    //GlobalLoginId is deliberately carried even when the view is missing: it is what
                    //distinguishes "signed in but the sign-in view could not be read" from "nobody is
                    //signed in". The legacy code treated a null View as a denial, which LoginInfo
                    //reproduces through its all-false/zero rights.
                    GlobalLoginId = login.GlobalLoginId.ToLong(),
                    Catalog = login.Catalog,
                    LoginId = login.LoginId.ToLong()
                };
            }

            return new LoginInfo
            {
                GlobalLoginId = login.GlobalLoginId.ToLong(),
                Catalog = login.Catalog,
                LoginId = login.LoginId.ToLong(),

                UserId = view.UserId.ToInt(),
                RoleId = view.RoleId.ToInt(),
                Role = view.Role,
                RoleCode = view.RoleCode,
                RoleName = view.RoleName,
                IsAdmin = view.IsAdmin.ToBool(),
                IsSystem = view.IsSystem.ToBool(),
                Elevated = view.Elevated.ToBool(),
                AllowTransactionPosting = view.AllowTransactionPosting.ToBool(),
                Browser = view.Browser,
                IpAddress = view.IpAddress,
                LoginDateTime = view.LoginDateTime ?? DateTime.MinValue,
                RemoteUser = view.RemoteUser,
                Culture = view.Culture,
                UserName = view.UserName,
                FullName = view.FullName,
                NickName = view.NickName,
                Office = view.Office,
                OfficeId = view.OfficeId.ToInt(),
                OfficeCode = view.OfficeCode,
                OfficeName = view.OfficeName,
                RegistrationDate = view.RegistrationDate ?? DateTime.MinValue,
                CurrencyCode = view.CurrencyCode,
                PoBox = view.PoBox,
                AddressLine1 = view.AddressLine1,
                AddressLine2 = view.AddressLine2,
                Street = view.Street,
                City = view.City,
                State = view.State,
                ZipCode = view.ZipCode,
                Country = view.Country,
                Phone = view.Phone,
                Fax = view.Fax,
                Email = view.Email,
                Url = view.Url,
                RegistrationNumber = view.RegistrationNumber,
                PanNumber = view.PanNumber
            };
        }

        /// <summary>
        ///     The circuit-safe equivalent of <c>AppUsers.GetCurrentUserDB()</c>.
        /// </summary>
        public async Task<string> GetCatalogAsync()
        {
            MetaLogin login = await this.GetCurrentAsync().ConfigureAwait(false);

            return login == null ? string.Empty : login.Catalog;
        }

        /// <summary>
        ///     Loads the login cache entry for the current subject —
        ///     <c>AppUsers.SetCurrentLogin()</c> without the <c>HttpContext</c> dependency.
        /// </summary>
        public async Task SetCurrentLoginAsync()
        {
            long globalLoginId = await this.GetGlobalLoginIdAsync().ConfigureAwait(false);

            if (globalLoginId > 0)
            {
                AppUsers.SetCurrentLogin(globalLoginId);
            }
        }

        /// <summary>
        ///     The <c>globalLoginId</c> of a principal: the subject claim holds it as a string, which is
        ///     the contract the <c>FormsAuthenticationTicket</c> name established.
        /// </summary>
        public static long GetGlobalLoginId(ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null)
            {
                return 0;
            }

            long globalLoginId;

            long.TryParse(user.Identity.Name, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out globalLoginId);

            return globalLoginId;
        }

        private MetaLogin GetLogin()
        {
            try
            {
                return this.GetCurrentAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Verbose("Could not resolve the current login. {Exception}.", ex);
                return null;
            }
        }

        private async Task<ClaimsPrincipal> GetUserAsync()
        {
            if (this.authenticationStateProvider != null)
            {
                try
                {
                    AuthenticationState state = await this.authenticationStateProvider
                        .GetAuthenticationStateAsync().ConfigureAwait(false);

                    if (state != null && state.User != null && state.User.Identity != null &&
                        state.User.Identity.IsAuthenticated)
                    {
                        return state.User;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    //Outside a component/circuit scope the provider can refuse to answer; the HTTP
                    //fallback below is then the correct source.
                    Log.Verbose("The authentication state provider could not answer. {Exception}.", ex);
                }
            }

            HttpContext context = this.httpContextAccessor == null ? null : this.httpContextAccessor.HttpContext;

            return context == null ? null : context.User;
        }

        private void CaptureRequestInfo()
        {
            HttpContext context = this.httpContextAccessor == null ? null : this.httpContextAccessor.HttpContext;

            if (context == null)
            {
                return;
            }

            string forwarded = context.Request.Headers["X-Forwarded-For"].ToString();

            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                this.remoteIpAddress = forwarded.Split(',')[0].Trim();
                return;
            }

            if (context.Connection.RemoteIpAddress != null)
            {
                this.remoteIpAddress = context.Connection.RemoteIpAddress.ToString();
            }
        }
    }
}
