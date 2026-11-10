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

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MixERP.Net.Framework;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Session access for library code. ASP.NET Core sessions store strings/bytes only, so
    ///     values are kept as JSON and read back with <see cref="GetSessionKey{T}" />.
    ///     Requires <c>AddSession()</c>/<c>UseSession()</c> in the host.
    /// </summary>
    public static class SessionHelper
    {
        private static ISession GetSession()
        {
            HttpContext context = WebContext.Current;

            if (context == null || context.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>() == null)
            {
                return null;
            }

            return context.Session;
        }

        public static void RemoveSessionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ISession session = GetSession();

            if (session != null && session.Keys.Contains(key))
            {
                session.Remove(key);
            }
        }

        public static void AddSessionKey(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ISession session = GetSession();

            if (session != null)
            {
                session.SetString(key, JsonSerializer.Serialize(value));
            }
        }

        public static T GetSessionKey<T>(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                ISession session = GetSession();
                string value = session?.GetString(key);

                if (!string.IsNullOrEmpty(value))
                {
                    return JsonSerializer.Deserialize<T>(value);
                }
            }

            return default(T);
        }
    }
}
