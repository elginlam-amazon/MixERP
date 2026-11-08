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

using System.Collections.Generic;
using System.Globalization;

namespace MixERP.Net.Common.Models
{
    /// <summary>
    ///     Framework-neutral replacement for the WebForms <c>GridView</c> column model.
    ///     A Blazor grid component binds against <see cref="Columns" />: each
    ///     <see cref="GridColumn" /> names the bound data field, its header text, the
    ///     format string to apply to the cell value and the CSS classes to render on the
    ///     header and the data cell.
    /// </summary>
    public class GridViewModel
    {
        private readonly IList<GridColumn> columns = new List<GridColumn>();

        public IList<GridColumn> Columns
        {
            get { return this.columns; }
        }
    }

    /// <summary>
    ///     Replacement for <c>System.Web.UI.WebControls.BoundField</c>: a single data-bound
    ///     grid column.
    /// </summary>
    public class GridColumn
    {
        /// <summary>
        ///     The property/column name this grid column is bound to (old <c>BoundField.DataField</c>).
        /// </summary>
        public string DataField { get; set; }

        /// <summary>
        ///     The column header caption (old <c>BoundField.HeaderText</c>).
        /// </summary>
        public string HeaderText { get; set; }

        /// <summary>
        ///     A composite format string applied to the cell value
        ///     (old <c>BoundField.DataFormatString</c>, for example <c>{0:N2}</c>).
        /// </summary>
        public string DataFormatString { get; set; }

        /// <summary>
        ///     CSS class rendered on the header cell (old <c>HeaderStyle.CssClass</c>).
        /// </summary>
        public string HeaderCssClass { get; set; }

        /// <summary>
        ///     CSS class rendered on the data cell (old <c>ItemStyle.CssClass</c>).
        /// </summary>
        public string ItemCssClass { get; set; }

        /// <summary>
        ///     Applies <see cref="DataFormatString" /> to a cell value, reproducing the
        ///     formatting WebForms performed when rendering a <c>BoundField</c>.
        /// </summary>
        public string FormatValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(this.DataFormatString))
            {
                return value.ToString();
            }

            return string.Format(CultureInfo.CurrentCulture, this.DataFormatString, value);
        }
    }

    /// <summary>
    ///     Framework-neutral replacement for <c>System.Web.UI.WebControls.ListControl</c>
    ///     (drop-down list / select element) used by
    ///     <see cref="MixERP.Net.Common.Helpers.DropDownListHelper" />. A Blazor
    ///     <c>&lt;select&gt;</c> binds against <see cref="Items" />.
    /// </summary>
    public class SelectList
    {
        private readonly IList<SelectListItem> items = new List<SelectListItem>();

        public IList<SelectListItem> Items
        {
            get { return this.items; }
        }

        /// <summary>
        ///     Deselects every item (old <c>ListControl.ClearSelection()</c>).
        /// </summary>
        public void ClearSelection()
        {
            foreach (SelectListItem item in this.items)
            {
                item.Selected = false;
            }
        }

        /// <summary>
        ///     The value of the first selected item, or an empty string when nothing is selected
        ///     (old <c>ListControl.SelectedValue</c>).
        /// </summary>
        public string SelectedValue
        {
            get
            {
                foreach (SelectListItem item in this.items)
                {
                    if (item.Selected)
                    {
                        return item.Value;
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        ///     Finds an item by its value, or null (old <c>ListItemCollection.FindByValue</c>).
        /// </summary>
        public SelectListItem FindByValue(string value)
        {
            foreach (SelectListItem item in this.items)
            {
                if (string.Equals(item.Value, value, System.StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }
    }

    /// <summary>
    ///     Replacement for <c>System.Web.UI.WebControls.ListItem</c>.
    /// </summary>
    public class SelectListItem
    {
        public SelectListItem()
        {
        }

        public SelectListItem(string text, string value)
        {
            this.Text = text;
            this.Value = value;
        }

        public string Text { get; set; }

        public string Value { get; set; }

        public bool Selected { get; set; }
    }

    /// <summary>
    ///     Minimal honest replacement for the WebForms control tree used by
    ///     <see cref="MixERP.Net.Common.PageUtility.FindControlIterative" />. Blazor has no
    ///     server-side control tree, so a component (or a markup builder) that wants the old
    ///     "find a descendant by id" behaviour exposes its own DOM id and children through
    ///     this interface.
    /// </summary>
    public interface IMixERPControl
    {
        /// <summary>
        ///     The DOM id of this element/component.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     The child elements/components of this element/component. Never null.
        /// </summary>
        IEnumerable<IMixERPControl> Children { get; }
    }
}
