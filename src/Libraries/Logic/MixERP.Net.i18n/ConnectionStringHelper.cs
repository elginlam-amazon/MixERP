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

using System.Xml.Linq;
using Npgsql;

namespace MixERP.Net.i18n
{
    internal static class ConnectionStringHelper
    {
        public static string GetConnectionString()
        {
            string host = GetDbServerParameter("Server");
            string database = GetDbServerParameter("Database");
            string userId = GetDbServerParameter("UserId");
            string password = GetDbServerParameter("Password");
            string portValue = GetDbServerParameter("Port");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(portValue))
            {
                // Fail loudly rather than handing out a connection string pointing at nothing.
                throw new InvalidOperationException(
                    "The MixERP database server configuration file could not be read from '" +
                    ResolvePath(I18nRuntime.Options.DbServerConfigFileLocation) +
                    "'. Call IServiceCollection.AddMixErpI18n(configuration, environment) during host " +
                    "startup and make sure DbServerConfigFileLocation points at a file under the content root.");
            }

            int port = int.Parse(portValue);

            return GetConnectionString(host, database, userId, password, port);
        }

        private static string GetDbServerParameter(string keyName)
        {
            return GetConfigurationValue("DbServerConfigFileLocation", keyName);
        }

        /// <summary>
        /// Reads a key from the external database server config file. Replaces the
        /// .NET Framework combination of the hosting-environment path mapper,
        /// the exe-configuration file map and the mapped-exe-configuration opener.
        /// </summary>
        private static string GetConfigurationValue(string configFileName, string sectionName)
        {
            if (configFileName == null)
            {
                return string.Empty;
            }

            string relativePath = I18nRuntime.Options.DbServerConfigFileLocation;

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

            string contentRoot = I18nRuntime.Options.ContentRootPath;

            if (string.IsNullOrWhiteSpace(contentRoot))
            {
                contentRoot = AppContext.BaseDirectory;
            }

            return Path.Combine(contentRoot, normalized);
        }

        private static string GetConnectionString(string host, string database, string username, string password,
            int port)
        {
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                Username = username,
                Password = password,
                Port = port,
                Pooling = true,
                // The legacy "SSL = true" flag was removed from Npgsql; SslMode.Prefer carries
                // the same meaning (use TLS when the server offers it, plain text otherwise).
                SslMode = SslMode.Prefer,
                MinPoolSize = 10,
                MaxPoolSize = 100,
                ApplicationName = "MixERP"
            };

            return connectionStringBuilder.ConnectionString;
        }
    }
}
