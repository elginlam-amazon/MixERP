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

using System.Text;
using MixERP.Net.Common.Models;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Grid column factory.
    ///     <para>
    ///         SIGNATURE CHANGE: the WebForms <c>GridView</c> / <c>BoundField</c> / <c>ITemplate</c>
    ///         types do not exist on modern .NET. The column model is now
    ///         <see cref="GridViewModel" /> + <see cref="GridColumn" />, which a Blazor grid
    ///         component binds against; the data-format string and the right-align CSS classes are
    ///         carried through unchanged.
    ///     </para>
    /// </summary>
    public static class GridViewHelper
    {
        public static void AddDataBoundControl(GridViewModel grid, string dataField, string headerText, string dataFormatString = "", bool alignRight = false)
        {
            if (grid == null)
            {
                return;
            }

            GridColumn field = new GridColumn();
            field.DataField = dataField;
            field.HeaderText = headerText;
            field.DataFormatString = dataFormatString;

            if (alignRight)
            {
                field.HeaderCssClass = "text right";
                field.ItemCssClass = "text right";
            }

            grid.Columns.Add(field);
        }

        /// <summary>
        ///     The row-select column template: a Semantic-UI toggle checkbox.
        ///     <para>
        ///         SIGNATURE CHANGE: <c>ITemplate.InstantiateIn(Control)</c> became
        ///         <c>InstantiateIn(StringBuilder)</c> — the template appends its markup to the
        ///         supplied accumulator (and returns it) instead of adding controls to a parent.
        ///         The rendered markup is identical to what WebForms produced:
        ///         <c>&lt;div class="ui toggle checkbox"&gt;&lt;input type="checkbox" /&gt;&lt;label&gt;&lt;/label&gt;&lt;/div&gt;</c>.
        ///     </para>
        /// </summary>
        public class GridViewSelectTemplate
        {
            public string InstantiateIn(StringBuilder container)
            {
                string markup = HtmlControlHelper.GetToggleCheckBox("<input type=\"checkbox\" /><label></label>");

                if (container != null)
                {
                    container.Append(markup);
                }

                return markup;
            }
        }
    }
}