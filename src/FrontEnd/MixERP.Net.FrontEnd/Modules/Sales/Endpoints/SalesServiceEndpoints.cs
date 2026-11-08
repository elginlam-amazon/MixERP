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

namespace MixERP.Net.Core.Modules.Sales.Endpoints
{
    /// <summary>
    ///     The single entry point the HOST calls to map every Sales module service endpoint:
    ///     <code>app.MapSalesServiceEndpoints();</code>
    ///     <para>
    ///         NOTHING SELF-REGISTERS. This module exposes only static
    ///         <c>Map...(this IEndpointRouteBuilder)</c> extension methods, so the host keeps full
    ///         control of composition and ordering - this aggregator is a convenience over the
    ///         eight per-service methods, each of which may equally be called on its own.
    ///     </para>
    /// </summary>
    public static class SalesServiceEndpoints
    {
        public static IEndpointRouteBuilder MapSalesServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapSalesDirectSalesService();
            endpoints.MapSalesDeliveryService();
            endpoints.MapSalesOrderService();
            endpoints.MapSalesQuotationService();
            endpoints.MapSalesReturnService();

            endpoints.MapSalesReceiptAccountsService();
            endpoints.MapSalesReceiptCurrenciesService();
            endpoints.MapSalesReceiptTransactionPostingService();

            return endpoints;
        }
    }
}
