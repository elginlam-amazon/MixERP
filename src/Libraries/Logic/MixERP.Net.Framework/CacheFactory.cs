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

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Application-wide cache abstraction. Replaces the .NET Framework
    ///     <c>System.Runtime.Caching.MemoryCache.Default</c> singleton with
    ///     <see cref="IMemoryCache" />.
    /// </summary>
    public interface ICacheFactory
    {
        /// <summary>
        ///     Adds the value against the supplied key, overwriting any value already cached
        ///     under that key. Blank keys and null values are ignored.
        /// </summary>
        void AddToDefaultCache(string key, object value);

        /// <summary>
        ///     Returns the cached value for the supplied key, or null when the key is blank
        ///     or nothing is cached against it.
        /// </summary>
        object GetFromDefaultCacheByKey(string key);
    }

    /// <summary>
    ///     <see cref="IMemoryCache" />-backed implementation of <see cref="ICacheFactory" />.
    /// </summary>
    public sealed class MemoryCacheFactory : ICacheFactory
    {
        private readonly IMemoryCache cache;

        public MemoryCacheFactory(IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(cache);
            this.cache = cache;
        }

        public void AddToDefaultCache(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            // The original implementation used a default CacheItemPolicy, which carries no
            // expiration at all. MemoryCacheEntryOptions without any expiration set is the
            // exact equivalent, so cached entries live for the lifetime of the process (or
            // until they are explicitly overwritten) just as they did before.
            this.cache.Set(key, value, new MemoryCacheEntryOptions());
        }

        public object GetFromDefaultCacheByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return this.cache.Get(key);
        }
    }

    /// <summary>
    ///     Static facade preserving the original <c>CacheFactory</c> API surface used
    ///     throughout MixERP. It delegates to a process-wide <see cref="ICacheFactory" />.
    ///     Hosts that compose a DI container should call <see cref="Configure" /> during
    ///     startup so that the cache registered in the container is the one being used.
    /// </summary>
    public static class CacheFactory
    {
        private static ICacheFactory instance =
            new MemoryCacheFactory(new MemoryCache(new MemoryCacheOptions()));

        /// <summary>
        ///     Points the static facade at the application's own cache instance. Called from
        ///     the host's startup code, e.g.
        ///     <c>CacheFactory.Configure(app.Services.GetRequiredService&lt;ICacheFactory&gt;())</c>.
        ///     Until it is called, a self-contained in-memory cache is used, so the facade is
        ///     never in an uninitialized (null) state.
        /// </summary>
        public static void Configure(ICacheFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            instance = factory;
        }

        /// <summary>
        ///     The cache currently backing the static facade.
        /// </summary>
        public static ICacheFactory Current
        {
            get { return instance; }
        }

        public static void AddToDefaultCache(string key, object value)
        {
            instance.AddToDefaultCache(key, value);
        }

        public static object GetFromDefaultCacheByKey(string key)
        {
            return instance.GetFromDefaultCacheByKey(key);
        }
    }
}
