// Auto-generated stubs for net10.0 migration (WebForms / WebServices / SignalR / FrontEnd)
// These allow FrontEnd module projects to compile without the actual WebForms runtime.
#pragma warning disable CS0067, CS0108, CS0109, CS0114, CS0628, CS0649, CS0169, CS3021, CS0436
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

// -----------------------------------------------------------------------
// System.Web.UI
// -----------------------------------------------------------------------
namespace System.Web.UI
{
    public enum ValidateRequestMode { Enabled, Disabled, Inherit }

    public class Control : IDisposable
    {
        public string ID { get; set; }
        public bool Visible { get; set; } = true;
        public bool EnableViewState { get; set; } = true;
        public List<object> Controls { get; } = new List<object>();
        public virtual dynamic Page { get; set; }
        public virtual System.Web.HttpRequest Request => System.Web.HttpContext.Current?.Request;
        public virtual void DataBind() { }
        public virtual void RenderControl(object writer) { }
        public virtual string ResolveUrl(string relativeUrl) => relativeUrl;
        public virtual void Dispose() { }
    }

    public class UserControl : Control
    {
        public System.Web.HttpServerUtility Server => System.Web.HttpContext.Current?.Server;
        public ValidateRequestMode ValidateRequestMode { get; set; }
    }

    public class TemplateControl : Control
    {
        public virtual Control LoadControl(string virtualPath) => null;
        public virtual Control LoadControl(Type t, object[] parameters) => null;
    }

    public class Page : TemplateControl
    {
        public System.Web.HttpServerUtility Server => System.Web.HttpContext.Current?.Server;
        public new System.Web.HttpRequest Request => System.Web.HttpContext.Current?.Request;
        public System.Web.HttpResponse Response => System.Web.HttpContext.Current?.Response;
        public dynamic Session => System.Web.HttpContext.Current?.Session;
        public System.Security.Principal.IPrincipal User => System.Web.HttpContext.Current?.User;
        public System.Web.HttpContext Context => System.Web.HttpContext.Current;
        public new string ResolveUrl(string relativeUrl) => relativeUrl;
        public string ResolveClientUrl(string relativeUrl) => relativeUrl;
        public bool IsPostBack => false;
        public string Theme { get; set; }
        public string MasterPageFile { get; set; }
        protected virtual void InitializeCulture() { }
        protected virtual void OnInit(EventArgs e) { }
        protected virtual void OnLoad(EventArgs e) { }
        protected virtual void OnPreRender(EventArgs e) { }
        public dynamic Master { get; set; }
    }

    public class MasterPage : TemplateControl
    {
        public dynamic Page { get; set; }
        public System.Web.HttpContext Context => System.Web.HttpContext.Current;
        public System.Web.HttpApplicationState Application => System.Web.HttpContext.Current?.Application;
        protected virtual void OnLoad(EventArgs e) { }
        protected virtual void OnInit(EventArgs e) { }
        protected virtual void OnPreRender(EventArgs e) { }
        public virtual void Dispose() { }
    }

    public class CompositeControl : Control
    {
        protected virtual void CreateChildControls() { }
        protected void EnsureChildControls() { }
        protected virtual void RecreateChildControls() { EnsureChildControls(); }
        protected virtual void Render(HtmlTextWriter writer) { }
        protected virtual void RenderChildren(HtmlTextWriter writer) { }
        protected virtual void RenderContents(HtmlTextWriter writer) { }
    }

    public class HtmlTextWriter { }

    public class PlaceHolder : Control { }

    public class ScriptManager : Control
    {
        public static dynamic GetCurrent(dynamic page) => null;
        public static void RegisterStartupScript(dynamic page, Type t, string key, string script, bool addScriptTags) { }
        public static void RegisterClientScriptInclude(dynamic page, Type t, string key, string url) { }
    }

    public interface ITemplate { void InstantiateIn(Control container); }
    public interface INamingContainer { }
    public interface IPostBackEventHandler { void RaisePostBackEvent(string eventArgument); }
    public interface IPostBackDataHandler
    {
        bool LoadPostData(string postDataKey, NameValueCollection postCollection);
        void RaisePostDataChangedEvent();
    }
}

