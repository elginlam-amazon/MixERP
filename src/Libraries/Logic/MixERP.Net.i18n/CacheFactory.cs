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

namespace MixERP.Net.i18n
{
    /// <summary>
    ///     Thin delegation to the application-wide
    ///     <see cref="MixERP.Net.Framework.ICacheFactory" />, which is
    ///     <c>IMemoryCache</c>-backed. Replaces the former
    ///     <c>System.Runtime.Caching.MemoryCache.Default</c> usage, so the localization
    ///     cache and the rest of MixERP now share one cache instance instead of two
    ///     parallel ones.
    /// </summary>
    internal static class CacheFactory
    {
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

            MixERP.Net.Framework.CacheFactory.AddToDefaultCache(key, value);
        }

        internal static object GetFromDefaultCacheByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return MixERP.Net.Framework.CacheFactory.GetFromDefaultCacheByKey(key);
        }
    }
}
