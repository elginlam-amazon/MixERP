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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MixERP.Net.Framework.Contracts.Currency;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    public sealed class Converter : ICurrencyConverter
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;

        /// <summary>
        ///     Required by <c>CurrencyConverter.GetAll()</c> and by
        ///     <c>CurrencyData.GetExchangeRates</c>, both of which discover this provider by
        ///     reflection and create it with <c>Activator.CreateInstance</c>. Dependencies are
        ///     taken from <see cref="OpenExchangeRatesRuntime" />, which the host wires at startup.
        /// </summary>
        public Converter()
        {
        }

        /// <summary>
        ///     Preferred constructor for callers that resolve this provider from the DI container.
        /// </summary>
        public Converter(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        public string ConverterName
        {
            get { return "Open Exchange Rates"; }
        }

        public string BaseCurrency { get; set; }
        public List<string> CurrencyCodes { get; set; }

        public bool Enabled
        {
            get { return Config.IsEnabled(); }
        }

        public string ConfigFileName
        {
            get { return Config.ConfigFileName; }
        }

        public async Task<IEnumerable<CurrencyConversionResult>> GetResultAsync(
            CancellationToken cancellationToken)
        {
            Config config = new Config(this.configuration);

            Processor processor = new Processor(this.GetClient(), config.ApiUrl, config.AppIdKey,
                config.AppId, config.CurrenciesKey, this.CurrencyCodes, config.BaseCurrencyKey,
                this.BaseCurrency, config.SpecificCurrencies, config.MediaType, config.UserAgent,
                config.ResultSubKey, config.DecimalPlaces);

            return await processor.ProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        private HttpClient GetClient()
        {
            if (this.httpClientFactory != null)
            {
                return this.httpClientFactory.CreateClient(OpenExchangeRatesRuntime.HttpClientName);
            }

            return OpenExchangeRatesRuntime.CreateClient();
        }
    }
}
