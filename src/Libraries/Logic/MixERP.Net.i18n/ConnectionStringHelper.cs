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
            int port = int.Parse(GetDbServerParameter("Port"), System.Globalization.CultureInfo.InvariantCulture);

            return GetConnectionString(host, database, userId, password, port);
        }

        private static string GetDbServerParameter(string keyName)
        {
            return I18nSettings.Current.GetDbServerParameter(keyName);
        }

        private static string GetConnectionString(string host, string database, string username, string password,
            int port)
        {
            // Npgsql 8 removed NpgsqlConnectionStringBuilder.SyncNotification (the
            // asynchronous-notification listener is always on now) and the legacy SSL
            // boolean (superseded by SslMode). UserName was renamed to Username.
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                Username = username,
                Password = password,
                Port = port,
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
