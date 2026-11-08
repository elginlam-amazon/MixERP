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

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    /// <summary>
    ///     Supplies the DI-managed <see cref="IHttpClientFactory" /> and <see cref="IConfiguration" />
    ///     to this library's reflection-instantiated entry point.
    ///     <para>
    ///         <see cref="Converter" /> is discovered and created by
    ///         <c>MixERP.Net.Framework.Contracts.Currency.CurrencyConverter</c> through
    ///         <c>Activator.CreateInstance</c>, so it must keep a parameterless constructor and
    ///         cannot take its dependencies by constructor injection alone. This seam follows the
    ///         same pattern the already-migrated sibling libraries use
    ///         (<c>MixERP.Net.ApplicationState.AppState.UseHttpContextAccessor</c> and
    ///         <c>MixERP.Net.Framework.CacheFactory.UseCache</c>): the host wires it once at
    ///         startup, and <see cref="Converter" /> also exposes a DI constructor for callers
    ///         that resolve it from the container directly.
    ///     </para>
    ///     The host application should call both methods once during startup:
    ///     <code>
    ///     builder.Services.AddHttpClient(OpenExchangeRatesRuntime.HttpClientName);
    ///
    ///     var app = builder.Build();
    ///
    ///     OpenExchangeRatesRuntime.UseHttpClientFactory(
    ///         app.Services.GetRequiredService&lt;IHttpClientFactory&gt;());
    ///     OpenExchangeRatesRuntime.UseConfiguration(app.Configuration);
    ///     </code>
    ///     When the host has not wired the factory, this library falls back to a private
    ///     <see cref="IHttpClientFactory" /> of its own rather than throwing, so the converter
    ///     keeps working. That fallback is a real factory with proper handler pooling — this
    ///     library never constructs an <see cref="HttpClient" /> per call.
    /// </summary>
    public static class OpenExchangeRatesRuntime
    {
        /// <summary>
        ///     The name of the <see cref="IHttpClientFactory" /> client this library requests.
        /// </summary>
        public const string HttpClientName = "OpenExchangeRates";

        private static readonly object SyncRoot = new object();
        private static IHttpClientFactory httpClientFactory;
        private static IConfiguration configuration;
        private static IHttpClientFactory fallbackFactory;

        /// <summary>
        ///     Supplies the DI-managed <see cref="IHttpClientFactory" /> to this library.
        /// </summary>
        public static void UseHttpClientFactory(IHttpClientFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            lock (SyncRoot)
            {
                httpClientFactory = factory;
            }
        }

        /// <summary>
        ///     Supplies the application configuration to this library. This is where the
        ///     OpenExchangeRates API key (<c>OpenExchangeRates:AppId</c>) is read from, so that
        ///     the key can live in an environment variable or user secret instead of the
        ///     database or a committed config file.
        /// </summary>
        public static void UseConfiguration(IConfiguration appConfiguration)
        {
            if (appConfiguration == null)
            {
                throw new ArgumentNullException(nameof(appConfiguration));
            }

            lock (SyncRoot)
            {
                configuration = appConfiguration;
            }
        }

        /// <summary>
        ///     The configuration the host supplied, or <c>null</c> when it has not been wired.
        ///     A <c>null</c> value makes the settings fall back to their database values, which
        ///     is exactly what the pre-migration code did.
        /// </summary>
        internal static IConfiguration Configuration
        {
            get { return configuration; }
        }

        /// <summary>
        ///     Produces the <see cref="HttpClient" /> used to call the OpenExchangeRates API.
        ///     Never disposed by the caller — the factory owns handler lifetime.
        /// </summary>
        internal static HttpClient CreateClient()
        {
            IHttpClientFactory factory = httpClientFactory ?? GetFallbackFactory();
            return factory.CreateClient(HttpClientName);
        }

        /// <summary>
        ///     A private <see cref="IHttpClientFactory" /> used only when the host never called
        ///     <see cref="UseHttpClientFactory" />. This is a real factory — it pools and rotates
        ///     handlers exactly as the host's would — so an unwired host degrades to "works, but
        ///     with its own connection pool" rather than leaking a socket per call.
        /// </summary>
        private static IHttpClientFactory GetFallbackFactory()
        {
            if (fallbackFactory != null)
            {
                return fallbackFactory;
            }

            lock (SyncRoot)
            {
                if (fallbackFactory == null)
                {
                    fallbackFactory = new ServiceCollection()
                        .AddHttpClient()
                        .BuildServiceProvider()
                        .GetRequiredService<IHttpClientFactory>();
                }
            }

            return fallbackFactory;
        }
    }
}
