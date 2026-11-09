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
using System.Globalization;

namespace MixERP.Net.Common.Helpers
{
    public static class HtmlControlHelper
    {
        // Stubs for WebForms HTML helper methods (originally returned HtmlGenericControl)
        public static dynamic GetSegment() => null;
        public static dynamic GetSegment(string cssClass) => null;
        public static dynamic GetForm() => null;
        public static dynamic GetFields(string cssClass = "") => null;
        public static dynamic GetField(string cssClass = "") => null;
        public static dynamic GetLabel(string text, string targetId = "") => null;
        public static dynamic GetDivider() => null;
        public static dynamic GetFormSegment() => null;
        public static dynamic GetFormSegment(string cssClass) => null;
        public static dynamic GetIcon(string cssClass) => null;
        public static dynamic GetInlineFields() => null;
        public static dynamic GetPageHeader(string text) => null;
        public static void AddListItem(dynamic container, string label, object value) { }
        public static dynamic GetModal() => null;
        public static dynamic GetModal(string cssClass, string id) => null;
        public static dynamic GetLeftIconInput() => null;
        public static dynamic GetLeftIconInput(string iconClass, string inputType, string id) => null;
        public static dynamic GetLeftIconInput(string iconClass, string inputType, string id, string cssClass) => null;
        public static dynamic GetModalHeader(string title) => null;
        public static dynamic GetModalHeader(string title, string iconCssClass) => null;

        public static string GetLabelHtml(string text)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label>{0}</label>", text);
        }

        public static string GetLabelHtml(string text, string targetControlId)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label for='{1}'>{0}</label>", text, targetControlId);
        }
    }
}
