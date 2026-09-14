// Web Forms Compatibility Stubs for .NET 10 Migration
// These stubs allow Web Forms code to compile under .NET 10.
// Functionality must be migrated to ASP.NET Core Razor Pages / MVC.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1822 // Mark members as static

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace System.Web.UI
{
    public enum ValidateRequestMode { Inherit = 0, Enabled = 1, Disabled = 2 }

    public interface ITemplate
    {
        void InstantiateIn(Control container);
    }

    public class ControlCollection : IEnumerable<Control>
    {
        private readonly List<Control> _list = new();
        public void Add(Control c) { _list.Add(c); }
        public void Remove(Control c) { _list.Remove(c); }
        public void Clear() { _list.Clear(); }
        public int Count => _list.Count;
        public Control this[int index] => _list[index];
        public IEnumerator<Control> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }

    public class AttributeCollection
    {
        private readonly Dictionary<string, string> _dict = new();
        public void Add(string key, string value) { _dict[key] = value; }
        public void Remove(string key) { _dict.Remove(key); }
        public string this[string key] { get => _dict.TryGetValue(key, out var v) ? v : null; set => _dict[key] = value; }
    }

    public class Control : IDisposable
    {
        public virtual string ID { get; set; }
        public virtual string ClientID => ID;
        public bool Visible { get; set; } = true;
        public ControlCollection Controls { get; } = new ControlCollection();
        public Control Parent { get; internal set; }
        public Page Page { get; set; }
        public AttributeCollection Attributes { get; } = new AttributeCollection();

        protected virtual void OnLoad(EventArgs e) { }
        protected virtual void OnInit(EventArgs e) { }
        protected virtual void OnPreRender(EventArgs e) { }
        protected virtual void CreateChildControls() { }
        protected virtual void RecreateChildControls() { CreateChildControls(); }

        public virtual void RenderControl(HtmlTextWriter writer) { }
        protected virtual void Render(HtmlTextWriter writer) { }
        public Control FindControl(string id) => null;
        public virtual void Dispose() { GC.SuppressFinalize(this); }
        public string ResolveUrl(string relativeUrl) => relativeUrl;
        public string ResolveClientUrl(string relativeUrl) => relativeUrl;
        public Control NamingContainer { get; internal set; }
        public UI.ClientIDMode ClientIDMode { get; set; }

        protected EventArgs EventArgs => EventArgs.Empty;
    }

    public class HtmlTextWriter : TextWriter
    {
        private readonly TextWriter _writer;
        public HtmlTextWriter(TextWriter writer) { _writer = writer; }
        public HtmlTextWriter() { _writer = TextWriter.Null; }
        public override System.Text.Encoding Encoding => _writer.Encoding;
        public new void Write(string s) => _writer.Write(s);
        public new void WriteLine(string s) => _writer.WriteLine(s);
        public void WriteBeginTag(string tag) => _writer.Write("<" + tag);
        public void WriteEndTag(string tag) => _writer.Write("</" + tag + ">");
        public void WriteAttribute(string name, string value) => _writer.Write($" {name}=\"{value}\"");
        public new void Write(char value) => _writer.Write(value);
        public void RenderBeginTag(string tagName) => _writer.Write("<" + tagName + ">");
        public void RenderBeginTag(HtmlTextWriterTag tag) => _writer.Write("<" + tag.ToString().ToLower() + ">");
        public void RenderEndTag() => _writer.Write("</div>");
        public void AddAttribute(HtmlTextWriterAttribute key, string value) { }
        public void AddStyleAttribute(string key, string value) { }
        protected override void Dispose(bool disposing) { if (disposing) _writer?.Dispose(); }
    }

    public class Page : TemplateControl
    {
        public Web.HttpRequest Request => Web.HttpContext.Current?.Request;
        public Web.HttpResponse Response => Web.HttpContext.Current?.Response;
        public Web.SessionState.HttpSessionState Session => Web.HttpContext.Current?.Session;
        public Web.HttpServerUtility Server => Web.HttpContext.Current?.Server;
        public object Application => null;
        public MasterPage Master { get; set; }
        public bool IsPostBack { get; set; }
        public bool IsValid { get; set; } = true;
        public Control Header { get; } = new Control();
        public string Title { get; set; }
        public Web.HttpContext Context => Web.HttpContext.Current;
        public ClientScriptManager ClientScript { get; } = new ClientScriptManager();
        public bool IsAuthenticated => Request?.IsAuthenticated ?? false;

        public string ResolveUrl(string relativeUrl) => relativeUrl;
        public string ResolveClientUrl(string relativeUrl) => relativeUrl;
        public void Validate() { }

        protected virtual PageStatePersister PageStatePersister => null;

        protected override void OnLoad(EventArgs e) { base.OnLoad(e); }
        protected new virtual void InitializeCulture() { }
        protected virtual void OnPreInit(EventArgs e) { }
        protected virtual void OnPreLoad(EventArgs e) { }
        protected virtual void OnPreRenderComplete(EventArgs e) { }
        protected virtual void OnUnload(EventArgs e) { }
        public bool AutoEventWireup { get; set; } = true;
        public string Theme { get; set; } = string.Empty;
        public string SkinID { get; set; } = string.Empty;
        public string MasterPageFile { get; set; }
        public bool MaintainScrollPositionOnPostBack { get; set; }
        public int ScriptTimeout { get; set; } = 90;
        public System.Security.Principal.IPrincipal User => Context?.User;
        public Web.Routing.RouteData RouteData { get; } = new Web.Routing.RouteData();
        public ValidateRequestMode ValidateRequestMode { get; set; }
        public string Form { get; set; }
    }

    public class ClientScriptManager
    {
        public string GetWebResourceUrl(Type type, string resourceName) => string.Empty;
        public void RegisterStartupScript(Type type, string key, string script) { }
        public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags) { }
        public void RegisterClientScriptBlock(Type type, string key, string script) { }
        public void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags) { }
    }

    public class PageStatePersister
    {
        public PageStatePersister(Page page) { }
        public virtual void Load() { }
        public virtual void Save() { }
    }

    public class UserControl : TemplateControl
    {
        public Web.HttpRequest Request => Web.HttpContext.Current?.Request;
        public Web.HttpResponse Response => Web.HttpContext.Current?.Response;
        public Web.SessionState.HttpSessionState Session => Web.HttpContext.Current?.Session;
        public Web.HttpServerUtility Server => Web.HttpContext.Current?.Server;
        public Web.HttpContext Context => Web.HttpContext.Current;
        public Page Page { get; set; }
        public bool IsPostBack { get; set; }

        public string ResolveUrl(string relativeUrl) => relativeUrl;
        protected virtual void OnLoad(EventArgs e) { }
    }

    public class MasterPage : Control
    {
        public Web.HttpContext Context => Web.HttpContext.Current;
        public Page Page { get; set; }
        public object Application => null;
        public Web.HttpApplicationState HttpApplicationState => null;
    }

    public class ContentPlaceHolder : Control { }

    public class ScriptManager : Control
    {
        public static ScriptManager GetCurrent(Page page) => new ScriptManager();
        public static void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) { }
        public static void RegisterStartupScript(Page page, Type type, string key, string script, bool addScriptTags) { }
        public static void RegisterClientScriptBlock(Control control, Type type, string key, string script, bool addScriptTags) { }
        public void RegisterAsyncPostBackControl(Control control) { }
        public bool EnablePartialRendering { get; set; } = true;
        public bool AsyncPostBackErrorMessage { get; set; }
        public bool AllowCustomErrorsRedirect { get; set; }
    }
}

