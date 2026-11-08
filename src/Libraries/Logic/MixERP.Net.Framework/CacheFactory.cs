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
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Application-wide object cache.
    ///     Replaces the .NET Framework default <c>ObjectCache</c> singleton with
    ///     <see cref="IMemoryCache" /> from <c>Microsoft.Extensions.Caching.Memory</c>.
    ///     The host application should call <see cref="UseCache" /> during startup
    ///     (<c>CacheFactory.UseCache(app.Services.GetRequiredService&lt;IMemoryCache&gt;())</c>) so that the
    ///     library shares the DI-managed cache instance. When the host does not do so, a private
    ///     process-wide <see cref="MemoryCache" /> is created lazily, preserving the original behavior
    ///     of the framework's default cache.
    /// </summary>
    public static class CacheFactory
    {
        private static readonly object SyncRoot = new object();
        private static IMemoryCache cache;
        private static bool usingFallback;
        private static bool fallbackServedEntries;

        /// <summary>
        ///     The cache instance used by this factory. Never null.
        /// </summary>
        public static IMemoryCache Cache
        {
            get
            {
                if (cache != null)
                {
                    return cache;
                }

                lock (SyncRoot)
                {
                    if (cache == null)
                    {
                        cache = new MemoryCache(new MemoryCacheOptions());
                        usingFallback = true;
                    }
                }

                return cache;
            }
        }

        /// <summary>
        ///     Supplies the DI-managed <see cref="IMemoryCache" /> to this factory. Call once at startup.
        /// </summary>
        public static void UseCache(IMemoryCache memoryCache)
        {
            if (memoryCache == null)
            {
                throw new ArgumentNullException(nameof(memoryCache));
            }

            lock (SyncRoot)
            {
                if (fallbackServedEntries && !ReferenceEquals(cache, memoryCache))
                {
                    // Swapping the cache discards whatever the fallback already holds. That must not happen
                    // silently — call UseCache before anything else touches the cache.
                    Log.Warning(
                        "CacheFactory was already in use with its private fallback cache when the application supplied the shared cache; entries cached until now have been discarded. Call CacheFactory.UseCache during startup, before any other initialization.");
                }

                cache = memoryCache;
                usingFallback = false;
                fallbackServedEntries = false;
            }
        }

        public static void AddToDefaultCache(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            IMemoryCache target = Cache;

            if (usingFallback)
            {
                fallbackServedEntries = true;
            }

            target.Set(key, value);
        }

        public static object GetFromDefaultCacheByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            object value;
            return Cache.TryGetValue(key, out value) ? value : null;
        }
    }
}
