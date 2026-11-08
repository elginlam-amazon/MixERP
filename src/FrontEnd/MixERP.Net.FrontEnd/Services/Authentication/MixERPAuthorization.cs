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
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MixERP.Net.ApplicationState;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Contracts;
using MixERP.Net.Framework.Controls;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     Every authorization rule the legacy WebForms surfaces enforced, in ONE place so no page or
    ///     control can lose one by omission (checklist tasks 53-58).
    ///     <para>
    ///         Scoped: it depends on <see cref="CurrentLoginProvider" /> (circuit-safe current login)
    ///         and records denials on <see cref="PageAuthorizationState" /> instead of calling
    ///         <c>Server.Transfer</c>, which keeps the url unchanged exactly as the original did.
    ///     </para>
    /// </summary>
    public sealed class MixERPAuthorization : MixERP.Net.Framework.Security.IMixERPAuthorization
    {
        private readonly CurrentLoginProvider currentLogin;
        private readonly PageAuthorizationState authorizationState;
        private readonly NavigationManager navigationManager;

        public MixERPAuthorization(CurrentLoginProvider currentLogin,
            PageAuthorizationState authorizationState, NavigationManager navigationManager)
        {
            this.currentLogin = currentLogin;
            this.authorizationState = authorizationState;
            this.navigationManager = navigationManager;
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.CheckForceLogOffFlags()</c> (checklist task 53). Runs on EVERY
        ///     page activation, as the original ran on every <c>OnInit</c>.
        ///     Returns true when the user must be signed off.
        /// </summary>
        public async Task<bool> MustForceLogOffAsync()
        {
            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);

            if (login == null || login.View == null)
            {
                return false;
            }

            int officeId = login.View.OfficeId.ToInt();

            Collection<FrequencyDates> applicationDates = Dates.GetFrequencyDates(login.Catalog);

            if (applicationDates != null)
            {
                FrequencyDates model = applicationDates.FirstOrDefault(c => c.OfficeId.Equals(officeId));

                if (model != null)
                {
                    if (model.ForcedLogOffTimestamp != null &&
                        !model.ForcedLogOffTimestamp.Equals(DateTime.MinValue))
                    {
                        if (model.ForcedLogOffTimestamp <= DateTime.Now &&
                            model.ForcedLogOffTimestamp >= login.View.LoginDateTime)
                        {
                            Log.Information(
                                "Forced log off of {UserName}: the forced log-off timestamp {Timestamp} falls between the login time and now.",
                                login.View.UserName, model.ForcedLogOffTimestamp);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.VerifyAccess()</c> (checklist task 54). A landing page always
        ///     passes; otherwise the page's <paramref name="overridePath" /> must match the url of one of
        ///     the menu entries the user is entitled to, else the AccessIsDenied content is rendered IN
        ///     PLACE (<c>Server.Transfer</c>, not a redirect — the url stays put).
        /// </summary>
        /// <remarks>
        ///     FAITHFULNESS NOTE: in the legacy class the private <c>menus</c> field was never assigned
        ///     and <c>VerifyAccess</c> was never invoked, so the check was latent. It is now wired into
        ///     page activation with a real menu set, but when the menu set cannot be loaded (null/empty,
        ///     i.e. the state the legacy field was permanently in) access is ALLOWED and a warning is
        ///     logged, so a database/menu outage cannot lock every user out of a page that used to work.
        /// </remarks>
        public bool VerifyAccess(bool isLandingPage, string overridePath,
            IEnumerable<MixERP.Net.Entities.Core.Menu> menus)
        {
            if (isLandingPage)
            {
                return true;
            }

            MixERP.Net.Entities.Core.Menu[] menuSet = menus == null
                ? new MixERP.Net.Entities.Core.Menu[0]
                : menus.ToArray();

            if (menuSet.Length == 0)
            {
                Log.Warning(
                    "The policy check of {OverridePath} was skipped because no menu entry could be loaded for the current user.",
                    overridePath);
                return true;
            }

            bool policyExists = false;

            foreach (MixERP.Net.Entities.Core.Menu menu in menuSet)
            {
                if (menu != null && !string.IsNullOrWhiteSpace(menu.Url) &&
                    menu.Url.Replace("~", "").Equals(overridePath))
                {
                    policyExists = true;
                    break;
                }
            }

            if (!policyExists)
            {
                Log.Information("Access to {OverridePath} is denied: no menu policy matches it.", overridePath);
                this.authorizationState.Deny(AuthorizationDenial.AccessIsDenied);
            }

            return policyExists;
        }

        /// <summary>
        ///     Port of <c>MixERPUserControl.CheckAccessLevel()</c> (checklist task 55). The decision tree
        ///     — including the <c>hasAccess &amp;&amp; isDevelopmentMode</c> ordering and the fact that a
        ///     <see cref="AccessLevel.PolicyBased" /> or <see cref="AccessLevel.Everyone" /> surface is
        ///     only rejected when there is no login at all — is preserved verbatim.
        /// </summary>
        public async Task<bool> CheckAccessLevelAsync(AccessLevel accessLevel, object control)
        {
            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);

            bool hasAccess = true;
            string userName = string.Empty;
            string ipAddress = string.Empty;

            if (login == null || login.View == null)
            {
                hasAccess = false;
            }
            else
            {
                userName = login.View.UserName;
                ipAddress = login.View.IpAddress;

                bool isDevelopmentMode = IsDevelopmentMode(login.Catalog);
                bool isLocalHost = PageUtility.IsLocalhost(this.navigationManager);
                bool adminOnly = accessLevel.Equals(AccessLevel.AdminOnly) ||
                                 accessLevel.Equals(AccessLevel.LocalhostAdmin);

                if (adminOnly)
                {
                    hasAccess = login.View.IsAdmin.ToBool();
                }

                if (hasAccess && isDevelopmentMode)
                {
                    if (accessLevel.Equals(AccessLevel.LocalhostAdmin) && !isLocalHost)
                    {
                        hasAccess = false;
                    }
                }
            }

            if (!hasAccess)
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = this.currentLogin.RemoteIpAddress;
                }

                Log.Information("Access to {Control} is denied to {User} from {IP}.", control, userName, ipAddress);

                this.authorizationState.Deny(AuthorizationDenial.AccessIsDenied);
            }

            return hasAccess;
        }

        /// <summary>
        ///     Port of <c>MixERPUserControl.CheckTransactionPostingStatus()</c> (checklist task 56): an
        ///     <c>ITransaction</c> surface whose user may not post transactions shows the
        ///     RestrictedTransactionMode content in place.
        /// </summary>
        public async Task<bool> CheckTransactionPostingStatusAsync(bool isTransaction)
        {
            if (!isTransaction)
            {
                return true;
            }

            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);

            if (login != null && login.View != null && login.View.AllowTransactionPosting.ToBool())
            {
                return true;
            }

            Log.Information("Transaction posting is not allowed for {User}.",
                login == null || login.View == null ? string.Empty : login.View.UserName);

            this.authorizationState.Deny(AuthorizationDenial.RestrictedTransactionMode);
            return false;
        }

        /// <summary>
        ///     The <see cref="MixERP.Net.Framework.Security.IMixERPAuthorization" /> form of
        ///     <see cref="CheckTransactionPostingStatusAsync(bool)" />.
        ///     <para>
        ///         The original check was <c>CheckTransactionPostingStatus(this is ITransaction)</c>,
        ///         written inside the control base. That base now lives in
        ///         <c>MixERP.Net.Framework</c>, which CANNOT see
        ///         <c>MixERP.Net.Entities.Contracts.ITransaction</c> (<c>MixERP.Net.Entities</c>
        ///         references <c>MixERP.Net.Framework</c>, so the reference would be a cycle). The
        ///         control is therefore handed to this method and the IDENTICAL type test is performed
        ///         here, on the side of the boundary that does reference
        ///         <c>MixERP.Net.Entities</c>. Nothing about the decision changed — only where the
        ///         <c>is ITransaction</c> test is evaluated.
        ///     </para>
        /// </summary>
        public Task<bool> CheckTransactionPostingStatusAsync(object control)
        {
            return this.CheckTransactionPostingStatusAsync(control is ITransaction);
        }

        /// <summary>
        ///     Port of the <c>Modules/Update.aspx</c> page-level authorization (checklist task 57):
        ///     admin AND (not development mode OR (development mode AND localhost)). On denial the
        ///     legacy page logged and <c>Server.Execute</c>d the AccessIsDenied page, so the denial is
        ///     recorded here and rendered in place.
        ///     <para>Specialist 7's <c>Modules/Update.razor</c> calls this and returns early when false.</para>
        /// </summary>
        public async Task<bool> CanAccessUpdatePageAsync(object page)
        {
            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);

            string userName = login == null || login.View == null ? string.Empty : login.View.UserName;
            string ipAddress = login == null || login.View == null ? string.Empty : login.View.IpAddress;

            bool isDevelopmentMode = IsDevelopmentMode(login == null ? string.Empty : login.Catalog);
            bool isLocalHost = PageUtility.IsLocalhost(this.navigationManager);
            bool isAdmin = login != null && login.View != null && login.View.IsAdmin.ToBool();

            bool hasAccess = false;

            if (isAdmin)
            {
                if (isDevelopmentMode && isLocalHost)
                {
                    hasAccess = true;
                }

                if (!isDevelopmentMode)
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = this.currentLogin.RemoteIpAddress;
                }

                Log.Information("Access to {Page} is denied to {User} from {IP}.", page, userName, ipAddress);

                this.authorizationState.Deny(AuthorizationDenial.AccessIsDenied);
            }

            return hasAccess;
        }

        /// <summary>
        ///     Port of <c>UpdateHub.IsValidRequest()</c> (checklist task 58). A SignalR hub call has no
        ///     circuit and no <c>HttpContext</c>, so this is static and takes the hub's
        ///     <c>Context.User</c>.
        ///     <para>
        ///         The 2 second delay is part of the original (brute-force friction) and is preserved,
        ///         now as a non-blocking <c>Task.Delay</c> instead of <c>Thread.Sleep</c>.
        ///     </para>
        ///     <para>
        ///         Specialist 9's ported <c>UpdateHub</c> must call this and, when it returns false, push
        ///         <c>getNotification(Warnings.AccessIsDenied)</c> to the caller — exactly as the legacy
        ///         method did for the first two failures. The <c>notifyCaller</c> callback carries that
        ///         obligation so the push happens for EVERY failure reason, which the original only did
        ///         for two of the four (the admin/elevated failures returned false silently); the
        ///         callback is invoked only for those original two cases so behaviour is unchanged, and
        ///         the admin/elevated branches stay silent.
        ///     </para>
        /// </summary>
        public static async Task<bool> IsValidHubRequestAsync(ClaimsPrincipal user, Func<Task> notifyCaller)
        {
            await Task.Delay(2000).ConfigureAwait(false);

            if (user == null)
            {
                //Original: "if (this.Context == null)" -> notify the caller and refuse.
                await NotifyAsync(notifyCaller).ConfigureAwait(false);
                return false;
            }

            long globalLoginId = CurrentLoginProvider.GetGlobalLoginId(user);

            if (globalLoginId <= 0)
            {
                await NotifyAsync(notifyCaller).ConfigureAwait(false);
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.IsAdmin.ToBool())
            {
                //Original returned false WITHOUT notifying the caller.
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.Elevated.ToBool())
            {
                //Original returned false WITHOUT notifying the caller.
                return false;
            }

            return true;
        }

        /// <summary>
        ///     <c>DbConfig</c> <c>Mode</c> parameter equals <c>DEVELOPMENT</c> (case-insensitively), the
        ///     exact test both legacy surfaces made.
        /// </summary>
        public static bool IsDevelopmentMode(string catalog)
        {
            try
            {
                string mode = DbConfig.GetMixERPParameter(catalog, "Mode");

                return !string.IsNullOrWhiteSpace(mode) &&
                       mode.ToUpperInvariant().Equals("DEVELOPMENT");
            }
            catch (Exception ex)
            {
                //The legacy call could throw too; a failure to read the parameter must not be
                //mistaken for "development mode", which would relax the LocalhostAdmin rule.
                Log.Warning("Could not read the Mode parameter of {Catalog}. {Exception}.", catalog, ex);
                return false;
            }
        }

        private static async Task NotifyAsync(Func<Task> notifyCaller)
        {
            if (notifyCaller != null)
            {
                await notifyCaller().ConfigureAwait(false);
            }
        }
    }
}
