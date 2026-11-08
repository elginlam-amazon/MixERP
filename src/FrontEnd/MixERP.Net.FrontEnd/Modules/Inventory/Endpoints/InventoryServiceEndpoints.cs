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

namespace MixERP.Net.Core.Modules.Inventory.Endpoints
{
    /// <summary>
    ///     The single entry point the HOST calls to map every Inventory module service endpoint:
    ///     <code>app.MapInventoryServiceEndpoints();</code>
    ///     <para>
    ///         NOTHING SELF-REGISTERS. This module exposes only static
    ///         <c>Map...(this IEndpointRouteBuilder)</c> extension methods, so the host keeps full
    ///         control of composition and ordering - this aggregator is a convenience over the
    ///         seven per-service methods, each of which may equally be called on its own.
    ///     </para>
    ///     <para>
    ///         It covers the EIGHT deleted <c>.asmx</c> services of this module:
    ///         <c>ItemData</c>, <c>PartyData</c>, <c>TransferAuthorization</c>,
    ///         <c>TransferAcknowledgement</c> and the four <c>Entry/*</c> ones (the
    ///         <c>TransferDelivery</c> mapper carries both of its web methods).
    ///     </para>
    /// </summary>
    public static class InventoryServiceEndpoints
    {
        public static IEndpointRouteBuilder MapInventoryServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapInventoryItemDataService();
            endpoints.MapInventoryPartyDataService();
            endpoints.MapInventoryTransferAuthorizationService();
            endpoints.MapInventoryTransferAcknowledgementService();
            endpoints.MapInventoryAdjustmentService();
            endpoints.MapInventoryTransferService();
            endpoints.MapInventoryTransferDeliveryService();
            endpoints.MapInventoryTransferRequestService();

            return endpoints;
        }
    }
}
