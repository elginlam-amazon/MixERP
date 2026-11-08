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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MixERP.Net.i18n
{
    /// <summary>
    /// Startup wiring for the MixERP localization library.
    /// </summary>
    public static class I18nServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="I18nOptions" /> and supplies configuration plus the host content
        /// root to the library's static localization helpers. MUST be called from the host's
        /// <c>Program.cs</c>; without it, the database-backed resources and the
        /// <c>Resources.js</c> endpoint cannot resolve the database server config file.
        /// </summary>
        public static IServiceCollection AddMixErpI18n(this IServiceCollection services,
            IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            I18nOptions options = BuildOptions(configuration, environment);

            services.Configure<I18nOptions>(o =>
            {
                o.DbServerConfigFileLocation = options.DbServerConfigFileLocation;
                o.SupressMissingResourceException = options.SupressMissingResourceException;
                o.ContentRootPath = options.ContentRootPath;
            });

            services.AddMemoryCache();

            I18nRuntime.Configure(options);

            return services;
        }

        private static I18nOptions BuildOptions(IConfiguration configuration, IHostEnvironment environment)
        {
            I18nOptions options = new I18nOptions();

            // Bind the dedicated section first, when present.
            configuration.GetSection(I18nOptions.SectionName).Bind(options);

            // Fall back to the flat keys carried over from the legacy Web.config <appSettings>,
            // so an unmodified appsettings.json migration keeps working.
            string location = configuration["DbServerConfigFileLocation"];

            if (!string.IsNullOrWhiteSpace(location))
            {
                options.DbServerConfigFileLocation = location;
            }

            string supress = configuration["SupressMissingResourceException"];

            if (!string.IsNullOrWhiteSpace(supress))
            {
                // Preserve the legacy semantics exactly: the literal "TRUE", case-insensitive.
                options.SupressMissingResourceException = supress.Trim().ToUpperInvariant().Equals("TRUE");
            }

            options.ContentRootPath = environment.ContentRootPath;

            return options;
        }
    }
}
