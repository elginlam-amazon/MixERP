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

namespace MixERP.Net.FrontEnd.Base
{
    /// <summary>
    ///     The legacy type name, kept so the many <c>: MixERPWebpage</c> declarations keep resolving
    ///     while the pages are converted. The whole implementation moved to
    ///     <see cref="MixERPAuthorizedPageBase" /> / <see cref="MixERPPageBase" />
    ///     (<c>Components/Base/</c>), which are Blazor component bases; nothing of
    ///     <c>System.Web</c>, <c>FormsAuthentication</c> or <c>HttpContext.Current</c> remains.
    ///     <para>
    ///         New and converted pages should inherit <see cref="MixERPAuthorizedPageBase" /> (or
    ///         <see cref="MixERPPageBase" /> with <c>SkipLoginCheck = true</c> for the anonymous SignIn
    ///         and Install pages) directly.
    ///     </para>
    /// </summary>
    public class MixERPWebpage : MixERPAuthorizedPageBase
    {
    }
}
