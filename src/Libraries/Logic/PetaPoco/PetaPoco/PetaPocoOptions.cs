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

using System.Collections.Generic;

namespace PetaPoco
{
    /// <summary>
    /// Settings this library used to read ambiently from the legacy app-settings reader,
    /// the legacy connection-string collection and the legacy hosting-environment path
    /// mapper. Bound from <c>IConfiguration</c> by
    /// <see cref="PetaPocoServiceCollectionExtensions.AddPetaPoco" />.
    /// </summary>
    public class PetaPocoOptions
    {
        /// <summary>
        /// Configuration section name used when binding from <c>IConfiguration</c>.
        /// </summary>
        public const string SectionName = "PetaPoco";

        /// <summary>
        /// Content-root relative location of the external database server config file.
        /// Legacy app setting: <c>DbServerConfigFileLocation</c>
        /// (e.g. "/Resource/Configs/DbServer.config").
        /// </summary>
        public string DbServerConfigFileLocation { get; set; } = "/Resource/Configs/DbServer.config";

        /// <summary>
        /// Absolute path of the host's content root, used to resolve
        /// <see cref="DbServerConfigFileLocation" /> and every other legacy app setting that
        /// pointed at a config file. Replaces the legacy hosting-environment path mapper.
        /// </summary>
        public string ContentRootPath { get; set; } = string.Empty;

        /// <summary>
        /// Additional legacy app settings whose values are content-root relative config file
        /// locations, keyed by the legacy app-setting key. Allows
        /// <see cref="ConfigurationHelper.GetConfigurationValue" /> to keep resolving any
        /// <c>*ConfigFileLocation</c> key the way the legacy app-settings reader did.
        /// </summary>
        public Dictionary<string, string> ConfigFileLocations { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Named connection strings, replacing the legacy connection-string collection used by
        /// the <see cref="Database(string)" /> constructor. Order is preserved so the legacy
        /// "use the first connection string when no name is supplied" behaviour still holds.
        /// </summary>
        public List<ConnectionStringEntry> ConnectionStrings { get; set; } =
            new List<ConnectionStringEntry>();
    }

    /// <summary>
    /// One named connection string, mirroring the legacy connection-string settings element.
    /// </summary>
    public class ConnectionStringEntry
    {
        public string Name { get; set; } = string.Empty;

        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// ADO.NET provider invariant name (e.g. "Npgsql"). Empty means the legacy default,
        /// "System.Data.SqlClient", is applied by the consumer.
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;
    }
}
