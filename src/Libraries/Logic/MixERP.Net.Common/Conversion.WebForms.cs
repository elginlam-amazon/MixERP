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

// Web Forms only part of Conversion. System.Web.UI.WebControls.Unit has no modern .NET
// equivalent because Web Forms was never ported, so this file is kept as source for the
// per-page Razor re-authoring tracked in summary.md and is excluded from the net10.0
// compile. See knowledge base doc 25.

using System.Globalization;
using System.Web.UI.WebControls;

namespace MixERP.Net.Common
{
    public static partial class Conversion
    {
        public static Unit TryCastUnit(object value)
        {
            if (value == null)
            {
                return new Unit();
            }

            if (value is Unit)
            {
                return (Unit) value;
            }


            return Unit.Parse(value.ToString(), CultureInfo.DefaultThreadCurrentCulture);
        }
    }
}
