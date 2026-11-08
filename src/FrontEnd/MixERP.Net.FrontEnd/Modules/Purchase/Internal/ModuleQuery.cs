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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace MixERP.Net.Core.Modules.Purchase.Internal
{
    /// <summary>
    ///     The replacement for the WebForms <c>this.Request["TranId"]</c> /
    ///     <c>this.Request.QueryString["TranId"]</c> reads of the ported Purchase controls.
    ///     <para>
    ///         A module control is rendered through the host's <c>/Modules/{*path}</c> route
    ///         (<c>ModuleHost.razor</c>), so it is NOT a routable component and cannot declare a
    ///         <c>[SupplyParameterFromQuery]</c> parameter of its own: those are bound by the
    ///         <c>Router</c>, which never sees these controls. The query string is therefore read
    ///         off <see cref="NavigationManager.Uri" />, which is circuit-safe (there is no
    ///         <c>HttpContext</c> over SignalR).
    ///     </para>
    ///     <para>
    ///         <c>Request["key"]</c> searched query string, then form, then cookies. The Purchase
    ///         controls are only ever reached by a GET link carrying <c>?TranId=</c> (every caller
    ///         is a <c>ViewPath</c>/<c>ReportPath</c> href built by StockTransactionView or the
    ///         transaction check list), so the query string is the only one of the three
    ///         collections that was ever populated.
    ///     </para>
    /// </summary>
    internal static class ModuleQuery
    {
        internal static string Get(NavigationManager navigationManager, string key)
        {
            if (navigationManager == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            string query = new Uri(navigationManager.Uri).Query;

            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            StringValues value;

            if (QueryHelpers.ParseQuery(query).TryGetValue(key, out value))
            {
                return value.ToString();
            }

            return null;
        }
    }
}
