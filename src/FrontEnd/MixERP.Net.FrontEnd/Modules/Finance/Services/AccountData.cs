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

using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Entities.Office;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using MixERP.Net.Common.Models;

using Microsoft.AspNetCore.Mvc;
using MixERP.Net.Framework.Web;
namespace MixERP.Net.Core.Modules.Finance.Services
{
    /// <summary>
    ///     Replaces the /Modules/Finance/Services/AccountData.asmx ASP.NET AJAX script service (same url and JSON contract).
    /// </summary>
    [Route("Modules/Finance/Services/AccountData.asmx")]
    public class AccountData : ScriptServiceController
    {
        [HttpPost("AccountNumberExists")]
        public bool AccountNumberExists(string accountNumber)
        {
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return AccountHelper.AccountNumberExists(AppUsers.GetCurrentUserDB(), accountNumber);
            }

            return false;
        }

        [HttpPost("CashRepositoryCodeExists")]
        public bool CashRepositoryCodeExists(string cashRepositoryCode)
        {
            return CashRepositories.CashRepositoryCodeExists(AppUsers.GetCurrentUserDB(), cashRepositoryCode);
        }

        [HttpPost("GetAccounts")]
        public Collection<ListItem> GetAccounts()
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

        [HttpPost("GetCashRepositories")]
        public Collection<ListItem> GetCashRepositories()
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

        [HttpPost("GetCashRepositoriesByAccountNumber")]
        public Collection<ListItem> GetCashRepositoriesByAccountNumber(string accountNumber)
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

        [HttpPost("GetCashRepositoryBalance")]
        public decimal GetCashRepositoryBalance(int cashRepositoryId, string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), cashRepositoryId);
            }

            return CashRepositories.GetBalance(AppUsers.GetCurrentUserDB(), cashRepositoryId, currencyCode);
        }

        [HttpPost("GetCostCenters")]
        public Collection<ListItem> GetCostCenters()
        {
            Collection<ListItem> values = new Collection<ListItem>();


            foreach (CostCenter costCenter in CostCenters.GetCostCenters(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(costCenter.CostCenterName,
                    costCenter.CostCenterId.ToString(CultureInfo.InvariantCulture)));
            }

            return values;
        }

        [HttpPost("GetCurrencies")]
        public Collection<ListItem> GetCurrencies()
        {
            Collection<ListItem> values = new Collection<ListItem>();

            foreach (Currency currency in Currencies.GetCurrencies(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new ListItem(currency.CurrencyCode, currency.CurrencyCode));
            }

            return values;
        }

        [HttpPost("GetCurrenciesByAccountNumber")]
        public Collection<ListItem> GetCurrenciesByAccountNumber(string accountNumber)
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

        [HttpPost("HasBalance")]
        public bool HasBalance(string cashRepositoryCode, string currencyCode, decimal credit)
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

        [HttpPost("IsCashAccount")]
        public bool IsCashAccount(string accountNumber)
        {
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                return AccountHelper.IsCashAccount(AppUsers.GetCurrentUserDB(), accountNumber);
            }

            return false;
        }

        [HttpPost("ListAccounts")]
        public Collection<ListItem> ListAccounts()
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