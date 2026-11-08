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
using Microsoft.AspNetCore.Components;
using Serilog;

namespace MixERP.Net.Framework.Errors
{
    /// <summary>
    ///     Blazor Server implementation of <see cref="IMixERPErrorHandler" />. It stores the exception in
    ///     circuit-scoped <see cref="MixERPErrorState" /> (the replacement for <c>Session["ex"]</c>) and
    ///     navigates to <see cref="RuntimeErrorRoute" />, which replaces the WebForms
    ///     <c>Server.TransferRequest("~/Site/RuntimeError.aspx", true)</c> call.
    ///     Register as scoped: <c>services.AddScoped&lt;MixERPErrorState&gt;()</c> and
    ///     <c>services.AddScoped&lt;IMixERPErrorHandler, MixERPErrorHandler&gt;()</c>.
    /// </summary>
    public class MixERPErrorHandler : IMixERPErrorHandler
    {
        /// <summary>
        ///     Route that replaces <c>~/Site/RuntimeError.aspx</c>.
        /// </summary>
        public const string RuntimeErrorRoute = "/site/runtime-error";

        private readonly MixERPErrorState errorState;
        private readonly NavigationManager navigationManager;

        [System.CLSCompliant(false)]
        public MixERPErrorHandler(MixERPErrorState errorState, NavigationManager navigationManager)
        {
            if (errorState == null)
            {
                throw new ArgumentNullException(nameof(errorState));
            }

            if (navigationManager == null)
            {
                throw new ArgumentNullException(nameof(navigationManager));
            }

            this.errorState = errorState;
            this.navigationManager = navigationManager;
        }

        public void Handle(MixERPException exception)
        {
            if (exception == null)
            {
                return;
            }

            this.errorState.Set(exception);
            Log.Information("Exception object was added to session.");

            this.navigationManager.NavigateTo(RuntimeErrorRoute, false);
        }
    }
}
