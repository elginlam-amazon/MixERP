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
using MixERP.Net.Entities.Core;


using Microsoft.AspNetCore.Mvc;
using MixERP.Net.Framework.Web;
namespace MixERP.Net.Core.Modules.Sales.Services.Receipt
{
    /// <summary>
    ///     Replaces the /Modules/Sales/Services/Receipt/Currencies.asmx ASP.NET AJAX script service (same url and JSON contract).
    /// </summary>
    [Route("Modules/Sales/Services/Receipt/Currencies.asmx")]
    public class Currencies : ScriptServiceController
    {
        [HttpPost("GetCurrencies")]
        public System.Collections.ObjectModel.Collection<MixERP.Net.Common.Models.ListItem> GetCurrencies()
        {
            System.Collections.ObjectModel.Collection<MixERP.Net.Common.Models.ListItem> values = new System.Collections.ObjectModel.Collection<MixERP.Net.Common.Models.ListItem>();

            foreach (Currency currency in Data.Helpers.Currencies.GetCurrencies(AppUsers.GetCurrentUserDB()))
            {
                values.Add(new MixERP.Net.Common.Models.ListItem(currency.CurrencyCode, currency.CurrencyCode));
            }

            return values;
        }

        [HttpPost("GetExchangeRate")]
        public decimal GetExchangeRate(string sourceCurrencyCode, string destinationCurrencyCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCurrencyCode))
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(destinationCurrencyCode))
            {
                return 0;
            }

            if (sourceCurrencyCode.Equals(destinationCurrencyCode))
            {
                return 1;
            }

            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();

            decimal exchangeRate = Data.Helpers.Transaction.GetExchangeRate(AppUsers.GetCurrentUserDB(), officeId, sourceCurrencyCode, destinationCurrencyCode);

            return exchangeRate;
        }

        [HttpPost("GetHomeCurrency")]
        public string GetHomeCurrency()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            return Data.Helpers.Currencies.GetHomeCurrency(AppUsers.GetCurrentUserDB(), officeId);
        }
    }
}