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

using Microsoft.Extensions.DependencyInjection;

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Registers this library's services with the host's DI container.
    /// </summary>
    public static class MixERPFrameworkServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers the cache factory, the per-circuit error state and the exception
        ///     handler. Hosts should call this from <c>Program.cs</c>:
        ///     <code>
        ///     builder.Services.AddMixERPFramework();
        ///     // after building the app, bridge the static facades:
        ///     CacheFactory.Configure(app.Services.GetRequiredService&lt;ICacheFactory&gt;());
        ///     </code>
        /// </summary>
        public static IServiceCollection AddMixERPFramework(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddMemoryCache();
            services.AddSingleton<ICacheFactory, MemoryCacheFactory>();

            // Scoped == one instance per Blazor circuit (or per request outside Blazor),
            // which is what Session["ex"] used to provide.
            services.AddScoped<MixERPErrorState>();
            services.AddScoped<IMixERPExceptionHandler, MixERPExceptionHandler>();

            return services;
        }
    }
}