// -----------------------------------------------------------------------
// System.Web.UI.WebControls
// -----------------------------------------------------------------------
namespace System.Web.UI.WebControls
{
    public enum ValidatorDisplay { None, Static, Dynamic }
    public enum ValidationDataType { String, Integer, Double, Date, Currency }
    public enum ValidationCompareOperator { Equal, NotEqual, GreaterThan, GreaterThanEqual, LessThan, LessThanEqual, DataTypeCheck }
    public enum GridLines { None, Horizontal, Vertical, Both }
    public enum SortDirection { Ascending, Descending }
    public enum ButtonType { Button, Link, Image }
    public enum TextBoxMode { SingleLine, MultiLine, Password, Color, Date, DateTime, DateTimeLocal, Email, Month, Number, Range, Search, Phone, Time, Url, Week }
    public enum ImageAlign { NotSet, AbsBottom, AbsMiddle, Baseline, Bottom, Left, Middle, Right, TextTop, Top }
    public enum ListSelectionMode { Single, Multiple }
    public enum RepeatDirection { Horizontal, Vertical }
    public enum RepeatLayout { Table, Flow, OrderedList, UnorderedList }
    public enum BorderStyle { NotSet, None, Dotted, Dashed, Solid, Double, Groove, Ridge, Inset, Outset }
    public enum HorizontalAlign { NotSet, Left, Center, Right, Justify }
    public enum VerticalAlign { NotSet, Top, Middle, Bottom }
    public enum PagerPosition { Bottom, Top, TopAndBottom }
    public enum ParameterDirection { Input, Output, InputOutput, ReturnValue }

    public sealed class Unit
    {
        public Unit(double n, dynamic t = null) { }
        public Unit(string s) { }
        public static Unit Pixel(int n) => new Unit(n.ToString());
        public static Unit Percentage(double n) => new Unit(n.ToString());
        public override string ToString() => "";
    }

    public class Style { public string CssClass { get; set; } public Unit Width { get; set; } }
    public class TableItemStyle : Style { }

    public class WebControl : System.Web.UI.Control
    {
        public string CssClass { get; set; }
        public string ToolTip { get; set; }
        public bool Enabled { get; set; } = true;
        public Unit Width { get; set; }
        public Unit Height { get; set; }
        public string OnClientClick { get; set; }
        public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>();
    }

    // CompositeControl lives only in System.Web.UI — do not duplicate here

    public class Button : WebControl
    {
        public string Text { get; set; }
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
        public bool CausesValidation { get; set; }
        public short TabIndex { get; set; }
        public event EventHandler Click;
        public event EventHandler<dynamic> Command;
    }

    public class LinkButton : WebControl
    {
        public string Text { get; set; }
        public string CommandName { get; set; }
        public string CommandArgument { get; set; }
        public bool CausesValidation { get; set; }
        public event EventHandler Click;
    }

    public class ImageButton : WebControl
    {
        public string ImageUrl { get; set; }
        public string AlternateText { get; set; }
        public bool CausesValidation { get; set; }
        public event EventHandler<dynamic> Click;
    }

    public class TextBox : WebControl
    {
        public string Text { get; set; }
        public int MaxLength { get; set; }
        public bool ReadOnly { get; set; }
        public TextBoxMode TextMode { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
    }

    public class DropDownList : WebControl
    {
        public dynamic Items => null;
        public string SelectedValue { get; set; }
        public dynamic SelectedItem => null;
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }
        public dynamic DataSource { get; set; }
        public event EventHandler DataBound { add { } remove { } }
        public event EventHandler SelectedIndexChanged { add { } remove { } }
    }

    public class ListBox : WebControl
    {
        public dynamic Items => null;
        public string SelectedValue { get; set; }
        public ListSelectionMode SelectionMode { get; set; }
    }

    public class HiddenField : System.Web.UI.Control { public string Value { get; set; } }

    public class Label : WebControl { public string Text { get; set; } public string AssociatedControlID { get; set; } }
    public class Literal : WebControl { public string Text { get; set; } }

    public class Panel : WebControl
    {
        public string DefaultButton { get; set; }
        public new Dictionary<string, string> Style { get; } = new Dictionary<string, string>();
    }
    public class PlaceHolder : System.Web.UI.PlaceHolder { }
    public class ContentPlaceHolder : System.Web.UI.PlaceHolder { }

