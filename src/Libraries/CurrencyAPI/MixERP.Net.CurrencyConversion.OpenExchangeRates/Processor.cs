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

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MixERP.Net.Framework.Contracts.Currency;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    internal sealed class Processor
    {
        private readonly HttpClient client;

        internal Processor(HttpClient client, string apiUrl, string appIdKey, string appId, string currenciesKey,
            List<string> currencies, string baseCurrencyKey, string baseCurrency, bool specificCurrencies,
            string mediaType, string userAgent, string resultSubKey, int decimalPlaces)
        {
            this.client = client;
            this.ApiUrl = apiUrl;
            this.AppIdKey = appIdKey;
            this.AppId = appId;
            this.CurrenciesKey = currenciesKey;
            this.Currencies = currencies;
            this.BaseCurrencyKey = baseCurrencyKey;
            this.BaseCurrency = baseCurrency;
            this.SpecificCurrencies = specificCurrencies;
            this.MediaType = mediaType;
            this.UserAgent = userAgent;
            this.ResultSubKey = resultSubKey;
            this.DecimalPlaces = decimalPlaces;
        }

        internal string ApiUrl { get; private set; }
        internal string AppIdKey { get; private set; }
        internal string AppId { get; private set; }
        internal string CurrenciesKey { get; private set; }
        internal List<string> Currencies { get; private set; }
        internal string BaseCurrencyKey { get; private set; }
        internal string BaseCurrency { get; private set; }
        internal bool SpecificCurrencies { get; private set; }
        internal string MediaType { get; private set; }
        internal string UserAgent { get; private set; }
        internal string ResultSubKey { get; private set; }
        internal int DecimalPlaces { get; private set; }

        internal async Task<IEnumerable<CurrencyConversionResult>> ProcessAsync(
            CancellationToken cancellationToken)
        {
            string url =
                new UrlHelper(this.ApiUrl, this.AppIdKey, this.AppId, this.CurrenciesKey, this.Currencies,
                    this.BaseCurrencyKey, this.BaseCurrency, this.SpecificCurrencies).GetUrl();

            // The Accept and User-Agent headers are shaped per request rather than on the
            // client: the client is pooled and shared by IHttpClientFactory, so mutating its
            // DefaultRequestHeaders (as the pre-migration per-call client did) would leak
            // this tenant's settings onto every other caller of the same named client.
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.Clear();

                if (!string.IsNullOrWhiteSpace(this.MediaType))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(this.MediaType));
                }

                if (!string.IsNullOrWhiteSpace(this.UserAgent))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", this.UserAgent);
                }

                using (HttpResponseMessage response =
                    await this.client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);

                    // Deserialized with Newtonsoft so that `content[key]` indexing and the
                    // JObject dynamic member semantics Parser relies on (item.Name/item.Value)
                    // behave exactly as they did under WebApi.Client's ReadAsAsync<dynamic>().
                    //
                    // A failure response does not necessarily carry the API's documented JSON
                    // error body — a proxy or gateway can return an HTML page — so a parse
                    // failure must not escape as a JsonReaderException and hide the real HTTP
                    // status from callers that catch OpenExchangeRatesException.
                    object content;
                    JsonException parseFailure;

                    if (!TryDeserialize(json, out content, out parseFailure))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            throw new OpenExchangeRatesException(
                                BuildErrorMessage(null, response), parseFailure);
                        }

                        throw new OpenExchangeRatesException(BuildErrorMessage(null, response));
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        Parser parser = new Parser(content, this.ResultSubKey, this.DecimalPlaces);
                        return parser.Parse();
                    }

                    throw new OpenExchangeRatesException(BuildErrorMessage(content, response));
                }
            }
        }

        private static bool TryDeserialize(string json, out object content,
            out JsonException parseFailure)
        {
            try
            {
                content = JsonConvert.DeserializeObject<dynamic>(json);
                parseFailure = null;
                return true;
            }
            catch (JsonException exception)
            {
                content = null;
                parseFailure = exception;
                return false;
            }
        }

        /// <summary>
        ///     Reproduces the original failure message — "status (message) - description" read
        ///     from the API's error body. Falls back to the transport status when the body is
        ///     absent or is not the documented error shape, so a gateway error page surfaces as
        ///     a usable message instead of a NullReferenceException.
        /// </summary>
        private static string BuildErrorMessage(object content, HttpResponseMessage response)
        {
            JObject error = content as JObject;

            if (error != null)
            {
                string statusCode = ReadMember(error, "status");
                string reasonPhrase = ReadMember(error, "message");
                string description = ReadMember(error, "description");

                if (statusCode != null || reasonPhrase != null || description != null)
                {
                    return statusCode + " (" + reasonPhrase + ") - " + description;
                }
            }

            return ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)
                   + " (" + response.ReasonPhrase + ")";
        }

        private static string ReadMember(JObject content, string key)
        {
            JToken value;

            if (!content.TryGetValue(key, out value) || value == null
                || value.Type == JTokenType.Null)
            {
                return null;
            }

            return value.ToString();
        }
    }
}
