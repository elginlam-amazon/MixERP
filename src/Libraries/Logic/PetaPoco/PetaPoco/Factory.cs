using System.Globalization;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Npgsql;

namespace PetaPoco
{
    public sealed class Factory
    {
        private const string ProviderName = "Npgsql";

        // Originally a static field initializer. On modern .NET a static field initializer
        // runs at type load — before the host has configured the settings provider — which
        // would bake in an empty string (or surface a TypeInitializationException) for every
        // call site. Evaluated lazily instead, so the value is read the first time it is
        // actually needed, after startup has configured the provider. The member stays a
        // public static string, so all existing call sites are unchanged.
        private static readonly Lazy<string> metaDatabase =
            new Lazy<string>(() => ConfigurationHelper.GetDbServerParameter("MetaDatabase"));

        public static string MetaDatabase
        {
            get { return metaDatabase.Value; }
        }

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
                if (ex.SqlState.StartsWith("P", StringComparison.Ordinal))
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
                if (ex.SqlState.StartsWith("P", StringComparison.Ordinal))
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
                if (ex.SqlState.StartsWith("P", StringComparison.Ordinal))
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
                if (ex.SqlState.StartsWith("P", StringComparison.Ordinal))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        // Npgsql 8 moved the SQLSTATE off NpgsqlException: the server-side error detail now
        // lives on PostgresException (a subclass) as SqlState. NpgsqlException.Code no
        // longer exists. The resource lookup and the "resource equals the code means no
        // translation exists, fall back to the raw message" rule are unchanged.
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

            // Npgsql 8 renamed UserName to Username and removed SyncNotification (the
            // asynchronous-notification listener is always on now). The legacy SSL boolean
            // is also gone; it is superseded by SslMode, which was already set to Prefer.
            // In Npgsql 8, SslMode.Prefer encrypts when the server supports it but does not
            // validate the server certificate (only VerifyCA/VerifyFull do), which matches the
            // Npgsql 2.x behaviour; TrustServerCertificate is obsolete and no longer needed.
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = ConfigurationHelper.GetDbServerParameter("Server"),
                Database = database,
                Username = ConfigurationHelper.GetDbServerParameter("UserId"),
                Password = ConfigurationHelper.GetDbServerParameter("Password"),
                Port = int.Parse(ConfigurationHelper.GetDbServerParameter("Port"), CultureInfo.InvariantCulture),
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
