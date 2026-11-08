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

using System.Globalization;
using System.Resources;

namespace MixERP.Net.i18n
{
    public class ResourceManager
    {
        private const string ResourcesCacheKey = "Resources";

        /// <summary>
        /// When true, a missing resource returns its key instead of throwing. Supplied by the
        /// host through <see cref="I18nServiceCollectionExtensions.AddMixErpI18n" />; defaults
        /// to false (non-suppressing), matching the legacy Web.config value.
        /// </summary>
        private static bool SupressException => I18nRuntime.Options.SupressMissingResourceException;

        public static string GetString(string resourceClass, string resourceKey, string cultureCode = null)
        {
            string result = TryGetResourceFromCache(resourceClass, resourceKey, cultureCode);

            if (result == null)
            {
                if (SupressException)
                {
                    return resourceKey;
                }

                throw new MissingManifestResourceException("Resource " + resourceClass + "." + resourceKey +
                                                           " was not found.");
            }

            return result;
        }

        public static string TryGetResourceFromCache(string resourceClass, string resourceKey,
            string cultureCode = null)
        {
            return TryGetResourceFromCache(resourceClass, resourceKey, cultureCode, false);
        }

        private static string TryGetResourceFromCache(string resourceClass, string resourceKey, string cultureCode,
            bool initialized)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            if (!string.IsNullOrWhiteSpace(cultureCode))
            {
                culture = new CultureInfo(cultureCode);
            }

            IDictionary<string, string> cache =
                CacheFactory.GetFromDefaultCacheByKey(ResourcesCacheKey) as IDictionary<string, string>;

            if (cache == null || cache.Count.Equals(0))
            {
                if (initialized)
                {
                    // The database returned no rows — return a miss instead of recursing forever.
                    return null;
                }

                InitializeResources();
                return TryGetResourceFromCache(resourceClass, resourceKey, cultureCode, true);
            }

            string cacheKey = resourceClass + "." + culture.Name + "." + resourceKey;
            string result;
            cache.TryGetValue(cacheKey, out result);

            if (result != null)
            {
                return result;
            }

            //Fall back to parent culture
            while (true)
            {
                if (!string.IsNullOrWhiteSpace(culture.Parent.Name))
                {
                    cacheKey = resourceClass + "." + culture.Parent.Name + "." + resourceKey;
                    cache.TryGetValue(cacheKey, out result);

                    if (result != null)
                    {
                        return result;
                    }

                    culture = culture.Parent;
                    continue;
                }
                break;
            }

            //Fall back to invariant culture
            cacheKey = resourceClass + "." + resourceKey;
            cache.TryGetValue(cacheKey, out result);

            return result;
        }

        private static void InitializeResources()
        {
            IDictionary<string, string> resources = DbResources.GetLocalizedResources();
            CacheFactory.AddToDefaultCache(ResourcesCacheKey, resources);
        }
    }
}