    public class Table : WebControl { public dynamic Rows => null; public GridLines GridLines { get; set; } }
    public class TableRow : WebControl { public dynamic Cells => null; public TableRowSection TableSection { get; set; } }
    public class TableCell : WebControl { public string Text { get; set; } public int ColumnSpan { get; set; } public int RowSpan { get; set; } }
    public class TableHeaderCell : WebControl { public string Text { get; set; } }
    public class TableHeaderRow : WebControl { public dynamic Cells => null; public TableRowSection TableSection { get; set; } }

    public class GridView : WebControl
    {
        public dynamic Rows => null;
        public dynamic HeaderRow => null;
        public bool AutoGenerateColumns { get; set; }
        public dynamic Columns => null;
        public bool AllowPaging { get; set; }
        public bool AllowSorting { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int PageCount => 0;
        public bool ShowHeader { get; set; } = true;
        public bool ShowFooter { get; set; }
        public string DataKeyNames { get; set; }
        public dynamic DataKeys => null;
        public dynamic SelectedRow => null;
        public dynamic PagerStyle { get; } = new TableItemStyle();
        public GridLines GridLines { get; set; }
        public string EmptyDataText { get; set; }
        public dynamic DataSource { get; set; }
        public void DataBind() { }
        public event EventHandler<System.Web.UI.WebControls.GridViewRowEventArgs> RowDataBound;
        public event EventHandler<System.Web.UI.WebControls.GridViewRowEventArgs> DataBound;
        public event EventHandler<dynamic> RowCreated;
        public event EventHandler<dynamic> PageIndexChanging;
        public event EventHandler<dynamic> Sorting;
        public BorderStyle BorderStyle { get; set; }
    }

    public class ListItem : IDisposable
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
        public ListItem() { }
        public ListItem(string text) { Text = text; }
        public ListItem(string text, string value) { Text = text; Value = value; }
        public void Dispose() { }
    }

    public class ListControl : WebControl { public dynamic Items => null; public string SelectedValue { get; set; } public dynamic SelectedItem => null; }
    public class CheckBoxList : WebControl { }
    public class RadioButtonList : WebControl { }
    public class CheckBox : WebControl { public bool Checked { get; set; } public string Text { get; set; } public event EventHandler CheckedChanged; }
    public class RadioButton : WebControl { public bool Checked { get; set; } public string Text { get; set; } public string GroupName { get; set; } }

    public class Image : WebControl { public string ImageUrl { get; set; } public string AlternateText { get; set; } }
    public class HyperLink : WebControl { public string Text { get; set; } public string NavigateUrl { get; set; } public string Target { get; set; } }

    public class FileUpload : WebControl
    {
        public bool HasFile { get; set; }
        public System.IO.Stream FileContent { get; set; }
        public string FileName { get; set; }
        public int FileBytes { get; set; }
        public dynamic PostedFile { get; set; }
        public void SaveAs(string path) { }
    }

    public class RequiredFieldValidator : WebControl
    {
        public string ControlToValidate { get; set; }
        public string ErrorMessage { get; set; }
        public bool EnableClientScript { get; set; }
        public ValidatorDisplay Display { get; set; }
        public bool SetFocusOnError { get; set; }
    }

    public class RegularExpressionValidator : WebControl
    {
        public string ControlToValidate { get; set; }
        public string ErrorMessage { get; set; }
        public bool EnableClientScript { get; set; }
        public string ValidationExpression { get; set; }
        public ValidatorDisplay Display { get; set; }
        public bool SetFocusOnError { get; set; }
    }

    public class CompareValidator : WebControl
    {
        public string ControlToValidate { get; set; }
        public string ControlToCompare { get; set; }
        public string ErrorMessage { get; set; }
        public ValidationCompareOperator Operator { get; set; }
        public ValidationDataType Type { get; set; }
        public ValidatorDisplay Display { get; set; }
    }

    public class RangeValidator : WebControl
    {
        public string ControlToValidate { get; set; }
        public string ErrorMessage { get; set; }
        public string MinimumValue { get; set; }
        public string MaximumValue { get; set; }
        public ValidationDataType Type { get; set; }
    }

    public class BoundField { public string DataField { get; set; } public string HeaderText { get; set; } public bool Visible { get; set; } = true; }
    public class TemplateField
    {
        public string HeaderText { get; set; }
        public dynamic ItemTemplate { get; set; }
        public dynamic FooterTemplate { get; set; }
        public dynamic HeaderTemplate { get; set; }
        public TableItemStyle ItemStyle { get; } = new TableItemStyle();
        public TableItemStyle HeaderStyle { get; } = new TableItemStyle();
        public bool Visible { get; set; } = true;
    }

