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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Inventory.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Transactions;

namespace MixERP.Net.Core.Modules.Inventory.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/PartyData.asmx</c>
    ///     <c>[ScriptService]</c> web service - the module's party / shipping-address lookup
    ///     surface.
    ///     <para>
    ///         Each former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below; the endpoint delegate only binds the posted arguments and wraps the
    ///         result in the preserved <c>{"d": ...}</c> / HTTP-500 envelope, so every caller's
    ///         change is URL-ONLY.
    ///     </para>
    ///     <para>
    ///         <c>GetParties</c> IS CROSS-MODULE SURFACE: the Purchase module's
    ///         <c>wwwroot/js/Reorder.js</c> calls it to fill its per-row supplier select, and the
    ///         Purchase porting pass PREDICTED this route as
    ///         <c>/services/inventory/partydata/getparties</c>. That prediction is CORRECT and is
    ///         what is mapped here, so no cross-module javascript had to be touched.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are KEPT here ON PURPOSE -
    ///         a minimal-API endpoint has a live <c>HttpContext</c>, which is the one place they
    ///         resolve correctly, exactly as inside the ASMX. The ban applies to component code.
    ///     </para>
    ///     <para>
    ///         Every endpoint is <c>.RequireAuthorization()</c> (the deleted
    ///         <c>Modules/web.config</c>'s <c>&lt;deny users="?" /&gt;</c>; these methods read the
    ///         signed-in user's catalog and office, and party due amounts are customer financial
    ///         data) plus <c>.DisableAntiforgery()</c>, matching the ASMX.
    ///     </para>
    /// </summary>
    public static class InventoryPartyDataEndpoints
    {
        private const string RoutePrefix = "/services/inventory/partydata/";

        public static IEndpointRouteBuilder MapInventoryPartyDataService(this IEndpointRouteBuilder endpoints)
        {
            Map(endpoints, "getaddressbypartycode", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetAddressByPartyCode(p.GetString("partyCode"));
            });

            Map(endpoints, "getparties", context => Task.FromResult<object>(GetParties()));

            Map(endpoints, "getpartycodebypartyid", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetPartyCodeByPartyId(p.GetInt32("partyId"));
            });

            Map(endpoints, "getpartydue", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetPartyDue(p.GetString("partyCode"));
            });

            Map(endpoints, "getpartyview", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetPartyView(p.GetString("partyCode"));
            });

            Map(endpoints, "getshippingaddresses", async context =>
            {
                AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                return GetShippingAddresses(p.GetString("partyCode"));
            });

            return endpoints;
        }

        private static void Map(IEndpointRouteBuilder endpoints, string method,
            Func<HttpContext, Task<object>> handler)
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

        //-------------------------------------------------------------------------------
        // The former [WebMethod] bodies, carried over verbatim - including the composite
        // address format string, whose InvariantCulture and its {1} = Environment.NewLine
        // placements are exactly what the legacy select rendered.
        //-------------------------------------------------------------------------------

        private static Collection<ListItem> GetAddressByPartyCode(string partyCode)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (ShippingAddressView view in ShippingAddresses.GetShippingAddressView(AppUsers.GetCurrentUserDB(), partyCode))
            {
                string address = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{1}{3}{4}{5}{1}{6}{4}{7}", view.AddressLine1, Environment.NewLine, view.AddressLine2, view.Street, ", ", view.City, view.State, view.Country);

                values.Add(new ListItem(view.ShippingAddressCode, address));
            }

            return values;
        }

        private static Collection<ListItem> GetParties()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Party party in Parties.GetParties(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(party.PartyName, party.PartyCode));
            }

            return values;
        }

        private static string GetPartyCodeByPartyId(int partyId)
        {
            return Parties.GetPartyCodeByPartyId(AppUsers.GetCurrentUserDB(), partyId);
        }

        private static DbGetPartyTransactionSummaryResult GetPartyDue(string partyCode)
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            return Parties.GetPartyDue(AppUsers.GetCurrentUserDB(), officeId, partyCode);
        }

        private static PartyView GetPartyView(string partyCode)
        {
            return Parties.GetPartyView(AppUsers.GetCurrentUserDB(), partyCode);
        }

        private static IEnumerable<ShippingAddress> GetShippingAddresses(string partyCode)
        {
            return Parties.GetShippingAddresses(AppUsers.GetCurrentUserDB(), partyCode);
        }
    }
}
