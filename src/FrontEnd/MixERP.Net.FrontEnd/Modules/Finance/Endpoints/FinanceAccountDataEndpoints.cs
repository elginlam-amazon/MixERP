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
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Office;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;

namespace MixERP.Net.Core.Modules.Finance.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted <c>Services/AccountData.asmx</c>
    ///     <c>[ScriptService]</c> web service.
    ///     <para>
    ///         Every former <c>[WebMethod]</c> body is carried over VERBATIM into a private static
    ///         method below, including the <c>IsAdmin</c> branches of <c>GetAccounts</c> /
    ///         <c>ListAccounts</c> - which are CONFIDENTIALITY FILTERS, not cosmetics: a
    ///         non-admin must keep getting the non-confidential account list only. The endpoint
    ///         level <c>.RequireAuthorization()</c> matches the deleted
    ///         <c>Modules/web.config</c>'s <c>&lt;deny users="?" /&gt;</c>; it only requires A
    ///         user and therefore does not replace those branches.
    ///     </para>
    ///     <para>
    ///         <c>System.Web.UI.WebControls.ListItem</c> became <see cref="ListItem" /> - the
    ///         <c>{Text, Value}</c> shape <c>mixerp-core.js</c>'s <c>ajaxDataBind</c> /
    ///         <c>bindAjaxData</c> read off every element of <c>msg.d</c>, which is why
    ///         <c>js/entry/JournalVoucher.js</c> and <c>js/reports/AccountStatement.js</c> change
    ///         by URL only.
    ///     </para>
    ///     <para>
    ///         <c>AppUsers.GetCurrent()</c> / <c>GetCurrentUserDB()</c> are KEPT here on purpose -
    ///         a minimal-API endpoint has a live <c>HttpContext</c>, which is the one place they
    ///         resolve correctly (over a Blazor circuit they return an EMPTY login).
    ///     </para>
    /// </summary>
    public static class FinanceAccountDataEndpoints
    {
        private const string Prefix = "/services/finance/accountdata/";

        public static IEndpointRouteBuilder MapFinanceAccountDataService(this IEndpointRouteBuilder endpoints)
        {
            Map(endpoints, "accountnumberexists",
                p => AccountNumberExists(p.GetString("accountNumber")));

            Map(endpoints, "cashrepositorycodeexists",
                p => CashRepositoryCodeExists(p.GetString("cashRepositoryCode")));

            Map(endpoints, "getaccounts", p => GetAccounts());

            Map(endpoints, "getcashrepositories", p => GetCashRepositories());

            Map(endpoints, "getcashrepositoriesbyaccountnumber",
                p => GetCashRepositoriesByAccountNumber(p.GetString("accountNumber")));

            Map(endpoints, "getcashrepositorybalance",
                p => GetCashRepositoryBalance(p.GetInt32("cashRepositoryId"), p.GetString("currencyCode")));

            Map(endpoints, "getcostcenters", p => GetCostCenters());

            Map(endpoints, "getcurrencies", p => GetCurrencies());

            Map(endpoints, "getcurrenciesbyaccountnumber",
                p => GetCurrenciesByAccountNumber(p.GetString("accountNumber")));

            Map(endpoints, "hasbalance",
                p => HasBalance(p.GetString("cashRepositoryCode"), p.GetString("currencyCode"),
                    p.GetDecimal("credit")));

            Map(endpoints, "iscashaccount", p => IsCashAccount(p.GetString("accountNumber")));

            Map(endpoints, "listaccounts", p => ListAccounts());

            return endpoints;
        }

        private static void Map(IEndpointRouteBuilder endpoints, string method,
            Func<AsmxParameters, object> body)
        {
            endpoints.MapMethods(Prefix + method, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters p = await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(body(p));
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
        // The former [WebMethod] bodies, carried over verbatim.
        //-------------------------------------------------------------------------------

        private static bool AccountNumberExists(string accountNumber)
        {
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return AccountHelper.AccountNumberExists(AppUsers.GetCurrentUserDB(), accountNumber);
            }

            return false;
        }

        private static bool CashRepositoryCodeExists(string cashRepositoryCode)
        {
            return CashRepositories.CashRepositoryCodeExists(AppUsers.GetCurrentUserDB(), cashRepositoryCode);
        }

        private static Collection<ListItem> GetAccounts()
        {
            bool allowParentAccountInGlTransaction = DbConfig.GetSwitch(AppUsers.GetCurrentUserDB(),
                "AllowParentAccountInGLTransaction");

            if (allowParentAccountInGlTransaction)
            {
                if (AppUsers.GetCurrent().View.IsAdmin.ToBool())
                {
                    return GetValues(AccountHelper.GetAccounts(AppUsers.GetCurrentUserDB()));
                }

                return GetValues(AccountHelper.GetNonConfidentialAccounts(AppUsers.GetCurrentUserDB()));
            }

            if (AppUsers.GetCurrent().View.IsAdmin.ToBool())
            {
                return GetValues(AccountHelper.GetChildAccounts(AppUsers.GetCurrentUserDB()));
            }

            return GetValues(AccountHelper.GetNonConfidentialChildAccounts(AppUsers.GetCurrentUserDB()));
        }

        private static Collection<ListItem> GetCashRepositories()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            foreach (
                CashRepository cashRepository in
                    CashRepositories.GetCashRepositories(AppUsers.GetCurrentUserDB(), officeId))
            {
                values.Add(new ListItem(cashRepository.CashRepositoryName,
                    cashRepository.CashRepositoryId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static Collection<ListItem> GetCashRepositoriesByAccountNumber(string accountNumber)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            if (AccountHelper.IsCashAccount(AppUsers.GetCurrentUserDB(), accountNumber))
            {
                int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

                foreach (
                    CashRepository cashRepository in
                        CashRepositories.GetCashRepositories(AppUsers.GetCurrentUserDB(), officeId))
                {
                    values.Add(new ListItem(cashRepository.CashRepositoryName, cashRepository.CashRepositoryCode));
                }
            }

            return values;
        }

        private static decimal GetCashRepositoryBalance(int cashRepositoryId, string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), cashRepositoryId);
            }

            return CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), cashRepositoryId, currencyCode);
        }

        private static Collection<ListItem> GetCostCenters()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (CostCenter costCenter in CostCenters.GetCostCenters(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(costCenter.CostCenterName,
                    costCenter.CostCenterId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        private static Collection<ListItem> GetCurrencies()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Currency currency in Currencies.GetCurrencies(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(currency.CurrencyCode, currency.CurrencyCode));
            }

            return values;
        }

        private static Collection<ListItem> GetCurrenciesByAccountNumber(string accountNumber)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return values;
            }

            string currencyCode = Currencies.GetCurrencyCode(AppUsers.GetCurrentUserDB(), accountNumber);

            values.Add(new ListItem(currencyCode, currencyCode));

            return values;
        }

        private static bool HasBalance(string cashRepositoryCode, string currencyCode, decimal credit)
        {
            if (credit.Equals(0))
            {
                return true;
            }

            if (credit < 0)
            {
                throw new MixERPException(Warnings.NegativeValueSupplied);
            }

            decimal balance = CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), cashRepositoryCode, currencyCode);

            if (balance > credit)
            {
                return true;
            }

            return false;
        }

        private static bool IsCashAccount(string accountNumber)
        {
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return AccountHelper.IsCashAccount(AppUsers.GetCurrentUserDB(), accountNumber);
            }

            return false;
        }

        private static Collection<ListItem> ListAccounts()
        {
            if (AppUsers.GetCurrent().View.IsAdmin.ToBool())
            {
                return GetValues(AccountHelper.GetAccounts(AppUsers.GetCurrentUserDB()));
            }

            return GetValues(AccountHelper.GetNonConfidentialAccounts(AppUsers.GetCurrentUserDB()));
        }

        private static Collection<ListItem> GetValues(IEnumerable<Account> accounts)
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Account account in accounts)
            {
                values.Add(new ListItem(account.AccountName, account.AccountNumber));
            }

            return values;
        }
    }
}
