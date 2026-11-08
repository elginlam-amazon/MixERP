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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.Framework.Contracts.Currency;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    /// <summary>
    ///     Host-side wiring for the OpenExchangeRates converter, mirroring
    ///     <c>services.AddCurrencylayer()</c> on the sibling provider. One call registers the
    ///     named <c>OpenExchangeRates</c> HTTP client and the converter itself.
    ///     <para>
    ///         Because this provider is also reached through reflection
    ///         (<c>Activator.CreateInstance</c>), which cannot inject anything, the host must
    ///         additionally hand the resolved factory and configuration to
    ///         <see cref="OpenExchangeRatesRuntime" /> — that is what
    ///         <see cref="UseOpenExchangeRates" /> does. Register during service configuration
    ///         and activate after the container is built:
    ///     </para>
    ///     <code>
    ///     builder.Services.AddOpenExchangeRates();
    ///
    ///     var app = builder.Build();
    ///
    ///     app.Services.UseOpenExchangeRates(app.Configuration);
    ///     </code>
    /// </summary>
    public static class OpenExchangeRatesServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the named HTTP client and the converter.
        /// </summary>
        public static IServiceCollection AddOpenExchangeRates(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpClient(OpenExchangeRatesRuntime.HttpClientName);

            // Scoped, so that each request resolves the provider's settings against the
            // current user's catalog rather than reusing the first caller's tenant.
            services.AddScoped<Converter>();
            services.AddScoped<ICurrencyConverter>(sp => sp.GetRequiredService<Converter>());

            return services;
        }

        /// <summary>
        ///     Supplies the container's <see cref="IHttpClientFactory" /> and the application
        ///     configuration to the static seam used by the reflection-created instances.
        ///     Call once, after the container is built.
        /// </summary>
        public static IServiceProvider UseOpenExchangeRates(this IServiceProvider services,
            IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            OpenExchangeRatesRuntime.UseHttpClientFactory(
                services.GetRequiredService<System.Net.Http.IHttpClientFactory>());

            if (configuration != null)
            {
                OpenExchangeRatesRuntime.UseConfiguration(configuration);
            }

            return services;
        }
    }
}
