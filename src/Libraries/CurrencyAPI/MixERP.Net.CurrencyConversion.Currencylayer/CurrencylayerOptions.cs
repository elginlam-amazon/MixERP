namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    /// <summary>
    /// Strongly-typed replacement for the former static <c>Config</c> fields. Bound from
    /// configuration section <c>Currencylayer</c> (appsettings.json / user-secrets /
    /// environment variables) layered on top of the per-tenant values stored in
    /// <c>config.currency_layer</c>.
    /// </summary>
    public sealed class CurrencylayerOptions
    {
        public const string SectionName = "Currencylayer";

        public string UserAgent { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;

        /// <summary>
        /// Currencylayer API access key. Never stored in a *.config file — supply it through
        /// configuration (<c>Currencylayer:AccessKey</c>), user-secrets or the
        /// <c>Currencylayer__AccessKey</c> environment variable.
        /// </summary>
        public string AccessKey { get; set; } = string.Empty;

        public string ApiUrl { get; set; } = string.Empty;
        public string AccessKeyName { get; set; } = string.Empty;
        public string CurrenciesKey { get; set; } = string.Empty;
        public string SourceKey { get; set; } = string.Empty;
        public int DecimalPlaces { get; set; }
        public string FormatKey { get; set; } = string.Empty;
        public string DefaultFormat { get; set; } = string.Empty;
        public string ResultSubKey { get; set; } = string.Empty;
        public bool RemoveSourceCurrencyFromResult { get; set; }
        public bool Enabled { get; set; }

        internal CurrencylayerOptions Clone()
        {
            return new CurrencylayerOptions
            {
                UserAgent = this.UserAgent,
                MediaType = this.MediaType,
                AccessKey = this.AccessKey,
                ApiUrl = this.ApiUrl,
                AccessKeyName = this.AccessKeyName,
                CurrenciesKey = this.CurrenciesKey,
                SourceKey = this.SourceKey,
                DecimalPlaces = this.DecimalPlaces,
                FormatKey = this.FormatKey,
                DefaultFormat = this.DefaultFormat,
                ResultSubKey = this.ResultSubKey,
                RemoveSourceCurrencyFromResult = this.RemoveSourceCurrencyFromResult,
                Enabled = this.Enabled
            };
        }
    }
}
