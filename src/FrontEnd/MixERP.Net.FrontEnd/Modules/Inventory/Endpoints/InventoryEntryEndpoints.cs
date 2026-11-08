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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Inventory.Data.Domains;
using MixERP.Net.Core.Modules.Inventory.Data.Helpers;
using MixERP.Net.Core.Modules.Inventory.Data.Transactions;
using MixERP.Net.Entities;
using MixERP.Net.Entities.Models.Transactions;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.Inventory.Endpoints
{
    /// <summary>
    ///     The minimal-API replacements for the four deleted
    ///     <c>Services/Entry/*.asmx</c> <c>[ScriptService]</c> web services of this module -
    ///     <c>Adjustment</c>, <c>Transfer</c>, <c>TransferDelivery</c> and
    ///     <c>TransferRequest</c>.
    ///     <para>
    ///         Each former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below - including the insufficient-stock pre-check of Adjustment and
    ///         Transfer, every <c>MixERPException</c> guard, and each service's own
    ///         <c>Log.Warning("Could not save inventory ... entry. {Exception}", ex); throw;</c>
    ///         wrapper. The endpoint delegate only binds the posted arguments and wraps the result
    ///         in the preserved <c>{"d": ...}</c> / HTTP-500 envelope, so the javascript change is
    ///         URL-ONLY.
    ///     </para>
    ///     <para>
    ///         THE GRID PARSERS, AND WHY THEY DIFFER FROM EACH OTHER. The legacy services all
    ///         used <c>JavaScriptSerializer.Deserialize&lt;dynamic&gt;</c> and then indexed the
    ///         result, but NOT the same way, because their callers post two different shapes:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <c>Adjustment</c> receives <c>models</c> - an array of OBJECTS
    ///             (<c>JSON.stringify</c> of <c>MixERP.StockAdjustmentModel</c>) - and reads
    ///             <c>item["StoreName"]</c>, <c>["ItemCode"]</c>, <c>["ItemName"]</c>,
    ///             <c>["UnitName"]</c>, <c>["Quantity"]</c> BY NAME, with the transfer type
    ///             hard-coded to <c>Credit</c>.
    ///         </item>
    ///         <item>
    ///             <c>Transfer</c> receives <c>data</c> - an array of ARRAYS
    ///             (<c>tableToJSON</c> of the grid) - and reads <c>item[0..5]</c> POSITIONALLY,
    ///             with <c>item[0]</c> the <c>"Dr"</c>/<c>"Cr"</c> side.
    ///         </item>
    ///         <item>
    ///             <c>TransferDelivery</c> and <c>TransferRequest</c> receive the same
    ///             array-of-arrays but SHIFTED by one (<c>item[0..4]</c>, no side column -
    ///             their forms run with <c>HideSides = true</c>) and hard-code
    ///             <c>Debit</c>.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         All three index schemes are reproduced exactly, with Newtonsoft's
    ///         <c>JArray</c>/<c>JToken</c> in place of the removed <c>JavaScriptSerializer</c>.
    ///         Each parser also keeps its original's behaviour for a non-array payload: the legacy
    ///         <c>foreach</c> over a non-enumerable <c>dynamic</c> threw, and the
    ///         <c>models == null || models.Count.Equals(0)</c> guards of the callers downstream
    ///         are what turned an empty grid into <c>Warnings.GridViewEmpty</c>.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are KEPT here ON PURPOSE -
    ///         a minimal-API endpoint has a live <c>HttpContext</c>, which is the one place they
    ///         resolve correctly, exactly as inside the ASMX. The ban applies to component code.
    ///     </para>
    ///     <para>
    ///         Every endpoint is <c>.RequireAuthorization()</c> (the deleted
    ///         <c>Modules/web.config</c>'s <c>&lt;deny users="?" /&gt;</c>: all four read the
    ///         signed-in user's office/user/login id and post a stock transaction as them) plus
    ///         <c>.DisableAntiforgery()</c>, matching the ASMX, which ran no antiforgery check.
    ///     </para>
    /// </summary>
    public static class InventoryEntryEndpoints
    {
        private const string AdjustmentSaveRoute = "/services/inventory/entry/adjustment/save";
        private const string TransferSaveRoute = "/services/inventory/entry/transfer/save";
        private const string TransferDeliverySaveRoute = "/services/inventory/entry/transferdelivery/save";
        private const string TransferDeliveryGetModelRoute = "/services/inventory/entry/transferdelivery/getmodel";
        private const string TransferRequestSaveRoute = "/services/inventory/entry/transferrequest/save";

        /// <summary>
        ///     Replaces <c>Services/Entry/Adjustment.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/Adjustment.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryAdjustmentService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(AdjustmentSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveAdjustment(
                        p.GetDateTime("valueDate"),
                        p.GetString("referenceNumber"),
                        p.GetString("statementReference"),
                        p.GetString("models")));
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
        ///     Replaces <c>Services/Entry/Transfer.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/Transfer.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryTransferService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(TransferSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveTransfer(
                        p.GetDateTime("valueDate"),
                        p.GetString("referenceNumber"),
                        p.GetString("statementReference"),
                        p.GetString("data")));
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
        ///     Replaces <c>Services/Entry/TransferDelivery.asmx</c> (<c>Save</c> and
        ///     <c>GetModel</c>), both called by <c>wwwroot/js/entry/TransferDelivery.js</c>.
        ///     <c>GetModel</c>'s result is read member-by-member client side
        ///     (<c>StoreName</c>, <c>ItemCode</c>, <c>ItemName</c>, <c>UnitName</c>,
        ///     <c>Quantity</c>), which the Newtonsoft serialization preserves.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryTransferDeliveryService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(TransferDeliverySaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveTransferDelivery(
                        p.GetDateTime("valueDate"),
                        p.GetString("referenceNumber"),
                        p.GetString("statementReference"),
                        p.GetInt64("requestId"),
                        p.GetInt32("sourceStoreId"),
                        p.GetInt32("shipperId"),
                        p.GetString("data")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(TransferDeliveryGetModelRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetTransferDeliveryModel(p.GetInt64("requestId")));
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
        ///     Replaces <c>Services/Entry/TransferRequest.asmx</c> (<c>Save</c>), called by
        ///     <c>wwwroot/js/entry/TransferRequest.js</c>.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryTransferRequestService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(TransferRequestSaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveTransferRequest(
                        p.GetDateTime("valueDate"),
                        p.GetString("referenceNumber"),
                        p.GetString("statementReference"),
                        p.GetString("data")));
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

        private static long SaveAdjustment(DateTime valueDate, string referenceNumber, string statementReference,
            string models)
        {
            try
            {
                Collection<StockAdjustmentDetail> stockAdjustmentModels = GetAdjustmentModels(models);
                foreach (StockAdjustmentDetail model in stockAdjustmentModels)
                {
                    if (model.TransferTypeEnum == TransactionTypeEnum.Credit)
                    {
                        decimal existingQuantity = Items.CountItemInStock(AppUsers.GetCurrentUserDB(), model.ItemCode, model.UnitName, model.StoreName);

                        if (existingQuantity < model.Quantity)
                        {
                            throw new MixERPException(string.Format(CultureInfo.CurrentCulture, Errors.InsufficientStockWarning, Conversion.TryCastInteger(existingQuantity), model.UnitName, model.ItemName));
                        }
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return StockAdjustment.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate, referenceNumber, statementReference, stockAdjustmentModels);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save inventory adjustment entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveTransfer(DateTime valueDate, string referenceNumber, string statementReference,
            string data)
        {
            try
            {
                Collection<StockAdjustmentDetail> stockTransferModels = GetTransferModels(data);

                foreach (StockAdjustmentDetail model in stockTransferModels)
                {
                    if (model.TransferTypeEnum == TransactionTypeEnum.Credit)
                    {
                        decimal existingQuantity = Items.CountItemInStock(AppUsers.GetCurrentUserDB(), model.ItemCode,
                            model.UnitName, model.StoreName);

                        if (existingQuantity < model.Quantity)
                        {
                            throw new MixERPException(string.Format(CultureInfo.CurrentCulture,
                                Errors.InsufficientStockWarning, Conversion.TryCastInteger(existingQuantity),
                                model.UnitName, model.ItemName));
                        }
                    }
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return StockTransfer.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate,
                    referenceNumber, statementReference, stockTransferModels);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save inventory transfer entry. {Exception}", ex);
                throw;
            }
        }

        private static long SaveTransferDelivery(DateTime valueDate, string referenceNumber,
            string statementReference, long requestId, int sourceStoreId, int shipperId, string data)
        {
            try
            {
                Collection<StockAdjustmentDetail> models = GetSidelessModels(data);

                if (requestId <= 0)
                {
                    throw new MixERPException(Warnings.AccessIsDenied);
                }


                if (models == null || models.Count.Equals(0))
                {
                    throw new MixERPException(Warnings.GridViewEmpty);
                }

                if (sourceStoreId <= 0)
                {
                    throw new MixERPException(Warnings.InvalidStore);
                }

                if (shipperId <= 0)
                {
                    throw new MixERPException(Warnings.InvalidShippingCompany);
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return StockTransferDelivery.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, requestId, valueDate,
                    referenceNumber, statementReference, shipperId, sourceStoreId, models);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save inventory transfer entry. {Exception}", ex);
                throw;
            }
        }

        private static IEnumerable<StockTransferDeliveryModel> GetTransferDeliveryModel(long requestId)
        {
            if (requestId <= 0)
            {
                throw new MixERPException(Warnings.AccessIsDenied);
            }

            return StockTransferDelivery.GetModel(AppUsers.GetCurrentUserDB(), requestId);
        }

        private static long SaveTransferRequest(DateTime valueDate, string referenceNumber,
            string statementReference, string data)
        {
            try
            {
                Collection<StockAdjustmentDetail> models = GetSidelessModels(data);

                if (models == null || models.Count.Equals(0))
                {
                    throw new MixERPException(Warnings.GridViewEmpty);
                }

                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                return StockTransferRequest.Add(AppUsers.GetCurrentUserDB(), officeId, userId, loginId, valueDate,
                    referenceNumber, statementReference, models);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save inventory transfer entry. {Exception}", ex);
                throw;
            }
        }

        //-------------------------------------------------------------------------------
        // The three grid parsers - see the class remarks for why they differ.
        //-------------------------------------------------------------------------------

        /// <summary>
        ///     Port of <c>Services/Entry/Adjustment.asmx.cs</c>'s <c>GetModels</c>: an array of
        ///     OBJECTS, read BY NAME, with the transfer type hard-coded to <c>Credit</c>.
        /// </summary>
        private static Collection<StockAdjustmentDetail> GetAdjustmentModels(string json)
        {
            Collection<StockAdjustmentDetail> models = new Collection<StockAdjustmentDetail>();

            foreach (JToken item in Parse(json))
            {
                StockAdjustmentDetail detail = new StockAdjustmentDetail();
                const TransactionTypeEnum typeEnum = TransactionTypeEnum.Credit;

                detail.TransferTypeEnum = typeEnum;
                detail.StoreName = Conversion.TryCastString(Text(item["StoreName"]));
                detail.ItemCode = Conversion.TryCastString(Text(item["ItemCode"]));
                detail.ItemName = Conversion.TryCastString(Text(item["ItemName"]));
                detail.UnitName = Conversion.TryCastString(Text(item["UnitName"]));
                detail.Quantity = Conversion.TryCastInteger(Text(item["Quantity"]));

                models.Add(detail);
            }

            return models;
        }

        /// <summary>
        ///     Port of <c>Services/Entry/Transfer.asmx.cs</c>'s <c>GetModels</c>: an array of
        ///     ARRAYS read POSITIONALLY, where <c>item[0]</c> is the <c>"Dr"</c>/<c>"Cr"</c>
        ///     side. Only the exact string <c>"Dr"</c> selects Debit - everything else, including
        ///     a missing cell, stays Credit, as the original's <c>Equals("Dr")</c> did.
        /// </summary>
        private static Collection<StockAdjustmentDetail> GetTransferModels(string json)
        {
            Collection<StockAdjustmentDetail> models = new Collection<StockAdjustmentDetail>();

            foreach (JToken item in Parse(json))
            {
                StockAdjustmentDetail detail = new StockAdjustmentDetail();
                TransactionTypeEnum typeEnum = TransactionTypeEnum.Credit;

                if (Conversion.TryCastString(Text(item[0])).ToString().Equals("Dr"))
                {
                    typeEnum = TransactionTypeEnum.Debit;
                }

                detail.TransferTypeEnum = typeEnum;
                detail.StoreName = Conversion.TryCastString(Text(item[1]));
                detail.ItemCode = Conversion.TryCastString(Text(item[2]));
                detail.ItemName = Conversion.TryCastString(Text(item[3]));
                detail.UnitName = Conversion.TryCastString(Text(item[4]));
                detail.Quantity = Conversion.TryCastInteger(Text(item[5]));

                models.Add(detail);
            }

            return models;
        }

        /// <summary>
        ///     Port of the IDENTICAL <c>GetModels</c> of
        ///     <c>Services/Entry/TransferDelivery.asmx.cs</c> and
        ///     <c>Services/Entry/TransferRequest.asmx.cs</c>: the same array of arrays SHIFTED by
        ///     one, because both forms run with <c>HideSides = true</c> and so render no
        ///     transaction-type column, with the transfer type hard-coded to <c>Debit</c>. The two
        ///     legacy copies were byte-identical, so they are one method here - they cannot drift.
        /// </summary>
        private static Collection<StockAdjustmentDetail> GetSidelessModels(string json)
        {
            Collection<StockAdjustmentDetail> models = new Collection<StockAdjustmentDetail>();

            foreach (JToken item in Parse(json))
            {
                StockAdjustmentDetail detail = new StockAdjustmentDetail();
                detail.TransferTypeEnum = TransactionTypeEnum.Debit;

                detail.StoreName = Conversion.TryCastString(Text(item[0]));
                detail.ItemCode = Conversion.TryCastString(Text(item[1]));
                detail.ItemName = Conversion.TryCastString(Text(item[2]));
                detail.UnitName = Conversion.TryCastString(Text(item[3]));
                detail.Quantity = Conversion.TryCastInteger(Text(item[4]));

                models.Add(detail);
            }

            return models;
        }

        /// <summary>
        ///     The replacement for <c>JavaScriptSerializer.Deserialize&lt;dynamic&gt;</c> plus the
        ///     <c>foreach</c> over its result. A blank payload yields no rows, which is the
        ///     empty-grid path the callers' own <c>GridViewEmpty</c> guard then reports; a
        ///     non-array payload throws, exactly as iterating a non-enumerable dynamic did.
        /// </summary>
        private static JArray Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new JArray();
            }

            return (JArray)JToken.Parse(json);
        }

        /// <summary>
        ///     A single cell as the <c>dynamic</c> indexer used to hand it to
        ///     <c>Conversion.TryCast*</c>: its text, or null when absent/JSON null.
        /// </summary>
        private static string Text(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return token.ToString();
        }
    }
}
