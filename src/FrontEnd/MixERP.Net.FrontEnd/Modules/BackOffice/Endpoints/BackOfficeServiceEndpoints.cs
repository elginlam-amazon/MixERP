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

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The single entry point the HOST calls to map every BackOffice module endpoint:
    ///     <code>app.MapBackOfficeServiceEndpoints();</code>
    ///     <para>
    ///         NOTHING SELF-REGISTERS. This module exposes only static
    ///         <c>Map...(this IEndpointRouteBuilder)</c> extension methods, so the host keeps
    ///         full control of composition and ordering - this aggregator is a convenience over
    ///         the per-service methods, each of which may equally be called on its own.
    ///     </para>
    ///     <para>
    ///         It covers the FOUR deleted <c>.asmx</c> services of this module
    ///         (<c>Admin/ReportWriter</c>, <c>Attachments</c>, <c>OTS/OpeningInventory</c>,
    ///         <c>TaxData</c>) AND the deleted <c>Handlers/ImageHandler.ashx</c>, which is an
    ///         endpoint now too. The image handler is included here deliberately: its url is
    ///         hard-coded in <c>wwwroot/js/AttachmentManager.ascx.js</c>, so forgetting it
    ///         would leave every attachment thumbnail broken, and a single call cannot forget
    ///         half of the attachment manager.
    ///     </para>
    /// </summary>
    public static class BackOfficeServiceEndpoints
    {
        public static IEndpointRouteBuilder MapBackOfficeServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapBackOfficeReportWriterService();
            endpoints.MapBackOfficeAttachmentsService();
            endpoints.MapBackOfficeOpeningInventoryService();
            endpoints.MapBackOfficeTaxDataService();
            endpoints.MapBackOfficeImageHandler();

            return endpoints;
        }
    }
}
