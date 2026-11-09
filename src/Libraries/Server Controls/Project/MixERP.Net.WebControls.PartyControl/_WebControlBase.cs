// Auto-generated stub
#pragma warning disable CS0067, CS0108, CS0109, CS0114, CS0628, CS0649, CS0169, CS3021
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MixERP.Net.WebControls.PartyControl
{
    /// <summary>Stub base class replacing System.Web.UI.Control hierarchy.</summary>
    public class WebControlBase : IDisposable
    {
        private readonly List<object> _controlList = new List<object>();
        public virtual dynamic Controls => _controlList;
        public virtual dynamic Page => null;
        public virtual string ID { get; set; }
        public virtual string CssClass { get; set; } = string.Empty;
        public virtual dynamic Width { get; set; }
        public virtual dynamic Height { get; set; }
        public virtual bool Visible { get; set; } = true;
        public virtual bool Enabled { get; set; } = true;
        public virtual string ClientID => ID;
        public virtual string UniqueID => ID;
        public virtual string ToolTip { get; set; }
        public virtual dynamic Rows => null;
        public virtual dynamic HeaderRow => null;
        public virtual dynamic FooterRow => null;
        public virtual dynamic DataSource { get; set; }
        public virtual string EmptyDataText { get; set; }
        public virtual dynamic GridLines { get; set; }
        public virtual dynamic Columns => null;
        public virtual bool AllowPaging { get; set; }
        public virtual bool AllowSorting { get; set; }
        public virtual int PageIndex { get; set; }
        public virtual int PageSize { get; set; } = 10;
        public virtual int PageCount => 0;
        public virtual bool AutoGenerateColumns { get; set; } = true;
        public virtual bool ShowHeader { get; set; } = true;
        public virtual bool ShowFooter { get; set; }
        public virtual string DataKeyNames { get; set; }
        public virtual dynamic DataKeys => null;
        public virtual int SelectedIndex { get; set; } = -1;
        public virtual dynamic SelectedRow => null;
        public virtual dynamic RowStyle => null;
        public virtual dynamic HeaderStyle => null;
        public virtual dynamic FooterStyle => null;
        public virtual dynamic AlternatingRowStyle => null;
        public virtual string Text { get; set; }
        public virtual int MaxLength { get; set; }
        public virtual bool ReadOnly { get; set; }
        public virtual dynamic Items => null;
        public virtual dynamic SelectedItem => null;
        public virtual string SelectedValue { get; set; }
        public virtual dynamic BackColor { get; set; }
        public virtual dynamic ForeColor { get; set; }
        public virtual dynamic BorderColor { get; set; }
        public virtual dynamic Font { get; set; }
        public virtual dynamic Style => null;
        public virtual dynamic Attributes => null;
        protected dynamic ViewState => null;
        public virtual string ResolveUrl(string relativeUrl) => relativeUrl;
        public virtual string ResolveClientUrl(string relativeUrl) => relativeUrl;
        protected void EnsureChildControls() { }
        protected virtual void CreateChildControls() { }
        protected virtual void RecreateChildControls() { }
        protected virtual void Render(object writer) { }
        protected virtual void RenderChildren(object writer) { }
        protected virtual void RenderContents(object writer) { }
        public virtual void DataBind() { }
        public virtual void RenderControl(object writer) { }
        public virtual void Focus() { }
        protected virtual void OnInit(EventArgs e) { }
        protected virtual void OnLoad(EventArgs e) { }
        protected virtual void OnPreRender(EventArgs e) { }
        protected virtual void OnUnload(EventArgs e) { }
        protected virtual void OnDataBound(EventArgs e) { }
        protected virtual void OnRowDataBound(dynamic e) { }
        protected virtual void OnRowCreated(dynamic e) { }
        protected virtual void OnSelectedIndexChanged(EventArgs e) { }
        protected virtual void OnBubbleEvent(object source, EventArgs args) { }
        private bool _disposed;
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing) { _disposed = disposing; }
    }

    public class HtmlGenericControl : IDisposable
    {
        public HtmlGenericControl() { Attributes = new Dictionary<string, string>(); Controls = new List<object>(); }
        public HtmlGenericControl(string tag) : this() { TagName = tag; }
        public string TagName { get; set; }
        public string InnerText { get; set; }
        public string InnerHtml { get; set; }
        public Dictionary<string, string> Attributes { get; }
        public List<object> Controls { get; }
        public string ID { get; set; }
        public bool Visible { get; set; } = true;
        public string Style { get; set; }
        public void Dispose() { }
    }
    public class HtmlControl : IDisposable
    {
        public HtmlControl() { Attributes = new Dictionary<string, string>(); Controls = new List<object>(); }
        public Dictionary<string, string> Attributes { get; }
        public List<object> Controls { get; }
        public string ID { get; set; }
        public bool Visible { get; set; } = true;
        public void Dispose() { }
    }
    public class HtmlContainerControl : HtmlControl { }
    public class HtmlAnchor : HtmlControl
    {
        public string HRef { get; set; }
        public string Name { get; set; }
        public string Target { get; set; }
        public string Title { get; set; }
        public string InnerText { get; set; }
        public string InnerHtml { get; set; }
    }
    public class HtmlInputButton : HtmlControl
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public event EventHandler ServerClick { add { } remove { } }
    }
    public class HtmlInputText : HtmlControl { public string Value { get; set; } public string Type { get; set; } }
    public class HtmlInputHidden : HtmlControl { public string Value { get; set; } }
    public class HtmlInputControl : HtmlControl { public string Value { get; set; } public string Type { get; set; } }
    public class HtmlTable : HtmlControl { public dynamic Rows { get; } }
    public class HtmlTableRow : HtmlControl { public dynamic Cells { get; } }
    public class HtmlTableCell : HtmlControl { public string InnerText { get; set; } public string InnerHtml { get; set; } public int ColSpan { get; set; } public int RowSpan { get; set; } }
    public class HtmlSelect : HtmlControl { public string Value { get; set; } public dynamic Items { get; } }
    public class HtmlImage : HtmlControl { public string Src { get; set; } public string Alt { get; set; } }
    public class HtmlLink : HtmlControl { public string Href { get; set; } }
    public class Control : WebControlBase { }

    public interface IHttpHandler { bool IsReusable { get; } void ProcessRequest(object context); }

    public enum ValidatorDisplay { None, Static, Dynamic }
    public enum ValidationDataType { String, Integer, Double, Date, Currency }
    public enum ValidationCompareOperator { Equal, NotEqual, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, DataTypeCheck }
    public enum DataControlRowType { Header, Footer, DataRow, Separator, EmptyDataRow, Pager }
    public enum TableRowSection { TableHeader, TableBody, TableFooter }
    public enum GridLines { None, Horizontal, Vertical, Both }
    public enum SortDirection { Ascending, Descending }
    public enum ButtonType { Button, Link, Image }
    public enum TextBoxMode { SingleLine, MultiLine, Password, Color, Date, DateTime, DateTimeLocal, Email, Month, Number, Range, Search, Phone, Time, Url, Week }
    public enum ImageAlign { NotSet, AbsBottom, AbsMiddle, Baseline, Bottom, Left, Middle, Right, TextTop, Top }
    public enum ListSelectionMode { Single, Multiple }
    public enum RepeatDirection { Horizontal, Vertical }
    public enum RepeatLayout { Table, Flow, OrderedList, UnorderedList }
    public enum BulletStyle { Numbered, LowerAlpha, UpperAlpha, LowerRoman, UpperRoman, Disc, Circle, Square, CustomImage, NotSet }
    public enum BorderStyle { NotSet, None, Dotted, Dashed, Solid, Double, Groove, Ridge, Inset, Outset }
    public enum HorizontalAlign { NotSet, Left, Center, Right, Justify }
    public enum VerticalAlign { NotSet, Top, Middle, Bottom }
    public enum PagerPosition { Bottom, Top, TopAndBottom }
    
    public interface ITemplate { void InstantiateIn(dynamic container); }
    public interface INamingContainer { }
    public interface IPostBackEventHandler { void RaisePostBackEvent(string eventArgument); }
    public interface IPostBackDataHandler
    {
        bool LoadPostData(string postDataKey, NameValueCollection postCollection);
        void RaisePostDataChangedEvent();
    }
    // Additional WebForms control stubs (net10.0 migration)
    public class Panel : WebControlBase { }
    public class Literal : WebControlBase { public new string Text { get; set; } }
    public class Label : WebControlBase { public new string Text { get; set; } }
    public class Button : WebControlBase { public new string Text { get; set; } public string CommandName { get; set; } public string CommandArgument { get; set; } public bool CausesValidation { get; set; } public event System.EventHandler Click; public event System.EventHandler<dynamic> Command; }
    public class LinkButton : WebControlBase { public new string Text { get; set; } public string CommandName { get; set; } public string CommandArgument { get; set; } public bool CausesValidation { get; set; } public event System.EventHandler Click; }
    public class ImageButton : WebControlBase { public string ImageUrl { get; set; } public string AlternateText { get; set; } public bool CausesValidation { get; set; } public event System.EventHandler<dynamic> Click; }
    public class HiddenField : WebControlBase { }
    public class TextBox : WebControlBase { public new string Text { get; set; } public int Rows { get; set; } public int Columns { get; set; } public bool CausesValidation { get; set; } }
    public class DropDownList : WebControlBase { public new dynamic Items => null; public new string SelectedValue { get; set; } public new dynamic SelectedItem => null; }
    public class ListItem : IDisposable { public string Text { get; set; } public string Value { get; set; } public bool Selected { get; set; } public ListItem() { } public ListItem(string text) { Text = text; } public ListItem(string text, string value) { Text = text; Value = value; } public void Dispose() { } }
    public class ListControl : WebControlBase { public new dynamic Items => null; public new string SelectedValue { get; set; } public new dynamic SelectedItem => null; }
    public class GridView : WebControlBase { public new dynamic Rows => null; public new dynamic HeaderRow => null; public bool AutoGenerateColumns { get; set; } public new dynamic Columns => null; public bool EnableTheming { get; set; } }
    public class GridViewRow : WebControlBase { public DataControlRowType RowType { get; set; } public int DataItemIndex { get; set; } public dynamic DataItem { get; set; } public new dynamic Cells => null; }
    public class TemplateField : WebControlBase { public string HeaderText { get; set; } }
    public class TableHeaderCell : WebControlBase { public new string Text { get; set; } }
    public class TableHeaderRow : WebControlBase { public new dynamic Cells => null; public TableRowSection TableSection { get; set; } }
    public class TableCell : WebControlBase { public new string Text { get; set; } public int ColumnSpan { get; set; } public int RowSpan { get; set; } }
    public class TableRow : WebControlBase { public new dynamic Cells => null; public TableRowSection TableSection { get; set; } }
    public class Table : WebControlBase { public new dynamic Rows => null; }
    public class PlaceHolder : WebControlBase { }
    public class RequiredFieldValidator : WebControlBase { public string ControlToValidate { get; set; } public string ErrorMessage { get; set; } public bool EnableClientScript { get; set; } }
    public class CompareValidator : WebControlBase { public string ControlToValidate { get; set; } public string ControlToCompare { get; set; } public string ErrorMessage { get; set; } public ValidationCompareOperator Operator { get; set; } public ValidationDataType Type { get; set; } }
    public class HtmlButton : HtmlControl { public string Type { get; set; } public event System.EventHandler ServerClick { add { } remove { } } }
    public class HtmlTextArea : HtmlControl { public string Value { get; set; } public int Rows { get; set; } public int Cols { get; set; } }
    public enum ClientIDMode { Inherit, AutoID, Predictable, Static }
    public enum HtmlTextWriterTag { Unknown, A, Div, Span, Table, Tr, Td, Th, Input, Select, Option, Br, Img }
    public enum HtmlTextWriterAttribute { Class, Style, Id, Name, Href, Src, Type, Value, Alt }
    public sealed class Unit { public Unit(double n, dynamic type = null) { } public Unit(string s) { } public static Unit Pixel(int n) => new Unit(n.ToString()); public static Unit Percentage(double n) => new Unit(n.ToString()); public override string ToString() => ""; }
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WebResourceAttribute : System.Attribute { public WebResourceAttribute(string r, string c) { } public bool PerformSubstitution { get; set; } }
    public class MixERPGridView : WebControlBase { public new dynamic Rows => null; public new dynamic HeaderRow => null; public bool AutoGenerateColumns { get; set; } public new dynamic Columns => null; public bool EnableTheming { get; set; } }

}