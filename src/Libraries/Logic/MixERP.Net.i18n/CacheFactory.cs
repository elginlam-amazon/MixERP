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

using Microsoft.Extensions.Caching.Memory;

namespace MixERP.Net.i18n
{
    /// <summary>
    /// Process-wide resource cache. Replaces the legacy framework default object cache
    /// with a library-owned <see cref="IMemoryCache" /> instance, keeping the same internal API
    /// used by <see cref="ResourceManager" />.
    /// </summary>
    internal static class CacheFactory
    {
        private static readonly IMemoryCache Cache =
            new MemoryCache(new MemoryCacheOptions());

        internal static void AddToDefaultCache(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            // Add if absent, otherwise overwrite. No expiration policy, matching the
            // former CacheItemPolicy() default.
            Cache.Set(key, value, new MemoryCacheEntryOptions());
        }

        internal static object GetFromDefaultCacheByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return Cache.TryGetValue(key, out object value) ? value : null;
        }
    }
}
