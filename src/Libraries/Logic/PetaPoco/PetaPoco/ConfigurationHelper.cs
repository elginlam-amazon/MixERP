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
using System.Xml.Linq;

namespace PetaPoco
{
    internal static class ConfigurationHelper
    {
        /// <summary>
        /// Reads a key from an external config file whose content-root relative location is
        /// itself named by a legacy app-setting key. Replaces the .NET Framework combination of
        /// the hosting-environment path mapper, the exe-configuration file map and the
        /// mapped-exe-configuration opener. Returns <see cref="string.Empty" /> when the file or
        /// the key is absent, exactly as the legacy reader did.
        /// </summary>
        internal static string GetConfigurationValue(string configFileName, string sectionName)
        {
            if (configFileName == null)
            {
                return string.Empty;
            }

            string relativePath;

            if (!PetaPocoRuntime.Options.ConfigFileLocations.TryGetValue(configFileName, out relativePath))
            {
                // "DbServerConfigFileLocation" has a dedicated option so that the most common
                // lookup keeps working even when the host supplied only that one value.
                if (string.Equals(configFileName, "DbServerConfigFileLocation", StringComparison.Ordinal))
                {
                    relativePath = PetaPocoRuntime.Options.DbServerConfigFileLocation;
                }
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            string path = ResolvePath(relativePath);

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            XDocument document = XDocument.Load(path);
            XElement appSettings = document.Root?.Element("appSettings");

            if (appSettings == null)
            {
                return string.Empty;
            }

            foreach (XElement add in appSettings.Elements("add"))
            {
                if (string.Equals((string)add.Attribute("key"), sectionName, StringComparison.Ordinal))
                {
                    return (string)add.Attribute("value") ?? string.Empty;
                }
            }

            return string.Empty;
        }

        internal static string GetDbServerParameter(string key)
        {
            return GetConfigurationValue("DbServerConfigFileLocation", key);
        }

        /// <summary>
        /// Absolute path the external database server config file resolves to, for diagnostics.
        /// </summary>
        internal static string GetDbServerConfigFilePath()
        {
            string relativePath = PetaPocoRuntime.Options.DbServerConfigFileLocation;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return "(DbServerConfigFileLocation is not configured)";
            }

            return ResolvePath(relativePath);
        }

        /// <summary>
        /// Resolves a content-root relative path, normalising the leading separator and the
        /// Windows-style separators found in the legacy configuration so the path also works
        /// on Linux.
        /// </summary>
        private static string ResolvePath(string relativePath)
        {
            string normalized = relativePath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            string contentRoot = PetaPocoRuntime.Options.ContentRootPath;

            if (string.IsNullOrWhiteSpace(contentRoot))
            {
                contentRoot = AppContext.BaseDirectory;
            }

            return Path.Combine(contentRoot, normalized);
        }
    }
}
