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
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PetaPoco
{
    /// <summary>
    /// Startup wiring for the MixERP PetaPoco data-access library.
    /// </summary>
    public static class PetaPocoServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="PetaPocoOptions" /> and supplies configuration plus the host
        /// content root to this library's static data-access helpers. MUST be called from the
        /// host's <c>Program.cs</c>; without it, <see cref="Factory" /> cannot resolve the
        /// database server config file and <see cref="Database" /> cannot resolve named
        /// connection strings.
        /// </summary>
        public static IServiceCollection AddPetaPoco(this IServiceCollection services,
            IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

            PetaPocoOptions options = BuildOptions(configuration, environment);

            services.Configure<PetaPocoOptions>(o =>
            {
                o.DbServerConfigFileLocation = options.DbServerConfigFileLocation;
                o.ContentRootPath = options.ContentRootPath;
                o.ConfigFileLocations = options.ConfigFileLocations;
                o.ConnectionStrings = options.ConnectionStrings;
            });

            PetaPocoRuntime.Configure(options);

            return services;
        }

        private static PetaPocoOptions BuildOptions(IConfiguration configuration,
            IHostEnvironment environment)
        {
            PetaPocoOptions options = new PetaPocoOptions();

            // Bind the dedicated section first, when present.
            configuration.GetSection(PetaPocoOptions.SectionName).Bind(options);

            // Fall back to the flat keys carried over from the legacy Web.config <appSettings>,
            // so an unmodified appsettings.json migration keeps working.
            string location = configuration["DbServerConfigFileLocation"];

            if (!string.IsNullOrWhiteSpace(location))
            {
                options.DbServerConfigFileLocation = location;
            }

            if (options.ConfigFileLocations == null)
            {
                options.ConfigFileLocations = new Dictionary<string, string>();
            }

            // Every legacy app setting whose key ends in "ConfigFileLocation" named an external
            // config file. Carry them all across so GetConfigurationValue keeps resolving any
            // key the legacy app-settings reader could resolve.
            foreach (IConfigurationSection child in configuration.GetChildren())
            {
                if (child.Key != null &&
                    child.Key.EndsWith("ConfigFileLocation", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(child.Value))
                {
                    options.ConfigFileLocations[child.Key] = child.Value;
                }
            }

            options.ConfigFileLocations["DbServerConfigFileLocation"] =
                options.DbServerConfigFileLocation;

            if (options.ConnectionStrings == null)
            {
                options.ConnectionStrings = new List<ConnectionStringEntry>();
            }

            // Also accept the standard "ConnectionStrings" section, which is where the legacy
            // <connectionStrings> element is migrated to. Entries already supplied through the
            // dedicated PetaPoco section win, matching bind-first precedence.
            foreach (IConfigurationSection child in
                configuration.GetSection("ConnectionStrings").GetChildren())
            {
                if (string.IsNullOrWhiteSpace(child.Key) || string.IsNullOrWhiteSpace(child.Value))
                {
                    continue;
                }

                if (options.ConnectionStrings.Exists(e =>
                        string.Equals(e.Name, child.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                options.ConnectionStrings.Add(new ConnectionStringEntry
                {
                    Name = child.Key,
                    ConnectionString = child.Value,
                    ProviderName = configuration["ConnectionStringProviders:" + child.Key] ?? string.Empty
                });
            }

            options.ContentRootPath = environment.ContentRootPath;

            return options;
        }
    }
}