namespace System.Web.UI.WebControls
{
    public class Style
    {
        public string CssClass { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string Width { get; set; }
    }

    public class TableItemStyle : Style
    {
        public Unit Width { get; set; }
        public Unit Height { get; set; }
        public string Wrap { get; set; }
        public string HorizontalAlign { get; set; }
        public string VerticalAlign { get; set; }
    }
    public class TableStyle : Style
    {
        public Unit Width { get; set; }
        public Unit Height { get; set; }
    }

    public class WebControl : UI.Control
    {
        public virtual string CssClass { get; set; }
        public bool Enabled { get; set; } = true;
        public Style ControlStyle { get; } = new Style();
        public virtual Unit Width { get; set; }
        public virtual Unit Height { get; set; }
        public string ToolTip { get; set; } = string.Empty;
        public bool EnableTheming { get; set; } = true;
        public bool EnableViewState { get; set; } = true;
        public string SkinID { get; set; } = string.Empty;
        public StyleCollection Style { get; } = new StyleCollection();
        public string AccessKey { get; set; } = string.Empty;
        public int TabIndex { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string BorderColor { get; set; }
        public FontInfo Font { get; } = new FontInfo();
    }

    public struct Unit
    {
        public double Value { get; }
        public UnitType Type { get; }
        public Unit(double value) { Value = value; Type = UnitType.Pixel; }
        public Unit(double value, UnitType type) { Value = value; Type = type; }
        public static Unit Parse(string value) => new Unit();
        public static Unit Parse(string value, System.Globalization.CultureInfo culture) => new Unit();
        public static Unit Pixel(int value) => new Unit(value, UnitType.Pixel);
        public static Unit Percentage(double value) => new Unit(value, UnitType.Percentage);
        public static Unit Empty => new Unit();
        public bool IsEmpty => Value == 0;
        public override string ToString() => $"{Value}{(Type == UnitType.Pixel ? "px" : "%")}";
        public static implicit operator Unit(int value) => new Unit(value, UnitType.Pixel);
        public static implicit operator Unit(double value) => new Unit(value, UnitType.Pixel);
    }

    public enum UnitType { Pixel, Percentage, Em, Ex, Point, Pica, Inch, Mm, Cm }

    public class ListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
        public ListItem() { }
        public ListItem(string text) { Text = text; Value = text; }
        public ListItem(string text, string value) { Text = text; Value = value; }
    }

