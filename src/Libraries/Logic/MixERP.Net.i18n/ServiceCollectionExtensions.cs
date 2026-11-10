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
using MixERP.Net.Framework;

namespace MixERP.Net.i18n
{
    /// <summary>
    ///     Registers this library's services with the host's DI container.
    /// </summary>
    public static class MixERPI18nServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the localization settings provider and the Resources.js script
        ///     builder, and ensures the shared cache (<see cref="ICacheFactory" />) is
        ///     available. Hosts should call this from <c>Program.cs</c>:
        ///     <code>
        ///     builder.Services.AddMixERPLocalization(builder.Configuration);
        ///
        ///     var app = builder.Build();
        ///
        ///     // Bridge the static facades the generated Resources.* classes use:
        ///     app.Services.UseMixERPLocalization();
        ///
        ///     app.MapResourcesEndpoint();   // GET /Resources.js
        ///     </code>
        ///     Expected configuration keys (previously Web.config &lt;appSettings&gt;):
        ///     <c>DbServerConfigFileLocation</c> and
        ///     <c>SupressMissingResourceException</c>, either at the configuration root or
        ///     under the <c>MixERP</c> section.
        /// </summary>
        public static IServiceCollection AddMixERPLocalization(this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            // Root-level keys first (so the original Web.config key names bind verbatim),
            // then the optional MixERP section which, when present, wins.
            services.Configure<I18nOptions>(configuration);
            services.Configure<I18nOptions>(configuration.GetSection(I18nOptions.SectionName));

            services.AddSingleton<II18nSettingsProvider, I18nSettingsProvider>();
            services.AddSingleton<ResourcesScriptBuilder>();

            // ICacheFactory backs the "Resources" localization cache. Registering the
            // framework services is idempotent, so calling AddMixERPFramework() in the
            // host as well is harmless.
            services.AddMixERPFramework();

            return services;
        }

        /// <summary>
        ///     Points this library's static facades at the host's configured services.
        ///     Required because the 14,896-line generated <c>Resources.*</c> classes and
        ///     the 31 consuming projects call <see cref="ResourceManager" /> and
        ///     <see cref="CurrentCulture" /> statically.
        /// </summary>
        public static IServiceProvider UseMixERPLocalization(this IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            I18nSettings.Configure(services.GetRequiredService<II18nSettingsProvider>());
            MixERP.Net.Framework.CacheFactory.Configure(services.GetRequiredService<ICacheFactory>());

            return services;
        }
    }
}
