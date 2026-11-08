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

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     Replacement for <c>System.Web.UI.ValidateRequestMode</c>, which does not exist in ASP.NET Core.
    ///     ASP.NET Core performs no implicit request validation, so this value is advisory: components that
    ///     accept markup-bearing input inspect it to decide whether to HTML-encode or sanitize the value.
    /// </summary>
    public enum MixERPValidateRequestMode
    {
        /// <summary>
        ///     Inherit the containing page's/parent component's setting (the original default).
        /// </summary>
        Inherit = 0,

        /// <summary>
        ///     Reject/encode potentially dangerous markup in the control's input.
        /// </summary>
        Enabled = 1,

        /// <summary>
        ///     Accept raw markup in the control's input.
        /// </summary>
        Disabled = 2
    }
}
