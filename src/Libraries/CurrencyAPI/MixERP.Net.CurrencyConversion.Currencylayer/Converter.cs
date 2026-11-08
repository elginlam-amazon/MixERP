using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MixERP.Net.Framework.Contracts.Currency;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    public sealed class Converter : ICurrencyConverter
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOptionsSnapshot<CurrencylayerOptions> options;

        /// <summary>
        ///     Required by <c>CurrencyConverter.GetAll()</c> and by
        ///     <c>CurrencyData.GetExchangeRates</c>, both of which discover this provider by
        ///     reflection and create it with <c>Activator.CreateInstance</c>. Settings are then
        ///     read per call from the current user's database (with an environment-variable
        ///     override for the API access key) and the shared pooled HTTP client is used.
        /// </summary>
        public Converter()
        {
        }

        /// <summary>
        ///     Preferred constructor for callers that resolve this provider from the DI container
        ///     (registered by <c>services.AddCurrencylayer()</c>).
        /// </summary>
        public Converter(IHttpClientFactory httpClientFactory, IOptionsSnapshot<CurrencylayerOptions> options)
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options;
        }

        public string ConverterName
        {
            get { return "Currencylayer"; }
        }

        public string BaseCurrency { get; set; }
        public List<string> CurrencyCodes { get; set; }

        /// <summary>
        ///     Evaluated per access (never cached at type load), preserving the original
        ///     case-insensitive "TRUE" semantics of the stored <c>Enabled</c> parameter.
        /// </summary>
        public bool Enabled
        {
            get { return this.GetOptions().Enabled; }
        }

        public string ConfigFileName
        {
            get { return Config.ConfigFileName; }
        }

        public async Task<IEnumerable<CurrencyConversionResult>> GetResultAsync(
            CancellationToken cancellationToken)
        {
            CurrencylayerOptions current = this.GetOptions();

            Processor processor = new Processor(this.GetClient(), current.ApiUrl, current.AccessKeyName,
                current.AccessKey, current.CurrenciesKey, this.CurrencyCodes, current.SourceKey, this.BaseCurrency,
                current.MediaType, current.RemoveSourceCurrencyFromResult, current.UserAgent, current.ResultSubKey,
                current.DecimalPlaces, current.FormatKey, current.DefaultFormat);

            return await processor.ProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        private CurrencylayerOptions GetOptions()
        {
            if (this.options != null)
            {
                return this.options.Value;
            }

            return Config.ReadWithEnvironmentOverrides();
        }

        private HttpClient GetClient()
        {
            if (this.httpClientFactory != null)
            {
                return this.httpClientFactory.CreateClient(CurrencylayerHttpClient.ClientName);
            }

            return CurrencylayerHttpClient.Shared;
        }
    }
}
