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
    ///     Settings this library used to read directly out of
    ///     <c>Web.config</c>'s <c>&lt;appSettings&gt;</c> through
    ///     <c>ConfigurationManager.AppSettings</c>. They are now bound from
    ///     <c>appsettings.json</c> (root level keys, so the original key names keep
    ///     working verbatim).
    /// </summary>
    public class I18nOptions
    {
        /// <summary>
        ///     Configuration section name. The keys also bind from the configuration
        ///     root, so either of the following works:
        ///     <code>
        ///     { "DbServerConfigFileLocation": "~/MixERP.Installation.config" }
        ///     { "MixERP": { "DbServerConfigFileLocation": "~/MixERP.Installation.config" } }
        ///     </code>
        /// </summary>
        public const string SectionName = "MixERP";

        /// <summary>
        ///     Application-relative path (e.g. <c>~/MixERP.Installation.config</c>) of the
        ///     external XML file whose <c>&lt;appSettings&gt;</c> carry the database server
        ///     parameters (Server, Database, UserId, Password, Port).
        /// </summary>
        public string DbServerConfigFileLocation { get; set; } = string.Empty;

        /// <summary>
        ///     When "TRUE" (case-insensitive), a missing localized resource returns the
        ///     resource key instead of throwing
        ///     <see cref="System.Resources.MissingManifestResourceException" />.
        ///     Kept as a string so the original "TRUE"/"true" config values bind unchanged.
        /// </summary>
        public string SupressMissingResourceException { get; set; } = string.Empty;
    }
}
