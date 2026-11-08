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

using MixERP.Net.Framework;
using MixERP.Net.i18n;
using MixERP.Net.i18n.Resources;
using Npgsql;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace MixERP.Net.DbFactory
{
    public class DbNotificationArgs : EventArgs
    {
        /// <summary>
        ///     The server notice that raised this notification. Npgsql removed the legacy
        ///     NpgsqlError type; PostgresNotice is its replacement. Note that the notice text
        ///     is exposed as <see cref="PostgresNotice.MessageText" />, not "Message".
        /// </summary>
        [System.CLSCompliant(false)]
        public PostgresNotice Notice { get; set; }

        public string Message { get; set; }
    }

    public class DbOperation
    {
        // Deliberately a field, not a property or an event: callers such as
        // Finance.Data/EODOperation subscribe with "operation.Listen += this.Listen;" and
        // this library reads it into a local before invoking it.
        public EventHandler<DbNotificationArgs> Listen;

        [CLSCompliant(false)]
        public static bool ExecuteNonQuery(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        using (NpgsqlConnection connection = new NpgsqlConnection(DbConnection.GetConnectionString(catalog)))
                        {
                            command.Connection = connection;
                            connection.Open();

                            command.ExecuteNonQuery();
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        [CLSCompliant(false)]
        public static bool ExecuteNonQuery(string catalog, string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return false;
            }

            return ExecuteNonQuery(catalog, new NpgsqlCommand(sql));
        }

        /// <summary>
        ///     Npgsql no longer exposes the error code as NpgsqlException.Code; the SQLSTATE now
        ///     lives on PostgresException.SqlState. The "P" class covers PL/pgSQL raised errors,
        ///     which MixERP maps onto its own localized error resources.
        /// </summary>
        private static bool IsPostgresPlpgsqlError(PostgresException ex)
        {
            return ex.SqlState != null && ex.SqlState.StartsWith("P", StringComparison.Ordinal);
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

        [CLSCompliant(false)]
        public static NpgsqlDataAdapter GetDataAdapter(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        // The adapter (and the connection its command is bound to) is returned to
                        // the caller, so neither may be disposed here. The connection is left
                        // closed on purpose: NpgsqlDataAdapter.Fill opens it and closes it again,
                        // which returns it to the pool.
                        command.Connection = new NpgsqlConnection(DbConnection.GetConnectionString(catalog));
                        return new NpgsqlDataAdapter(command);
                    }
                }

                return null;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        [CLSCompliant(false)]
        public static NpgsqlDataReader GetDataReader(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        // The reader outlives this method, so the connection must not be disposed
                        // on the success path. CommandBehavior.CloseConnection hands connection
                        // ownership to the reader: closing/disposing the reader releases the
                        // connection. That transfer only happens once a reader is actually
                        // returned, so a throwing Open/ExecuteReader must dispose the connection
                        // itself or it would be orphaned open and never returned to the pool.
                        NpgsqlConnection connection = new NpgsqlConnection(DbConnection.GetConnectionString(catalog));

                        try
                        {
                            command.Connection = connection;
                            connection.Open();
                            return command.ExecuteReader(CommandBehavior.CloseConnection);
                        }
                        catch
                        {
                            connection.Dispose();
                            throw;
                        }
                    }
                }

                return null;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        [CLSCompliant(false)]
        public static DataSet GetDataSet(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (ValidateCommand(command))
                {
                    NpgsqlDataAdapter adapter = GetDataAdapter(catalog, command);

                    if (adapter == null)
                    {
                        return null;
                    }

                    try
                    {
                        // The DataSet is returned, so it must not be disposed here.
                        DataSet set = new DataSet();
                        adapter.Fill(set);
                        set.Locale = CultureInfo.CurrentUICulture;
                        return set;
                    }
                    finally
                    {
                        NpgsqlConnection connection = adapter.SelectCommand?.Connection;
                        adapter.Dispose();

                        if (connection != null)
                        {
                            connection.Dispose();
                        }
                    }
                }

                return null;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        [CLSCompliant(false)]
        public static DataTable GetDataTable(NpgsqlCommand command, string connectionString)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                        {
                            command.Connection = connection;

                            using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                            {
                                // Fill completes before this method returns, so disposing the
                                // connection and the adapter here is correct. The DataTable
                                // itself is returned and therefore must NOT be disposed.
                                DataTable dataTable = new DataTable();
                                dataTable.Locale = Thread.CurrentThread.CurrentCulture;
                                adapter.Fill(dataTable);
                                return dataTable;
                            }
                        }
                    }
                }

                return null;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        [CLSCompliant(false)]
        public static DataTable GetDataTable(string catalog, NpgsqlCommand command)
        {
            return GetDataTable(command, DbConnection.GetConnectionString(catalog));
        }

        [CLSCompliant(false)]
        public static DataView GetDataView(string catalog, NpgsqlCommand command)
        {
            if (ValidateCommand(command))
            {
                // The DataView is returned to the caller and must not be disposed here.
                return new DataView(GetDataTable(catalog, command));
            }

            return null;
        }

        [CLSCompliant(false)]
        public static object GetScalarValue(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        using (NpgsqlConnection connection = new NpgsqlConnection(DbConnection.GetConnectionString(catalog)))
                        {
                            command.Connection = connection;
                            connection.Open();
                            return command.ExecuteScalar();
                        }
                    }
                }

                return null;
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        public static bool IsServerAvailable(string catalog)
        {
            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(DbConnection.GetConnectionString(catalog)))
                {
                    connection.Open();
                }

                return true;
            }
            catch (NpgsqlException ex)
            {
                // NpgsqlException (the base type) is correct here: this path reports
                // connection-level failures, not server error responses.
                Log.Warning("Server is not available: {Exception}.", ex);
            }

            return false;
        }

        [System.CLSCompliant(false)]
        public void ListenNonQuery(string catalog, NpgsqlCommand command)
        {
            try
            {
                if (command != null)
                {
                    if (ValidateCommand(command))
                    {
                        ThreadStart queryStart = delegate
                        {
                            try
                            {
                                using (
                                    NpgsqlConnection connection =
                                        new NpgsqlConnection(DbConnection.GetConnectionString(catalog)))
                                {
                                    command.Connection = connection;

                                    // Subscribe BEFORE opening so no notice is missed.
                                    connection.Notice += this.Connection_Notice;
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                            }
                            catch (PostgresException ex)
                            {
                                string errorMessage = ex.Message;

                                if (IsPostgresPlpgsqlError(ex))
                                {
                                    errorMessage = GetDBErrorResource(ex);
                                }

                                this.RaiseError(errorMessage);
                            }
                            catch (NpgsqlException ex)
                            {
                                this.RaiseError(ex.Message);
                            }
                        };

                        // Holds the background thread open long enough to drain the trailing
                        // notices the server emits after the command completes.
                        queryStart += () => { Thread.Sleep(15000); };

                        Thread query = new Thread(queryStart) { IsBackground = true };
                        query.Start();
                    }
                }
            }
            catch (PostgresException ex)
            {
                if (IsPostgresPlpgsqlError(ex))
                {
                    string errorMessage = GetDBErrorResource(ex);
                    throw new MixERPException(errorMessage, ex);
                }

                throw;
            }
        }

        /// <summary>
        ///     Surfaces a failure through the Listen feed with no Notice attached. Subscribers
        ///     (for example EODOperation) distinguish an error from a progress notice by exactly
        ///     that: a null Notice with a non-blank Message.
        /// </summary>
        private void RaiseError(string errorMessage)
        {
            EventHandler<DbNotificationArgs> listen = this.Listen;

            if (listen != null)
            {
                DbNotificationArgs args = new DbNotificationArgs
                {
                    Message = errorMessage
                };

                listen(this, args);
            }
        }

        private static Collection<string> GetCommandTextParameterCollection(string commandText)
        {
            Collection<string> parameters = new Collection<string>();

            foreach (Match match in Regex.Matches(commandText, @"@(\w+)"))
            {
                parameters.Add(match.Value);
            }

            return parameters;
        }

        private static bool ValidateCommand(NpgsqlCommand command)
        {
            return ValidateParameters(command);
        }

        private static bool ValidateParameters(NpgsqlCommand command)
        {
            Collection<string> commandTextParameters = GetCommandTextParameterCollection(command.CommandText);

            foreach (NpgsqlParameter npgsqlParameter in command.Parameters)
            {
                bool match = false;

                foreach (string commandTextParameter in commandTextParameters)
                {
                    if (npgsqlParameter.ParameterName.Equals(commandTextParameter))
                    {
                        match = true;
                    }
                }

                if (!match)
                {
                    throw new InvalidOperationException(string.Format(CurrentCulture.GetCurrentUICulture(),
                        Warnings.InvalidParameterName, npgsqlParameter.ParameterName));
                }
            }

            return true;
        }

        private void Connection_Notice(object sender, NpgsqlNoticeEventArgs e)
        {
            EventHandler<DbNotificationArgs> listen = this.Listen;

            if (listen != null)
            {
                DbNotificationArgs args = new DbNotificationArgs
                {
                    Notice = e.Notice,

                    // PostgresNotice exposes the notice text as MessageText; the legacy
                    // NpgsqlError.Message member no longer exists.
                    Message = e.Notice.MessageText
                };

                listen(this, args);
            }
        }
    }
}
