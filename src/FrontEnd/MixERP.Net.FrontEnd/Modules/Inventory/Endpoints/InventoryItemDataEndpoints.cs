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
using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Inventory.Data.Domains;
using MixERP.Net.Core.Modules.Inventory.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Office;

namespace MixERP.Net.Core.Modules.Inventory.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/ItemData.asmx</c>
    ///     <c>[ScriptService]</c> web service - the module's item / unit / store / shipper
    ///     lookup surface, and the most widely consumed service in the whole solution.
    ///     <para>
    ///         Each former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below; the endpoint delegate does nothing but bind the arguments the caller
    ///         posts (through <see cref="AsmxCompatibility.ReadParametersAsync" />) and wrap the
    ///         result in the <c>{"d": ...}</c> / HTTP-500 envelope the callers already parse. So
    ///         every caller's change is URL-ONLY - no response handling moved.
    ///     </para>
    ///     <para>
    ///         <c>Collection&lt;ListItem&gt;</c> return values use
    ///         <see cref="ListItem" />, this project's replacement for the removed
    ///         <c>System.Web.UI.WebControls.ListItem</c>. ITS JSON SHAPE IS THE CONTRACT:
    ///         <c>mixerp-core.js</c>'s <c>ajaxDataBind</c> reads <c>this.Text</c> and
    ///         <c>this.Value</c> off every element of <c>msg.d</c>, and Newtonsoft serializes
    ///         those two PascalCase names unchanged.
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
    ///         <c>Modules/web.config</c>'s <c>&lt;deny users="?" /&gt;</c> - these methods read the
    ///         signed-in user's catalog, office and user id, and <c>GetStores</c> is explicitly
    ///         scoped to them. <c>.DisableAntiforgery()</c> matches the ASMX, which ran no
    ///         antiforgery check.
    ///     </para>
    /// </summary>
    public static class InventoryItemDataEndpoints
    {
        private const string RoutePrefix = "/services/inventory/itemdata/";

        /// <summary>
        ///     Replaces <c>Services/ItemData.asmx</c>. Callers of the individual methods:
        ///     <c>GetStores</c> - <c>wwwroot/js/Adjustment.js</c>,
        ///     <c>wwwroot/js/reports/AccountStatement.js</c> and the three
        ///     <c>Entry/*</c> components' <c>StoreServiceUrl</c> / <c>SourceStore</c> binding;
        ///     <c>GetItems</c> - <c>reports/AccountStatement.js</c> and the three
        ///     <c>ItemServiceUrl</c>s; <c>GetUnits</c> - the three <c>UnitServiceUrl</c>s AND
        ///     <c>Modules/Purchase/wwwroot/js/Reorder.js</c>;
        ///     <c>GetItemCodeByItemId</c> - the three <c>ItemIdQuerySericeUrl</c>s;
        ///     <c>GetShippers</c> - <c>Entry/TransferDelivery</c>'s
        ///     <c>ShippingCompanyServiceUrl</c>. The remaining methods were public service
        ///     surface (<c>BackOffice/wwwroot/js/ots/OpeningInventory.ascx.js</c> and
        ///     <c>Purchase/wwwroot/js/Reorder.js</c> reach several of them) and are all ported.
        /// </summary>
        public static IEndpointRouteBuilder MapInventoryItemDataService(this IEndpointRouteBuilder endpoints)
        {
            Map(endpoints, "countiteminstock", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return CountItemInStock(p.GetString("itemCode"), p.GetInt32("unitId"), p.GetInt32("storeId"));
            });

            Map(endpoints, "getagents", context => GetAgents());

            Map(endpoints, "getitemcodebyitemid", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetItemCodeByItemId(p.GetInt32("itemId"));
            });

            Map(endpoints, "getitems", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetItems(p.GetString("tranBook"));
            });

            Map(endpoints, "getpaymentterms", context => GetPaymentTerms());

            Map(endpoints, "getprice", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetPrice(p.GetString("tranBook"), p.GetString("itemCode"), p.GetString("partyCode"),
                    p.GetInt32("priceTypeId"), p.GetInt32("unitId"));
            });

            Map(endpoints, "getpricetypes", context => GetPriceTypes());

            Map(endpoints, "getshippers", context => GetShippers());

            Map(endpoints, "getstores", context => GetStores());

            Map(endpoints, "gettaxrate", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetTaxRate(p.GetString("itemCode"));
            });

            Map(endpoints, "getunits", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetUnits(p.GetString("itemCode"));
            });

            Map(endpoints, "isstockitem", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return IsStockItem(p.GetString("itemCode"));
            });

            Map(endpoints, "itemcodeexists", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return ItemCodeExists(p.GetString("itemCode"));
            });

            Map(endpoints, "unitnameexists", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return UnitNameExists(p.GetString("unitName"));
            });

            Map(endpoints, "getcompounditemdetails", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetCompoundItemDetails(p.GetString("compoundItemCode"), p.GetString("salesTaxCode"),
                    p.GetString("tranBook"), p.GetInt32("storeId"), p.GetString("partyCode"),
                    p.GetInt32("priceTypeId"));
            });

            return endpoints;
        }

        private static void Map(IEndpointRouteBuilder endpoints, string method,
            Func<HttpContext, System.Threading.Tasks.Task<object>> handler)
        {
            endpoints.MapMethods(RoutePrefix + method, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(await handler(context).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();
        }

        private static void Map(IEndpointRouteBuilder endpoints, string method, Func<HttpContext, object> handler)
        {
            Map(endpoints, method,
                context => System.Threading.Tasks.Task.FromResult(handler(context)));
        }

        //-------------------------------------------------------------------------------
        // The former [WebMethod] bodies, carried over verbatim.
        //-------------------------------------------------------------------------------

        private static decimal CountItemInStock(string itemCode, int unitId, int storeId)
        {
            return Items.CountItemInStock(AppUsers.GetCurrentUserDB(), itemCode, unitId, storeId);
        }

        private static Collection<ListItem> GetAgents()
        {
            Collection<ListItem> values = new Collection<ListItem>();


            foreach (Salesperson salesperson in Salespersons.GetSalespersons(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(salesperson.SalespersonName,
                    salesperson.SalespersonId.ToString(DateTimeFormatInfo.InvariantInfo)));
            }

            return values;
        }

        private static string GetItemCodeByItemId(int itemId)
        {
            return Items.GetItemCodeByItemId(AppUsers.GetCurrentUserDB(), itemId);
        }

        private static Collection<SalesItem> GetItems(string tranBook)
        {
            if (string.IsNullOrWhiteSpace(tranBook))
            {
                return new Collection<SalesItem>();
            }

            if (tranBook.ToUpperInvariant().Equals("SALES"))
            {
                return GetAllItems();
            }

            return GetStockItems();
        }

        private static Collection<ListItem> GetPaymentTerms()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (PaymentTerm paymentTerm in PaymentTerms.GetPaymentTerms(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(paymentTerm.PaymentTermName,
                    paymentTerm.PaymentTermId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static decimal GetPrice(string tranBook, string itemCode, string partyCode, int priceTypeId, int unitId)
        {
            decimal price = 0;

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return 0;
            }

            switch (tranBook)
            {
                case "Sales":
                    price = Items.GetItemSellingPrice(AppUsers.GetCurrentUserDB(), itemCode, partyCode, priceTypeId,
                        unitId);
                    break;

                case "Purchase":
                    price = Items.GetItemCostPrice(AppUsers.GetCurrentUserDB(), itemCode, partyCode, unitId);
                    break;
            }

            return price;
        }

        private static Collection<ListItem> GetPriceTypes()
        {
            Collection<ListItem> values = new Collection<ListItem>();


            foreach (PriceType priceType in PriceTypes.GetPriceTypes(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(priceType.PriceTypeName,
                    priceType.PriceTypeId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static Collection<ListItem> GetShippers()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Shipper shipper in Shippers.GetShippers(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(shipper.CompanyName, shipper.ShipperId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static Collection<ListItem> GetStores()
        {
            int userId = AppUsers.GetCurrent().View.UserId.ToInt();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            Collection<ListItem> values = new Collection<ListItem>();

            IEnumerable<Store> stores = Stores.GetStores(AppUsers.GetCurrentUserDB(), officeId, userId);

            foreach (Store store in stores)
            {
                values.Add(new ListItem(store.StoreName, store.StoreId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static decimal GetTaxRate(string itemCode)
        {
            return Items.GetTaxRate(AppUsers.GetCurrentUserDB(), itemCode);
        }

        private static Collection<ListItem> GetUnits(string itemCode)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            using (DataTable table = Units.GetUnitViewByItemCode(AppUsers.GetCurrentUserDB(), itemCode))
            {
                foreach (DataRow dr in table.Rows)
                {
                    values.Add(new ListItem(dr["unit_name"].ToString(), dr["unit_id"].ToString()));
                }

                return values;
            }
        }

        private static bool IsStockItem(string itemCode)
        {
            return Items.IsStockItem(AppUsers.GetCurrentUserDB(), itemCode);
        }

        private static bool ItemCodeExists(string itemCode)
        {
            return Items.ItemExistsByCode(AppUsers.GetCurrentUserDB(), itemCode);
        }

        private static bool UnitNameExists(string unitName)
        {
            return Units.UnitExistsByName(AppUsers.GetCurrentUserDB(), unitName);
        }

        private static IEnumerable<DbGetCompoundItemDetailsResult> GetCompoundItemDetails(string compoundItemCode,
            string salesTaxCode, string tranBook, int storeId, string partyCode, int priceTypeId)
        {
            return Items.GetCompoundItemDetails(AppUsers.GetCurrentUserDB(), compoundItemCode, salesTaxCode, tranBook,
                storeId, partyCode, priceTypeId);
        }

        /// <summary>
        ///     The ASMX's PRIVATE <c>GetItems()</c> overload. Renamed to <c>GetAllItems</c> here
        ///     only because the public <c>GetItems(string)</c> above is now a static method in the
        ///     same class and C# cannot overload on a removed instance/static distinction; the
        ///     body and the call site are unchanged.
        /// </summary>
        private static Collection<SalesItem> GetAllItems()
        {
            Collection<SalesItem> values = new Collection<SalesItem>();

            foreach (Item item in Items.GetItems(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new SalesItem
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    IsCompoundItem = false
                });
            }

            foreach (CompoundItem item in Items.GetCompoundItems(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new SalesItem
                {
                    ItemId = item.CompoundItemId,
                    ItemCode = item.CompoundItemCode,
                    ItemName = item.CompoundItemName,
                    IsCompoundItem = true
                });
            }

            return values;
        }

        private static Collection<SalesItem> GetStockItems()
        {
            Collection<SalesItem> values = new Collection<SalesItem>();

            foreach (Item item in Items.GetStockItems(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new SalesItem
                {
                    ItemId = item.ItemId,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    IsCompoundItem = false
                });
            }

            return values;
        }
    }
}
