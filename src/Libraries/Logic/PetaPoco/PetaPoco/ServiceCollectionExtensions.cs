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

namespace PetaPoco
{
    /// <summary>
    ///     Registers this library's services with the host's DI container.
    /// </summary>
    public static class PetaPocoServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the <see cref="IConfiguration" />-backed connection-string source
        ///     that replaces <c>ConfigurationManager.ConnectionStrings</c>. Hosts should
        ///     call this from <c>Program.cs</c>:
        ///     <code>
        ///     builder.Services.AddPetaPoco(builder.Configuration);
        ///
        ///     var app = builder.Build();
        ///
        ///     // Bridge the static facade used by the Database(string name) constructor:
        ///     app.Services.UsePetaPoco();
        ///     </code>
        ///     The database server parameters used by <see cref="Factory" /> come from the
        ///     external installation config file instead, and are supplied by
        ///     <c>MixERP.Net.i18n</c>'s settings provider — so the host must also call
        ///     <c>AddMixERPLocalization(builder.Configuration)</c> and
        ///     <c>app.Services.UseMixERPLocalization()</c>, and keep the
        ///     <c>DbServerConfigFileLocation</c> key configured.
        /// </summary>
        public static IServiceCollection AddPetaPoco(this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddSingleton<IConnectionStringSource>(
                new ConfigurationConnectionStringSource(configuration));

            return services;
        }

        /// <summary>
        ///     Points this library's static facade at the host's configured services.
        ///     Required because <see cref="Database(string)" /> is a public constructor
        ///     with no injection point, exactly as it was when it read the process-wide
        ///     <c>ConfigurationManager</c>.
        /// </summary>
        public static IServiceProvider UsePetaPoco(this IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            PetaPocoConfiguration.Configure(services.GetRequiredService<IConnectionStringSource>());

            return services;
        }
    }
}
