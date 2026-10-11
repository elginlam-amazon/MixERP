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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using MixERP.Net.Web.UI.HtmlControls;

namespace MixERP.Net.Web.UI.WebControls
{
    public enum TableRowSection
    {
        TableBody = 0,
        TableHeader = 1,
        TableFooter = 2
    }

    public enum GridLines
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = 3
    }

    public class TableCell : WebControl
    {
        public TableCell() : base(HtmlTextWriterTag.Td)
        {
        }

        protected TableCell(HtmlTextWriterTag tag) : base(tag)
        {
        }

        public virtual string Text { get; set; } = string.Empty;
        public virtual int ColumnSpan { get; set; }
        public virtual int RowSpan { get; set; }
        public virtual HorizontalAlign HorizontalAlign { get; set; }
        public virtual VerticalAlign VerticalAlign { get; set; }
        public virtual bool Wrap { get; set; } = true;

        internal TableItemStyle ItemStyle { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (this.ColumnSpan > 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, this.ColumnSpan.ToString(CultureInfo.InvariantCulture));
            }

            if (this.RowSpan > 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Rowspan, this.RowSpan.ToString(CultureInfo.InvariantCulture));
            }

            if (this.HorizontalAlign != HorizontalAlign.NotSet)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Align, this.HorizontalAlign.ToString().ToLowerInvariant());
            }

            if (this.VerticalAlign != VerticalAlign.NotSet)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, this.VerticalAlign.ToString().ToLowerInvariant());
            }

            if (!this.Wrap)
            {
                this.Style["white-space"] = "nowrap";
            }

            if (this.ItemStyle != null && string.IsNullOrWhiteSpace(this.CssClass) && !string.IsNullOrWhiteSpace(this.ItemStyle.CssClass))
            {
                this.CssClass = this.ItemStyle.CssClass;
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (this.HasControls())
            {
                base.RenderContents(writer);
                return;
            }

            writer.Write(this.Text);
        }
    }

    public class TableHeaderCell : TableCell
    {
        public TableHeaderCell() : base(HtmlTextWriterTag.Th)
        {
        }

        public virtual string Scope { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.Scope))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Scope, this.Scope);
            }

            base.AddAttributesToRender(writer);
        }
    }

    public sealed class TableCellCollection : IEnumerable<TableCell>
    {
        private readonly TableRow owner;

        internal TableCellCollection(TableRow owner)
        {
            this.owner = owner;
        }

        public int Count => this.owner.Controls.OfType<TableCell>().Count();
        public TableCell this[int index] => this.owner.Controls.OfType<TableCell>().ElementAt(index);

        public int Add(TableCell cell)
        {
            this.owner.Controls.Add(cell);
            return this.Count - 1;
        }

        public void AddAt(int index, TableCell cell)
        {
            this.owner.Controls.AddAt(index, cell);
        }

        public void AddRange(TableCell[] cells)
        {
            foreach (TableCell cell in cells)
            {
                this.Add(cell);
            }
        }

        public void Remove(TableCell cell)
        {
            this.owner.Controls.Remove(cell);
        }

        public void RemoveAt(int index)
        {
            this.owner.Controls.Remove(this[index]);
        }

        public void Clear()
        {
            this.owner.Controls.Clear();
        }

        public int GetCellIndex(TableCell cell)
        {
            return this.owner.Controls.OfType<TableCell>().ToList().IndexOf(cell);
        }

        public IEnumerator<TableCell> GetEnumerator()
        {
            return this.owner.Controls.OfType<TableCell>().ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class TableRow : WebControl
    {
        public TableRow() : base(HtmlTextWriterTag.Tr)
        {
            this.Cells = new TableCellCollection(this);
        }

        public virtual TableCellCollection Cells { get; }
        public virtual TableRowSection TableSection { get; set; }
        public virtual HorizontalAlign HorizontalAlign { get; set; }
        public virtual VerticalAlign VerticalAlign { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (this.HorizontalAlign != HorizontalAlign.NotSet)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Align, this.HorizontalAlign.ToString().ToLowerInvariant());
            }

            if (this.VerticalAlign != VerticalAlign.NotSet)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, this.VerticalAlign.ToString().ToLowerInvariant());
            }

            base.AddAttributesToRender(writer);
        }
    }

    public class TableHeaderRow : TableRow
    {
        public TableHeaderRow()
        {
            this.TableSection = TableRowSection.TableHeader;
        }
    }

    public class TableFooterRow : TableRow
    {
        public TableFooterRow()
        {
            this.TableSection = TableRowSection.TableFooter;
        }
    }

    public sealed class TableRowCollection : IEnumerable<TableRow>
    {
        private readonly Table owner;

        internal TableRowCollection(Table owner)
        {
            this.owner = owner;
        }

        public int Count => this.owner.Controls.OfType<TableRow>().Count();
        public TableRow this[int index] => this.owner.Controls.OfType<TableRow>().ElementAt(index);

        public int Add(TableRow row)
        {
            this.owner.Controls.Add(row);
            return this.Count - 1;
        }

        public void AddAt(int index, TableRow row)
        {
            this.owner.Controls.AddAt(index, row);
        }

        public void AddRange(TableRow[] rows)
        {
            foreach (TableRow row in rows)
            {
                this.Add(row);
            }
        }

        public void Remove(TableRow row)
        {
            this.owner.Controls.Remove(row);
        }

        public void RemoveAt(int index)
        {
            this.owner.Controls.Remove(this[index]);
        }

        public void Clear()
        {
            this.owner.Controls.Clear();
        }

        public int GetRowIndex(TableRow row)
        {
            return this.owner.Controls.OfType<TableRow>().ToList().IndexOf(row);
        }

        public IEnumerator<TableRow> GetEnumerator()
        {
            return this.owner.Controls.OfType<TableRow>().ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class Table : WebControl
    {
        public Table() : base(HtmlTextWriterTag.Table)
        {
            this.Rows = new TableRowCollection(this);
        }

        public virtual TableRowCollection Rows { get; }
        public virtual int CellPadding { get; set; } = -1;
        public virtual int CellSpacing { get; set; } = -1;
        public virtual GridLines GridLines { get; set; }
        public virtual string Caption { get; set; }
        public virtual HorizontalAlign HorizontalAlign { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (this.CellSpacing >= 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, this.CellSpacing.ToString(CultureInfo.InvariantCulture));
            }

            if (this.CellPadding >= 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, this.CellPadding.ToString(CultureInfo.InvariantCulture));
            }

            switch (this.GridLines)
            {
                case GridLines.Both:
                    writer.AddAttribute("rules", "all");
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
                    break;
                case GridLines.Horizontal:
                    writer.AddAttribute("rules", "rows");
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
                    break;
                case GridLines.Vertical:
                    writer.AddAttribute("rules", "cols");
                    writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
                    break;
            }

            if (this.HorizontalAlign != HorizontalAlign.NotSet)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Align, this.HorizontalAlign.ToString().ToLowerInvariant());
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.Caption))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Caption);
                writer.Write(this.Caption);
                writer.RenderEndTag();
            }

            List<TableRow> rows = this.Controls.OfType<TableRow>().ToList();
            bool sectioned = rows.Any(r => r.TableSection != TableRowSection.TableBody);

            if (!sectioned)
            {
                foreach (Control control in this.Controls.ToArray())
                {
                    control.RenderControl(writer);
                }

                return;
            }

            RenderSection(writer, rows, TableRowSection.TableHeader, "thead");
            RenderSection(writer, rows, TableRowSection.TableBody, "tbody");
            RenderSection(writer, rows, TableRowSection.TableFooter, "tfoot");
        }

        private static void RenderSection(HtmlTextWriter writer, List<TableRow> rows, TableRowSection section, string tag)
        {
            List<TableRow> sectionRows = rows.Where(r => r.TableSection == section).ToList();

            if (sectionRows.Count == 0)
            {
                return;
            }

            writer.RenderBeginTag(tag);

            foreach (TableRow row in sectionRows)
            {
                row.RenderControl(writer);
            }

            writer.RenderEndTag();
        }
    }

    public enum DataControlRowType
    {
        Header = 0,
        Footer = 1,
        DataRow = 2,
        Separator = 3,
        Pager = 4,
        EmptyDataRow = 5
    }

    [Flags]
    public enum DataControlRowState
    {
        Normal = 0,
        Alternate = 1,
        Selected = 2,
        Edit = 4,
        Insert = 8
    }

    public enum DataControlCellType
    {
        Header = 0,
        Footer = 1,
        DataCell = 2
    }

    public class DataControlFieldCell : TableCell
    {
        public DataControlFieldCell(DataControlField containingField) : this(HtmlTextWriterTag.Td, containingField)
        {
        }

        protected DataControlFieldCell(HtmlTextWriterTag tag, DataControlField containingField) : base(tag)
        {
            this.ContainingField = containingField;
        }

        public DataControlField ContainingField { get; }
    }

    public class DataControlFieldHeaderCell : DataControlFieldCell
    {
        public DataControlFieldHeaderCell(DataControlField containingField) : base(HtmlTextWriterTag.Th, containingField)
        {
        }

        public string Scope { get; set; } = "col";

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.Scope))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Scope, this.Scope);
            }

            base.AddAttributesToRender(writer);
        }
    }

    public abstract class DataControlField
    {
        protected DataControlField()
        {
            this.HeaderStyle = new TableItemStyle();
            this.ItemStyle = new TableItemStyle();
            this.FooterStyle = new TableItemStyle();
            this.ControlStyle = new Style();
        }

        public virtual string HeaderText { get; set; } = string.Empty;
        public virtual string FooterText { get; set; } = string.Empty;
        public virtual string HeaderImageUrl { get; set; }
        public virtual string SortExpression { get; set; }
        public virtual bool Visible { get; set; } = true;
        public virtual bool ShowHeader { get; set; } = true;
        public virtual string AccessibleHeaderText { get; set; }
        public TableItemStyle HeaderStyle { get; }
        public TableItemStyle ItemStyle { get; }
        public TableItemStyle FooterStyle { get; }
        public Style ControlStyle { get; }

        public virtual void InitializeCell(DataControlFieldCell cell, DataControlCellType cellType, DataControlRowState rowState, int rowIndex)
        {
            switch (cellType)
            {
                case DataControlCellType.Header:
                    cell.Text = this.ShowHeader ? this.HeaderText : string.Empty;
                    cell.ItemStyle = this.HeaderStyle;
                    break;
                case DataControlCellType.Footer:
                    cell.Text = this.FooterText;
                    cell.ItemStyle = this.FooterStyle;
                    break;
                default:
                    cell.ItemStyle = this.ItemStyle;
                    break;
            }
        }

        /// <summary>
        ///     Populates a data cell from the data item of its row.
        /// </summary>
        protected internal virtual void BindCell(DataControlFieldCell cell, object dataItem)
        {
        }
    }

    public class BoundField : DataControlField
    {
        public virtual string DataField { get; set; } = string.Empty;
        public virtual string DataFormatString { get; set; } = string.Empty;
        public virtual bool HtmlEncode { get; set; } = true;
        public virtual bool HtmlEncodeFormatString { get; set; } = true;
        public virtual string NullDisplayText { get; set; } = string.Empty;
        public virtual bool ReadOnly { get; set; }
        public virtual bool ApplyFormatInEditMode { get; set; }
        public virtual bool ConvertEmptyStringToNull { get; set; } = true;

        protected internal override void BindCell(DataControlFieldCell cell, object dataItem)
        {
            object value = DataSourceAccess.GetObject(dataItem, this.DataField);
            cell.Text = this.FormatDataValue(value, this.HtmlEncode);
        }

        protected virtual string FormatDataValue(object dataValue, bool encode)
        {
            if (dataValue == null || dataValue == DBNull.Value)
            {
                return this.NullDisplayText;
            }

            string text;

            if (string.IsNullOrEmpty(this.DataFormatString))
            {
                text = Convert.ToString(dataValue, CultureInfo.CurrentCulture);
                return encode ? System.Net.WebUtility.HtmlEncode(text) : text;
            }

            if (encode && this.HtmlEncodeFormatString)
            {
                text = string.Format(CultureInfo.CurrentCulture, this.DataFormatString, dataValue);
                return System.Net.WebUtility.HtmlEncode(text);
            }

            object formatValue = encode ? System.Net.WebUtility.HtmlEncode(Convert.ToString(dataValue, CultureInfo.CurrentCulture)) : dataValue;

            if (encode && !(dataValue is string))
            {
                formatValue = dataValue;
            }

            return string.Format(CultureInfo.CurrentCulture, this.DataFormatString, formatValue);
        }
    }

    /// <summary>
    ///     A bound field generated automatically from the data source schema (AutoGenerateColumns).
    /// </summary>
    public sealed class AutoGeneratedField : BoundField
    {
        public AutoGeneratedField(string dataField)
        {
            this.DataField = dataField;
            this.HeaderText = dataField;
            this.DataType = typeof(object);
        }

        public Type DataType { get; set; }
    }

    public class TemplateField : DataControlField
    {
        public virtual ITemplate ItemTemplate { get; set; }
        public virtual ITemplate AlternatingItemTemplate { get; set; }
        public virtual ITemplate EditItemTemplate { get; set; }
        public virtual ITemplate HeaderTemplate { get; set; }
        public virtual ITemplate FooterTemplate { get; set; }
        public virtual ITemplate InsertItemTemplate { get; set; }

        public override void InitializeCell(DataControlFieldCell cell, DataControlCellType cellType, DataControlRowState rowState, int rowIndex)
        {
            base.InitializeCell(cell, cellType, rowState, rowIndex);

            ITemplate template = null;

            switch (cellType)
            {
                case DataControlCellType.Header:
                    template = this.HeaderTemplate;
                    break;
                case DataControlCellType.Footer:
                    template = this.FooterTemplate;
                    break;
                case DataControlCellType.DataCell:
                    template = (rowState & DataControlRowState.Alternate) != 0 && this.AlternatingItemTemplate != null
                        ? this.AlternatingItemTemplate
                        : this.ItemTemplate;
                    break;
            }

            if (template != null)
            {
                cell.Text = string.Empty;
                template.InstantiateIn(cell);
            }
            else if (cellType == DataControlCellType.DataCell)
            {
                cell.Text = "&nbsp;";
            }
        }
    }

    public sealed class DataControlFieldCollection : IEnumerable<DataControlField>
    {
        private readonly List<DataControlField> fields = new List<DataControlField>();

        public int Count => this.fields.Count;
        public DataControlField this[int index] => this.fields[index];

        public void Add(DataControlField field)
        {
            this.fields.Add(field);
        }

        public void Insert(int index, DataControlField field)
        {
            this.fields.Insert(index, field);
        }

        public void Remove(DataControlField field)
        {
            this.fields.Remove(field);
        }

        public void RemoveAt(int index)
        {
            this.fields.RemoveAt(index);
        }

        public void Clear()
        {
            this.fields.Clear();
        }

        public bool Contains(DataControlField field)
        {
            return this.fields.Contains(field);
        }

        public int IndexOf(DataControlField field)
        {
            return this.fields.IndexOf(field);
        }

        public IEnumerator<DataControlField> GetEnumerator()
        {
            return this.fields.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class GridViewRow : TableRow, IDataItemContainer
    {
        public GridViewRow(int rowIndex, int dataItemIndex, DataControlRowType rowType, DataControlRowState rowState)
        {
            this.RowIndex = rowIndex;
            this.DataItemIndex = dataItemIndex;
            this.RowType = rowType;
            this.RowState = rowState;
        }

        public virtual int RowIndex { get; }
        public virtual DataControlRowType RowType { get; }
        public virtual DataControlRowState RowState { get; set; }
        public virtual object DataItem { get; set; }
        public virtual int DataItemIndex { get; }
        public int DisplayIndex => this.RowIndex;
    }

    public sealed class GridViewRowCollection : IEnumerable<GridViewRow>
    {
        private readonly List<GridViewRow> rows;

        internal GridViewRowCollection(List<GridViewRow> rows)
        {
            this.rows = rows;
        }

        public int Count => this.rows.Count;
        public GridViewRow this[int index] => this.rows[index];

        public IEnumerator<GridViewRow> GetEnumerator()
        {
            return this.rows.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class GridViewRowEventArgs : EventArgs
    {
        public GridViewRowEventArgs(GridViewRow row)
        {
            this.Row = row;
        }

        public GridViewRow Row { get; }
    }

    public delegate void GridViewRowEventHandler(object sender, GridViewRowEventArgs e);

    /// <summary>
    ///     Data grid rendering a data source as an HTML table (replacement for System.Web.UI.WebControls.GridView).
    ///     Rows are created on DataBind; RowCreated and RowDataBound are raised like in Web Forms.
    /// </summary>
    public class GridView : WebControl, INamingContainer
    {
        private readonly List<GridViewRow> dataRows = new List<GridViewRow>();
        private Table table;

        public GridView() : base(HtmlTextWriterTag.Table)
        {
            this.Columns = new DataControlFieldCollection();
            this.HeaderStyle = new TableItemStyle();
            this.RowStyle = new TableItemStyle();
            this.AlternatingRowStyle = new TableItemStyle();
            this.FooterStyle = new TableItemStyle();
            this.SelectedRowStyle = new TableItemStyle();
            this.EditRowStyle = new TableItemStyle();
            this.EmptyDataRowStyle = new TableItemStyle();
            this.PagerStyle = new TableItemStyle();
            this.Rows = new GridViewRowCollection(this.dataRows);
        }

        public event GridViewRowEventHandler RowCreated;
        public event GridViewRowEventHandler RowDataBound;
        public event EventHandler DataBound;

        public virtual object DataSource { get; set; }
        public virtual string DataMember { get; set; }
        public virtual string[] DataKeyNames { get; set; } = new string[0];
        public virtual bool AutoGenerateColumns { get; set; } = true;
        public virtual bool ShowHeader { get; set; } = true;
        public virtual bool ShowFooter { get; set; }
        public virtual bool ShowHeaderWhenEmpty { get; set; }
        public virtual string EmptyDataText { get; set; } = string.Empty;
        public virtual GridLines GridLines { get; set; } = GridLines.Both;
        public virtual int CellPadding { get; set; } = -1;
        public virtual int CellSpacing { get; set; }
        public virtual bool UseAccessibleHeader { get; set; } = true;
        public virtual string Caption { get; set; }
        public virtual bool AllowPaging { get; set; }
        public virtual bool AllowSorting { get; set; }
        public virtual int PageSize { get; set; } = 10;
        public virtual int PageIndex { get; set; }

        public DataControlFieldCollection Columns { get; }
        public GridViewRowCollection Rows { get; }
        public GridViewRow HeaderRow { get; private set; }
        public GridViewRow FooterRow { get; private set; }

        public TableItemStyle HeaderStyle { get; }
        public TableItemStyle RowStyle { get; }
        public TableItemStyle AlternatingRowStyle { get; }
        public TableItemStyle FooterStyle { get; }
        public TableItemStyle SelectedRowStyle { get; }
        public TableItemStyle EditRowStyle { get; }
        public TableItemStyle EmptyDataRowStyle { get; }
        public TableItemStyle PagerStyle { get; }

        public override void DataBind()
        {
            this.OnDataBinding(EventArgs.Empty);
            this.CreateTable(DataSourceAccess.GetEnumerable(this.DataSource, this.DataMember));
            this.OnDataBound(EventArgs.Empty);
        }

        protected virtual void OnDataBound(EventArgs e)
        {
            this.DataBound?.Invoke(this, e);
        }

        protected virtual void OnRowCreated(GridViewRowEventArgs e)
        {
            this.RowCreated?.Invoke(this, e);
        }

        protected virtual void OnRowDataBound(GridViewRowEventArgs e)
        {
            this.RowDataBound?.Invoke(this, e);
        }

        private void CreateTable(IEnumerable source)
        {
            if (this.table != null)
            {
                this.Controls.Remove(this.table);
            }

            this.dataRows.Clear();
            this.HeaderRow = null;
            this.FooterRow = null;

            this.table = new Table {CellSpacing = this.CellSpacing, CellPadding = this.CellPadding, GridLines = this.GridLines, Caption = this.Caption};
            this.Controls.Add(this.table);

            List<object> items = source?.Cast<object>().ToList() ?? new List<object>();
            List<DataControlField> fields = this.Columns.Where(c => c.Visible).ToList();

            if (this.AutoGenerateColumns)
            {
                fields.AddRange(GenerateFields(source, items));
            }

            if (items.Count == 0 && !this.ShowHeaderWhenEmpty)
            {
                if (!string.IsNullOrEmpty(this.EmptyDataText))
                {
                    GridViewRow emptyRow = new GridViewRow(-1, -1, DataControlRowType.EmptyDataRow, DataControlRowState.Normal);
                    TableCell cell = new TableCell {Text = this.EmptyDataText, ColumnSpan = Math.Max(fields.Count, 1)};
                    emptyRow.Cells.Add(cell);
                    ApplyStyle(emptyRow, this.EmptyDataRowStyle);
                    this.table.Rows.Add(emptyRow);
                }

                return;
            }

            if (this.ShowHeader)
            {
                this.HeaderRow = this.CreateRow(-1, -1, DataControlRowType.Header, DataControlRowState.Normal, fields, null, false);
                ApplyStyle(this.HeaderRow, this.HeaderStyle);
            }

            for (int i = 0; i < items.Count; i++)
            {
                DataControlRowState state = i % 2 == 1 ? DataControlRowState.Alternate : DataControlRowState.Normal;
                GridViewRow row = this.CreateRow(i, i, DataControlRowType.DataRow, state, fields, items[i], true);
                ApplyStyle(row, state == DataControlRowState.Alternate && !string.IsNullOrWhiteSpace(this.AlternatingRowStyle.CssClass) ? this.AlternatingRowStyle : this.RowStyle);
                this.dataRows.Add(row);
            }

            if (this.ShowFooter)
            {
                this.FooterRow = this.CreateRow(-1, -1, DataControlRowType.Footer, DataControlRowState.Normal, fields, null, false);
                ApplyStyle(this.FooterRow, this.FooterStyle);
            }
        }

        private GridViewRow CreateRow(int rowIndex, int dataItemIndex, DataControlRowType rowType, DataControlRowState state, List<DataControlField> fields,
            object dataItem, bool dataBind)
        {
            GridViewRow row = new GridViewRow(rowIndex, dataItemIndex, rowType, state);

            foreach (DataControlField field in fields)
            {
                DataControlFieldCell cell;
                DataControlCellType cellType;

                switch (rowType)
                {
                    case DataControlRowType.Header:
                        cell = this.UseAccessibleHeader ? new DataControlFieldHeaderCell(field) : new DataControlFieldCell(field);
                        cellType = DataControlCellType.Header;
                        break;
                    case DataControlRowType.Footer:
                        cell = new DataControlFieldCell(field);
                        cellType = DataControlCellType.Footer;
                        break;
                    default:
                        cell = new DataControlFieldCell(field);
                        cellType = DataControlCellType.DataCell;
                        break;
                }

                row.Cells.Add(cell);
                field.InitializeCell(cell, cellType, state, rowIndex);
            }

            row.DataItem = dataItem;
            this.OnRowCreated(new GridViewRowEventArgs(row));
            this.table.Rows.Add(row);

            if (dataBind)
            {
                foreach (DataControlFieldCell cell in row.Cells.OfType<DataControlFieldCell>())
                {
                    cell.ContainingField.BindCell(cell, dataItem);
                }

                row.DataBind();
                this.OnRowDataBound(new GridViewRowEventArgs(row));
            }
            else if (rowType == DataControlRowType.Header || rowType == DataControlRowType.Footer)
            {
                this.OnRowDataBound(new GridViewRowEventArgs(row));
            }

            return row;
        }

        private static void ApplyStyle(TableRow row, TableItemStyle style)
        {
            if (style != null && !string.IsNullOrWhiteSpace(style.CssClass) && string.IsNullOrWhiteSpace(row.CssClass))
            {
                row.CssClass = style.CssClass;
            }
        }

        private static IEnumerable<DataControlField> GenerateFields(IEnumerable source, List<object> items)
        {
            DataView view = source as DataView;

            if (view != null)
            {
                return view.Table.Columns.Cast<DataColumn>().Select(c => (DataControlField)new AutoGeneratedField(c.ColumnName) {DataType = c.DataType});
            }

            object first = items.FirstOrDefault();

            if (first is DataRowView rowView)
            {
                return rowView.Row.Table.Columns.Cast<DataColumn>().Select(c => (DataControlField)new AutoGeneratedField(c.ColumnName) {DataType = c.DataType});
            }

            if (first == null)
            {
                return Enumerable.Empty<DataControlField>();
            }

            return System.ComponentModel.TypeDescriptor.GetProperties(first).Cast<System.ComponentModel.PropertyDescriptor>()
                .Select(p => (DataControlField)new AutoGeneratedField(p.Name) {DataType = p.PropertyType});
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            if (this.table == null)
            {
                return;
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            this.table.ID = null;
            this.table.CssClass = this.CssClass;
            this.table.Width = this.Width;
            this.table.Height = this.Height;
            this.table.BorderStyle = this.BorderStyle;
            this.table.Style["border-collapse"] = "collapse";

            foreach (string key in this.Attributes.Keys)
            {
                if (!string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    this.table.Attributes[key] = this.Attributes[key];
                }
            }

            foreach (string key in this.Style.Keys)
            {
                this.table.Style[key] = this.Style[key];
            }

            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            }

            this.table.RenderControl(writer);

            writer.RenderEndTag();
        }
    }
}
