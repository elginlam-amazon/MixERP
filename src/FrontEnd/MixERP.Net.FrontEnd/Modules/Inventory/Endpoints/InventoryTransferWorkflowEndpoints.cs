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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Inventory.Data.Transactions;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;

namespace MixERP.Net.Core.Modules.Inventory.Endpoints
{
    /// <summary>
    ///     The minimal-API replacements for the deleted
    ///     <c>Services/TransferAuthorization.asmx</c> and
    ///     <c>Services/TransferAcknowledgement.asmx</c> <c>[ScriptService]</c> web services -
    ///     the three state-changing steps of the stock-transfer workflow (authorize, reject,
    ///     receive).
    ///     <para>
    ///         Each former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below, INCLUDING ITS OWN AUTHORIZATION: every one of the three begins with
    ///         <c>if (!AppUsers.GetCurrent().View.IsAdmin.ToBool()) throw new
    ///         MixERPException(Warnings.AccessIsDenied);</c> followed by the
    ///         <c>tranId &lt;= 0</c> guard. THAT ADMIN CHECK IS THE ONLY THING STANDING BETWEEN A
    ///         SIGNED-IN NON-ADMIN AND AUTHORIZING A TRANSFER, so it is reproduced exactly and
    ///         deliberately NOT widened or replaced by the endpoint-level
    ///         <c>.RequireAuthorization()</c> - that only requires A user, this requires an ADMIN.
    ///     </para>
    ///     <para>
    ///         The refusal surfaces as the message of the preserved
    ///         <c>{Message, StackTrace, ExceptionType}</c> + HTTP 500 payload, which is exactly
    ///         what the callers' <c>logAjaxErrorMessage(xhr)</c> displays - the deleted
    ///         <c>Services/web.config</c>'s <c>&lt;customErrors mode="Off" /&gt;</c> behaviour.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are KEPT here ON PURPOSE -
    ///         a minimal-API endpoint has a live <c>HttpContext</c>, which is the one place they
    ///         resolve correctly. Over a circuit they would return an EMPTY login, i.e.
    ///         <c>IsAdmin == false</c>, which would make all three refuse unconditionally.
    ///     </para>
    /// </summary>
    public static class InventoryTransferWorkflowEndpoints
    {
        private const string AuthorizeRoute = "/services/inventory/transferauthorization/authorize";
        private const string RejectRoute = "/services/inventory/transferauthorization/reject";
        private const string ReceiveRoute = "/services/inventory/transferacknowledgement/receive";

        /// <summary>
        ///     Replaces <c>Services/TransferAuthorization.asmx</c> (<c>Authorize</c>,
        ///     <c>Reject</c>), called by <c>wwwroot/js/TransferAuthorization.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryTransferAuthorizationService(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(AuthorizeRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Authorize(p.GetInt64("tranId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(RejectRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Reject(p.GetInt64("tranId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        /// <summary>
        ///     Replaces <c>Services/TransferAcknowledgement.asmx</c> (<c>Receive</c>), called by
        ///     <c>wwwroot/js/TransferAcknowledgement.js</c>, which navigates to the returned
        ///     transaction's check list - so the <c>long</c> return value inside <c>msg.d</c> is
        ///     load-bearing.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryTransferAcknowledgementService(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(ReceiveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Receive(p.GetInt64("tranId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        //-------------------------------------------------------------------------------
        // The former [WebMethod] bodies, carried over verbatim.
        //-------------------------------------------------------------------------------

        private static string Authorize(long tranId)
        {
            bool isAdmin = AppUsers.GetCurrent().View.IsAdmin.ToBool();

            if (!isAdmin)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            if (tranId <= 0)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            int userId = AppUsers.GetCurrent().View.UserId.ToInt();

            StockTransfer.Authorize(AppUsers.GetCurrentUserDB(), userId, tranId);
            return "OK";
        }

        private static string Reject(long tranId)
        {
            bool isAdmin = AppUsers.GetCurrent().View.IsAdmin.ToBool();

            if (!isAdmin)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            if (tranId <= 0)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            int userId = AppUsers.GetCurrent().View.UserId.ToInt();

            StockTransfer.Reject(AppUsers.GetCurrentUserDB(), userId, tranId);
            return "OK";
        }

        private static long Receive(long tranId)
        {
            bool isAdmin = AppUsers.GetCurrent().View.IsAdmin.ToBool();

            if (!isAdmin)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            if (tranId <= 0)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            int userId = AppUsers.GetCurrent().View.UserId.ToInt();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

            return StockTransferAcknowledgement.Receive(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, tranId);
        }
    }
}
