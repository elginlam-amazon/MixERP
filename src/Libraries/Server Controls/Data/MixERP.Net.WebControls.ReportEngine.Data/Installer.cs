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
using System;

namespace MixERP.Net.WebControls.ReportEngine.Data
{
    public static class Installer
    {
        public static void InstallReport(string catalog, string menuCode, string parentMenuCode, int level, string menuText, string path)
        {
            const string sql = @"INSERT INTO core.menus(menu_text, url, menu_code, level, parent_menu_id)
                            SELECT @MenuText, @Path, @MenuCode, @Level, core.get_menu_id(@ParentMenuCode)
                            WHERE NOT EXISTS(SELECT * FROM core.menus WHERE menu_code=@MenuCode);";

            using (NpgsqlCommand command = new NpgsqlCommand(sql))
            {
                // Npgsql 6+ rejects a CLR null passed to AddWithValue; DBNull.Value preserves
                // the original "send SQL NULL" semantics of Npgsql 2.x for these nullable strings.
                command.Parameters.AddWithValue("@MenuCode", (object)menuCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@MenuText", (object)menuText ?? DBNull.Value);
                command.Parameters.AddWithValue("@Path", (object)path ?? DBNull.Value);
                command.Parameters.AddWithValue("@Level", level);
                command.Parameters.AddWithValue("@ParentMenuCode", (object)parentMenuCode ?? DBNull.Value);

                DbOperation.ExecuteNonQuery(catalog, command);
            }
        }
    }
}