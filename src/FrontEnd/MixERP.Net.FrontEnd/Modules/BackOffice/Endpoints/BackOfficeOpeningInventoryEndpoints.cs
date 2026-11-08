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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Transactions;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted
    ///     <c>Services/OTS/OpeningInventory.asmx</c> <c>[ScriptService]</c> web service, called
    ///     by <c>wwwroot/js/ots/OpeningInventory.ascx.js</c>'s <c>save(...)</c>.
    ///     <para>
    ///         The former <c>[WebMethod]</c> body and its <c>GetStockDetails</c> parser are
    ///         carried over VERBATIM, including the user/office/login ids being taken from the
    ///         SIGNED-IN LOGIN rather than from the caller - the opening-stock rows are posted
    ///         against the current office and cannot be redirected at another one.
    ///     </para>
    ///     <para>
    ///         THE POSITIONAL GRID PARSE IS BEHAVIOUR, NOT TRANSPORT. The caller sends the
    ///         rendered <c>#OpeningInventoryGridView</c> through <c>tableToJSON</c>, i.e. an
    ///         array of ARRAYS, and the original read <c>item[0]</c> = ItemCode,
    ///         <c>item[2]</c> = StoreName, <c>item[3]</c> = Quantity, <c>item[4]</c> = UnitName,
    ///         <c>item[5]</c> = Amount - deliberately SKIPPING <c>item[1]</c> (the item name)
    ///         and <c>item[6]</c> (the row total, which the stored procedure recomputes). That
    ///         exact index scheme is preserved; it is the reason the ported component emits all
    ///         eight grid columns in the original order.
    ///         <c>JavaScriptSerializer.Deserialize&lt;dynamic&gt;</c> became Newtonsoft
    ///         <c>JArray</c>/<c>JToken</c> (same module, same Newtonsoft 13.0.4 reference as the
    ///         report-writer service), with the same
    ///         <c>Conversion.TryCastInteger</c>/<c>TryCastDecimal</c> coercions, so a
    ///         non-numeric cell still becomes 0 instead of throwing.
    ///     </para>
    /// </summary>
    public static class BackOfficeOpeningInventoryEndpoints
    {
        private const string SaveRoute = "/services/backoffice/ots/openinginventory/save";

        public static IEndpointRouteBuilder MapBackOfficeOpeningInventoryService(
            this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(SaveRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(Save(
                        parameters.GetDateTime("valueDate"),
                        parameters.GetString("referenceNumber"),
                        parameters.GetString("statementReference"),
                        parameters.GetString("jsonDetails")));
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
        // The former [WebMethod] body, carried over verbatim.
        //
        // AppUsers.GetCurrent() / GetCurrentUserDB() are KEPT here on purpose: a minimal-API
        // endpoint has a live HttpContext, which is the one place they resolve correctly.
        //-------------------------------------------------------------------------------

        private static long Save(DateTime valueDate, string referenceNumber, string statementReference,
            string jsonDetails)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonDetails))
                {
                    return 0;
                }

                int userId = AppUsers.GetCurrent().View.UserId.ToInt();
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
                long loginId = AppUsers.GetCurrent().View.LoginId.ToLong();

                Collection<OpeningStockType> details = GetStockDetails(jsonDetails);

                return Data.OneTimeSetup.OpeningInventory.Save(AppUsers.GetCurrentUserDB(), officeId, userId,
                    loginId, valueDate, referenceNumber, statementReference, details);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not save opening inventory entry. {Exception}", ex);
                throw;
            }
        }

        /// <summary>
        ///     Port of the private <c>GetStockDetails</c>. The <c>item[0]</c>, <c>item[2]</c>,
        ///     <c>item[3]</c>, <c>item[4]</c>, <c>item[5]</c> index scheme - and the two skipped
        ///     indices - are the original's.
        /// </summary>
        private static Collection<OpeningStockType> GetStockDetails(string json)
        {
            Collection<OpeningStockType> details = new Collection<OpeningStockType>();

            JArray result = JToken.Parse(json) as JArray;

            if (result != null)
            {
                foreach (JToken item in result)
                {
                    OpeningStockType detail = new OpeningStockType();
                    detail.ItemCode = GetValue(item, 0);
                    detail.StoreName = GetValue(item, 2);
                    detail.Quantity = Conversion.TryCastInteger(GetValue(item, 3));
                    detail.UnitName = GetValue(item, 4);
                    detail.Amount = Conversion.TryCastDecimal(GetValue(item, 5));

                    details.Add(detail);
                }
            }

            return details;
        }

        private static string GetValue(JToken item, int index)
        {
            JArray row = item as JArray;

            if (row == null || index >= row.Count)
            {
                return null;
            }

            JToken token = row[index];

            return token == null || token.Type == JTokenType.Null ? null : token.ToString();
        }
    }
}
