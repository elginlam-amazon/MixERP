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
using MixERP.Net.Entities;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace MixERP.Net.WebControls.StockTransactionViewFactory.Helpers
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     Builds the StockTransactionView grid's column set.
    ///     <para>
    ///         SIGNATURE CHANGE: the first parameter was a
    ///         <c>System.Web.UI.WebControls.GridView</c> whose <c>Columns</c> collection of
    ///         <c>DataControlField</c>/<c>TemplateField</c> entries was mutated. Both types are
    ///         removed from modern .NET, so the collection is now the already-migrated
    ///         <see cref="MixERPGridViewColumn" /> model
    ///         (<c>MixERP.Net.WebControls.Common</c>) that <c>MixERPGridView.razor</c> binds
    ///         against. The method NAME, the column ORDER and every header caption are
    ///         unchanged.
    ///     </para>
    ///     <para>
    ///         *** LOAD-BEARING COLUMN POSITIONS ***
    ///         <c>StockTransactionView.js</c> <c>getSelectedItems()</c> reads
    ///         <c>checkBoxColumnPosition = "2"</c> and <c>idColumnPosition = "3"</c> — the
    ///         Select column and the Id column by 1-based INDEX (the same positions the former
    ///         <c>RowDataBound</c> handler reached through <c>e.Row.Cells[1]</c>/<c>Cells[2]</c>).
    ///         Reordering these columns compiles green and silently breaks every
    ///         selection / merge / flag feature. The trailing
    ///         <c>FlagBackgroundColor</c> / <c>FlagForegroundColor</c> columns are likewise read
    ///         by <c>updateFlagColor()</c> / <c>createFlaggedRows(grid)</c> and must keep being
    ///         rendered.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public static class GridViewColumnHelper
    {
        /// <summary>
        ///     1-based position of the select-checkbox column, as hard-coded in
        ///     StockTransactionView.js (<c>checkBoxColumnPosition</c>).
        /// </summary>
        public const int CheckBoxColumnPosition = 2;

        /// <summary>
        ///     1-based position of the Id column, as hard-coded in StockTransactionView.js
        ///     (<c>idColumnPosition</c>).
        /// </summary>
        public const int IdColumnPosition = 3;

        /// <summary>
        ///     Populates <paramref name="grid" /> with the view's columns, in the original
        ///     order. The Actions column gets the static icon template; use the overload below
        ///     to supply the per-row preview/checklist urls.
        /// </summary>
        public static void AddColumns(IList<MixERPGridViewColumn> grid, SubTranBook book)
        {
            AddColumns(grid, book, null, null);
        }

        /// <summary>
        ///     Populates <paramref name="grid" /> with the view's columns, in the original
        ///     order.
        /// </summary>
        /// <param name="grid">The column collection to fill (formerly <c>GridView.Columns</c>).</param>
        /// <param name="book">The sub transaction book; <c>Receipt</c> suppresses the PriceType and book columns, as before.</param>
        /// <param name="previewUrlProvider">
        ///     Supplies the row's resolved preview url — the former
        ///     <c>ProductViewGridView_RowDataBound</c> logic
        ///     (<c>PreviewUrl + "?TranId=" + id</c>, resolved). Return null/blank for no print
        ///     action on that row. The COMPONENT owns this per-row url logic; this helper owns
        ///     the static icon/title/order markup.
        /// </param>
        /// <param name="checklistUrlProvider">
        ///     Supplies the row's resolved checklist url (<c>ChecklistUrl + "?TranId=" + id</c>).
        /// </param>
        public static void AddColumns(IList<MixERPGridViewColumn> grid, SubTranBook book, Func<object, string> previewUrlProvider, Func<object, string> checklistUrlProvider)
        {
            if (grid == null)
            {
                return;
            }

            grid.Clear();

            AddTemplateFields(grid, previewUrlProvider, checklistUrlProvider);

            AddDataBoundColumn(grid, "Id", Titles.Id);
            AddDataBoundColumn(grid, "ValueDate", Titles.ValueDate, "{0:d}");
            AddDataBoundColumn(grid, "Office", Titles.Office);
            AddDataBoundColumn(grid, "ReferenceNumber", Titles.ReferenceNumber);
            AddDataBoundColumn(grid, "Party", Titles.Party);

            if (book != SubTranBook.Receipt)
            {
                AddDataBoundColumn(grid, "PriceType", Titles.PriceType);
            }

            AddDataBoundColumn(grid, "Amount", Titles.Amount);
            AddDataBoundColumn(grid, "TransactionTs", Titles.TransactionTimestamp, "{0:d}");
            AddDataBoundColumn(grid, "User", Titles.User);
            AddDataBoundColumn(grid, "StatementReference", Titles.StatementReference);

            if (book != SubTranBook.Receipt)
            {
                AddDataBoundColumn(grid, "book", Titles.Book);
            }

            //Read client-side by updateFlagColor()/createFlaggedRows() in StockTransactionView.js.
            AddDataBoundColumn(grid, "FlagBackgroundColor", Titles.FlagBackgroundColor);
            AddDataBoundColumn(grid, "FlagForegroundColor", Titles.FlagForegroundColor);
        }

        /// <summary>
        ///     Replaces <c>GridViewHelper.AddDataBoundControl(grid, …)</c>, whose first parameter
        ///     was a removed <c>GridView</c>. The optional <c>dataFormatString</c> had no home on
        ///     <see cref="MixERPGridViewColumn" />, so a format string is honoured through a cell
        ///     template that applies it with <see cref="string.Format(IFormatProvider, string, object)" />
        ///     in the current culture — which is exactly what <c>BoundField.DataFormatString</c>
        ///     did.
        /// </summary>
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

        /// <summary>
        ///     Reads a row's field, supporting both of the shapes <c>MixERPGridView.DataSource</c>
        ///     accepts (a <see cref="DataRow" /> or an entity).
        /// </summary>
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

        /// <summary>
        ///     Port of the former <c>AddTemplateFields(GridView)</c>: the Actions column (header
        ///     width 90) followed by the Select column. ORDER IS A JS CONTRACT — see the type
        ///     remarks.
        /// </summary>
        private static void AddTemplateFields(IList<MixERPGridViewColumn> grid, Func<object, string> previewUrlProvider, Func<object, string> checklistUrlProvider)
        {
            MixERPGridViewColumn actionColumn = new MixERPGridViewColumn();
            actionColumn.HeaderText = Titles.Actions;

            //actionTemplateField.HeaderStyle.Width = 90; — a Unit-typed WebForms style property.
            //The rendered result was an inline width on the header cell, so it is expressed as
            //the header cell's css width here.
            actionColumn.HeaderCssClass = ActionColumnHeaderCssClass;
            actionColumn.Template = StockTransactionViewActionTemplate.Create(previewUrlProvider, checklistUrlProvider);

            grid.Add(actionColumn);

            MixERPGridViewColumn checkBoxColumn = new MixERPGridViewColumn();
            checkBoxColumn.HeaderText = Titles.Select;
            checkBoxColumn.Template = StockTransactionViewSelectTemplate.Create();

            grid.Add(checkBoxColumn);
        }

        /// <summary>
        ///     The former <c>HeaderStyle.Width = 90</c> of the Actions column, as a css class the
        ///     suite's stylesheet sizes. Kept public so the hosting component can emit the
        ///     matching rule / inline style.
        /// </summary>
        public const string ActionColumnHeaderCssClass = "actions-header";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The Actions cell: the checklist, print and go-to-top anchors, in that order.
        ///     <para>
        ///         SIGNATURE CHANGE: was an <c>ITemplate</c> adding <c>HtmlAnchor</c> /
        ///         <c>HtmlGenericControl</c> instances to a container. Both the interface and the
        ///         controls are removed, so the template is a
        ///         <see cref="RenderFragment{TValue}" /> over the row item — which is also what
        ///         lets the per-row hrefs be supplied, work the former
        ///         <c>ProductViewGridView_RowDataBound</c> did by digging the anchors back out of
        ///         <c>e.Row.Cells[0]</c> with <c>FindControl</c>.
        ///     </para>
        ///     <para>
        ///         The ids <c>ChecklistAnchor</c> and <c>PrintAnchor</c> are a JS/row-wiring
        ///         contract and are emitted literally. <c>ClientIDMode.Predictable</c> is dropped:
        ///         there is no naming container to mangle the ids any more.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static class StockTransactionViewActionTemplate
        {
            /// <summary>
            ///     Builds the Actions cell template.
            /// </summary>
            /// <param name="previewUrlProvider">
            ///     Row =&gt; resolved preview url. When it returns a non-blank value the print
            ///     anchor gets the original
            ///     <c>onclick="showWindow('…');return false;"</c> behaviour; when it returns
            ///     null/blank the anchor renders without it, exactly as the original left it
            ///     unwired for a blank <c>PreviewUrl</c>.
            /// </param>
            /// <param name="checklistUrlProvider">Row =&gt; resolved checklist url, set as the checklist anchor's href.</param>
            public static RenderFragment<object> Create(Func<object, string> previewUrlProvider, Func<object, string> checklistUrlProvider)
            {
                return row => builder => Render(builder, row, previewUrlProvider, checklistUrlProvider);
            }

            private static void Render(RenderTreeBuilder builder, object row, Func<object, string> previewUrlProvider, Func<object, string> checklistUrlProvider)
            {
                string checklistUrl = checklistUrlProvider == null ? null : checklistUrlProvider(row);
                string previewUrl = previewUrlProvider == null ? null : previewUrlProvider(row);

                //Checklist anchor.
                builder.OpenElement(0, "a");
                builder.AddAttribute(1, "id", "ChecklistAnchor");
                builder.AddAttribute(2, "title", Labels.GoToChecklistWindow);

                if (!string.IsNullOrWhiteSpace(checklistUrl))
                {
                    builder.AddAttribute(3, "href", checklistUrl);
                }

                builder.OpenElement(4, "i");
                builder.AddAttribute(5, "class", "list icon");
                builder.CloseElement();
                builder.CloseElement();

                //Print anchor.
                builder.OpenElement(6, "a");
                builder.AddAttribute(7, "id", "PrintAnchor");
                builder.AddAttribute(8, "title", Titles.Print);

                if (!string.IsNullOrWhiteSpace(previewUrl))
                {
                    builder.AddAttribute(9, "onclick", "showWindow('" + previewUrl + "');return false;");
                }

                builder.OpenElement(10, "i");
                builder.AddAttribute(11, "class", "print icon");
                builder.CloseElement();
                builder.CloseElement();

                //Go-to-top anchor. It carried no id in the original.
                builder.OpenElement(12, "a");
                builder.AddAttribute(13, "title", Labels.GoToTop);
                builder.AddAttribute(14, "onclick", "window.scroll(0);");

                builder.OpenElement(15, "i");
                builder.AddAttribute(16, "class", "arrow up icon");
                builder.CloseElement();
                builder.CloseElement();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     The Select cell: the Semantic-UI toggle checkbox.
        ///     <para>
        ///         SIGNATURE CHANGE: was an <c>ITemplate</c> building a
        ///         <c>div</c> + <c>HtmlInputCheckBox</c> + empty <c>label</c> control tree. The
        ///         markup is byte-identical, produced through the already-migrated
        ///         <c>HtmlControlHelper.GetToggleCheckBox(innerHtml)</c>. The trailing empty
        ///         <c>&lt;label&gt;</c> is REQUIRED by Semantic UI — without it the toggle does
        ///         not render.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static class StockTransactionViewSelectTemplate
        {
            /// <summary>The id <c>SelectCheckBox</c> is read by the suite's selection scripts.</summary>
            private static readonly string Markup =
                HtmlControlHelper.GetToggleCheckBox("<input type=\"checkbox\" id=\"SelectCheckBox\" /><label></label>");

            public static RenderFragment<object> Create()
            {
                return row => builder => builder.AddContent(0, (MarkupString)Markup);
            }
        }
    }
}
