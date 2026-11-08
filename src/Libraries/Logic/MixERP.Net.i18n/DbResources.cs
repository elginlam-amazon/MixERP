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

using Npgsql;

namespace MixERP.Net.i18n
{
    internal static class DbResources
    {
        /// <summary>
        /// Reads every localized resource as a flat key/value dictionary. Called from the
        /// synchronous <see cref="ResourceManager" /> surface consumed by the generated
        /// resource facades, so it stays synchronous rather than blocking on an async call.
        /// </summary>
        internal static IDictionary<string, string> GetLocalizedResources()
        {
            Dictionary<string, string> resources = new Dictionary<string, string>();
            const string sql = "SELECT * FROM localization.localized_resource_view;";

            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionStringHelper.GetConnectionString()))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string key = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0).ToString();
                            string value = reader.IsDBNull(1) ? string.Empty : reader.GetValue(1).ToString();

                            resources[key] = value;
                        }
                    }
                }
            }

            return resources;
        }

        /// <summary>
        /// Reads the localization table for a language. Consumed by the asynchronous
        /// <c>Resources.js</c> endpoint.
        /// </summary>
        internal static async Task<IEnumerable<LocalizedResource>> GetLocalizationTableAsync(string language,
            CancellationToken cancellationToken = default)
        {
            List<LocalizedResource> resources = new List<LocalizedResource>();
            const string sql = "SELECT * FROM localization.get_localization_table(@Language);";

            await using (NpgsqlConnection connection =
                new NpgsqlConnection(ConnectionStringHelper.GetConnectionString()))
            {
                await connection.OpenAsync(cancellationToken);

                await using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Language", (object)language ?? DBNull.Value);

                    await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        int idIndex = reader.GetOrdinal("id");
                        int resourceClassIndex = reader.GetOrdinal("resource_class");
                        int keyIndex = reader.GetOrdinal("key");
                        int originalIndex = reader.GetOrdinal("original");
                        int translatedIndex = reader.GetOrdinal("translated");

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            LocalizedResource resource = new LocalizedResource();

                            resource.Id = reader.IsDBNull(idIndex)
                                ? 0
                                : long.Parse(reader.GetValue(idIndex).ToString());
                            resource.ResourceClass = ReadString(reader, resourceClassIndex);
                            resource.Key = ReadString(reader, keyIndex);
                            resource.Original = ReadString(reader, originalIndex);
                            resource.Translated = ReadString(reader, translatedIndex);

                            resources.Add(resource);
                        }
                    }
                }
            }

            return resources;
        }

        private static string ReadString(NpgsqlDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString();
        }
    }
}
