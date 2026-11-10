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

using Microsoft.Extensions.Configuration;

namespace PetaPoco
{
    /// <summary>
    ///     A named connection string together with the ADO.NET provider that serves it.
    ///     This is the modern replacement for
    ///     <c>System.Configuration.ConnectionStringSettings</c>, which the
    ///     <see cref="Database" /> constructor used to read out of
    ///     <c>ConfigurationManager.ConnectionStrings</c>.
    /// </summary>
    public sealed class ConnectionStringEntry
    {
        public ConnectionStringEntry(string name, string connectionString, string providerName)
        {
            this.Name = name ?? string.Empty;
            this.ConnectionString = connectionString ?? string.Empty;
            this.ProviderName = providerName ?? string.Empty;
        }

        /// <summary>
        ///     The configured name, i.e. the key under <c>ConnectionStrings</c>.
        /// </summary>
        public string Name { get; }

        public string ConnectionString { get; }

        /// <summary>
        ///     The configured provider name, or <see cref="string.Empty" /> when the
        ///     configuration does not state one. An empty value makes
        ///     <see cref="Database" /> fall back to its original
        ///     <c>System.Data.SqlClient</c> default.
        /// </summary>
        public string ProviderName { get; }
    }

    /// <summary>
    ///     Supplies the named connection strings that
    ///     <see cref="Database(string)" /> resolves. Replaces
    ///     <c>ConfigurationManager.ConnectionStrings</c>, which does not exist on modern
    ///     .NET.
    /// </summary>
    public interface IConnectionStringSource
    {
        /// <summary>
        ///     The first configured connection string, or <c>null</c> when none is
        ///     configured. Backs the original "use first?" behaviour of
        ///     <c>ConfigurationManager.ConnectionStrings[0]</c> for an empty name.
        /// </summary>
        ConnectionStringEntry First { get; }

        /// <summary>
        ///     The connection string configured under <paramref name="name" />, or
        ///     <c>null</c> when there is none.
        /// </summary>
        ConnectionStringEntry Get(string name);
    }

    /// <summary>
    ///     <see cref="IConfiguration" />-backed <see cref="IConnectionStringSource" />.
    ///     Reads the standard <c>ConnectionStrings</c> section, supporting both shapes:
    ///     <code>
    ///     { "ConnectionStrings": { "Default": "Host=localhost;Database=mixerp" } }
    ///     { "ConnectionStrings": { "Default": { "ConnectionString": "Host=localhost;Database=mixerp",
    ///                                           "ProviderName": "Npgsql" } } }
    ///     </code>
    ///     The second shape carries the <c>providerName</c> attribute that
    ///     <c>&lt;connectionStrings&gt;</c> in Web.config used to carry.
    /// </summary>
    public sealed class ConfigurationConnectionStringSource : IConnectionStringSource
    {
        private const string SectionName = "ConnectionStrings";

        private readonly IConfiguration configuration;

        public ConfigurationConnectionStringSource(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            this.configuration = configuration;
        }

        public ConnectionStringEntry First
        {
            get
            {
                foreach (IConfigurationSection child in this.configuration.GetSection(SectionName).GetChildren())
                {
                    return Read(child);
                }

                return null;
            }
        }

        public ConnectionStringEntry Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            IConfigurationSection section = this.configuration.GetSection(SectionName).GetSection(name);

            if (!section.Exists())
            {
                return null;
            }

            return Read(section);
        }

        private static ConnectionStringEntry Read(IConfigurationSection section)
        {
            // A plain string value ("Default": "Host=...") exposes itself as the section
            // value; an object value exposes ConnectionString/ProviderName children.
            string connectionString = section.Value ?? section["ConnectionString"];
            string providerName = section.Value == null ? section["ProviderName"] : null;

            return new ConnectionStringEntry(section.Key, connectionString, providerName);
        }
    }

    /// <summary>
    ///     An empty source, used until a host configures a real one. Keeps
    ///     <see cref="PetaPocoConfiguration" /> out of a null state, so the
    ///     <see cref="Database(string)" /> constructor raises its original
    ///     "Can't find a connection string with the name ..."
    ///     <see cref="InvalidOperationException" /> rather than a
    ///     <see cref="NullReferenceException" />.
    /// </summary>
    internal sealed class EmptyConnectionStringSource : IConnectionStringSource
    {
        public ConnectionStringEntry First
        {
            get { return null; }
        }

        public ConnectionStringEntry Get(string name)
        {
            return null;
        }
    }

    /// <summary>
    ///     Static facade over <see cref="IConnectionStringSource" />, preserving the
    ///     parameterless-configuration style of the original
    ///     <c>Database(string connectionStringName)</c> constructor (which read a process
    ///     wide <c>ConfigurationManager</c>). Hosts call <see cref="Configure" /> during
    ///     startup, e.g.
    ///     <c>PetaPocoConfiguration.Configure(new ConfigurationConnectionStringSource(builder.Configuration))</c>.
    /// </summary>
    public static class PetaPocoConfiguration
    {
        private static IConnectionStringSource connectionStrings = new EmptyConnectionStringSource();

        public static void Configure(IConnectionStringSource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            connectionStrings = source;
        }

        /// <summary>
        ///     Points the facade at the application's <see cref="IConfiguration" />.
        /// </summary>
        public static void Configure(IConfiguration configuration)
        {
            Configure(new ConfigurationConnectionStringSource(configuration));
        }

        public static IConnectionStringSource ConnectionStrings
        {
            get { return connectionStrings; }
        }
    }
}
