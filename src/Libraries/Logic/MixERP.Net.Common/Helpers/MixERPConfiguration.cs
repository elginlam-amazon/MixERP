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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Static composition seam replacing <c>System.Configuration.ConfigurationManager</c>,
    ///     <c>System.Web.Hosting.HostingEnvironment</c> and <c>System.Web.HttpContext.Current</c>
    ///     for this library. Every dependent host application must call one of the
    ///     <see cref="Initialize(IConfiguration, string)" /> overloads once, during startup
    ///     composition, before any MixERP.Net.Common helper is used.
    ///     The library degrades safely (content root falls back to
    ///     <see cref="AppContext.BaseDirectory" />, settings resolve to empty) when not initialized.
    /// </summary>
    public static class MixERPConfiguration
    {
        private static IConfiguration configuration;
        private static IHttpContextAccessor httpContextAccessor;
        private static string contentRoot;

        /// <summary>
        ///     Initializes the configuration seam.
        /// </summary>
        /// <param name="configuration">The host application configuration (appsettings.json / legacy web.config appSettings).</param>
        /// <param name="contentRootPath">The physical content root path of the host application (IWebHostEnvironment.ContentRootPath).</param>
        public static void Initialize(IConfiguration configuration, string contentRootPath)
        {
            Initialize(configuration, contentRootPath, null);
        }

        /// <summary>
        ///     Initializes the configuration seam along with the HTTP context accessor used by
        ///     session/request helpers.
        /// </summary>
        [System.CLSCompliant(false)]
        public static void Initialize(IConfiguration configuration, string contentRootPath, IHttpContextAccessor httpContextAccessor)
        {
            MixERPConfiguration.configuration = configuration;
            MixERPConfiguration.httpContextAccessor = httpContextAccessor;

            if (!string.IsNullOrWhiteSpace(contentRootPath))
            {
                MixERPConfiguration.contentRoot = contentRootPath;
            }
        }

        /// <summary>
        ///     The configured content root, or the application base directory when the seam was not initialized.
        /// </summary>
        public static string ContentRootPath
        {
            get { return string.IsNullOrWhiteSpace(contentRoot) ? AppContext.BaseDirectory : contentRoot; }
        }

        internal static IConfiguration Configuration
        {
            get { return configuration; }
        }

        /// <summary>
        ///     The ambient <see cref="HttpContext" />, or null when there is none
        ///     (background work, or a Blazor Server circuit outside the initial request).
        /// </summary>
        internal static HttpContext HttpContext
        {
            get
            {
                IHttpContextAccessor accessor = httpContextAccessor;
                return accessor == null ? null : accessor.HttpContext;
            }
        }

        /// <summary>
        ///     Equivalent of <c>ConfigurationManager.AppSettings[key]</c>. Looks the key up both under
        ///     an "appSettings" section (migrated web.config shape) and at the configuration root.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || configuration == null)
            {
                return null;
            }

            string value = configuration["appSettings:" + key];

            if (string.IsNullOrEmpty(value))
            {
                value = configuration[key];
            }

            return value;
        }

        /// <summary>
        ///     Equivalent of <c>HostingEnvironment.MapPath(virtualPath)</c>: resolves an application
        ///     relative ("~/x/y" or "x/y") path against the content root. Rooted paths are returned
        ///     normalized.
        /// </summary>
        public static string MapPath(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                return null;
            }

            string path = virtualPath.Trim();

            //A leading "/" or "\" means "application root relative" on Windows, NOT rooted —
            //hence the exclusion. On Linux every absolute path starts with "/", so the guard
            //must be scoped to Windows or "/var/mixerp/mixerp.config" would be silently
            //rewritten to "<ContentRoot>/var/mixerp/mixerp.config".
            bool appRelative = path.StartsWith("~", StringComparison.Ordinal);
            bool windowsAppRoot = Path.DirectorySeparatorChar == '\\'
                                  && (path.StartsWith("/", StringComparison.Ordinal)
                                      || path.StartsWith("\\", StringComparison.Ordinal));

            if (!appRelative && !windowsAppRoot && Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            if (path.StartsWith("~", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            path = path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            return Path.GetFullPath(Path.Combine(ContentRootPath, path));
        }

        /// <summary>
        ///     Reads a single value out of the <c>appSettings</c> section of an external legacy
        ///     <c>*.config</c> file (the files MixERP keeps under its Resource directory).
        /// </summary>
        /// <returns>The value, or an empty string when the file, section or key is absent.</returns>
        public static string GetExternalConfigValue(string physicalConfigFilePath, string key)
        {
            if (string.IsNullOrWhiteSpace(physicalConfigFilePath) || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (!File.Exists(physicalConfigFilePath))
            {
                return string.Empty;
            }

            XDocument document = XDocument.Load(physicalConfigFilePath);
            XElement appSettings = GetAppSettingsElement(document, false);

            if (appSettings == null)
            {
                return string.Empty;
            }

            XElement add = FindAdd(appSettings, key);

            if (add == null)
            {
                return string.Empty;
            }

            XAttribute value = add.Attribute("value");
            return value == null ? string.Empty : value.Value;
        }

        /// <summary>
        ///     Writes a single value into the <c>appSettings</c> section of an external legacy
        ///     <c>*.config</c> file and saves the file back to the same path. This preserves the
        ///     read-write behaviour of the original <c>Configuration.Save</c> code path.
        /// </summary>
        /// <returns>True when the file was saved.</returns>
        public static bool SetExternalConfigValue(string physicalConfigFilePath, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return SetExternalConfigValues(physicalConfigFilePath, new[] { new System.Collections.Generic.KeyValuePair<string, string>(key, value) });
        }

        /// <summary>
        ///     Writes several values into the <c>appSettings</c> section of an external legacy
        ///     <c>*.config</c> file and saves the file back to the same path.
        /// </summary>
        public static bool SetExternalConfigValues(string physicalConfigFilePath, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> values)
        {
            if (string.IsNullOrWhiteSpace(physicalConfigFilePath) || values == null)
            {
                return false;
            }

            if (!File.Exists(physicalConfigFilePath))
            {
                return false;
            }

            XDocument document = XDocument.Load(physicalConfigFilePath, LoadOptions.PreserveWhitespace);
            XElement appSettings = GetAppSettingsElement(document, true);

            if (appSettings == null)
            {
                return false;
            }

            bool changed = false;

            foreach (System.Collections.Generic.KeyValuePair<string, string> item in values)
            {
                if (item.Key == null)
                {
                    continue;
                }

                XElement add = FindAdd(appSettings, item.Key);

                if (add == null)
                {
                    //The original System.Configuration code only updated existing keys.
                    continue;
                }

                add.SetAttributeValue("value", item.Value);
                changed = true;
            }

            if (changed)
            {
                document.Save(physicalConfigFilePath);
            }

            return changed;
        }

        private static XElement GetAppSettingsElement(XDocument document, bool create)
        {
            if (document == null || document.Root == null)
            {
                return null;
            }

            XElement appSettings = document.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "appSettings");

            if (appSettings == null && create)
            {
                appSettings = new XElement("appSettings");
                document.Root.Add(appSettings);
            }

            return appSettings;
        }

        private static XElement FindAdd(XElement appSettings, string key)
        {
            return appSettings.Elements().FirstOrDefault(e =>
            {
                if (e.Name.LocalName != "add")
                {
                    return false;
                }

                XAttribute attribute = e.Attribute("key");
                return attribute != null && string.Equals(attribute.Value, key, StringComparison.Ordinal);
            });
        }
    }
}