    public class ListItemCollection : IEnumerable<ListItem>
    {
        private readonly List<ListItem> _items = new();
        public void Add(ListItem item) { _items.Add(item); }
        public void Add(string text) { _items.Add(new ListItem(text)); }
        public void Insert(int index, ListItem item) { _items.Insert(index, item); }
        public void Insert(int index, string text) { _items.Insert(index, new ListItem(text)); }
        public void Remove(ListItem item) { _items.Remove(item); }
        public void Clear() { _items.Clear(); }
        public int Count => _items.Count;
        public ListItem this[int index] => _items[index];
        public ListItem FindByValue(string value) => _items.Find(i => i.Value == value);
        public ListItem FindByText(string text) => _items.Find(i => i.Text == text);
        public IEnumerator<ListItem> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    public class ListControl : WebControl
    {
        public System.Web.UI.WebControls.ListItemCollection Items { get; } = new System.Web.UI.WebControls.ListItemCollection();
        public UI.ClientIDMode ClientIDMode { get; set; }
        public string Text { get; set; } = string.Empty;
        public string SelectedValue { get => Items.FindByValue(Items.Count > 0 ? _selectedValue : null)?.Value ?? string.Empty; set => _selectedValue = value; }
        private string _selectedValue;
        public ListItem SelectedItem => Items.FindByValue(_selectedValue);
        public string DataSourceID { get; set; }
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }
        public object DataSource { get; set; }
        public void ClearSelection() { foreach (var i in Items) i.Selected = false; }
        public void DataBind() { }
#pragma warning disable CS0067
        public event EventHandler DataBound;
#pragma warning restore CS0067
    }

    public class DropDownList : ListControl { }
    public class ListBox : ListControl { }
    public class CheckBoxList : ListControl { }
    public class RadioButtonList : ListControl
    {
        public RepeatDirection RepeatDirection { get; set; }
        public RepeatLayout RepeatLayout { get; set; }
        public int RepeatColumns { get; set; }
        public string TextAlign { get; set; }
    }

    public class TextBox : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public string Placeholder { get; set; }
        public TextBoxMode TextMode { get; set; }
        public UI.ClientIDMode ClientIDMode { get; set; }
        public int Rows { get; set; } = 1;
        public int Columns { get; set; } = 20;
        public int MaxLength { get; set; }
        public bool AutoPostBack { get; set; }
        public bool AutoCompleteType { get; set; }
    }

    public enum TextBoxMode { SingleLine, MultiLine, Password, Number, Email }

