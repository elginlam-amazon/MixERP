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
    /// Settings this library used to read ambiently from the legacy app-settings reader
    /// and the legacy hosting-environment path mapper. Bound from <c>IConfiguration</c> by
    /// <see cref="I18nServiceCollectionExtensions.AddMixErpI18n" />.
    /// </summary>
    public class I18nOptions
    {
        /// <summary>
        /// Configuration section name used when binding from <c>IConfiguration</c>.
        /// </summary>
        public const string SectionName = "MixErpI18n";

        /// <summary>
        /// Content-root relative location of the external database server config file.
        /// Legacy app setting: <c>DbServerConfigFileLocation</c> (e.g. "/Resource/Configs/DbServer.config").
        /// </summary>
        public string DbServerConfigFileLocation { get; set; } = "/Resource/Configs/DbServer.config";

        /// <summary>
        /// When true, a missing resource returns its key instead of throwing
        /// <see cref="System.Resources.MissingManifestResourceException" />.
        /// Legacy app setting: <c>SupressMissingResourceException</c> (Web.config value: false).
        /// </summary>
        public bool SupressMissingResourceException { get; set; }

        /// <summary>
        /// Absolute path of the host's content root, used to resolve
        /// <see cref="DbServerConfigFileLocation" />. Replaces the legacy hosting-environment path mapper.
        /// </summary>
        public string ContentRootPath { get; set; } = string.Empty;
    }
}
