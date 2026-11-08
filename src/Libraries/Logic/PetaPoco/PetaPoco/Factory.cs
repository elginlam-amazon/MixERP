using System;
using System.Collections.Generic;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Npgsql;

namespace PetaPoco
{
    public sealed class Factory
    {
        private const string ProviderName = "Npgsql";

        /// <summary>
        /// Name of the MixERP meta database, read from the external database server config file.
        /// Evaluated lazily so that it is read AFTER the host has supplied configuration through
        /// <see cref="PetaPocoServiceCollectionExtensions.AddPetaPoco" /> — a static field
        /// initializer would have run before startup wiring and captured an empty value.
        /// </summary>
        public static string MetaDatabase => ConfigurationHelper.GetDbServerParameter("MetaDatabase");

        public static IEnumerable<T> Get<T>(string catalog, string sql, params object[] args)
        {
            try
            {
                using (Database db = new Database(GetConnectionString(catalog), ProviderName))
                {
                    return db.Query<T>(sql, args);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState.StartsWith("P"))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        public static object Insert(string catalog, object poco)
        {
            try
            {
                using (Database db = new Database(GetConnectionString(catalog), ProviderName))
                {
                    return db.Insert(poco);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState.StartsWith("P"))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        public static T Scalar<T>(string catalog, string sql, params object[] args)
        {
            try
            {
                using (Database db = new Database(GetConnectionString(catalog), ProviderName))
                {
                    return db.ExecuteScalar<T>(sql, args);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState.StartsWith("P"))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        public static void NonQuery(string catalog, string sql, params object[] args)
        {
            try
            {
                using (Database db = new Database(GetConnectionString(catalog), ProviderName))
                {
                    db.Execute(sql, args);
                }
            }
            catch (PostgresException ex)
            {
                if (ex.SqlState.StartsWith("P"))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        // The legacy NpgsqlException.Code carried the PostgreSQL SQLSTATE; on Npgsql 10 the
        // server-side error is PostgresException and the same value is exposed as SqlState.
        private static string GetDBErrorResource(PostgresException ex)
        {
            string message = DbErrors.Get(ex.SqlState);

            if (message == ex.SqlState)
            {
                return ex.Message;
            }

            return message;
        }

        public static string GetConnectionString(string catalog = "")
        {
            string host = ConfigurationHelper.GetDbServerParameter("Server");
            string database = ConfigurationHelper.GetDbServerParameter("Database");
            string portValue = ConfigurationHelper.GetDbServerParameter("Port");

            if (!string.IsNullOrWhiteSpace(catalog))
            {
                database = catalog;
            }

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(portValue))
            {
                // Fail with an actionable message rather than handing out a connection string
                // pointing at nothing, or an anonymous FormatException out of int.Parse.
                throw new InvalidOperationException(
                    "The MixERP database server configuration file could not be read from '" +
                    ConfigurationHelper.GetDbServerConfigFilePath() +
                    "'. Call IServiceCollection.AddPetaPoco(configuration, environment) during host " +
                    "startup and make sure DbServerConfigFileLocation points at a file under the " +
                    "content root.");
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                // Npgsql renamed UserName to Username.
                Username = ConfigurationHelper.GetDbServerParameter("UserId"),
                Password = ConfigurationHelper.GetDbServerParameter("Password"),
                Port = int.Parse(portValue),
                Pooling = true,
                // The legacy "SyncNotification" keyword was removed from Npgsql: asynchronous
                // notification delivery is now always handled by the connection's own message
                // reader, so there is no connection-string equivalent to carry over.
                // The legacy "SSL = true" flag was likewise removed; SslMode.Prefer carries the
                // same meaning (use TLS when the server offers it, plain text otherwise).
                SslMode = SslMode.Prefer,
                MinPoolSize = 10,
                MaxPoolSize = 100,
                ApplicationName = "MixERP"
            };

            return connectionStringBuilder.ConnectionString;
        }
    }
}