    public class Label : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public string AssociatedControlID { get; set; }
    }

    public class CheckBox : WebControl
    {
        public bool Checked { get; set; }
        public string Text { get; set; } = string.Empty;
        public event EventHandler CheckedChanged;
    }

    public class RadioButton : CheckBox { }

    public class Button : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
        public string OnClientClick { get; set; }
        public event EventHandler Click;
        public event CommandEventHandler Command;
        public bool CausesValidation { get; set; } = true;
        public string ValidationGroup { get; set; } = string.Empty;
        public void PerformClick() => Click?.Invoke(this, EventArgs.Empty);
    }

    public delegate void CommandEventHandler(object sender, CommandEventArgs e);
    public class CommandEventArgs : EventArgs
    {
        public string CommandName { get; }
        public string CommandArgument { get; }
        public CommandEventArgs(string commandName, string commandArgument) { CommandName = commandName; CommandArgument = commandArgument; }
    }

    public class LinkButton : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
        public event EventHandler Click;
        public event CommandEventHandler Command;
        public bool CausesValidation { get; set; } = true;
        public string ValidationGroup { get; set; } = string.Empty;
    }

    public class ImageButton : WebControl
    {
        public string ImageUrl { get; set; }
        public string AlternateText { get; set; }
        public string OnClientClick { get; set; }
        public event ImageClickEventHandler Click;
        public event CommandEventHandler Command;
        public bool CausesValidation { get; set; } = true;
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
    }

    public delegate void ImageClickEventHandler(object sender, ImageClickEventArgs e);
    public class ImageClickEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }
    }

    public class HyperLink : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public string NavigateUrl { get; set; }
        public string Target { get; set; }
    }

    public class Literal : WebControl
    {
        public string Text { get; set; } = string.Empty;
    }

    public class Panel : WebControl
    {
        public string GroupingText { get; set; }
    }

    public class PlaceHolder : WebControl { }

    public class Image : WebControl
    {
        public string ImageUrl { get; set; }
        public string AlternateText { get; set; }
    }

    public class HiddenField : WebControl
    {
        public string Value { get; set; } = string.Empty;
        public UI.ClientIDMode ClientIDMode { get; set; }
    }

    public class TableItemStyleInfo
    {
        public string CssClass { get; set; }
    }

    public class DataControlField
    {
        public string HeaderText { get; set; }
        public string DataFormatString { get; set; }
        public TableItemStyle HeaderStyle { get; } = new TableItemStyle();
        public TableItemStyle ItemStyle { get; } = new TableItemStyle();
        public bool Visible { get; set; } = true;
        public bool HtmlEncode { get; set; } = true;
    }

    public class BoundField : DataControlField
    {
        public string DataField { get; set; }
        public string NullDisplayText { get; set; }
    }

    public class ButtonField : DataControlField
    {
        public string CommandName { get; set; }
        public string Text { get; set; }
        public ButtonType ButtonType { get; set; }
    }

    public enum ButtonType { Button, Image, Link }

    public class DataControlFieldCollection : Collection<DataControlField>
    {
        public void Add(DataControlField field) { base.Add(field); }
    }

    public class GridView : WebControl
    {
        public DataControlFieldCollection Columns { get; } = new DataControlFieldCollection();
        public string DataSourceID { get; set; }
        public object DataSource { get; set; }
        public bool AutoGenerateColumns { get; set; } = true;
        public bool AllowPaging { get; set; }
        public bool AllowSorting { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = 10;
        public bool ShowHeader { get; set; } = true;
        public TableStyle HeaderStyle { get; } = new TableStyle();
        public TableStyle RowStyle { get; } = new TableStyle();
        public TableStyle FooterStyle { get; } = new TableStyle();
        public TableStyle AlternatingRowStyle { get; } = new TableStyle();
        public TableStyle PagerStyle { get; } = new TableStyle();
        public TableStyle SelectedRowStyle { get; } = new TableStyle();
        public TableStyle EditRowStyle { get; } = new TableStyle();
        public GridLines GridLines { get; set; }
        public string EmptyDataText { get; set; } = string.Empty;
        public TableRowCollection Rows { get; } = new TableRowCollection();
        public GridViewRow HeaderRow { get; } = new GridViewRow();
        public void DataBind() { }
        public GridViewRow FooterRow { get; } = new GridViewRow();
        public bool ShowFooter { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public Unit BorderWidth { get; set; }
        public int CellPadding { get; set; } = -1;
        public int CellSpacing { get; set; }
        public HorizontalAlign HorizontalAlign { get; set; }
        public bool UseAccessibleHeader { get; set; } = true;
        protected virtual void OnDataBound(EventArgs e) { }
        protected virtual void OnRowDataBound(GridViewRowEventArgs e) { }
#pragma warning disable CS0067
        public event GridViewRowEventHandler RowDataBound;
        public event EventHandler DataBound;
        public event GridViewRowEventHandler RowCreated;
        public event GridViewRowEventHandler RowCommand;
#pragma warning restore CS0067
    }

    public class DataList : WebControl
    {
        public string DataSourceID { get; set; }
        public object DataSource { get; set; }
        public UI.ITemplate ItemTemplate { get; set; }
        public UI.ITemplate HeaderTemplate { get; set; }
        public UI.ITemplate FooterTemplate { get; set; }
        public void DataBind() { }
    }

    public class Repeater : WebControl
    {
        public string DataSourceID { get; set; }
        public object DataSource { get; set; }
        public UI.ITemplate ItemTemplate { get; set; }
        public UI.ITemplate HeaderTemplate { get; set; }
        public UI.ITemplate FooterTemplate { get; set; }
        public void DataBind() { }
    }

    public class TableCell : WebControl
    {
        public string Text { get; set; } = string.Empty;
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }
        public string HorizontalAlign { get; set; }
        public new StyleCollection Style { get; } = new StyleCollection();
    }

    public class TableHeaderCell : TableCell { }

    public class TableHeaderRow : TableRow
    {
        public new TableRowSection TableSection { get; set; } = TableRowSection.TableHeader;
    }

    public class TableRow : WebControl
    {
        public TableCellCollection Cells { get; } = new TableCellCollection();
        public TableRowSection TableSection { get; set; } = TableRowSection.TableBody;
    }

    public class Table : WebControl
    {
        public new TableRowCollection Rows { get; } = new TableRowCollection();
        public StyleCollection Style { get; } = new StyleCollection();
        public GridLines GridLines { get; set; }
        public string Caption { get; set; }
    }

    public class BaseDataList : WebControl
    {
        public object DataSource { get; set; }
        public void DataBind() { }
    }

    public class CustomValidator : WebControl
    {
        public string ControlToValidate { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsValid { get; set; } = true;
        public bool EnableClientScript { get; set; } = true;
        public string InitialValue { get; set; } = string.Empty;
        public ValidatorDisplay Display { get; set; }
        public string ValidationGroup { get; set; } = string.Empty;
        public bool SetFocusOnError { get; set; }
    }

    public class RequiredFieldValidator : CustomValidator { }
    public class RangeValidator : CustomValidator
    {
        public string MinimumValue { get; set; }
        public string MaximumValue { get; set; }
        public ValidationDataType Type { get; set; }
    }
    public class RegularExpressionValidator : CustomValidator
    {
        public string ValidationExpression { get; set; }
    }
    public class CompareValidator : CustomValidator
    {
        public ValidationDataType Type { get; set; }
        public ValidationCompareOperator Operator { get; set; }
        public string ControlToCompare { get; set; }
        public string ValueToCompare { get; set; }
    }
}

namespace System.Web.UI.HtmlControls
{
    public class HtmlControl : UI.Control
    {
        public string TagName { get; set; } = "div";
        public new UI.AttributeCollection Attributes { get; } = new UI.AttributeCollection();
    }

    public class HtmlContainerControl : HtmlControl
    {
        public string InnerHtml { get; set; } = string.Empty;
        public string InnerText { get; set; } = string.Empty;
    }

    public class HtmlGenericControl : HtmlContainerControl, IDisposable
    {
        public HtmlGenericControl() { TagName = "span"; }
        public HtmlGenericControl(string tag) { TagName = tag; }
        public new void Dispose() { GC.SuppressFinalize(this); }
    }

    public class HtmlInputControl : HtmlControl
    {
        public string Type { get; protected set; } = "text";
        public string Value { get; set; } = string.Empty;
        public string Name { get; set; }
        public bool Disabled { get; set; }
    }

    public class HtmlInputText : HtmlInputControl
    {
        public HtmlInputText() { Type = "text"; }
        public HtmlInputText(string type) { Type = type; }
        public string Placeholder { get; set; }
        public bool ReadOnly { get; set; }
        public int MaxLength { get; set; }
        public int Size { get; set; }
    }

    public class HtmlInputHidden : HtmlInputControl
    {
        public HtmlInputHidden() { Type = "hidden"; }
    }

    public class HtmlInputCheckBox : HtmlInputControl, IDisposable
    {
        public HtmlInputCheckBox() { Type = "checkbox"; }
        public bool Checked { get; set; }
        public new UI.ClientIDMode ClientIDMode { get; set; }
        public new void Dispose() { GC.SuppressFinalize(this); }
    }

    public class HtmlInputRadioButton : HtmlInputControl
    {
        public HtmlInputRadioButton() { Type = "radio"; }
        public bool Checked { get; set; }
        public new UI.ClientIDMode ClientIDMode { get; set; }
        public string GroupName { get; set; }
    }

    public class HtmlInputButton : HtmlInputControl
    {
        public HtmlInputButton() { Type = "button"; }
        public HtmlInputButton(string type) { Type = type; }
        public string ServerClick { get; set; }
        public bool CausesValidation { get; set; } = true;
    }

    public class HtmlInputSubmit : HtmlInputButton
    {
        public HtmlInputSubmit() { Type = "submit"; }
    }

    public class HtmlTextArea : HtmlContainerControl
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
        public bool ReadOnly { get; set; }
        public string Value { get; set; } = string.Empty;
        public int MaxLength { get; set; }
    }

    public class HtmlMeta : HtmlControl, IDisposable
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string HttpEquiv { get; set; }
        public new void Dispose() { GC.SuppressFinalize(this); }
    }

    public class HtmlImage : HtmlControl
    {
        public string Src { get; set; }
        public string Alt { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Border { get; set; }
    }

    public class HtmlAnchor : HtmlContainerControl
    {
        public string HRef { get; set; }
        public string Title { get; set; }
        public string Target { get; set; }
        public new UI.ClientIDMode ClientIDMode { get; set; }
#pragma warning disable CS0067
        public event EventHandler DataBinding;
        public event EventHandler ServerClick;
#pragma warning restore CS0067
            public string Name { get; set; }
    }

    public class HtmlForm : HtmlContainerControl
    {
        public string Action { get; set; }
        public string Method { get; set; } = "post";
        public string Enctype { get; set; }
    }

    public class HtmlTable : HtmlContainerControl
    {
        public HtmlTableRowCollection Rows { get; } = new HtmlTableRowCollection();
        public int Border { get; set; }
        public int CellPadding { get; set; }
        public int CellSpacing { get; set; }
        public StyleAttribute Style { get; } = new StyleAttribute();
    }
    public class HtmlTableRow : HtmlContainerControl
    {
        public HtmlTableCellCollection Cells { get; } = new HtmlTableCellCollection();
    }
    public class HtmlTableCell : HtmlContainerControl
    {
        public int ColSpan { get; set; }
        public int RowSpan { get; set; }
        public string Align { get; set; }
        public string VAlign { get; set; }
        public StyleAttribute Style { get; } = new StyleAttribute();
    }
    public class HtmlHead : HtmlContainerControl { }
    public class HtmlTitle : HtmlContainerControl { }

    public class HtmlSelect : HtmlContainerControl
    {
        public int Size { get; set; }
        public bool Multiple { get; set; }
        public string Value { get; set; } = string.Empty;
        public System.Web.UI.WebControls.ListItemCollection Items { get; } = new System.Web.UI.WebControls.ListItemCollection();
        public object DataSource { get; set; }
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }
        public void DataBind() { }
    }
}

