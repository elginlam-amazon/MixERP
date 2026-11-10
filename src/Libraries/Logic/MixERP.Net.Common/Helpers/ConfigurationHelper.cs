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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MixERP.Net.Framework;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Reads and writes the <c>appSettings</c> of MixERP's external configuration files
    ///     (DbServer.config, Parameters.config, ...). The application setting named by
    ///     <c>configFileName</c> holds the location of the file. The Web Forms version used
    ///     <c>ConfigurationManager.OpenMappedExeConfiguration</c>; the files are now read
    ///     with <see cref="XDocument" />, so the existing files keep working unchanged.
    /// </summary>
    public static class ConfigurationHelper
    {
        public static string GetConfigurationValue(string configFileName, string sectionName)
        {
            if (configFileName == null)
            {
                return string.Empty;
            }

            string location = WebContext.GetAppSetting(configFileName);

            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            string path = WebContext.MapPath(location);
            return ReadAppSetting(path, sectionName);
        }

        /// <summary>
        ///     Reads a key from the appSettings section of the given physical config file.
        ///     Returns an empty string when the file, section or key is missing.
        /// </summary>
        public static string ReadAppSetting(string path, string key)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(key) || !File.Exists(path))
            {
                return string.Empty;
            }

            XElement element = FindAppSetting(XDocument.Load(path), key);

            if (element == null)
            {
                return string.Empty;
            }

            return element.Attribute("value")?.Value ?? string.Empty;
        }

        public static string GetDbServerParameter(string key)
        {
            return GetConfigurationValue("DbServerConfigFileLocation", key);
        }

        public static string GetParameter(string key)
        {
            return GetConfigurationValue("ParameterConfigFileLocation", key);
        }

        public static string GetReportParameter(string key)
        {
            return GetConfigurationValue("ReportConfigFileLocation", key);
        }

        public static string GetResourceDirectory()
        {
            return WebContext.MapPath(WebContext.GetAppSetting("ResourceDirectory"));
        }

        public static string GetStockTransactionFactoryParameter(string key)
        {
            return GetConfigurationValue("StockTransactionFactoryConfigFileLocation", key);
        }

        public static string GetTransactionChecklistParameter(string key)
        {
            return GetConfigurationValue("TransactionChecklistConfigFileLocation", key);
        }

        public static string GetUpdaterParameter(string key)
        {
            return GetConfigurationValue("UpdaterConfigFileLocation", key);
        }

        public static string SetConfigurationValues(string configFileName, string sectionName, string value)
        {
            Collection<KeyValuePair<string, string>> sections = new Collection<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(sectionName, value)
            };

            SetConfigurationValues(configFileName, sections);

            return string.Empty;
        }

        /// <summary>
        ///     Updates existing appSettings keys in the given physical config file. Keys that are
        ///     not present are ignored, as before.
        /// </summary>
        public static void SetConfigurationValues(string configFileName,
            Collection<KeyValuePair<string, string>> sections)
        {
            if (string.IsNullOrWhiteSpace(configFileName) || !File.Exists(configFileName))
            {
                return;
            }

            XDocument document = XDocument.Load(configFileName);

            if (document.Root?.Element("appSettings") == null)
            {
                return;
            }

            if (sections != null && sections.Count > 0)
            {
                foreach (var section in sections)
                {
                    if (section.Key == null)
                    {
                        continue;
                    }

                    XElement element = FindAppSetting(document, section.Key);

                    if (element != null)
                    {
                        element.SetAttributeValue("value", section.Value);
                    }
                }
            }

            document.Save(configFileName);
        }

        private static XElement FindAppSetting(XDocument document, string key)
        {
            XElement appSettings = document.Root?.Element("appSettings");

            return appSettings?.Elements("add")
                .FirstOrDefault(x => string.Equals(x.Attribute("key")?.Value, key, StringComparison.Ordinal));
        }
    }
}
