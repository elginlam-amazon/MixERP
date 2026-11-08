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
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.i18n.Resources;
using MixERP.Net.TransactionGovernor.Transactions;
using MixERP.Net.WebControls.StockTransactionFactory.Helpers;
using Serilog;

namespace MixERP.Net.Core.Modules.Purchase.Endpoints
{
    /// <summary>
    ///     The minimal-API replacements for the five deleted
    ///     <c>Services/Entry/*.asmx</c> <c>[ScriptService]</c> web services of this module.
    ///     <para>
    ///         Each former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below; the endpoint delegate does nothing but bind the arguments the
    ///         javascript caller posts (through <see cref="AsmxCompatibility.ReadParametersAsync" />)
    ///         and wrap the result in the <c>{"d": ...}</c> / HTTP-500 envelope the callers already
    ///         parse. So the javascript change is URL-ONLY - no response handling moved.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are KEPT here ON PURPOSE.
    ///         The ban recorded in the module migration summary applies to COMPONENT code, which
    ///         runs over a SignalR circuit with no <c>HttpContext</c>; a minimal-API endpoint has a
    ///         live <c>HttpContext</c>, which is the one place those two resolve correctly - exactly
    ///         as they did inside the ASMX.
    ///     </para>
    ///     <para>
    ///         Every endpoint is <c>.RequireAuthorization()</c>, which reproduces the deleted
    ///         <c>Modules/web.config</c>'s <c>&lt;deny users="?" /&gt;</c>: all five services read
    ///         the signed-in user's office/user/login id and post a financial transaction as them.
    ///         <c>.DisableAntiforgery()</c> matches the ASMX, which ran no antiforgery check.
    ///     </para>
    /// </summary>
    public static class PurchaseEntryEndpoints
    {
        private const string DirectPurchaseSaveRoute = "/services/purchase/entry/directpurchase/save";
        private const string GrnSaveRoute = "/services/purchase/entry/grn/save";
        private const string OrderSaveRoute = "/services/purchase/entry/order/save";
        private const string ReorderSaveRoute = "/services/purchase/entry/reorder/save";
        private const string ReturnSaveRoute = "/services/purchase/entry/return/save";

        /// <summary>
        ///     Replaces <c>Services/Entry/DirectPurchase.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/DirectPurchase.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapPurchaseDirectPurchaseService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(DirectPurchaseSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveDirectPurchase(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("transactionType"),
                        p.GetInt32("costCenterId"),
                        p.GetString("attachmentsJSON")));
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
        ///     Replaces <c>Services/Entry/GRN.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/GRN.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapPurchaseGrnService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GrnSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveGrn(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetInt32("costCenterId"),
                        p.GetString("transactionIds"),
                        p.GetString("attachmentsJSON")));
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
        ///     Replaces <c>Services/Entry/Order.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/Order.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapPurchaseOrderService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(OrderSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveOrder(
                        p.GetDateTime("valueDate"),
                        p.GetString("partyCode"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("attachmentsJSON")));
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
        ///     Replaces <c>Services/Entry/Reorder.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/Reorder.js</c> - the only one of the five whose argument is a
        ///     COMPLEX-OBJECT ARRAY rather than primitives, bound by
        ///     <see cref="AsmxParameters.GetCollection{T}" />.
        /// </summary>
        public static IEndpointRouteBuilder MapPurchaseReorderService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(ReorderSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveReorder(
                        p.GetCollection<Data.Models.Reorder>("details")));
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
        ///     Replaces <c>Services/Entry/Return.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/Return.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapPurchaseReturnService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(ReturnSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveReturn(
                        p.GetInt64("tranId"),
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetInt32("priceTypeId"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("attachmentsJSON")));
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

        //=================================================================================
        // The former [WebMethod] bodies, verbatim.
        //=================================================================================

        private static long SaveDirectPurchase(DateTime valueDate, int storeId, string partyCode,
            string referenceNumber, string data, string statementReference, string transactionType, int costCenterId,
            string attachmentsJSON)
        {
            try
            {
                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, storeId);

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                bool isCredit = !string.IsNullOrWhiteSpace(transactionType) &&
                                !transactionType.ToUpperInvariant().Equals("CASH");
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.DirectPurchase.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId,
                    valueDate, storeId, isCredit, partyCode, details, costCenterId, referenceNumber,
                    statementReference, attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save direct purchase entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveGrn(DateTime valueDate, int storeId, string partyCode, string referenceNumber,
            string data, string statementReference, int costCenterId, string transactionIds, string attachmentsJSON)
        {
            try
            {
                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, storeId);
                Collection<long> tranIds = new Collection<long>();

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                if (!string.IsNullOrWhiteSpace(transactionIds))
                {
                    foreach (string transactionId in transactionIds.Split(','))
                    {
                        //PRESERVED: the GRN service parses its merged transaction ids with
                        //TryCastInteger even though the collection is Collection<long>. It is an
                        //upstream inconsistency (the Sales quotation service has the same one),
                        //not a transcription error.
                        tranIds.Add(Conversion.TryCastInteger(transactionId));
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.GRN.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate,
                    storeId, partyCode, details, costCenterId, referenceNumber, statementReference, tranIds,
                    attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save GRN entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveOrder(DateTime valueDate, string partyCode, string referenceNumber, string data,
            string statementReference, string attachmentsJSON)
        {
            try
            {
                //PRESERVED: the purchase order service passes storeId 0 to the detail collection
                //(a purchase order is a non-GL transaction that carries no store), unlike its
                //Sales counterpart which forwards the posted storeId.
                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, 0);

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.Order.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId,
                    "Purchase.Order", valueDate, partyCode, 0, details, referenceNumber, statementReference, null,
                    attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save purchase order entry. {Exception}", ex);
                throw;
            }
        }

        private static bool SaveReorder(Collection<Data.Models.Reorder> details)
        {
            try
            {
                if (details == null)
                {
                    throw new ArgumentNullException("details");
                }

                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

                return Data.Transactions.Reorder.Save(AppUsers.GetCurrentUserDB(), loginId, userId, officeId, details);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save purchase reorder entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveReturn(long tranId, DateTime valueDate, int storeId, string partyCode, int priceTypeId,
            string referenceNumber, string data, string statementReference, string attachmentsJSON)
        {
            try
            {
                if (!StockTransaction.IsValidStockTransactionByTransactionMasterId(AppUsers.GetCurrentUserDB(), tranId))
                {
                    throw new InvalidOperationException(Warnings.InvalidStockTransaction);
                }

                if (!StockTransaction.IsValidPartyByTransactionMasterId(AppUsers.GetCurrentUserDB(), tranId, partyCode))
                {
                    throw new InvalidOperationException(Warnings.InvalidParty);
                }

                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, storeId);

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.Return.PostTransaction(AppUsers.GetCurrentUserDB(), tranId, valueDate,
                    officeId, userId, loginId, storeId, partyCode, priceTypeId, referenceNumber, statementReference,
                    details, attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save purchase return entry. {Exception}", ex);
                throw;
            }
        }
    }
}
