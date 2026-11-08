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

using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MixERP.Net.Common.Helpers
{
    public static class ConfigurationHelper
    {
        /// <summary>
        ///     One-time initialization of the configuration seam. Every dependent host application
        ///     must call this during startup composition. Delegates to
        ///     <see cref="MixERPConfiguration.Initialize(IConfiguration, string, IHttpContextAccessor)" />.
        /// </summary>
        public static void Initialize(IConfiguration configuration, string contentRootPath)
        {
            MixERPConfiguration.Initialize(configuration, contentRootPath);
        }

        /// <summary>
        ///     One-time initialization of the configuration seam, including the HTTP context accessor
        ///     used by the session and request helpers.
        /// </summary>
        public static void Initialize(IConfiguration configuration, string contentRootPath, IHttpContextAccessor httpContextAccessor)
        {
            MixERPConfiguration.Initialize(configuration, contentRootPath, httpContextAccessor);
        }

        public static string GetConfigurationValue(string configFileName, string sectionName)
        {
            if (configFileName == null)
            {
                return string.Empty;
            }

            string path = MixERPConfiguration.MapPath(MixERPConfiguration.GetAppSetting(configFileName));

            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return MixERPConfiguration.GetExternalConfigValue(path, sectionName);
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
            return MixERPConfiguration.MapPath(MixERPConfiguration.GetAppSetting("ResourceDirectory"));
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
            //configFileName is the path of the target *.config file, as in the original implementation.
            string path = ResolveConfigFilePath(configFileName);

            if (!MixERPConfiguration.SetExternalConfigValue(path, sectionName, value))
            {
                //The original System.Configuration implementation surfaced an IO failure from
                //config.Save(...). Swallowing the result would turn a missing file, an absent
                //key or a mis-resolved path into a silent no-op the caller believes succeeded.
                Log.Warning("Could not persist configuration value {Key} to {Path}.", sectionName, path);
            }

            return string.Empty;
        }

        public static void SetConfigurationValues(string configFileName,
            Collection<KeyValuePair<string, string>> sections)
        {
            if (sections == null || sections.Count == 0)
            {
                return;
            }

            string path = ResolveConfigFilePath(configFileName);

            if (!MixERPConfiguration.SetExternalConfigValues(path, sections))
            {
                Log.Warning("Could not persist {Count} configuration value(s) to {Path}.", sections.Count, path);
            }
        }

        private static string ResolveConfigFilePath(string configFileName)
        {
            if (string.IsNullOrWhiteSpace(configFileName))
            {
                return null;
            }

            if (System.IO.Path.IsPathRooted(configFileName) && System.IO.File.Exists(configFileName))
            {
                return configFileName;
            }

            return MixERPConfiguration.MapPath(configFileName);
        }
    }
}
