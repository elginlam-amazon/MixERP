using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    /// <summary>
    /// Populates <see cref="CurrencylayerOptions"/> for the CURRENT request: the per-tenant
    /// values still come from <c>config.currency_layer</c> (they are editable from the admin UI),
    /// and anything present in the <c>Currencylayer</c> configuration section — most importantly
    /// the API access key, which must live in user-secrets / environment variables rather than in
    /// a *.config file — overrides them.
    /// </summary>
    internal sealed class CurrencylayerOptionsSetup : IConfigureOptions<CurrencylayerOptions>
    {
        private readonly IConfiguration configuration;

        public CurrencylayerOptionsSetup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Configure(CurrencylayerOptions options)
        {
            CurrencylayerOptions fromDatabase = Config.Read();

            options.UserAgent = fromDatabase.UserAgent;
            options.MediaType = fromDatabase.MediaType;
            options.AccessKey = fromDatabase.AccessKey;
            options.ApiUrl = fromDatabase.ApiUrl;
            options.AccessKeyName = fromDatabase.AccessKeyName;
            options.CurrenciesKey = fromDatabase.CurrenciesKey;
            options.SourceKey = fromDatabase.SourceKey;
            options.DecimalPlaces = fromDatabase.DecimalPlaces;
            options.FormatKey = fromDatabase.FormatKey;
            options.DefaultFormat = fromDatabase.DefaultFormat;
            options.ResultSubKey = fromDatabase.ResultSubKey;
            options.RemoveSourceCurrencyFromResult = fromDatabase.RemoveSourceCurrencyFromResult;
            options.Enabled = fromDatabase.Enabled;

            if (this.configuration == null)
            {
                return;
            }

            IConfigurationSection section = this.configuration.GetSection(CurrencylayerOptions.SectionName);

            if (section.Exists())
            {
                // Only the keys actually present in configuration win; the rest keep the DB value.
                section.Bind(options);
            }
        }
    }
}