namespace System.Web.Security
{
    public static class FormsAuthentication
    {
        public static string FormsCookiePath { get; } = "/";
        public static string FormsCookieName { get; } = ".ASPXAUTH";
        public static string LoginUrl { get; } = "/SignIn.aspx";
        public static string DefaultUrl { get; } = "/Default.aspx";
        public static bool SlidingExpiration { get; } = true;
        public static bool RequireSSL { get; }
        public static string CookieDomain { get; } = string.Empty;
        public static TimeSpan Timeout { get; } = TimeSpan.FromMinutes(20);
        public static bool EnableCrossAppRedirects { get; }
        public static void SetAuthCookie(string userName, bool createPersistentCookie) { }
        public static void SetAuthCookie(string userName, bool createPersistentCookie, string strCookiePath) { }
        public static void SignOut() { }
        public static void RedirectFromLoginPage(string userName, bool createPersistentCookie) { }
        public static void RedirectFromLoginPage(string userName, bool createPersistentCookie, string strCookiePath) { }
        public static void RedirectToLoginPage(string extraQueryString = "") { }
        public static string HashPasswordForStoringInConfigFile(string password, string passwordFormat) => password;
        public static string Encrypt(FormsAuthenticationTicket ticket) => ticket?.Name ?? string.Empty;
        public static FormsAuthenticationTicket Decrypt(string encryptedTicket) => null;
        public static FormsAuthenticationTicket RenewTicketIfOld(FormsAuthenticationTicket ticket) => ticket;
    }

