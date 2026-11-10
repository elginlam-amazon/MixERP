using System.Globalization;
using System.Resources;

namespace MixERP.Net.i18n
{
    public class ResourceManager
    {
        private static bool SupressException
        {
            get { return I18nSettings.Current.SupressMissingResourceException; }
        }

        public static string GetString(string resourceClass, string resourceKey, string cultureCode = null)
        {
            string result = TryGetResourceFromCache(resourceClass, resourceKey, cultureCode);

            if (result == null)
            {
                if (SupressException)
                {
                    return resourceKey;
                }

                throw new MissingManifestResourceException("Resource " + resourceClass + "." + resourceKey + " was not found.");
            }

            return result;
        }

        public static string TryGetResourceFromCache(string resourceClass, string resourceKey, string cultureCode = null)
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

            // IMemoryCache stores the value itself (there is no CacheItem wrapper type as
            // there was in System.Runtime.Caching), so the cached object is the dictionary.
            IDictionary<string, string> cache =
                CacheFactory.GetFromDefaultCacheByKey("Resources") as IDictionary<string, string>;

            if (cache == null || cache.Count.Equals(0))
            {
                if (initialized)
                {
                    // Already tried to populate the cache once during this call; the
                    // resources are genuinely unavailable. Returning null lets
                    // GetString apply the SupressMissingResourceException policy rather
                    // than recursing forever.
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
            CacheFactory.AddToDefaultCache("Resources", resources);
        }
    }
}
