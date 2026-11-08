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

using Microsoft.AspNetCore.Routing;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The single entry point the HOST calls to map every Finance module service endpoint:
    ///     <code>app.MapFinanceServiceEndpoints();</code>
    ///     <para>
    ///         NOTHING SELF-REGISTERS. This module exposes only static
    ///         <c>Map...(this IEndpointRouteBuilder)</c> extension methods, so the host keeps
    ///         full control of composition and ordering - this aggregator is a convenience over
    ///         the five per-service methods, each of which may equally be called on its own.
    ///     </para>
    ///     <para>
    ///         It covers the FIVE deleted <c>.asmx</c> services of this module:
    ///         <c>AccountData</c>, <c>CurrencyData</c>, <c>EODOperation</c>,
    ///         <c>Transactions</c> and <c>Entry/JournalVoucher</c>.
    ///     </para>
    /// </summary>
    public static class FinanceServiceEndpoints
    {
        public static IEndpointRouteBuilder MapFinanceServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapFinanceAccountDataService();
            endpoints.MapFinanceCurrencyDataService();
            endpoints.MapFinanceEODOperationService();
            endpoints.MapFinanceTransactionsService();
            endpoints.MapFinanceJournalVoucherEntryService();

            return endpoints;
        }
    }
}
