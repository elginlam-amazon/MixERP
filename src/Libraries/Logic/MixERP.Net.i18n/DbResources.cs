using System.Data;
using System.Globalization;
using Npgsql;

namespace MixERP.Net.i18n
{
    internal static class DbResources
    {
        internal static IDictionary<string, string> GetLocalizedResources()
        {
            Dictionary<string, string> resources = new Dictionary<string, string>();
            const string sql = "SELECT * FROM localization.localized_resource_view;";

            using (NpgsqlCommand command = new NpgsqlCommand(sql))
            {
                using (DataTable table = GetDataTable(command))
                {
                    if (table == null || table.Rows == null)
                    {
                        return resources;
                    }

                    foreach (DataRow row in table.Rows)
                    {
                        string key = row[0].ToString();
                        string value = row[1].ToString();

                        resources.Add(key, value);
                    }
                }
            }

            return resources;
        }

        internal static IEnumerable<LocalizedResource> GetLocalizationTable(string language)
        {
            List<LocalizedResource> resources = new List<LocalizedResource>();
            const string sql = "SELECT * FROM localization.get_localization_table(@Language);";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionStringHelper.GetConnectionString()))
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Language", language ?? string.Empty);

                    using (DataTable dataTable = Load(connection, command))
                    {
                        if (dataTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in dataTable.Rows)
                            {
                                LocalizedResource resource = new LocalizedResource();

                                resource.Id = long.Parse(row["id"].ToString(), CultureInfo.InvariantCulture);
                                resource.ResourceClass = row["resource_class"].ToString();
                                resource.Key = row["key"].ToString();
                                resource.Original = row["original"].ToString();
                                resource.Translated = row["translated"].ToString();

                                resources.Add(resource);
                            }
                        }
                    }
                }
            }

            return resources;
        }

        private static DataTable GetDataTable(NpgsqlCommand command)
        {
            string connectionString = ConnectionStringHelper.GetConnectionString();

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                command.Connection = connection;
                return Load(connection, command);
            }
        }

        /// <summary>
        ///     Npgsql 8 no longer ships NpgsqlDataAdapter, so the DataTable is filled from a
        ///     data reader instead of adapter.Fill. The connection has to be opened
        ///     explicitly now that no adapter does it implicitly.
        /// </summary>
        private static DataTable Load(NpgsqlConnection connection, NpgsqlCommand command)
        {
            DataTable dataTable = new DataTable();

            try
            {
                dataTable.Locale = CultureInfo.CurrentCulture;

                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }

                return dataTable;
            }
            catch
            {
                dataTable.Dispose();
                throw;
            }
        }
    }
}