    public sealed class FormsAuthenticationTicket
    {
        public FormsAuthenticationTicket(int version, string name, DateTime issueDate, DateTime expiration, bool isPersistent, string userData) 
        { 
            Version = version; Name = name; IssueDate = issueDate; Expiration = expiration; 
            IsPersistent = isPersistent; UserData = userData; 
        }
        public FormsAuthenticationTicket(int version, string name, DateTime issueDate, DateTime expiration, bool isPersistent, string userData, string cookiePath) 
        { 
            Version = version; Name = name; IssueDate = issueDate; Expiration = expiration; 
            IsPersistent = isPersistent; UserData = userData; CookiePath = cookiePath;
        }
        public int Version { get; }
        public string Name { get; }
        public DateTime IssueDate { get; }
        public DateTime Expiration { get; }
        public bool IsPersistent { get; }
        public string UserData { get; }
        public string CookiePath { get; } = "/";
        public bool Expired => Expiration < DateTime.UtcNow;
    }
}

namespace System.Web.Services
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class WebServiceAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class WebServiceBindingAttribute : Attribute
    {
        public WsiProfiles ConformsTo { get; set; }
    }

    public enum WsiProfiles { None = 0, BasicProfile1_1 = 1 }

    [AttributeUsage(AttributeTargets.Method)]
    public class WebMethodAttribute : Attribute
    {
        public bool EnableSession { get; set; }
        public string Description { get; set; }
        public int CacheDuration { get; set; }
        public string MessageName { get; set; }
    }

    public class WebService : UI.Control
    {
        public Web.HttpContext Context => Web.HttpContext.Current;
        public Web.HttpRequest Request => Web.HttpContext.Current?.Request;
        public Web.HttpResponse Response => Web.HttpContext.Current?.Response;
        public Web.SessionState.HttpSessionState Session => Web.HttpContext.Current?.Session;
        public Web.HttpServerUtility Server => Web.HttpContext.Current?.Server;
        public object Application => null;
    }
}

namespace System.Web.Services.Protocols
{
    // WebServiceAttribute is in System.Web.Services, not here
}

namespace System.Web.Script.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class ScriptMethodAttribute : Attribute
    {
        public ResponseFormat ResponseFormat { get; set; }
        public bool UseHttpGet { get; set; }
        public bool XmlSerializeString { get; set; }
    }

    public enum ResponseFormat { Json, Xml }
}

namespace System.Web.Script.Serialization
{
    public class JavaScriptSerializer
    {
        public string Serialize(object obj) => System.Text.Json.JsonSerializer.Serialize(obj);
        public T Deserialize<T>(string input) => System.Text.Json.JsonSerializer.Deserialize<T>(input);
        public object DeserializeObject(string input) => System.Text.Json.JsonSerializer.Deserialize<object>(input);
    }
}

// Note: System.Web.Hosting.HostingEnvironment is provided by Microsoft.AspNetCore.SystemWebAdapters
// Do not redefine it here to avoid CS0433 ambiguity errors.

namespace System.Web.Routing
{
    public class RouteCollection : Collection<RouteBase>
    {
        public Route MapRoute(string name, string url) => new Route(url, null);
        public Route MapRoute(string name, string url, object defaults) => new Route(url, null);
        public Route MapRoute(string name, string url, object defaults, object constraints) => new Route(url, null);
        public Route MapPageRoute(string routeName, string routeUrl, string physicalFile) => new Route(routeUrl, null);
        public Route MapPageRoute(string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess) => new Route(routeUrl, null);
        public void Ignore(string url) { }
    }

    public abstract class RouteBase { }

    public class Route : RouteBase
    {
        public string Url { get; }
        public IRouteHandler RouteHandler { get; set; }
        public Route(string url, IRouteHandler routeHandler) { Url = url; RouteHandler = routeHandler; }
    }

    public interface IRouteHandler { }

    public static class RouteTable
    {
        public static RouteCollection Routes { get; } = new RouteCollection();
    }

    public class RouteData
    {
        public IDictionary<string, object> Values { get; } = new Dictionary<string, object>();
    }
}

// Additional Web Forms stubs

