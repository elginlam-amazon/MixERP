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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Framework;

namespace MixERP.Net.ApplicationState
{
    /// <summary>
    ///     Registers this library's dependencies with the host's DI container and bridges
    ///     the static facades the library exposes to its callers.
    /// </summary>
    public static class MixERPApplicationStateServiceCollectionExtensions
    {
        /// <summary>
        ///     Registers <see cref="IHttpContextAccessor" /> (which replaces the .NET
        ///     Framework ambient HTTP context the login helpers used to read) together with
        ///     the MixERP framework cache services the application-state cache relies on.
        ///     Hosts call this from <c>Program.cs</c>:
        ///     <code>
        ///     builder.Services.AddMixERPApplicationState();
        ///     // after building the app:
        ///     app.Services.UseMixERPApplicationState();
        ///     </code>
        /// </summary>
        public static IServiceCollection AddMixERPApplicationState(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpContextAccessor();

            // Idempotent: AddMemoryCache/AddSingleton<ICacheFactory> are safe to call even
            // when the host has already registered the framework services itself.
            services.AddMixERPFramework();

            return services;
        }

        /// <summary>
        ///     Points the library's static facades at the built container. MUST be called
        ///     once during startup, after the application has been built — without it
        ///     <see cref="AppUsers.GetCurrent()" /> cannot see the signed-in user.
        /// </summary>
        public static IServiceProvider UseMixERPApplicationState(this IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            AppUserHttpContext.Configure(services.GetRequiredService<IHttpContextAccessor>());
            CacheFactory.Configure(services.GetRequiredService<ICacheFactory>());

            return services;
        }
    }
}
