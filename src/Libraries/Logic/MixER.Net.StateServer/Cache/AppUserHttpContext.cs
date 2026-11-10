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

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.ApplicationState.Cache
{
    /// <summary>
    ///     Static facade giving the static <see cref="AppUsers" /> helpers access to the
    ///     signed-in user. Replaces the legacy static HTTP context user principal, which does
    ///     not exist in ASP.NET Core, with <see cref="IHttpContextAccessor" />.
    ///     Hosts must call <see cref="Configure" /> during startup, exactly as they do for
    ///     <c>MixERP.Net.Framework.CacheFactory.Configure</c>:
    ///     <code>
    ///     builder.Services.AddHttpContextAccessor();
    ///     // after building the app:
    ///     AppUserHttpContext.Configure(app.Services.GetRequiredService&lt;IHttpContextAccessor&gt;());
    ///     </code>
    /// </summary>
    public static class AppUserHttpContext
    {
        private static IHttpContextAccessor accessor;

        /// <summary>
        ///     Points the facade at the host's own <see cref="IHttpContextAccessor" />.
        /// </summary>
        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            accessor = httpContextAccessor;
        }

        /// <summary>
        ///     Whether <see cref="Configure" /> has been called. Hosts can assert on this
        ///     during startup diagnostics.
        /// </summary>
        public static bool IsConfigured
        {
            get { return accessor != null; }
        }

        /// <summary>
        ///     The current principal, or null when there is no ambient HTTP context. The
        ///     context is legitimately absent outside a request — notably during Blazor
        ///     Server (SignalR) interactive rendering — so callers must null-check rather
        ///     than dereference.
        ///     A facade that was never configured is a DIFFERENT condition: it is a host
        ///     wiring bug that would otherwise make every signed-in user look anonymous,
        ///     so it fails loudly instead of silently returning null.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Configure" /> was not called during startup.
        /// </exception>
        public static ClaimsPrincipal User
        {
            get
            {
                IHttpContextAccessor current = accessor;

                if (current == null)
                {
                    throw new InvalidOperationException(
                        "MixERP.Net.ApplicationState.Cache.AppUserHttpContext.Configure was not called during startup. " +
                        "Call services.AddMixERPApplicationState() and, after the application is built, " +
                        "app.Services.UseMixERPApplicationState().");
                }

                return current.HttpContext?.User;
            }
        }

        /// <summary>
        ///     The authenticated user's identity name (the global login id in MixERP), or
        ///     null when nobody is signed in or no HTTP context is available.
        /// </summary>
        public static string UserName
        {
            get { return User?.Identity?.Name; }
        }
    }
}
