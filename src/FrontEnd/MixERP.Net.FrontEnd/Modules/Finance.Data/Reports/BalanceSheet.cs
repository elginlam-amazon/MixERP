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

using MixERP.Net.DbFactory;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;

namespace MixERP.Net.Core.Modules.Finance.Data.Reports
{
    public static class BalanceSheet
    {
        public static DataTable GetBalanceSheet(string catalog, DateTime previousPeriod, DateTime currentPeriod, int userId, int officeId, int factor)
        {
            const string sql = "SELECT * FROM transactions.get_balance_sheet(@Previous::date, @Current::date, @UserId::integer, @OfficeId::integer, @Factor::integer)";
            using (NpgsqlCommand command = new NpgsqlCommand(sql))
            {
                // Npgsql 6+ infers "timestamp" for an untyped DateTime, which does not bind to
                // the ::date casts in the SQL above. Bind these parameters as "date" explicitly.
                command.Parameters.Add(new NpgsqlParameter("@Previous", NpgsqlDbType.Date) { Value = previousPeriod.Date });
                command.Parameters.Add(new NpgsqlParameter("@Current", NpgsqlDbType.Date) { Value = currentPeriod.Date });
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@OfficeId", officeId);
                command.Parameters.AddWithValue("@Factor", factor);

                return DbOperation.GetDataTable(catalog, command);
            }
        }
    }
}