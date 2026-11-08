using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    internal sealed class UrlHelper
    {
        internal UrlHelper(string apiUrl, string appIdKey, string appId, string currenciesKey, List<string> currencies, string baseCurrencyKey, string baseCurrency, bool specificCurrencies)
        {
            this.ApiUrl = apiUrl;
            this.AppIdKey = appIdKey;
            this.AppId = appId;
            this.CurrenciesKey = currenciesKey;
            this.Currencies = currencies;
            this.BaseCurrencyKey = baseCurrencyKey;
            this.BaseCurrency = baseCurrency;
            this.SpecificCurrencies = specificCurrencies;
        }

        internal string ApiUrl { get; private set; }
        internal string AppIdKey { get; private set; }
        internal string AppId { get; private set; }
        internal string CurrenciesKey { get; private set; }
        internal List<string> Currencies { get; private set; }
        internal string BaseCurrencyKey { get; private set; }
        internal string BaseCurrency { get; private set; }
        internal bool SpecificCurrencies { get; private set; }

        internal string GetUrl()
        {
            string baseUrl = this.ApiUrl;
            IEnumerable<QueryString> keys = this.GetKeys();

            // Values are percent-encoded: the API key and the comma-joined currency list are
            // interpolated straight into the query string, so without encoding a value
            // containing '&' or '=' would corrupt the request. Key/value layout and ordering
            // are unchanged.
            var queryString = string.Join("&",
                keys.Select(
                    k => string.Format(CultureInfo.InvariantCulture, "{0}={1}", k.Name, Encode(k.Value)))
                    .ToArray());

            return baseUrl + "?" + queryString;
        }

        private static string Encode(string value)
        {
            return value == null ? string.Empty : Uri.EscapeDataString(value);
        }

        private IEnumerable<QueryString> GetKeys()
        {
            List<QueryString> keys = new List<QueryString>();

            keys.Add(new QueryString(this.AppIdKey, this.AppId));

            if (this.SpecificCurrencies)
            {
                keys.Add(new QueryString(this.CurrenciesKey, string.Join(",", this.Currencies)));
            }
            
            keys.Add(new QueryString(this.BaseCurrencyKey, this.BaseCurrency));

            return keys;
        }
    }
}