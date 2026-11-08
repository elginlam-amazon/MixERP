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

using Microsoft.AspNetCore.Components;
using MixERP.Net.FrontEnd.Components.Layout;

namespace MixERP.Net.FrontEnd.Base
{
    /// <summary>
    ///     The Blazor base of every web report page (checklist task 70). It was an EMPTY subclass of
    ///     <c>MixERPWebpage</c> in the original — it existed purely as the marker type that said "this
    ///     page is a report", and that is exactly what it still is, so no member was added.
    ///     <para>
    ///         The one behaviour it gains is the layout: WebForms report pages declared
    ///         <c>MasterPageFile="~/MixERPReportMaster.Master"</c> individually, and the
    ///         <see cref="LayoutAttribute" /> here is the Blazor equivalent — it is INHERITED, so every
    ///         page deriving from this class renders inside <see cref="ReportLayout" /> without
    ///         repeating <c>@layout</c>. A report page that needs a different layout still overrides it
    ///         with its own <c>@layout</c> directive.
    ///     </para>
    ///     <para>
    ///         The login/authorization chain is unchanged: <c>MixERPWebpage</c> is
    ///         <c>MixERPAuthorizedPageBase</c>, which carries <c>[Authorize]</c>.
    ///     </para>
    /// </summary>
    [Layout(typeof(ReportLayout))]
    public class MixERPWebReportPage : MixERPWebpage
    {
    }
}
