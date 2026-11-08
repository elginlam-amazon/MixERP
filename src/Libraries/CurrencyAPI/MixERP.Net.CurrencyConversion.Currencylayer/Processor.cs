using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MixERP.Net.Framework.Contracts.Currency;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    internal sealed class Processor
    {
        internal Processor(HttpClient client, string apiUrl, string accessKeyName, string accessKey,
            string currenciesKey, List<string> currencies, string sourceKey, string source, string mediaType,
            bool removeSourceCurrencyFromResult, string userAgent, string resultSubKey, int decimalPlaces,
            string formatKey, string defaultFormat)
        {
            this.Client = client;
            this.ApiUrl = apiUrl;
            this.AccessKeyName = accessKeyName;
            this.AccessKey = accessKey;
            this.CurrenciesKey = currenciesKey;
            this.Currencies = currencies;
            this.SourceKey = sourceKey;
            this.Source = source;
            this.MediaType = mediaType;
            this.RemoveSourceCurrencyFromResult = removeSourceCurrencyFromResult;
            this.UserAgent = userAgent;
            this.ResultSubKey = resultSubKey;
            this.DecimalPlaces = decimalPlaces;
            this.FormatKey = formatKey;
            this.DefaultFormat = defaultFormat;
        }

        /// <summary>
        /// Supplied by the caller (<see cref="System.Net.Http.IHttpClientFactory"/> or the shared
        /// pooled client). This class neither creates nor disposes it.
        /// </summary>
        internal HttpClient Client { get; private set; }

        internal string ApiUrl { get; private set; }
        internal string AccessKeyName { get; private set; }
        internal string AccessKey { get; private set; }
        internal string CurrenciesKey { get; private set; }
        internal List<string> Currencies { get; private set; }
        internal string SourceKey { get; private set; }
        internal string Source { get; private set; }
        internal string MediaType { get; private set; }
        internal bool RemoveSourceCurrencyFromResult { get; private set; }
        internal string UserAgent { get; private set; }
        internal string ResultSubKey { get; private set; }
        internal int DecimalPlaces { get; private set; }
        internal string FormatKey { get; private set; }
        internal string DefaultFormat { get; private set; }

        internal async Task<IEnumerable<CurrencyConversionResult>> ProcessAsync(
            CancellationToken cancellationToken)
        {
            string url =
                new UrlHelper(this.ApiUrl, this.AccessKeyName, this.AccessKey, this.CurrenciesKey, this.Currencies,
                    this.SourceKey, this.Source, this.FormatKey, this.DefaultFormat).GetUrl();

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (!string.IsNullOrWhiteSpace(this.MediaType))
                {
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(this.MediaType));
                }

                if (!string.IsNullOrWhiteSpace(this.UserAgent))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", this.UserAgent);
                }

                using (HttpResponseMessage response =
                    await this.Client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        // Preserved contract: callers treat null as "no rates available".
                        return null;
                    }

                    JObject content = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

                    Parser parser = new Parser(content, this.Source, this.RemoveSourceCurrencyFromResult,
                        this.ResultSubKey, this.DecimalPlaces);

                    return parser.Parse();
                }
            }
        }

        private static async Task<JObject> ReadJsonAsync(HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            using (Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            using (StreamReader streamReader = new StreamReader(stream))
            using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
            {
                JToken token = await JToken.ReadFromAsync(jsonReader, cancellationToken).ConfigureAwait(false);
                return token as JObject;
            }
        }
    }
}
