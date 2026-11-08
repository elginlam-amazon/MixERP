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

namespace MixERP.Net.FrontEnd.Components.Layout
{
    /// <summary>
    ///     The names of the layout sections that replaced the master pages'
    ///     <c>&lt;asp:ContentPlaceHolder&gt;</c>s (checklist tasks 61-64).
    ///     <para>
    ///         Each name is the ORIGINAL <c>ContentPlaceHolder</c> id, so a converted page targets the
    ///         same name its <c>&lt;asp:Content ContentPlaceHolderID="..."&gt;</c> targeted:
    ///     </para>
    ///     <code>
    ///     @* MixERPLayout / BackendLayout *@
    ///     &lt;SectionContent SectionName="@LayoutSections.StyleSheetContent"&gt;
    ///         &lt;link href="/css/pages/my-page.css" rel="stylesheet" /&gt;
    ///     &lt;/SectionContent&gt;
    ///     </code>
    ///     <para>
    ///         A page sets its TITLE and arbitrary head content with <c>&lt;PageTitle&gt;</c> /
    ///         <c>&lt;HeadContent&gt;</c> instead (they reach the <c>&lt;HeadOutlet&gt;</c> of
    ///         <c>App.razor</c>); the section names below are for the stylesheet/script blocks the
    ///         master pages rendered at fixed positions.
    ///     </para>
    /// </summary>
    public static class LayoutSections
    {
        /// <summary>
        ///     <c>MixERPMaster</c> / <c>BackendMaster</c> <c>ScriptContentPlaceholder</c>.
        /// </summary>
        public const string ScriptContent = "ScriptContentPlaceholder";

        /// <summary>
        ///     <c>MixERPMaster</c> / <c>BackendMaster</c> <c>StyleSheetContentPlaceholder</c>.
        /// </summary>
        public const string StyleSheetContent = "StyleSheetContentPlaceholder";

        /// <summary>
        ///     <c>MixERPMaster</c> / <c>BackendMaster</c> <c>BottomScriptContentPlaceholder</c> —
        ///     rendered AFTER the page body, as in the original.
        /// </summary>
        public const string BottomScriptContent = "BottomScriptContentPlaceholder";

        /// <summary>
        ///     <c>MixERPBlankMaster</c> <c>ContentPlaceHolderHead</c>.
        /// </summary>
        public const string BlankHeadContent = "ContentPlaceHolderHead";

        /// <summary>
        ///     <c>MixERPReportMaster</c> <c>head</c>.
        /// </summary>
        public const string ReportHeadContent = "head";
    }
}
