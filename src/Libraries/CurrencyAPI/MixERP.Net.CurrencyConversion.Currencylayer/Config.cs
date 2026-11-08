using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    /// <summary>
    /// Reads the Currencylayer parameters from the per-tenant <c>config.currency_layer</c> table.
    /// The values used to be cached in <c>static readonly</c> fields initialized at type load
    /// (which bound the whole library to the first request's catalog); they are now read on
    /// demand so each request resolves against its own user database.
    /// </summary>
    internal static class Config
    {
        internal const string ConfigFileName = "CurrencylayerConfigFile";

        /// <summary>
        /// Loads every Currencylayer parameter for the catalog of the current user.
        /// </summary>
        internal static CurrencylayerOptions Read()
        {
            return Read(AppUsers.GetCurrentUserDB());
        }

        /// <summary>
        /// Loads every Currencylayer parameter for the supplied catalog.
        /// </summary>
        internal static CurrencylayerOptions Read(string catalog)
        {
            return new CurrencylayerOptions
            {
                UserAgent = GetParameter(catalog, "UserAgent"),
                MediaType = GetParameter(catalog, "MediaType"),
                AccessKey = GetParameter(catalog, "APIAccessKey"),
                ApiUrl = GetParameter(catalog, "APIUrl"),
                AccessKeyName = GetParameter(catalog, "AccessKeyName"),
                CurrenciesKey = GetParameter(catalog, "CurrenciesKey"),
                SourceKey = GetParameter(catalog, "SourceKey"),
                DecimalPlaces = Conversion.TryCastInteger(GetParameter(catalog, "DecimalPlaces")),
                FormatKey = GetParameter(catalog, "FormatKey"),
                DefaultFormat = GetParameter(catalog, "DefaultFormat"),
                ResultSubKey = GetParameter(catalog, "ResultSubKey"),
                RemoveSourceCurrencyFromResult = IsTrue(GetParameter(catalog, "RemoveSourceCurrencyFromResult")),
                Enabled = IsTrue(GetParameter(catalog, "Enabled"))
            };
        }

        /// <summary>
        /// Used by the plugin-discovery path (<c>Activator.CreateInstance</c>), where no
        /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> is available: the
        /// per-tenant values come from the database and the API access key may still be
        /// overridden out-of-band by the <c>Currencylayer__AccessKey</c> environment variable,
        /// so the secret never has to be stored in a *.config file.
        /// </summary>
        internal static CurrencylayerOptions ReadWithEnvironmentOverrides()
        {
            CurrencylayerOptions options = Read();

            string accessKey = System.Environment.GetEnvironmentVariable("Currencylayer__AccessKey");

            if (!string.IsNullOrWhiteSpace(accessKey))
            {
                options.AccessKey = accessKey;
            }

            return options;
        }

        private static string GetParameter(string catalog, string key)
        {
            return DbConfig.GetCurrencylayerParameter(catalog, key) ?? string.Empty;
        }

        /// <summary>
        /// Preserves the original case-insensitive "TRUE" semantics of the stored flags.
        /// </summary>
        internal static bool IsTrue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.ToUpperInvariant().Equals("TRUE");
        }
    }
}
