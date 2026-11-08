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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.BackOffice.Data.Tax;

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/TaxData.asmx</c>
    ///     <c>[ScriptService]</c> web service.
    ///     <para>
    ///         ⚠ <c>getsalestaxes</c> IS A CROSS-MODULE CONTRACT THAT ALREADY EXISTS.
    ///         <c>Modules/Purchase/wwwroot/js/Reorder.js</c> was repointed by the Purchase pass
    ///         (recorded in <c>Modules/migration-summary.md</c> §9) at
    ///         <c>/services/backoffice/taxdata/getsalestaxes</c> as a PREDICTION of what this
    ///         pass would map. That prediction is honoured verbatim here, so <c>Reorder.js</c>
    ///         needed no change and the sales-tax select of the reorder grid resolves. The
    ///         third row of that file's <c>REVIEW</c> block is therefore closed.
    ///     </para>
    ///     <para>
    ///         The three former <c>[WebMethod]</c> bodies are carried over VERBATIM, including
    ///         the fact that <c>GetSalesTaxes</c> filters by the CURRENT USER'S OFFICE - the
    ///         office id comes from the signed-in login, never from the caller, so a caller
    ///         cannot ask for another office's tax set.
    ///     </para>
    ///     <para>
    ///         <c>GetSalesTax</c> and <c>GetSalesTaxId</c> have no javascript caller in the
    ///         solution today (<c>StockTransactionFactory</c>'s form reaches them through its
    ///         own configured <c>SalesTax*</c> urls), but they were public service surface and
    ///         are ported rather than dropped.
    ///     </para>
    /// </summary>
    public static class BackOfficeTaxDataEndpoints
    {
        /// <summary>
        ///     The route <c>Modules/Purchase/wwwroot/js/Reorder.js</c> already calls. Do not
        ///     change it without changing that file in the same commit.
        /// </summary>
        public const string GetSalesTaxesRoute = "/services/backoffice/taxdata/getsalestaxes";

        private const string GetSalesTaxRoute = "/services/backoffice/taxdata/getsalestax";
        private const string GetSalesTaxIdRoute = "/services/backoffice/taxdata/getsalestaxid";

        public static IEndpointRouteBuilder MapBackOfficeTaxDataService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetSalesTaxRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetSalesTax(
                        parameters.GetString("tranBook"),
                        parameters.GetInt32("storeId"),
                        parameters.GetString("partyCode"),
                        parameters.GetString("shippingAddressCode"),
                        parameters.GetInt32("priceTypeId"),
                        parameters.GetString("itemCode"),
                        parameters.GetDecimal("price"),
                        parameters.GetInt32("quantity"),
                        parameters.GetDecimal("discount"),
                        parameters.GetDecimal("shippingCharge"),
                        parameters.GetInt32("salesTaxId")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetSalesTaxesRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetSalesTaxes(parameters.GetString("tranBook")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetSalesTaxIdRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetSalesTaxId(
                        parameters.GetString("tranBook"),
                        parameters.GetInt32("storeId"),
                        parameters.GetString("partyCode"),
                        parameters.GetString("shippingAddressCode"),
                        parameters.GetInt32("priceTypeId"),
                        parameters.GetString("itemCode"),
                        parameters.GetInt32("unitId"),
                        parameters.GetDecimal("price")));
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
        //
        // AppUsers.GetCurrent() / GetCurrentUserDB() are KEPT here on purpose: a minimal-API
        // endpoint has a live HttpContext, which is the one place they resolve correctly. The
        // ban recorded in Modules/migration-summary.md applies to COMPONENT code only.
        //-------------------------------------------------------------------------------

        private static decimal GetSalesTax(string tranBook, int storeId, string partyCode,
            string shippingAddressCode, int priceTypeId, string itemCode, decimal price, int quantity,
            decimal discount, decimal shippingCharge, int salesTaxId)
        {
            return SalesTax.GetSalesTax(AppUsers.GetCurrentUserDB(), tranBook, storeId, partyCode,
                shippingAddressCode, priceTypeId, itemCode, price, quantity, discount, shippingCharge, salesTaxId);
        }

        /// <summary>
        ///     Gets a collection of sales taxes of the CURRENT office. Read by
        ///     <c>ajaxDataBind</c>, which indexes <c>this.Text</c> / <c>this.Value</c> off every
        ///     element of <c>msg.d</c> - hence <see cref="ListItem" />, the replacement for the
        ///     <c>System.Web.UI.WebControls.ListItem</c> the ASMX returned.
        /// </summary>
        private static Collection<ListItem> GetSalesTaxes(string tranBook)
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Net.Entities.Core.SalesTax salesTax in
                SalesTax.GetSalesTaxes(AppUsers.GetCurrentUserDB(), tranBook, officeId))
            {
                values.Add(new ListItem(salesTax.SalesTaxCode,
                    salesTax.SalesTaxId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static int GetSalesTaxId(string tranBook, int storeId, string partyCode,
            string shippingAddressCode, int priceTypeId, string itemCode, int unitId, decimal price)
        {
            return SalesTax.GetSalesTaxId(AppUsers.GetCurrentUserDB(), tranBook, storeId, partyCode,
                shippingAddressCode, priceTypeId, itemCode, unitId, price);
        }
    }
}
