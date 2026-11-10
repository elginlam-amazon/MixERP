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

using MixERP.Net.i18n;

namespace PetaPoco
{
    /// <summary>
    ///     Reads the database server parameters out of the external installation config
    ///     file whose location is held in the application's
    ///     <c>DbServerConfigFileLocation</c> setting.
    ///     The original implementation combined
    ///     <c>ConfigurationManager.AppSettings</c>,
    ///     <c>System.Web.Hosting.HostingEnvironment.MapPath</c> and
    ///     <c>ConfigurationManager.OpenMappedExeConfiguration</c>; none of those exist on
    ///     modern .NET. The identical lookup already exists, cross-platform, in
    ///     <see cref="II18nSettingsProvider.GetDbServerParameter" /> (IConfiguration for the
    ///     file location, the host content root instead of MapPath, and XDocument instead of
    ///     System.Configuration), so it is reused here rather than duplicated.
    /// </summary>
    internal static class ConfigurationHelper
    {
        /// <summary>
        ///     Name of the application setting that holds the path of the external database
        ///     server config file. This is the only config file this helper ever read.
        /// </summary>
        private const string DbServerConfigFileLocationKey = "DbServerConfigFileLocation";

        internal static string GetConfigurationValue(string configFileName, string sectionName)
        {
            if (configFileName == null)
            {
                return string.Empty;
            }

            // The original code resolved an arbitrary appSettings key to a file path and
            // read that file. The only key ever passed is DbServerConfigFileLocation, which
            // the shared settings provider already resolves and reads. Any other key has no
            // configured location, so it degrades to an empty value exactly as the original
            // did for a missing file/section/key.
            if (!configFileName.Equals(DbServerConfigFileLocationKey, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return I18nSettings.Current.GetDbServerParameter(sectionName);
        }

        internal static string GetDbServerParameter(string key)
        {
            return GetConfigurationValue(DbServerConfigFileLocationKey, key);
        }
    }
}