    public enum UpdatePanelUpdateMode { Always, Conditional }
    public class UpdatePanel : WebControl
    {
        public bool ChildrenAsTriggers { get; set; }
        public UpdatePanelUpdateMode UpdateMode { get; set; }
        public dynamic ContentTemplateContainer { get; } = new System.Web.UI.HtmlControls.HtmlGenericControl("div");
    }

    public class ScriptManager : Control
    {
        public static dynamic GetCurrent(dynamic page) => null;
        public static void RegisterStartupScript(dynamic page, Type t, string key, string script, bool addScriptTags) { }
        public static void RegisterClientScriptInclude(dynamic page, Type t, string key, string url) { }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WebResourceAttribute : Attribute { public WebResourceAttribute(string r, string c) { } public bool PerformSubstitution { get; set; } }
}

// -----------------------------------------------------------------------
// System.Web.UI.HtmlControls
// -----------------------------------------------------------------------
namespace System.Web.UI.HtmlControls
{
    public enum ClientIDMode { Inherit, AutoID, Predictable, Static }

    public class HtmlControl : System.Web.UI.Control
    {
        public HtmlControl() { }
        public string TagName { get; set; }
        public Dictionary<string, string> Attributes { get; } = new Dictionary<string, string>();
    }

    public class HtmlContainerControl : HtmlControl
    {
        public string InnerText { get; set; }
        public string InnerHtml { get; set; }
    }

    public class HtmlGenericControl : HtmlContainerControl
    {
        public HtmlGenericControl() { }
        public HtmlGenericControl(string tag) { TagName = tag; }
        public Dictionary<string, string> Style { get; } = new Dictionary<string, string>();
        public new List<object> Cells { get; } = new List<object>();
        public new List<object> Rows { get; } = new List<object>();
    }

    public class HtmlAnchor : HtmlContainerControl
    {
        public string HRef { get; set; }
        public string Name { get; set; }
        public string Target { get; set; }
        public string Title { get; set; }
        public dynamic NamingContainer => null;
        public event EventHandler DataBinding { add { } remove { } }
        public event EventHandler ServerClick { add { } remove { } }
    }

    public class HtmlButton : HtmlContainerControl
    {
        public HtmlButton() { }
        public string Type { get; set; }
        public event EventHandler ServerClick { add { } remove { } }
    }

    public class HtmlInputControl : HtmlControl
    {
        public string Value { get; set; }
        public string Type { get; set; }
    }

    public class HtmlInputText : HtmlInputControl { public int MaxLength { get; set; } public bool Disabled { get; set; } }
    public class HtmlInputPassword : HtmlInputControl { }
    public class HtmlInputHidden : HtmlInputControl { }
    public class HtmlInputFile : HtmlInputControl { public dynamic PostedFile { get; } }

    public class HtmlInputButton : HtmlInputControl
    {
        public event EventHandler ServerClick { add { } remove { } }
    }

    public class HtmlInputRadioButton : HtmlInputControl
    {
        public bool Checked { get; set; }
        public string Name { get; set; }
        public ClientIDMode ClientIDMode { get; set; }
    }

    public class HtmlInputCheckBox : HtmlInputControl { public bool Checked { get; set; } }
    public class HtmlInputReset : HtmlInputControl { }

    public class HtmlTextArea : HtmlControl { public string Value { get; set; } public int Rows { get; set; } public int Cols { get; set; } }
    public class HtmlSelect : HtmlControl { public string Value { get; set; } public dynamic Items { get; } public dynamic DataSource { get; set; } public string DataTextField { get; set; } public string DataValueField { get; set; } public string SelectedValue { get; set; } public void DataBind() { } }
    public class HtmlImage : HtmlControl { public string Src { get; set; } public string Alt { get; set; } }
    public class HtmlLink : HtmlControl { public string Href { get; set; } }
    public class HtmlForm : HtmlContainerControl { public string Action { get; set; } public string Method { get; set; } }
    public class HtmlMeta : HtmlControl { public string Content { get; set; } public string HttpEquiv { get; set; } }

    public class HtmlTable : HtmlGenericControl
    {
        public HtmlTable() : base("table") { }
        public new List<object> Rows { get; } = new List<object>();
    }

