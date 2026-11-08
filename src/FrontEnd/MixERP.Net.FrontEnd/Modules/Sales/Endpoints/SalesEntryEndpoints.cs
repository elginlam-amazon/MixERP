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
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.TransactionGovernor.Transactions;
using MixERP.Net.WebControls.StockTransactionFactory.Helpers;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.Sales.Endpoints
{
    /// <summary>
    ///     The five former <c>Modules/Sales/Services/Entry/*.asmx</c> <c>[ScriptService]</c> web
    ///     services, as minimal-API JSON endpoints. Each service keeps its own
    ///     <c>Map...(this IEndpointRouteBuilder)</c> extension method - nothing self-registers; the
    ///     host calls <see cref="SalesServiceEndpoints.MapSalesServiceEndpoints" /> once.
    ///     <para>
    ///         URLs (both GET and POST, as the deleted <c>&lt;webServices&gt;&lt;protocols&gt;</c>
    ///         block allowed):
    ///     </para>
    ///     <list type="bullet">
    ///         <item><c>/Modules/Sales/Services/Entry/DirectSales.asmx/Save</c> -&gt; <c>/services/sales/entry/directsales/save</c></item>
    ///         <item><c>/Modules/Sales/Services/Entry/Delivery.asmx/Save</c> -&gt; <c>/services/sales/entry/delivery/save</c></item>
    ///         <item><c>/Modules/Sales/Services/Entry/Order.asmx/Save</c> -&gt; <c>/services/sales/entry/order/save</c></item>
    ///         <item><c>/Modules/Sales/Services/Entry/Quotation.asmx/Save</c> -&gt; <c>/services/sales/entry/quotation/save</c></item>
    ///         <item><c>/Modules/Sales/Services/Entry/Return.asmx/Save</c> -&gt; <c>/services/sales/entry/return/save</c></item>
    ///     </list>
    ///     <para>
    ///         THE METHOD BODIES ARE THE ORIGINALS, UNCHANGED - including the store sales-allowed
    ///         check, the per-line stock availability check with its
    ///         <c>Warnings.InsufficientStockWarning</c> message, the transaction-id splitting, the
    ///         <c>Log.Warning(...); throw;</c> catch blocks, and the fact that Quotation parses its
    ///         transaction ids with <c>TryCastInteger</c> while its siblings use
    ///         <c>TryCastLong</c> (an upstream inconsistency, preserved).
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>AppUsers.GetCurrentUserDB()</c> ARE CORRECT HERE
    ///         and are deliberately kept: these are real HTTP endpoints with a live
    ///         <c>HttpContext</c>, which is the one place those helpers resolve properly. (They are
    ///         the calls that must NOT be used from component code, where there is no
    ///         <c>HttpContext</c> over the circuit.)
    ///     </para>
    ///     <para>
    ///         <c>.DisableAntiforgery()</c> - these are tokenless AJAX POSTs, exactly as the legacy
    ///         ASMX calls were. <c>.RequireAuthorization()</c> - the deleted
    ///         <c>Modules/web.config</c> had <c>&lt;deny users="?" /&gt;</c>, so the whole
    ///         <c>Modules/**</c> subtree, these services included, required an authenticated user.
    ///     </para>
    /// </summary>
    public static class SalesEntryEndpoints
    {
        public const string DirectSalesSaveRoute = "/services/sales/entry/directsales/save";
        public const string DeliverySaveRoute = "/services/sales/entry/delivery/save";
        public const string OrderSaveRoute = "/services/sales/entry/order/save";
        public const string QuotationSaveRoute = "/services/sales/entry/quotation/save";
        public const string ReturnSaveRoute = "/services/sales/entry/return/save";

        /// <summary>
        ///     Replaces <c>Services/Entry/DirectSales.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/DirectSales.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapSalesDirectSalesService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(DirectSalesSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveDirectSales(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetInt32("priceTypeId"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("transactionType"),
                        p.GetInt32("paymentTermId"),
                        p.GetInt32("salespersonId"),
                        p.GetInt32("shipperId"),
                        p.GetString("shippingAddressCode"),
                        p.GetDecimal("shippingCharge"),
                        p.GetInt32("costCenterId"),
                        p.GetString("transactionIds"),
                        p.GetString("attachmentsJSON"),
                        p.GetBoolean("nonTaxable")));
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
        ///     Replaces <c>Services/Entry/Delivery.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/Delivery.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapSalesDeliveryService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(DeliverySaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveDelivery(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetInt32("priceTypeId"),
                        p.GetInt32("paymentTermId"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetInt32("salespersonId"),
                        p.GetInt32("shipperId"),
                        p.GetString("shippingAddressCode"),
                        p.GetDecimal("shippingCharge"),
                        p.GetInt32("costCenterId"),
                        p.GetString("transactionIds"),
                        p.GetString("attachmentsJSON"),
                        p.GetBoolean("nonTaxable")));
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
        public static IEndpointRouteBuilder MapSalesOrderService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(OrderSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveOrder(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetInt32("priceTypeId"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("transactionIds"),
                        p.GetString("attachmentsJSON"),
                        p.GetBoolean("nonTaxable"),
                        p.GetInt32("salespersonId"),
                        p.GetInt32("shipperId"),
                        p.GetString("shippingAddressCode")));
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
        ///     Replaces <c>Services/Entry/Quotation.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/Quotation.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapSalesQuotationService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(QuotationSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveQuotation(
                        p.GetDateTime("valueDate"),
                        p.GetInt32("storeId"),
                        p.GetString("partyCode"),
                        p.GetInt32("priceTypeId"),
                        p.GetString("referenceNumber"),
                        p.GetString("data"),
                        p.GetString("statementReference"),
                        p.GetString("transactionIds"),
                        p.GetString("attachmentsJSON"),
                        p.GetBoolean("nonTaxable"),
                        p.GetInt32("salespersonId"),
                        p.GetInt32("shipperId"),
                        p.GetString("shippingAddressCode")));
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
        public static IEndpointRouteBuilder MapSalesReturnService(this IEndpointRouteBuilder endpoints)
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

        private static long SaveDirectSales(DateTime valueDate, int storeId, string partyCode, int priceTypeId,
            string referenceNumber, string data, string statementReference, string transactionType, int paymentTermId,
            int salespersonId, int shipperId, string shippingAddressCode, decimal shippingCharge, int costCenterId,
            string transactionIds, string attachmentsJSON, bool nonTaxable)
        {
            try
            {
                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, storeId);

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                bool isCredit = transactionType != null && !transactionType.ToUpperInvariant().Equals("CASH");

                if (!Data.Helpers.Stores.IsSalesAllowed(AppUsers.GetCurrentUserDB(), storeId))
                {
                    throw new InvalidOperationException("Sales is not allowed here.");
                }

                foreach (StockDetail model in details)
                {
                    if (Data.Helpers.Items.IsStockItem(AppUsers.GetCurrentUserDB(), model.ItemCode))
                    {
                        decimal available = Data.Helpers.Items.CountItemInStock(AppUsers.GetCurrentUserDB(),
                            model.ItemCode, model.UnitName, model.StoreId);

                        if (available < model.Quantity)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                Warnings.InsufficientStockWarning, available, model.UnitName, model.ItemCode));
                        }
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.DirectSales.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId,
                    valueDate, storeId, isCredit, paymentTermId, partyCode, salespersonId, priceTypeId, details,
                    shipperId, shippingAddressCode, shippingCharge, costCenterId, referenceNumber, statementReference,
                    attachments, nonTaxable);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save direct sales entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveDelivery(DateTime valueDate, int storeId, string partyCode, int priceTypeId,
            int paymentTermId, string referenceNumber, string data, string statementReference, int salespersonId,
            int shipperId, string shippingAddressCode, decimal shippingCharge, int costCenterId, string transactionIds,
            string attachmentsJSON, bool nonTaxable)
        {
            try
            {
                Collection<StockDetail> details = CollectionHelper.GetStockMasterDetailCollection(data, storeId);
                Collection<long> tranIds = new Collection<long>();

                Collection<Attachment> attachments = CollectionHelper.GetAttachmentCollection(attachmentsJSON);

                if (!Data.Helpers.Stores.IsSalesAllowed(AppUsers.GetCurrentUserDB(), storeId))
                {
                    throw new InvalidOperationException("Sales is not allowed here.");
                }

                foreach (StockDetail model in details)
                {
                    if (Data.Helpers.Items.IsStockItem(AppUsers.GetCurrentUserDB(), model.ItemCode))
                    {
                        decimal available = Data.Helpers.Items.CountItemInStock(AppUsers.GetCurrentUserDB(),
                            model.ItemCode, model.UnitName, model.StoreId);

                        if (available < model.Quantity)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                Warnings.InsufficientStockWarning, available, model.UnitName, model.ItemCode));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(transactionIds))
                {
                    foreach (string transactionId in transactionIds.Split(','))
                    {
                        tranIds.Add(Common.Conversion.TryCastLong(transactionId));
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.Delivery.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate,
                    storeId, partyCode, priceTypeId, paymentTermId, details, shipperId, shippingAddressCode,
                    shippingCharge, costCenterId, referenceNumber, salespersonId, statementReference, tranIds,
                    attachments, nonTaxable);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save sales delivery entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveOrder(DateTime valueDate, int storeId, string partyCode, int priceTypeId,
            string referenceNumber, string data, string statementReference, string transactionIds,
            string attachmentsJSON, bool nonTaxable, int salespersonId, int shipperId, string shippingAddressCode)
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
                        tranIds.Add(Common.Conversion.TryCastLong(transactionId));
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.Order.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate,
                    partyCode, priceTypeId, details, referenceNumber, statementReference, tranIds, attachments,
                    nonTaxable, salespersonId, shipperId, shippingAddressCode, storeId);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save sales order entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveQuotation(DateTime valueDate, int storeId, string partyCode, int priceTypeId,
            string referenceNumber, string data, string statementReference, string transactionIds,
            string attachmentsJSON, bool nonTaxable, int salespersonId, int shipperId, string shippingAddressCode)
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
                        //PRESERVED: the quotation service parses its merged transaction ids with
                        //TryCastInteger while every sibling uses TryCastLong. It is an upstream
                        //inconsistency, not a transcription error.
                        tranIds.Add(Common.Conversion.TryCastInteger(transactionId));
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return Data.Transactions.Quotation.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId,
                    valueDate, partyCode, priceTypeId, details, referenceNumber, statementReference, tranIds,
                    attachments, nonTaxable, salespersonId, shipperId, shippingAddressCode, storeId);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save sales quotation entry. {Exception}", ex);
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
                    officeId, userId, loginId, storeId,
                    partyCode, priceTypeId, referenceNumber, statementReference, details, attachments);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save sales return entry. {Exception}", ex);
                throw;
            }
        }
    }
}