namespace System.Web.UI
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WebResourceAttribute : Attribute
    {
        public WebResourceAttribute(string webResource, string contentType) { WebResource = webResource; ContentType = contentType; }
        public string WebResource { get; }
        public string ContentType { get; }
        public bool PerformSubstitution { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ToolboxDataAttribute : Attribute
    {
        public ToolboxDataAttribute(string data) { Data = data; }
        public string Data { get; }
    }
}

namespace System.Web.UI.WebControls
{
    public class CompositeControl : WebControl
    {
        protected override void CreateChildControls() { }
        protected override void RecreateChildControls() { CreateChildControls(); }
        protected override void Render(HtmlTextWriter writer) { }
        public void EnsureChildControls() { }
    }

    public class GridViewRowEventArgs : EventArgs
    {
        public GridViewRow Row { get; }
        public GridViewRowEventArgs(GridViewRow row) { Row = row; }
    }

    public class GridViewRow : TableRow
    {
        public DataControlRowType RowType { get; }
        public DataControlRowState RowState { get; }
        public int RowIndex { get; }
        public object DataItem { get; }
    }

    public enum DataControlRowType { Header, Footer, DataRow, Separator, EmptyDataRow, Pager }

    [Flags]
    public enum DataControlRowState { Normal = 0, Selected = 1, Edit = 2, Insert = 4, Alternate = 8 }

    public class DataGridItemEventArgs : EventArgs { }
    public class DataGridItem : TableRow { }
}

namespace System.Web.UI.HtmlControls
{
    public class HtmlButton : HtmlContainerControl
    {
        public string Type { get; set; } = "button";
        public bool Disabled { get; set; }
#pragma warning disable CS0067
        public event EventHandler ServerClick;
#pragma warning restore CS0067
    }

    public class HtmlInputImage : HtmlInputControl
    {
        public HtmlInputImage() { Type = "image"; }
        public string Src { get; set; }
        public string Alt { get; set; }
    }
}

namespace System.Web
{
    // Note: AspNetHostingPermissionLevel and AspNetHostingPermissionAttribute are provided
    // by the System.Security.Permissions NuGet package. Do not redefine them here.
}

namespace System.Web.Services.Protocols
{
    public class WebServiceHandlerFactory : System.Web.IHttpHandlerFactory
    {
        public System.Web.IHttpHandler GetHandler(System.Web.HttpContext context, string requestType, string url, string pathTranslated) => null;
        public void ReleaseHandler(System.Web.IHttpHandler handler) { }
    }
}


// Additional WebControl properties and enums

namespace System.Web.UI
{
    public enum ClientIDMode { AutoID = 0, Static = 1, Predictable = 2, Inherit = 3 }
}

namespace System.Web.UI.WebControls
{
    // Additional properties needed by server controls
    public enum ValidatorDisplay { None, Static, Dynamic }
    public enum ValidationDataType { String, Integer, Double, Date, Currency }
    public enum ValidationCompareOperator { Equal, NotEqual, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, DataTypeCheck }

    // Patch LinkButton with CausesValidation
    public partial class ButtonBase
    {
        public bool CausesValidation { get; set; } = true;
        public string ValidationGroup { get; set; } = string.Empty;
    }
}

// Comprehensive additional stub fixes

namespace System.Web.UI.WebControls
{
    public enum GridLines { None, Horizontal, Vertical, Both }
    public enum TableRowSection { TableHeader, TableBody, TableFooter }

    public class StyleCollection
    {
        private readonly Dictionary<string, string> _styles = new();
        private string _value = string.Empty;
        public string this[string key] { get => _styles.TryGetValue(key, out var v) ? v : null; set => _styles[key] = value; }
        public void Add(string key, string value) { _styles[key] = value; }
        public void Remove(string key) { _styles.Remove(key); }
        public string Value { get => _value; set => _value = value; }
    }

    public class DataControlFieldCell : TableCell
    {
        public DataControlField ContainingField { get; }
        public DataControlFieldCell(DataControlField field) { ContainingField = field; }
        public DataControlFieldCell(DataControlField field, HtmlTextWriterTag tagKey) { ContainingField = field; }
    }


    public class AutoGeneratedField : BoundField
    {
        public AutoGeneratedField(string dataField) { DataField = dataField; }
        public System.Type DataType { get; set; } = typeof(string);
    }
}

namespace System.Web.UI.WebControls
{
    // Patch Table to use proper mutable collection
    public partial class TableRowCollection : IEnumerable<TableRow>
    {
        private readonly List<TableRow> _rows = new();
        public void Add(TableRow row) { _rows.Add(row); }
        public void Remove(TableRow row) { _rows.Remove(row); }
        public void Clear() { _rows.Clear(); }
        public int Count => _rows.Count;
        public TableRow this[int index] => _rows[index];
        public IEnumerator<TableRow> GetEnumerator() => _rows.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
    }

    public partial class TableCellCollection : IEnumerable<TableCell>
    {
        private readonly List<TableCell> _cells = new();
        public void Add(TableCell cell) { _cells.Add(cell); }
        public void Remove(TableCell cell) { _cells.Remove(cell); }
        public void Clear() { _cells.Clear(); }
        public int Count => _cells.Count;
        public TableCell this[int index] => _cells[index];
        public IEnumerator<TableCell> GetEnumerator() => _cells.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _cells.GetEnumerator();
    }
}

namespace System.Web.UI.HtmlControls
{
    // Patch HtmlTable, HtmlTableRow, HtmlTableCell with collections
    public partial class HtmlTableRowCollection : IEnumerable<HtmlTableRow>
    {
        private readonly List<HtmlTableRow> _rows = new();
        public void Add(HtmlTableRow row) { _rows.Add(row); }
        public void Remove(HtmlTableRow row) { _rows.Remove(row); }
        public void Clear() { _rows.Clear(); }
        public int Count => _rows.Count;
        public HtmlTableRow this[int index] => _rows[index];
        public IEnumerator<HtmlTableRow> GetEnumerator() => _rows.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
    }

    public partial class HtmlTableCellCollection : IEnumerable<HtmlTableCell>
    {
        private readonly List<HtmlTableCell> _cells = new();
        public void Add(HtmlTableCell cell) { _cells.Add(cell); }
        public void Remove(HtmlTableCell cell) { _cells.Remove(cell); }
        public void Clear() { _cells.Clear(); }
        public int Count => _cells.Count;
        public HtmlTableCell this[int index] => _cells[index];
        public IEnumerator<HtmlTableCell> GetEnumerator() => _cells.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _cells.GetEnumerator();
    }

    public class HtmlInputFile : HtmlInputControl
    {
        public HtmlInputFile() { Type = "file"; }
        public string Accept { get; set; }
        public int MaxLength { get; set; }
        public int Size { get; set; }
    }

    public class HtmlInputPassword : HtmlInputText
    {
        public HtmlInputPassword() { Type = "password"; }
    }

    public class StyleAttribute
    {
        private readonly Dictionary<string, string> _styles = new();
        public string this[string key] { get => _styles.TryGetValue(key, out var v) ? v : null; set => _styles[key] = value; }
        public void Add(string key, string value) { _styles[key] = value; }
        public void Remove(string key) { _styles.Remove(key); }
    }
}

// More comprehensive stub additions

namespace System.Web.UI.WebControls
{
    public class FileUpload : WebControl
    {
        public bool HasFile { get; }
        public System.Web.HttpPostedFile PostedFile { get; }
        public string FileName { get; }
        public System.IO.Stream FileContent { get; }
        public void SaveAs(string filename) { }
    }

    public class UpdatePanel : UI.Control
    {
        public UpdatePanelUpdateMode UpdateMode { get; set; }
        public bool ChildrenAsTriggers { get; set; } = true;
        public UI.ITemplate ContentTemplate { get; set; }
        public UI.Control ContentTemplateContainer { get; } = new UI.Control();
    }

    public enum UpdatePanelUpdateMode { Always, Conditional }

    public class TemplateField : DataControlField
    {
        public UI.ITemplate ItemTemplate { get; set; }
        public UI.ITemplate EditItemTemplate { get; set; }
        public UI.ITemplate HeaderTemplate { get; set; }
        public UI.ITemplate FooterTemplate { get; set; }
        public UI.ITemplate InsertItemTemplate { get; set; }
    }

    public class HyperLinkField : DataControlField
    {
        public string DataNavigateUrlFields { get; set; }
        public string DataTextField { get; set; }
        public string NavigateUrl { get; set; }
        public string Text { get; set; }
    }

    public class CommandField : DataControlField
    {
        public bool ShowSelectButton { get; set; }
        public bool ShowEditButton { get; set; }
        public bool ShowDeleteButton { get; set; }
        public bool ShowInsertButton { get; set; }
    }

    public delegate void GridViewRowEventHandler(object sender, GridViewRowEventArgs e);
}

namespace System.Web.UI
{
    public enum HtmlTextWriterTag { Unknown, A, Acronym, Address, Area, B, Blockquote, Br, Button, Div, Em, FieldSet, Form, H1, H2, H3, H4, H5, H6, Head, Hr, Html, I, Img, Input, Label, Legend, Li, Link, Meta, Ol, Option, P, Pre, Script, Select, Span, Strong, Style, Table, Td, Textarea, Th, Title, Tr, Ul }

    public enum HtmlTextWriterAttribute { Class, Style, Id, Name, Type, Value, Href, Src, For, Target, Action, Method, Colspan, Rowspan, Border, Width, Height, Align, Valign, Cellpadding, Cellspacing, Summary, Title, Accesskey, Tabindex, Onclick }
}

// Additional stubs for FontInfo, RepeatDirection, etc.

namespace System.Web.UI.WebControls
{
    public class FontInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Overline { get; set; }
        public bool Strikeout { get; set; }
        public FontUnit Size { get; set; }
        public string[] Names { get; set; } = Array.Empty<string>();
        public void MergeWith(FontInfo font) { }
        public void CopyFrom(FontInfo font) { }
    }

    public struct FontUnit
    {
        public double Value { get; }
        public string Unit { get; }
        public FontUnit(double value) { Value = value; Unit = "pt"; }
        public static implicit operator FontUnit(double value) => new FontUnit(value);
        public static implicit operator FontUnit(int value) => new FontUnit(value);
    }

    public enum RepeatDirection { Horizontal, Vertical }
    public enum RepeatLayout { Table, Flow, OrderedList, UnorderedList }

    public class RadioButtonList_Ext // extending RadioButtonList is done inline
    {
    }
}