    public class HtmlTableRow : HtmlGenericControl
    {
        public HtmlTableRow() : base("tr") { }
        public new List<object> Cells { get; } = new List<object>();
    }

    public class HtmlTableCell : HtmlGenericControl
    {
        public HtmlTableCell() : base("td") { }
        public int ColSpan { get; set; }
        public int RowSpan { get; set; }
    }

}

// -----------------------------------------------------------------------
// System.Web.UI extensions (methods missing from SystemWebAdapters - must be in System.Web.UI
// namespace so that files with "using System.Web.UI;" automatically find the extension methods)
// -----------------------------------------------------------------------
namespace System.Web.UI
{
    public static class HttpServerUtilityExtensions
    {
        public static string HtmlDecode(this System.Web.HttpServerUtility server, string s)
            => System.Net.WebUtility.HtmlDecode(s) ?? s;
        public static string HtmlEncode(this System.Web.HttpServerUtility server, string s)
            => System.Net.WebUtility.HtmlEncode(s) ?? s;
        public static string UrlDecode(this System.Web.HttpServerUtility server, string s)
            => Uri.UnescapeDataString(s ?? string.Empty);
        public static string UrlEncode(this System.Web.HttpServerUtility server, string s)
            => Uri.EscapeDataString(s ?? string.Empty);
    }
}

// -----------------------------------------------------------------------
// System.Web.Services
// -----------------------------------------------------------------------
namespace System.Web.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WebServiceAttribute : Attribute
    {
        public string Namespace { get; set; }
        public string Description { get; set; }
        public WebServiceAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WebMethodAttribute : Attribute
    {
        public bool EnableSession { get; set; }
        public string Description { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class WebServiceBindingAttribute : Attribute
    {
        public WsiProfiles ConformsTo { get; set; }
        public string Name { get; set; }
        public WebServiceBindingAttribute() { }
    }

    public enum WsiProfiles { None, BasicProfile1_1 }

    public class WebService
    {
        public System.Web.HttpContext Context => System.Web.HttpContext.Current;
        public System.Web.HttpServerUtility Server => System.Web.HttpContext.Current?.Server;
        public System.Web.HttpRequest Request => System.Web.HttpContext.Current?.Request;
        public System.Web.HttpResponse Response => System.Web.HttpContext.Current?.Response;
        public dynamic Session => System.Web.HttpContext.Current?.Session;
    }
}

// -----------------------------------------------------------------------
// System.Web.Security
// -----------------------------------------------------------------------
namespace System.Web.Security
{
    public static class FormsAuthentication
    {
        public static string LoginUrl => "/SignIn.aspx";
        public static string FormsCookieName => ".ASPXAUTH";
        public static string FormsCookiePath => "/";
        public static string CookieDomain => string.Empty;
        public static void SignOut() { }
        public static void RedirectToLoginPage() { }
        public static void RedirectToLoginPage(string extraQueryString) { }
        public static string GetRedirectUrl(string userName, bool createPersistentCookie) => LoginUrl;
        public static string Encrypt(FormsAuthenticationTicket ticket) => string.Empty;
        public static FormsAuthenticationTicket Decrypt(string encryptedTicket) => null;
    }

    public sealed class FormsAuthenticationTicket
    {
        public FormsAuthenticationTicket(int version, string name, System.DateTime issueDate, System.DateTime expiration, bool isPersistent, string userData) { }
        public FormsAuthenticationTicket(int version, string name, System.DateTime issueDate, System.DateTime expiration, bool isPersistent, string userData, string cookiePath) { }
        public string Name { get; }
        public string UserData { get; }
        public System.DateTime Expiration { get; }
        public bool IsPersistent { get; }
        public string CookiePath { get; }
        public bool Expired { get; }
    }
}

// -----------------------------------------------------------------------
// System.Web.Script.Services
// -----------------------------------------------------------------------
namespace System.Web.Script.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ScriptServiceAttribute : Attribute { }
}

// -----------------------------------------------------------------------
// System.Web.Script.Serialization
// -----------------------------------------------------------------------
namespace System.Web.Script.Serialization
{
    public class JavaScriptSerializer
    {
        public string Serialize(object obj) => System.Text.Json.JsonSerializer.Serialize(obj);
        public T Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json);
        public object DeserializeObject(string json) => System.Text.Json.JsonSerializer.Deserialize<object>(json);
    }
}

// -----------------------------------------------------------------------
// System.Web.Routing
// -----------------------------------------------------------------------
namespace System.Web.Routing
{
    public class RouteCollection : System.Collections.ObjectModel.Collection<object>
    {
        public void Ignore(string url) { }
        public void Ignore(string url, object constraints) { }
        public void MapPageRoute(string routeName, string routeUrl, string physicalFile) { }
        public void MapPageRoute(string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess) { }
        public void MapPageRoute(string routeName, string routeUrl, string physicalFile, bool checkPhysicalUrlAccess, object defaults) { }
    }
    public class Route { public Route(string url, object handler) { } }
    public static class RouteTable { public static RouteCollection Routes { get; } = new RouteCollection(); }
}

// -----------------------------------------------------------------------
// Owin / Microsoft.Owin
// -----------------------------------------------------------------------
namespace Owin
{
    public interface IAppBuilder { }
    public static class AppBuilderExtensions
    {
        public static IAppBuilder MapSignalR(this IAppBuilder app) => app;
        public static IAppBuilder MapSignalR(this IAppBuilder app, string path) => app;
    }
}
namespace Microsoft.Owin
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class OwinStartupAttribute : Attribute
    {
        public OwinStartupAttribute(Type startupType) { }
        public OwinStartupAttribute(string friendlyName, Type startupType) { }
    }
}

