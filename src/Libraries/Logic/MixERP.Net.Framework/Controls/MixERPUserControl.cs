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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MixERP.Net.Framework.Security;
using Serilog;

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     Blazor component base for every MixERP user control. The eight <c>Modules/*</c> Razor Class
    ///     Libraries declare <c>: MixERPUserControl</c> against this exact name.
    ///     <para>
    ///         RELOCATED FROM THE HOST (<c>MixERP.Net.FrontEnd/Base/MixERPUserControl.cs</c>,
    ///         namespace <c>MixERP.Net.FrontEnd.Base</c>). The eight module projects each carried a
    ///         <c>ProjectReference</c> to the HOST so they could derive from it — the inverted
    ///         direction, which makes their components undiscoverable as RCLs. The class now lives in
    ///         this bottom-of-the-graph library, which every module already references, so the host
    ///         can reference the modules instead. The CLASS NAME is unchanged; only the namespace
    ///         moved, and every module <c>using MixERP.Net.FrontEnd.Base;</c> was rewritten to
    ///         <c>using MixERP.Net.Framework.Controls;</c>.
    ///     </para>
    ///     <para>
    ///         THE TWO HOST SERVICES ARE NOW INJECTED AS INTERFACES
    ///         (<see cref="IMixERPAuthorization" />, <see cref="ICurrentLogin" />) declared alongside
    ///         this class. The concrete <c>MixERPAuthorization</c> / <c>CurrentLoginProvider</c> could
    ///         NOT be moved here with it: they resolve the login through
    ///         <c>MixERP.Net.ApplicationState.Cache.AppUsers</c> and read <c>MixERP.Net.Common</c> /
    ///         <c>MixERP.Net.Entities</c> types, and ALL THREE of those projects reference THIS
    ///         assembly, so moving them would close a project reference cycle. The implementations stay
    ///         in the host and are registered against the interfaces in <c>Program.cs</c>; there is one
    ///         implementation of each and no duplicate, no shim.
    ///     </para>
    ///     <para>
    ///         Both authorization checks are enforced here, in the base, so an individual control
    ///         cannot lose one. A denial is recorded on the host's scoped
    ///         <c>PageAuthorizationState</c> and rendered IN PLACE by <c>AuthorizationOutlet</c>, which
    ///         reproduces the <c>Server.Transfer</c> semantics of the original (the response body is
    ///         replaced, the url is not).
    ///     </para>
    /// </summary>
    // Derives from ASP.NET Core ComponentBase, which is not CLS-compliant.
    [System.CLSCompliant(false)]
    public abstract class MixERPUserControl : MixERPUserControlBase
    {
        [Parameter]
        public virtual bool IsLandingPage { get; set; }

        public override AccessLevel AccessLevel
        {
            get { return AccessLevel.PolicyBased; }
        }

        [Inject]
        protected IMixERPAuthorization Authorization { get; set; }

        [Inject]
        protected ICurrentLogin CurrentLogin { get; set; }

        /// <summary>
        ///     True when the current user may not post transactions. Kept synchronous because control
        ///     markup reads it; it is refreshed by <see cref="InitializeAsync" />.
        /// </summary>
        public bool IsRestrictedMode { get; private set; }

        /// <summary>
        ///     Legacy entry point, preserved for the <c>Modules/*</c> call sites. The order of the
        ///     original is preserved exactly: log "{Control} initialized.", raise the control load, then
        ///     the access-level check, then the transaction-posting check.
        ///     <para>
        ///         The two checks are asynchronous now (they read the login cache through the circuit's
        ///         authentication state), so they are continued on the component's dispatcher. The
        ///         denial they record re-renders the authorization outlet, which is what makes the
        ///         denial visible — the same observable outcome as the synchronous
        ///         <c>Server.Transfer</c>.
        ///     </para>
        /// </summary>
        public void Initialize()
        {
            Log.Verbose("{Control} initialized.", this);
            this.OnControlLoad(this, new EventArgs());

            _ = this.InvokeAsync(this.RunAuthorizationChecksAsync);
        }

        /// <summary>
        ///     The awaitable form of <see cref="Initialize" />, for controls that are initialized from
        ///     <c>OnInitializedAsync</c>.
        /// </summary>
        public async Task InitializeAsync()
        {
            Log.Verbose("{Control} initialized.", this);
            this.OnControlLoad(this, new EventArgs());

            await this.RunAuthorizationChecksAsync().ConfigureAwait(true);
        }

        protected override async Task OnInitializedAsync()
        {
            //MixERPUserControlBase.OnInitialized has already raised OnControlLoad, so only the checks
            //are run here — running them in the base guarantees every control is covered.
            await base.OnInitializedAsync().ConfigureAwait(true);
            await this.RunAuthorizationChecksAsync().ConfigureAwait(true);
        }

        /// <summary>
        ///     <c>CheckAccessLevel()</c> followed by <c>CheckTransactionPostingStatus()</c>, in the
        ///     original order.
        /// </summary>
        protected async Task RunAuthorizationChecksAsync()
        {
            LoginInfo login = await this.CurrentLogin.GetLoginAsync().ConfigureAwait(true);

            //Identical to the original `login == null || login.View == null ||
            //!login.View.AllowTransactionPosting.ToBool()`: LoginInfo is never null and projects the
            //nullable column through the same ToBool() conversion, so an absent login and an absent
            //permission both yield restricted mode.
            this.IsRestrictedMode = login == null || !login.IsSignedIn || !login.AllowTransactionPosting;

            if (!await this.Authorization.CheckAccessLevelAsync(this.AccessLevel, this).ConfigureAwait(true))
            {
                return;
            }

            //The original passed `this is ITransaction`. ITransaction lives in MixERP.Net.Entities,
            //which references THIS assembly, so the control is passed instead and the host-side
            //implementation performs the same `control is ITransaction` test.
            await this.Authorization
                .CheckTransactionPostingStatusAsync(this)
                .ConfigureAwait(true);
        }
    }
}
