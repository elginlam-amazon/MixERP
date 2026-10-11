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

namespace MixERP.Net.Common.Helpers
{
    public static class ExpressionHelper
    {
        /// <summary>
        ///     Evaluates an arithmetic/logical expression and returns the result as string (empty when invalid).
        ///     Runtime C# compilation (CodeDom) is not supported on modern .NET, so expressions are evaluated
        ///     with the DataColumn expression engine, which also avoids executing arbitrary code.
        /// </summary>
        public static string Eval(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return string.Empty;
            }

            try
            {
                using (DataTable table = new DataTable {Locale = CultureInfo.InvariantCulture})
                {
                    object output = table.Compute(expression, string.Empty);
                    return Convert.ToString(output, CultureInfo.InvariantCulture);
                }
            }
            catch (EvaluateException)
            {
                return string.Empty;
            }
            catch (SyntaxErrorException)
            {
                return string.Empty;
            }
        }
    }
}