// -----------------------------------------------------------------------
// Microsoft.AspNet.SignalR
// -----------------------------------------------------------------------
namespace Microsoft.AspNet.SignalR
{
    public abstract class Hub : IDisposable
    {
        public dynamic Clients { get; } = null;
        public dynamic Context { get; } = null;
        public virtual void Dispose() { }
    }
}

// -----------------------------------------------------------------------
// MixERP.Net.FrontEnd.Base
// -----------------------------------------------------------------------
namespace MixERP.Net.FrontEnd.Base
{
    public abstract class MixERPUserControl : MixERP.Net.Framework.Controls.MixERPUserControlBase, IDisposable
    {
        public virtual bool IsLandingPage { get; set; }

        public override MixERP.Net.Framework.Controls.AccessLevel AccessLevel
            => MixERP.Net.Framework.Controls.AccessLevel.PolicyBased;

        public virtual System.Web.HttpRequest Request => System.Web.HttpContext.Current?.Request;
        public virtual System.Web.HttpResponse Response => System.Web.HttpContext.Current?.Response;
        public virtual System.Web.HttpServerUtility Server => System.Web.HttpContext.Current?.Server;
        public virtual string ResolveUrl(string relativeUrl) => relativeUrl;
        public virtual System.Web.UI.Page Page { get; set; }
        public System.Collections.Generic.List<object> Controls { get; } = new System.Collections.Generic.List<object>();
        public bool IsRestrictedMode => false;
        public void Initialize() { this.OnControlLoad(this, new EventArgs()); }
        private bool _userControlDisposed;
        public virtual void Dispose()
        {
            if (!_userControlDisposed) { Dispose(true); _userControlDisposed = true; }
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing) { }
    }

    public abstract class TransactionCheckListControl : MixERPUserControl
    {
        protected TransactionCheckListControl()
        {
            this.ValidateRequestMode = System.Web.UI.ValidateRequestMode.Disabled;
        }
        public System.Web.UI.ValidateRequestMode ValidateRequestMode { get; set; }
    }
}

// -----------------------------------------------------------------------
// MixERP.Net.FrontEnd.Controls
// -----------------------------------------------------------------------
namespace MixERP.Net.FrontEnd.Controls
{
    public sealed class Scrud : MixERP.Net.WebControls.ScrudFactory.ScrudForm
    {
        public Scrud()
        {
            this.Catalog = MixERP.Net.ApplicationState.Cache.AppUsers.GetCurrentUserDB();
        }
    }

    public sealed class Checklist : MixERP.Net.WebControls.TransactionChecklist.TransactionChecklistForm
    {
        public Checklist()
        {
            this.Catalog = MixERP.Net.ApplicationState.Cache.AppUsers.GetCurrentUserDB();
        }
    }

    public sealed class WebReport : MixERP.Net.WebControls.ReportEngine.Report
    {
        public WebReport()
        {
            this.Catalog = MixERP.Net.ApplicationState.Cache.AppUsers.GetCurrentUserDB();
        }
    }
}
