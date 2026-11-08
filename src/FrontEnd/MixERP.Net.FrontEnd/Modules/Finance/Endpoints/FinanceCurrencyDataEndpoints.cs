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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Finance.Data.Core;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Framework.Contracts.Currency;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/CurrencyData.asmx</c>
    ///     <c>[ScriptService]</c> web service, called by
    ///     <c>wwwroot/js/UpdateExchangeRates.js</c>.
    ///     <para>
    ///         ⚠ THE APM PAIR COLLAPSES BACK INTO ONE ASYNC ENDPOINT. The legacy service exposed
    ///         <c>BeginGetExchangeRates</c> / <c>EndGetExchangeRates</c> with a
    ///         <c>TaskCompletionSource</c> bridge, because classic ASMX cannot host a
    ///         <c>Task</c>-returning <c>[WebMethod]</c>; its own file comment said this pair
    ///         "collapses back into a single async method on the same route" once the project
    ///         left WebForms. That is exactly what happens here. The ROUTE NAME IS UNCHANGED -
    ///         the javascript always called the logical <c>GetExchangeRates</c>, never the
    ///         Begin/End members - so this is a transport simplification with no caller change
    ///         beyond the url, and the <c>GetAwaiter().GetResult()</c> of <c>End*</c> is gone
    ///         rather than carried over (no sync-over-async remains).
    ///     </para>
    ///     <para>
    ///         <c>SaveExchangeRates</c> is the one former Finance web method with a
    ///         complex-object array argument, bound here through
    ///         <c>AsmxParameters.GetCollection&lt;T&gt;</c> exactly as the
    ///         <c>JavaScriptSerializer</c> bound it (case-insensitive member matching, absent
    ///         members left at their default).
    ///     </para>
    /// </summary>
    public static class FinanceCurrencyDataEndpoints
    {
        private const string GetExchangeCurrenciesRoute = "/services/finance/currencydata/getexchangecurrencies";
        private const string GetExchangeRatesRoute = "/services/finance/currencydata/getexchangerates";
        private const string GetModulesRoute = "/services/finance/currencydata/getmodules";
        private const string SaveExchangeRatesRoute = "/services/finance/currencydata/saveexchangerates";

        public static IEndpointRouteBuilder MapFinanceCurrencyDataService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetExchangeCurrenciesRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(GetExchangeCurrencies());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetExchangeRatesRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    IEnumerable<CurrencyConversionResult> result =
                        await GetExchangeRatesAsync(p.GetString("moduleName"),
                            context == null ? CancellationToken.None : context.RequestAborted)
                            .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(result);
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(GetModulesRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(GetModules());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization();

            endpoints.MapMethods(SaveExchangeRatesRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    Collection<Data.Models.ExchangeRate> posted =
                        p.GetCollection<Data.Models.ExchangeRate>("exchangeRates");

                    List<Data.Models.ExchangeRate> exchangeRates =
                        posted == null ? null : posted.ToList();

                    return AsmxCompatibility.Envelope(SaveExchangeRates(exchangeRates));
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

        private static IEnumerable<Currency> GetExchangeCurrencies()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            return Currencies.GetExchangeCurrencies(AppUsers.GetCurrentUserDB(), officeId);
        }

        private static async Task<IEnumerable<CurrencyConversionResult>> GetExchangeRatesAsync(string moduleName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return null;
            }

            Type type = Type.GetType(moduleName);

            if (type == null)
            {
                return null;
            }

            object instance = Activator.CreateInstance(type);
            ICurrencyConverter converter = instance as ICurrencyConverter;

            if (converter == null)
            {
                return null;
            }

            IEnumerable<string> currencies = GetExchangeCurrencies().Select(c => c.CurrencyCode);

            converter.BaseCurrency = AppUsers.GetCurrent().View.CurrencyCode;
            converter.CurrencyCodes = currencies.ToList();

            return await converter.GetResultAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Collection<ListItem> GetModules()
        {
            Collection<ListItem> items = new Collection<ListItem>();

            foreach (CurrencyConverter module in CurrencyConverter.GetEnabled())
            {
                items.Add(new ListItem(module.Name, module.AssemblyQualifiedName));
            }

            return items;
        }

        private static bool SaveExchangeRates(List<Data.Models.ExchangeRate> exchangeRates)
        {
            if (exchangeRates == null || exchangeRates.Count().Equals(0))
            {
                return false;
            }

            string catalog = AppUsers.GetCurrentUserDB();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            string baseCurrency = AppUsers.GetCurrent().View.CurrencyCode;

            ExchangeRates.SaveExchangeRates(catalog, officeId, baseCurrency, exchangeRates);
            return true;
        }
    }
}
