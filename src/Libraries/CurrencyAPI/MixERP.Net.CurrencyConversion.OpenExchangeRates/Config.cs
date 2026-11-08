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

using Microsoft.Extensions.Configuration;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    /// <summary>
    ///     The OpenExchangeRates provider settings.
    ///     <para>
    ///         Before migration these were <c>static readonly</c> fields, so every value was read
    ///         from the database once, at type load, against whichever tenant catalog happened to
    ///         be current for the very first caller — and then frozen for the lifetime of the
    ///         process. Reading them per instance instead keeps the original source and semantics
    ///         while making the provider correct for this multi-tenant application: each call
    ///         resolves against the current user's catalog.
    ///     </para>
    ///     <para>
    ///         Every one of the original settings is preserved, read with the same key and the same
    ///         conversion. The one addition is <see cref="AppId" />, which now prefers
    ///         configuration over the database so the API key can be supplied as an environment
    ///         variable or user secret.
    ///     </para>
    /// </summary>
    internal sealed class Config
    {
        /// <summary>
        ///     The configuration key holding the API key, e.g. the environment variable
        ///     <c>OpenExchangeRates__AppId</c>.
        /// </summary>
        internal const string AppIdConfigurationKey = "OpenExchangeRates:AppId";

        internal const string ConfigFileName = "OpenExchangeRatesConfigFile";

        internal Config(IConfiguration configuration)
        {
            string catalog = AppUsers.GetCurrentUserDB();

            this.UserAgent = GetParameter(catalog, "UserAgent");
            this.MediaType = GetParameter(catalog, "MediaType");
            this.AppId = GetAppId(catalog, configuration);
            this.ApiUrl = GetParameter(catalog, "APIUrl");
            this.AppIdKey = GetParameter(catalog, "AppIdKey");
            this.CurrenciesKey = GetParameter(catalog, "CurrenciesKey");
            this.SpecificCurrencies = IsTrue(GetParameter(catalog, "SpecificCurrencies"));
            this.BaseCurrencyKey = GetParameter(catalog, "BaseCurrencyKey");
            this.DecimalPlaces = Conversion.TryCastInteger(GetParameter(catalog, "DecimalPlaces"));
            this.ResultSubKey = GetParameter(catalog, "ResultSubKey");
        }

        internal string UserAgent { get; private set; }
        internal string MediaType { get; private set; }
        internal string AppId { get; private set; }
        internal string ApiUrl { get; private set; }
        internal string AppIdKey { get; private set; }
        internal string CurrenciesKey { get; private set; }
        internal bool SpecificCurrencies { get; private set; }
        internal string BaseCurrencyKey { get; private set; }
        internal int DecimalPlaces { get; private set; }
        internal string ResultSubKey { get; private set; }

        /// <summary>
        ///     Whether this provider is switched on for the current tenant.
        /// </summary>
        internal static bool IsEnabled()
        {
            return IsTrue(GetParameter(AppUsers.GetCurrentUserDB(), "Enabled"));
        }

        /// <summary>
        ///     Resolves the API key from configuration first, falling back to the tenant's
        ///     database row when the configuration key is absent. The key is never written to
        ///     source or to appsettings.json — supply it through the environment variable
        ///     <c>OpenExchangeRates__AppId</c> or <c>dotnet user-secrets</c>.
        /// </summary>
        private static string GetAppId(string catalog, IConfiguration configuration)
        {
            IConfiguration source = configuration ?? OpenExchangeRatesRuntime.Configuration;

            if (source != null)
            {
                string configured = source[AppIdConfigurationKey];

                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return configured;
                }
            }

            return GetParameter(catalog, "AppId");
        }

        private static string GetParameter(string catalog, string key)
        {
            return DbConfig.GetOpenExchangeRatesParameter(catalog, key);
        }

        /// <summary>
        ///     The original comparison: <c>value.ToUpperInvariant().Equals("TRUE")</c>, but
        ///     null-safe. A missing row previously threw a NullReferenceException.
        /// </summary>
        private static bool IsTrue(string value)
        {
            return value != null && value.ToUpperInvariant().Equals("TRUE");
        }
    }
}