// More missing HTML controls and properties

namespace System.Web.UI.HtmlControls
{
    public class HtmlLink : HtmlControl
    {
        public string Href { get; set; }
        public string Rel { get; set; }
        public string Type { get; set; } = "text/css";
        public string Media { get; set; }
    }

    public class HtmlInputReset : HtmlInputControl
    {
        public HtmlInputReset() { Type = "reset"; }
    }

    public class HtmlGenericSelf : HtmlGenericControl
    {
        public HtmlGenericSelf() { }
        public HtmlGenericSelf(string tag) : base(tag) { }
    }
}

// NamingContainer is defined on Control

// ContentPlaceHolder in WebControls namespace (designer files reference it from here)
namespace System.Web.UI.WebControls
{
    // Designer files use System.Web.UI.WebControls.ContentPlaceHolder
    public class ContentPlaceHolder : System.Web.UI.ContentPlaceHolder { }
}

// TemplateControl and additional Page members

namespace System.Web.UI
{
    // TemplateControl is a base class for Page and UserControl
    public abstract class TemplateControl : Control
    {
        public Control LoadControl(string virtualPath) => new Control();
        public Control LoadControl(Type t, object[] parameters) => new Control();
        protected virtual void InitializeCulture() { }
    }
}

// BorderStyle enum and more missing GridView/WebControl properties

namespace System.Web.UI.WebControls
{
    public enum BorderStyle { NotSet = 0, None = 1, Dotted = 2, Dashed = 3, Solid = 4, Double = 5, Groove = 6, Ridge = 7, Inset = 8, Outset = 9 }
    public enum HorizontalAlign { NotSet = 0, Left = 1, Center = 2, Right = 3, Justify = 4 }
    public enum VerticalAlign { NotSet = 0, Top = 1, Middle = 2, Bottom = 3 }
}
