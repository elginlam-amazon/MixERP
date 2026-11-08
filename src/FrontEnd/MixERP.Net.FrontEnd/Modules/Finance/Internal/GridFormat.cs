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

using System;
using System.Data;
using System.Globalization;

namespace MixERP.Net.Core.Modules.Finance.Internal
{
    /// <summary>
    ///     Cell rendering for the four Finance report screens whose grid was a plain WebForms
    ///     <c>GridView</c> bound to a <c>DataTable</c> (<c>Reports/BalanceSheet</c>,
    ///     <c>Reports/RetainedEarnings</c>, <c>Reports/CashFlow</c>,
    ///     <c>Reports/ProfitAndLossAccount</c>).
    ///     <para>
    ///         THOSE GRIDS ARE POSITIONAL JAVASCRIPT CONTRACTS - their scripts address cells by
    ///         <c>td:nth-child(n)</c>, strip columns, and read literal cell TEXT (BalanceSheet's
    ///         script tests <c>html().toLowerCase() === "true"</c> on the retained-earning
    ///         column) - so the ported components emit the table HTML the GridView rendered
    ///         rather than hosting <c>MixERPGridView</c>, which would prepend its own
    ///         "ui striped table" classes and format numbers differently. These two helpers are
    ///         what keep the CELL TEXT identical:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="Format" /> reproduces <c>BoundField.DataFormatString</c>, which
    ///             formatted with the CURRENT culture (so "{0:N}" keeps the user's group and
    ///             decimal separators) and rendered <c>DBNull</c> / null as an EMPTY cell.
    ///         </item>
    ///         <item>
    ///             <see cref="Text" /> reproduces an unformatted <c>BoundField</c>: the value's
    ///             own <c>ToString()</c>, which is what produced the literal "True"/"False" the
    ///             BalanceSheet script depends on.
    ///         </item>
    ///     </list>
    /// </summary>
    internal static class GridFormat
    {
        internal static string Text(DataRow row, string column)
        {
            if (row == null || !row.Table.Columns.Contains(column))
            {
                return string.Empty;
            }

            return Text(row[column]);
        }

        internal static string Text(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return value.ToString();
        }

        internal static string Format(DataRow row, string column, string format)
        {
            if (row == null || !row.Table.Columns.Contains(column))
            {
                return string.Empty;
            }

            return Format(row[column], format);
        }

        internal static string Format(object value, string format)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(format))
            {
                return value.ToString();
            }

            return string.Format(CultureInfo.CurrentCulture, format, value);
        }
    }
}
