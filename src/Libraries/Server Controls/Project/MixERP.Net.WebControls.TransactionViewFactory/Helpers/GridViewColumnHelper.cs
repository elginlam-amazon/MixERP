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
using Microsoft.AspNetCore.Components.Rendering;
using MixERP.Net.Common.Helpers;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace MixERP.Net.WebControls.TransactionViewFactory.Helpers
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the TransactionView grid's column set.
    ///     <para>
    ///         SIGNATURE CHANGE: <c>AddColumns</c> took a
    ///         <c>System.Web.UI.WebControls.GridView</c> and mutated its <c>Columns</c>
    ///         collection of <c>DataControlField</c>/<c>TemplateField</c> entries. All of those
    ///         types are removed from modern .NET, so the collection is now the
    ///         already-migrated <see cref="MixERPGridViewColumn" /> model
    ///         (<c>MixERP.Net.WebControls.Common</c>) that <c>MixERPGridView.razor</c> binds
    ///         against. The method NAME, the column ORDER and every header caption are
    ///         unchanged. <c>grid.Columns.Clear()</c> becomes <c>grid.Clear()</c> on the list.
    ///     </para>
    ///     <para>
    ///         *** LOAD-BEARING COLUMN POSITIONS ***
    ///         <c>TransactionViewFactory.js</c> hard-codes
    ///         <c>checkBoxColumnPosition = "2"</c> and <c>idColumnPosition = "3"</c>
    ///         (1-based) in <c>getSelectedItems()</c>, reads the Book cell as
    ///         <c>row.find("td:nth-child(5)")</c> in <c>updatePreviewButtons()</c>, reaches the
    ///         action icons through <c>td:first-child</c>, resolves the row id from
    ///         <c>td:nth-child(3)</c> in <c>showCheckList/showPreview/showStockDetail</c>, and
    ///         <c>createFlaggedRows(grid)</c> reads the TRAILING <c>FlagBg</c>/<c>FlagFg</c>
    ///         columns. The order below satisfies all of those:
    ///         1 Actions, 2 Select, 3 Id, 4 TranCode, 5 Book, … , last-1 FlagBg, last FlagFg.
    ///         Reordering or dropping any column compiles green and silently kills selection,
    ///         flagging, print and the preview-button visibility rules.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static class GridViewColumnHelper
    {
        /// <summary>
        ///     1-based position of the select-checkbox column, as hard-coded in
        ///     TransactionViewFactory.js (<c>checkBoxColumnPosition</c>).
        /// </summary>
        internal const int CheckBoxColumnPosition = 2;

        /// <summary>
        ///     1-based position of the Id column, as hard-coded in TransactionViewFactory.js
        ///     (<c>idColumnPosition</c>, and the <c>td:nth-child(3)</c> row-id lookups).
        /// </summary>
        internal const int IdColumnPosition = 3;

        /// <summary>
        ///     1-based position of the Book column, read by <c>updatePreviewButtons()</c> as
        ///     <c>td:nth-child(5)</c>.
        /// </summary>
        internal const int BookColumnPosition = 5;

        /// <summary>
        ///     Populates <paramref name="grid" /> with the journal view's columns, in the
        ///     original order.
        /// </summary>
        /// <param name="grid">The column collection to fill (formerly <c>GridView.Columns</c>).</param>
        internal static void AddColumns(IList<MixERPGridViewColumn> grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.Clear();

            AddTemplateFields(grid);

            AddDataBoundColumn(grid, "TransactionMasterId", Titles.Id);
            AddDataBoundColumn(grid, "TransactionCode", Titles.TranCode);
            AddDataBoundColumn(grid, "Book", Titles.Book);
            AddDataBoundColumn(grid, "ValueDate", Titles.ValueDate, "{0:d}");
            AddDataBoundColumn(grid, "ReferenceNumber", Titles.ReferenceNumber);
            AddDataBoundColumn(grid, "StatementReference", Titles.StatementReference);

            AddDataBoundColumn(grid, "PostedBy", Titles.PostedBy);
            AddDataBoundColumn(grid, "Office", Titles.Office);

            AddDataBoundColumn(grid, "Status", Titles.Status);
            AddDataBoundColumn(grid, "VerifiedBy", Titles.VerifiedBy);
            AddDataBoundColumn(grid, "VerifiedOn", Titles.VerifiedOn, "{0:g}");
            AddDataBoundColumn(grid, "Reason", Titles.Reason);
            AddDataBoundColumn(grid, "TransactionTs", Titles.TransactionTimestamp, "{0:g}");

            //The two trailing flag-colour columns are read client-side by
            //updateFlagColor()/createFlaggedRows() in TransactionViewFactory.js. Their header
            //captions were LITERAL strings in the original - not Titles.* resources - and are
            //kept literal so the client contract and the rendered header are unchanged.
            AddDataBoundColumn(grid, "FlagBg", "FlagBg");
            AddDataBoundColumn(grid, "FlagFg", "FlagFg");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Replaces <c>GridViewHelper.AddDataBoundControl(grid, …)</c>, whose first parameter
        ///     was a removed <c>GridView</c>. (The already-migrated
        ///     <c>MixERP.Net.Common.Helpers.GridViewHelper.AddDataBoundControl</c> now fills a
        ///     <c>GridViewModel</c>/<c>GridColumn</c>, which is a different grid model from the
        ///     <see cref="MixERPGridView" /> this control renders, so the column is built here.)
        ///     <para>
        ///         The optional <c>dataFormatString</c> has no home on
        ///         <see cref="MixERPGridViewColumn" />, so a format string is honoured through a
        ///         cell template that applies it with
        ///         <see cref="string.Format(IFormatProvider, string, object)" /> in the current
        ///         culture — which is exactly what <c>BoundField.DataFormatString</c> did.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void AddDataBoundColumn(IList<MixERPGridViewColumn> grid, string dataField, string headerText, string dataFormatString = "")
        {
            MixERPGridViewColumn column = new MixERPGridViewColumn(dataField, headerText, typeof(string));

            if (!string.IsNullOrEmpty(dataFormatString))
            {
                string field = dataField;
                string format = dataFormatString;

                column.Template = row => builder =>
                {
                    builder.AddContent(0, FormatValue(GetValue(row, field), format));
                };
            }

            grid.Add(column);
        }

        private static string FormatValue(object value, string format)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.CurrentCulture, format, value);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Reads a row's field, supporting both of the shapes
        ///     <c>MixERPGridView.DataSource</c> accepts (a <see cref="DataRow" /> or an entity —
        ///     this control supplies <c>IEnumerable&lt;DbGetJournalViewResult&gt;</c>).
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static object GetValue(object row, string dataField)
        {
            if (row == null || string.IsNullOrEmpty(dataField))
            {
                return null;
            }

            DataRow dataRow = row as DataRow;

            if (dataRow != null)
            {
                return dataRow.Table.Columns.Contains(dataField) ? dataRow[dataField] : null;
            }

            System.Reflection.PropertyInfo property = row.GetType().GetProperty(dataField);

            return property == null ? null : property.GetValue(row, null);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of the former <c>AddTemplateFields(GridView)</c>: the Actions column
        ///     followed by the Select column. ORDER IS A JS CONTRACT — see the type remarks.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void AddTemplateFields(IList<MixERPGridViewColumn> grid)
        {
            MixERPGridViewColumn actionColumn = new MixERPGridViewColumn();
            actionColumn.HeaderText = Titles.Actions;
            actionColumn.Template = JournalViewActionTemplate.Create();

            grid.Add(actionColumn);

            MixERPGridViewColumn checkBoxColumn = new MixERPGridViewColumn();
            checkBoxColumn.HeaderText = Titles.Select;
            checkBoxColumn.Template = JournalViewSelectTemplate.Create();

            grid.Add(checkBoxColumn);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The Actions cell: the checklist, print (GL advice), stock-detail and go-to-top
        ///     icons, in that order.
        ///     <para>
        ///         SIGNATURE CHANGE: was an <c>ITemplate</c> adding four
        ///         <c>HtmlGenericControl("i")</c> instances to a container. Both the interface
        ///         and the control type are removed, so the template is a
        ///         <see cref="RenderFragment{TValue}" /> over the row item.
        ///     </para>
        ///     <para>
        ///         Every inline <c>onclick</c> is emitted VERBATIM. The three named handlers
        ///         live in <c>TransactionViewFactory.js</c> and reach the row's id through
        ///         <c>$(element).parent().parent().find("td:nth-child(3)")</c>, so the icons
        ///         must stay inside the FIRST cell (<c>updatePreviewButtons()</c> also finds
        ///         <c>.icon.print</c> / <c>.icon.grid.layout</c> through
        ///         <c>td:first-child</c>) and the Id must stay in cell 3.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static class JournalViewActionTemplate
        {
            internal static RenderFragment<object> Create()
            {
                return row => builder => Render(builder);
            }

            private static void Render(RenderTreeBuilder builder)
            {
                //Checklist icon.
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "icon list layout");
                builder.AddAttribute(2, "onclick", "showCheckList(this);");
                builder.CloseElement();

                //Preview (GL advice report) icon - updatePreviewButtons() hides/shows this.
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "icon print");
                builder.AddAttribute(5, "onclick", "showPreview(this);");
                builder.CloseElement();

                //Stock detail icon - updatePreviewButtons() hides/shows this.
                builder.OpenElement(6, "i");
                builder.AddAttribute(7, "class", "icon grid layout");
                builder.AddAttribute(8, "onclick", "showStockDetail(this);");
                builder.CloseElement();

                //Go to top icon.
                builder.OpenElement(9, "i");
                builder.AddAttribute(10, "class", "icon chevron circle up");
                builder.AddAttribute(11, "onclick", "window.scroll(0);");
                builder.CloseElement();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The Select cell: the Semantic-UI toggle checkbox.
        ///     <para>
        ///         SIGNATURE CHANGE: was <c>new GridViewHelper.GridViewSelectTemplate()</c>
        ///         assigned to a <c>TemplateField.ItemTemplate</c>. That helper still exists and
        ///         still owns the markup, but its <c>InstantiateIn</c> now appends to a
        ///         <see cref="StringBuilder" /> and returns the markup instead of adding
        ///         controls to a parent (see
        ///         <c>MixERP.Net.Common.Helpers.GridViewHelper.GridViewSelectTemplate</c>), so it
        ///         is called once here and rendered as a <see cref="MarkupString" />.
        ///     </para>
        ///     <para>
        ///         The produced markup is byte-identical to what WebForms emitted:
        ///         <c>&lt;div class="ui toggle checkbox"&gt;&lt;input type="checkbox" /&gt;&lt;label&gt;&lt;/label&gt;&lt;/div&gt;</c>.
        ///         <c>getSelectedItems()</c> finds the checkbox positionally (column 2), and the
        ///         row-click handler finds it as <c>td input:checkbox</c> — neither needs an id.
        ///         The trailing empty <c>&lt;label&gt;</c> is REQUIRED by Semantic UI.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static class JournalViewSelectTemplate
        {
            private static readonly string Markup =
                new GridViewHelper.GridViewSelectTemplate().InstantiateIn(new StringBuilder());

            internal static RenderFragment<object> Create()
            {
                return row => builder => builder.AddContent(0, (MarkupString)Markup);
            }
        }
    }
}
