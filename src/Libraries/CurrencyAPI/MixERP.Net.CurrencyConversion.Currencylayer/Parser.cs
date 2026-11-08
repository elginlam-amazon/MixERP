using System.Collections.Generic;
using System.Globalization;
using MixERP.Net.Common;
using MixERP.Net.Framework.Contracts.Currency;
using Newtonsoft.Json.Linq;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    internal sealed class Parser
    {
        internal Parser(JObject response, string source, bool removeSourceCurrencyFromResult, string resultSubKey,
            int decimalPlaces)
        {
            this.Response = response;
            this.Source = source;
            this.RemoveSourceCurrencyFromResult = removeSourceCurrencyFromResult;
            this.ResultSubKey = resultSubKey;
            this.DecimalPlaces = decimalPlaces;
        }

        internal JObject Response { get; private set; }
        internal string Source { get; private set; }
        internal bool RemoveSourceCurrencyFromResult { get; private set; }
        internal string ResultSubKey { get; private set; }
        internal int DecimalPlaces { get; private set; }

        internal IEnumerable<CurrencyConversionResult> Parse()
        {
            List<CurrencyConversionResult> result = new List<CurrencyConversionResult>();

            if (this.Response == null)
            {
                return result;
            }

            bool removeSource = this.RemoveSourceCurrencyFromResult;
            JToken success = this.Response["success"];

            if (!Conversion.TryCastBoolean(success == null ? null : success.ToString()))
            {
                JToken error = this.Response["error"];
                string errorCode = error == null ? string.Empty : (string)error["code"];
                string errorInfo = error == null ? string.Empty : (string)error["info"];

                throw new CurrencylayerException(errorCode + " " + errorInfo);
            }

            JObject response = this.Response[this.ResultSubKey] as JObject;

            if (response == null)
            {
                return result;
            }

            foreach (JProperty item in response.Properties())
            {
                string name = item.Name;
                decimal value = decimal.Round(
                    Conversion.TryCastDecimal(item.Value.ToString(), CultureInfo.InvariantCulture),
                    this.DecimalPlaces);

                if (removeSource)
                {
                    name = name.Replace(this.Source, string.Empty);
                }

                result.Add(new CurrencyConversionResult(name, value));
            }

            return result;
        }
    }
}
