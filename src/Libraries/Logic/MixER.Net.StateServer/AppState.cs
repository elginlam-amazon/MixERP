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
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace MixERP.Net.ApplicationState
{
    /// <summary>
    ///     Supplies the ambient authenticated user to this library's static entry points.
    ///     Replaces the .NET Framework ambient current-user static from the legacy web stack.
    ///     The host application must call <see cref="UseHttpContextAccessor" /> once during
    ///     startup, after <c>builder.Services.AddHttpContextAccessor()</c>:
    ///     <code>
    ///     AppState.UseHttpContextAccessor(app.Services.GetRequiredService&lt;IHttpContextAccessor&gt;());
    ///     </code>
    ///     When the host has not wired it, <see cref="CurrentUser" /> returns <c>null</c> rather
    ///     than throwing, so callers degrade to "no signed-in user" exactly as the original code
    ///     did when there was no ambient request.
    /// </summary>
    public static class AppState
    {
        private static IHttpContextAccessor accessor;

        /// <summary>
        ///     Supplies the DI-managed <see cref="IHttpContextAccessor" /> to this library.
        /// </summary>
        public static void UseHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            accessor = httpContextAccessor;
        }

        /// <summary>
        ///     The ambient request's user, or <c>null</c> when there is no current request
        ///     (background work, startup, or an unwired host).
        /// </summary>
        public static IPrincipal CurrentUser
        {
            get
            {
                IHttpContextAccessor current = accessor;

                if (current == null)
                {
                    return null;
                }

                HttpContext context = current.HttpContext;
                return context?.User;
            }
        }

        /// <summary>
        ///     The ambient request user's name, or an empty string when unavailable.
        /// </summary>
        public static string CurrentUserName
        {
            get
            {
                IPrincipal user = CurrentUser;
                return user?.Identity?.Name ?? string.Empty;
            }
        }
    }
}
