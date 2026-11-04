using System.Collections.Generic;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Npgsql;

namespace PetaPoco
{
    public sealed class Factory
    {
        private const string ProviderName = "Npgsql";
        public static string MetaDatabase = ConfigurationHelper.GetDbServerParameter("MetaDatabase");

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
            string database = ConfigurationHelper.GetDbServerParameter("Database");

            if (!string.IsNullOrWhiteSpace(catalog))
            {
                database = catalog;
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = ConfigurationHelper.GetDbServerParameter("Server"),
                Database = database,
                Username = ConfigurationHelper.GetDbServerParameter("UserId"),
                Password = ConfigurationHelper.GetDbServerParameter("Password"),
                Port = int.Parse(ConfigurationHelper.GetDbServerParameter("Port")),
                Pooling = true,
                SslMode = SslMode.Prefer,
                MinPoolSize = 10,
                MaxPoolSize = 100,
                ApplicationName = "MixERP"
            };

            return connectionStringBuilder.ConnectionString;
        }
    }
}