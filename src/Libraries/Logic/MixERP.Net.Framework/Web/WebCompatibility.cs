using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace MixERP.Net.Framework.Web
{
    /// <summary>
    /// Request scope used while legacy screens are incrementally re-authored as ASP.NET Core components.
    /// The middleware sets the current context; non-web callers receive an isolated in-memory context.
    /// </summary>
    public sealed class HttpContext
    {
        private static readonly AsyncLocal<HttpContext> CurrentScope = new AsyncLocal<HttpContext>();
        private readonly Microsoft.AspNetCore.Http.HttpContext coreContext;

        public HttpContext(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.coreContext = context;
            this.Request = new HttpRequest(context?.Request);
            this.Response = new HttpResponse(context?.Response);
            this.Session = new SessionState.HttpSessionState(context?.Session);
            this.Server = new HttpServerUtility(context);
            this.Items = context?.Items ?? new Dictionary<object, object>();
            this.User = context?.User;
        }

        public static HttpContext Current
        {
            get => CurrentScope.Value;
            set => CurrentScope.Value = value;
        }

        public Microsoft.AspNetCore.Http.HttpContext CoreContext => this.coreContext;
        public HttpRequest Request { get; }
        public HttpResponse Response { get; }
        public SessionState.HttpSessionState Session { get; }
        public HttpServerUtility Server { get; }
        public IDictionary<object, object> Items { get; }
        public IPrincipal User { get; set; }
        public object Handler { get; set; }
    }

    public sealed class HttpRequest
    {
        private readonly Microsoft.AspNetCore.Http.HttpRequest request;
        private readonly NameValueCollection query = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        private readonly NameValueCollection form = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        private readonly NameValueCollection serverVariables = new NameValueCollection(StringComparer.OrdinalIgnoreCase);

        internal HttpRequest(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            this.request = request;
            if (request == null) return;
            foreach (var value in request.Query) this.query[value.Key] = value.Value.ToString();
            this.serverVariables["REMOTE_ADDR"] = request.HttpContext.Connection.RemoteIpAddress?.ToString();
            this.serverVariables["HTTP_X_FORWARDED_FOR"] = request.Headers["X-Forwarded-For"].ToString();
        }

        public NameValueCollection QueryString => this.query;
        public NameValueCollection Form => this.form;
        public NameValueCollection Params
        {
            get
            {
                var values = new NameValueCollection(this.query);
                values.Add(this.form);
                return values;
            }
        }
        public NameValueCollection ServerVariables => this.serverVariables;
        public Uri Url
        {
            get
            {
                string scheme = this.request?.Scheme ?? "http";
                string host = this.request?.Host.Value ?? "localhost";
                string path = (this.request?.PathBase + this.request?.Path + this.request?.QueryString).ToString();
                return new Uri($"{scheme}://{host}{path}");
            }
        }
        public Uri UrlReferrer => Uri.TryCreate(this.request?.GetTypedHeaders().Referer?.ToString(), UriKind.Absolute, out var value) ? value : null;
        public string RawUrl => (this.request?.PathBase + this.request?.Path + this.request?.QueryString).ToString();
        public string Path => this.request?.Path.Value ?? string.Empty;
        public string PhysicalApplicationPath => AppContext.BaseDirectory;
        public string HttpMethod => this.request?.Method ?? "GET";
        public string UserAgent => this.request?.Headers.UserAgent.ToString() ?? string.Empty;
        public string UserHostAddress => this.request?.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        public bool IsLocal => this.request?.HttpContext.Connection.RemoteIpAddress == null || IPAddress.IsLoopback(this.request.HttpContext.Connection.RemoteIpAddress);
        public Stream InputStream => this.request?.Body ?? Stream.Null;
        public HttpBrowserCapabilities Browser => new HttpBrowserCapabilities(this.UserAgent);
        public HttpFileCollection Files => new HttpFileCollection(this.request);
        public string this[string key] => this.Params[key];
    }

    public sealed class HttpResponse
    {
        private readonly Microsoft.AspNetCore.Http.HttpResponse response;
        private readonly StringBuilder output = new StringBuilder();

        internal HttpResponse(Microsoft.AspNetCore.Http.HttpResponse response) => this.response = response;
        public int StatusCode { get => this.response?.StatusCode ?? 200; set { if (this.response != null) this.response.StatusCode = value; } }
        public string ContentType { get => this.response?.ContentType; set { if (this.response != null) this.response.ContentType = value; } }
        public Encoding ContentEncoding { get; set; } = Encoding.UTF8;
        public Stream OutputStream => this.response?.Body ?? Stream.Null;
        public TextWriter Output => new StringWriter(this.output, CultureInfo.InvariantCulture);
        public IHeaderDictionary Headers => this.response?.Headers ?? new HeaderDictionary();
        public HttpCookieCollection Cookies { get; } = new HttpCookieCollection();
        public bool IsClientConnected => !this.response?.HttpContext.RequestAborted.IsCancellationRequested ?? true;
        public void Clear() => this.output.Clear();
        public void ClearContent() => this.Clear();
        public void ClearHeaders() { if (this.response != null) this.response.Headers.Clear(); }
        public void End() { }
        public void Flush() { }
        public void Write(string value)
        {
            if (value == null) return;
            this.output.Append(value);
            if (this.response != null) this.response.WriteAsync(value).GetAwaiter().GetResult();
        }
        public void BinaryWrite(byte[] value)
        {
            if (value != null && this.response != null) this.response.Body.Write(value, 0, value.Length);
        }
        public void Redirect(string location) => this.Redirect(location, false);
        public void Redirect(string location, bool endResponse)
        {
            if (this.response != null) this.response.Redirect(VirtualPathUtility.ToAbsolute(location));
        }
    }

    public sealed class HttpServerUtility
    {
        private readonly Microsoft.AspNetCore.Http.HttpContext context;
        internal HttpServerUtility(Microsoft.AspNetCore.Http.HttpContext context) => this.context = context;
        public string MapPath(string path)
        {
            string relative = (path ?? string.Empty).TrimStart('~').TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
        }
        public string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
        public string HtmlDecode(string value) => WebUtility.HtmlDecode(value);
        public string UrlEncode(string value) => WebUtility.UrlEncode(value);
        public string UrlDecode(string value) => WebUtility.UrlDecode(value);
        public Exception GetLastError() => this.context?.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        public void ClearError() { }
    }

    public static class HttpUtility
    {
        public static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
        public static string HtmlDecode(string value) => WebUtility.HtmlDecode(value);
        public static string UrlEncode(string value) => WebUtility.UrlEncode(value);
        public static string UrlDecode(string value) => WebUtility.UrlDecode(value);
        public static NameValueCollection ParseQueryString(string query)
        {
            var result = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query ?? string.Empty)) result[pair.Key] = pair.Value.ToString();
            return result;
        }
    }

    public static class VirtualPathUtility
    {
        public static string ToAbsolute(string path) => string.IsNullOrEmpty(path) ? "/" : (path[0] == '~' ? path.Substring(1) : path);
        public static string GetDirectory(string path) => Path.GetDirectoryName(path)?.Replace('\\', '/') + "/";
    }

    public sealed class HttpBrowserCapabilities
    {
        public HttpBrowserCapabilities(string userAgent = "")
        {
            this.Browser = string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent;
            this.Browsers = new ArrayList { this.Browser };
        }
        public string Browser { get; set; }
        public ArrayList Browsers { get; }
        public bool IsMobileDevice => this.Browser.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public sealed class HttpFileCollection : IEnumerable<HttpPostedFile>
    {
        private readonly Microsoft.AspNetCore.Http.HttpRequest request;
        internal HttpFileCollection(Microsoft.AspNetCore.Http.HttpRequest request) => this.request = request;
        public int Count => this.request?.HasFormContentType == true ? this.request.Form.Files.Count : 0;
        public HttpPostedFile this[int index] => new HttpPostedFile(this.request.Form.Files[index]);
        public HttpPostedFile this[string name] => new HttpPostedFile(this.request.Form.Files.GetFile(name));
        public IEnumerator<HttpPostedFile> GetEnumerator() => (this.request?.Form.Files ?? new FormFileCollection()).Select(x => new HttpPostedFile(x)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public sealed class HttpPostedFile
    {
        private readonly IFormFile file;
        internal HttpPostedFile(IFormFile file) => this.file = file;
        public string FileName => this.file?.FileName;
        public int ContentLength => checked((int)(this.file?.Length ?? 0));
        public string ContentType => this.file?.ContentType;
        public Stream InputStream => this.file?.OpenReadStream() ?? Stream.Null;
        public void SaveAs(string path) { using var stream = File.Create(path); this.file?.CopyTo(stream); }
    }

    public sealed class HttpCookie
    {
        public HttpCookie(string name) : this(name, null) { }
        public HttpCookie(string name, string value) { this.Name = name; this.Value = value; }
        public string Name { get; }
        public string Value { get; set; }
        public DateTime Expires { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string Path { get; set; } = "/";
    }

    public sealed class HttpCookieCollection : IEnumerable<HttpCookie>
    {
        private readonly Dictionary<string, HttpCookie> cookies = new Dictionary<string, HttpCookie>(StringComparer.OrdinalIgnoreCase);
        public HttpCookie this[string name] { get => this.cookies.TryGetValue(name, out var value) ? value : null; set => this.cookies[name] = value; }
        public void Add(HttpCookie cookie) { if (cookie != null) this.cookies[cookie.Name] = cookie; }
        public void Remove(string name) => this.cookies.Remove(name);
        public IEnumerator<HttpCookie> GetEnumerator() => this.cookies.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public class HttpApplication { }
    public class HttpException : Exception { public HttpException() { } public HttpException(string message) : base(message) { } public HttpException(int code, string message) : base(message) => this.GetHttpCodeValue = code; private int GetHttpCodeValue { get; } public int GetHttpCode() => this.GetHttpCodeValue; }
    public interface IHttpHandler { bool IsReusable { get; } void ProcessRequest(HttpContext context); }
    public interface IHtmlString { string ToHtmlString(); }
    public sealed class HtmlString : IHtmlString { private readonly string value; public HtmlString(string value) => this.value = value ?? string.Empty; public string ToHtmlString() => this.value; public override string ToString() => this.value; }

    public class Control : IDisposable
    {
        private readonly StateBag viewState = new StateBag();
        public Control() => this.Controls = new ControlCollection(this);
        public string ID { get; set; }
        public string ClientID => this.ID ?? string.Empty;
        public virtual string UniqueID => this.ID ?? string.Empty;
        public Control Parent { get; internal set; }
        public Control NamingContainer => this.Parent;
        public ControlCollection Controls { get; }
        public virtual Page Page => this as Page ?? this.Parent?.Page;
        public bool Visible { get; set; } = true;
        public bool EnableViewState { get; set; } = true;
        protected StateBag ViewState => this.viewState;
        public virtual Control FindControl(string id) => this.Controls.FirstOrDefault(x => string.Equals(x.ID, id, StringComparison.Ordinal)) ?? this.Controls.Select(x => x.FindControl(id)).FirstOrDefault(x => x != null);
        public virtual void DataBind() { foreach (var control in this.Controls) control.DataBind(); }
        public virtual string ResolveUrl(string relativeUrl) => VirtualPathUtility.ToAbsolute(relativeUrl);
        public virtual void Dispose() { foreach (var control in this.Controls) control.Dispose(); }
    }

    public sealed class ControlCollection : IList<Control>
    {
        private readonly Control owner;
        private readonly List<Control> items = new List<Control>();
        public ControlCollection(Control owner) => this.owner = owner;
        public Control this[int index] { get => this.items[index]; set { this.items[index] = value; if (value != null) value.Parent = this.owner; } }
        public int Count => this.items.Count;
        public bool IsReadOnly => false;
        public void Add(Control item) { if (item != null) item.Parent = this.owner; this.items.Add(item); }
        public void AddAt(int index, Control item) { if (item != null) item.Parent = this.owner; this.items.Insert(index, item); }
        public void Clear() => this.items.Clear();
        public bool Contains(Control item) => this.items.Contains(item);
        public void CopyTo(Control[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex);
        public IEnumerator<Control> GetEnumerator() => this.items.GetEnumerator();
        public int IndexOf(Control item) => this.items.IndexOf(item);
        public void Insert(int index, Control item) => this.AddAt(index, item);
        public bool Remove(Control item) => this.items.Remove(item);
        public void RemoveAt(int index) => this.items.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public sealed class StateBag : Dictionary<string, object> { }
    public class UserControl : Control
    {
        public HttpRequest Request => HttpContext.Current?.Request;
        public HttpResponse Response => HttpContext.Current?.Response;
        public SessionState.HttpSessionState Session => HttpContext.Current?.Session;
        public HttpServerUtility Server => HttpContext.Current?.Server;
        public bool IsPostBack { get; set; }
    }
    public class MasterPage : UserControl { }
    public class Page : UserControl
    {
        private PageStatePersister pageStatePersister;
        public Page() { this.Header = new HtmlControls.HtmlHead(); this.Header.Parent = this; }
        public MasterPage Master { get; set; }
        public HtmlControls.HtmlHead Header { get; }
        public bool IsValid { get; set; } = true;
        public IPrincipal User => HttpContext.Current?.User;
        public ClientScriptManager ClientScript { get; } = new ClientScriptManager();
        protected virtual PageStatePersister PageStatePersister => this.pageStatePersister ??= new DefaultPageStatePersister(this);
        public void Validate() => this.IsValid = true;

        private sealed class DefaultPageStatePersister : PageStatePersister
        {
            public DefaultPageStatePersister(Page page) : base(page) { }
        }
    }
    public class PageStatePersister
    {
        protected PageStatePersister(Page page) => this.Page = page;
        protected Page Page { get; }
        public virtual void Load() { }
        public virtual void Save() { }
    }
    public enum ValidateRequestMode { Inherit, Enabled, Disabled }
    public sealed class ClientScriptManager
    {
        public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags) { }
        public void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags) { }
        public string GetWebResourceUrl(Type type, string resourceName)
        {
            string assembly = Uri.EscapeDataString(type?.Assembly.GetName().Name ?? string.Empty);
            string resource = Uri.EscapeDataString(resourceName ?? string.Empty);
            return $"/resources/{assembly}/{resource}";
        }
        public string GetPostBackEventReference(object options) => string.Empty;
    }
    public sealed class PostBackOptions { public PostBackOptions(Control target) { } }
    public sealed class HtmlTextWriter : StringWriter { public HtmlTextWriter(TextWriter writer) { this.InnerWriter = writer; } public TextWriter InnerWriter { get; } }
    public static class ScriptManager
    {
        public static void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags) { }
        public static void RegisterStartupScript(Page page, Type type, string key, string script, bool addScriptTags) { }
        public static void RegisterClientScriptBlock(Control control, Type type, string key, string script, bool addScriptTags) { }
    }
}

namespace MixERP.Net.Framework.Web.SessionState
{
    public sealed class HttpSessionState
    {
        private readonly ISession session;
        private readonly Dictionary<string, object> fallback = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        internal HttpSessionState(ISession session) => this.session = session;
        public object this[string key]
        {
            get
            {
                if (this.fallback.TryGetValue(key, out var value)) return value;
                string serialized = this.session?.GetString(key);
                return serialized;
            }
            set
            {
                this.fallback[key] = value;
                if (this.session != null) this.session.SetString(key, value?.ToString() ?? string.Empty);
            }
        }
        public string SessionID => this.session?.Id ?? string.Empty;
        public int Count => this.fallback.Count;
        public void Add(string key, object value) => this[key] = value;
        public void Remove(string key) { this.fallback.Remove(key); this.session?.Remove(key); }
        public void RemoveAll() { this.fallback.Clear(); this.session?.Clear(); }
        public void Clear() => this.RemoveAll();
        public void Abandon() => this.RemoveAll();
    }
}

namespace MixERP.Net.Framework.Web.HtmlControls
{
    using MixERP.Net.Framework.Web;
    using MixERP.Net.Framework.Web.WebControls;

    public class HtmlControl : Control
    {
        public AttributeCollection Attributes { get; } = new AttributeCollection();
        public CssStyleCollection Style { get; } = new CssStyleCollection();
        public bool Disabled { get; set; }
    }
    public class HtmlContainerControl : HtmlControl { public string InnerHtml { get; set; } public string InnerText { get; set; } }
    public class HtmlGenericControl : HtmlContainerControl { public HtmlGenericControl() : this("div") { } public HtmlGenericControl(string tag) => this.TagName = tag; public string TagName { get; } }
    public class HtmlInputControl : HtmlControl { public HtmlInputControl(string type) => this.Type = type; public string Type { get; } public string Value { get; set; } public string Name { get; set; } }
    public class HtmlInputText : HtmlInputControl { public HtmlInputText() : base("text") { } }
    public class HtmlInputHidden : HtmlInputControl { public HtmlInputHidden() : base("hidden") { } }
    public class HtmlInputCheckBox : HtmlInputControl { public HtmlInputCheckBox() : base("checkbox") { } public bool Checked { get; set; } }
    public class HtmlInputButton : HtmlInputControl { public HtmlInputButton() : base("button") { } }
    public class HtmlInputFile : HtmlInputControl { public HtmlInputFile() : base("file") { } public HttpPostedFile PostedFile { get; set; } }
    public class HtmlButton : HtmlContainerControl { public event EventHandler ServerClick; protected void OnServerClick() => this.ServerClick?.Invoke(this, EventArgs.Empty); }
    public class HtmlForm : HtmlContainerControl { public string Action { get; set; } public string Method { get; set; } = "post"; }
    public class HtmlAnchor : HtmlContainerControl { public string HRef { get; set; } public string Target { get; set; } }
    public class HtmlImage : HtmlControl { public string Src { get; set; } public string Alt { get; set; } }
    public class HtmlMeta : HtmlControl { public string Name { get; set; } public string Content { get; set; } }
    public class HtmlHead : HtmlContainerControl { public string Title { get; set; } }
}

namespace MixERP.Net.Framework.Web.WebControls
{
    using MixERP.Net.Framework.Web;

    public class AttributeCollection : Dictionary<string, string> { public void Add(string key, string value) => this[key] = value; }
    public class CssStyleCollection : Dictionary<string, string> { public void Add(string key, string value) => this[key] = value; }
    public struct Unit
    {
        private readonly string value;
        public Unit(string value) => this.value = value;
        public static Unit Parse(string value, IFormatProvider provider) => new Unit(value);
        public static Unit Parse(string value) => new Unit(value);
        public static Unit Pixel(int value) => new Unit(value + "px");
        public static Unit Percentage(double value) => new Unit(value.ToString(CultureInfo.InvariantCulture) + "%");
        public static Unit Empty => new Unit(string.Empty);
        public override string ToString() => this.value ?? string.Empty;
        public static implicit operator Unit(int value) => Pixel(value);
    }
    public class WebControl : Control
    {
        public string CssClass { get; set; }
        public Unit Width { get; set; }
        public Unit Height { get; set; }
        public bool Enabled { get; set; } = true;
        public string ToolTip { get; set; }
        public AttributeCollection Attributes { get; } = new AttributeCollection();
        public CssStyleCollection Style { get; } = new CssStyleCollection();
        public BorderStyle BorderStyle { get; set; }
        public Unit BorderWidth { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
    }
    public class PlaceHolder : Control { }
    public class ContentPlaceHolder : PlaceHolder { }
    public class LiteralControl : Control { public LiteralControl() { } public LiteralControl(string text) => this.Text = text; public string Text { get; set; } }
    public class Literal : Control { public string Text { get; set; } public LiteralMode Mode { get; set; } }
    public class Label : WebControl { public string Text { get; set; } public string AssociatedControlID { get; set; } }
    public class Panel : WebControl { public string DefaultButton { get; set; } public string GroupingText { get; set; } }
    public class Button : WebControl { public string Text { get; set; } public string OnClientClick { get; set; } public string CommandName { get; set; } public string CommandArgument { get; set; } public event EventHandler Click; public event CommandEventHandler Command; protected void OnClick() => this.Click?.Invoke(this, EventArgs.Empty); protected void OnCommand() => this.Command?.Invoke(this, new CommandEventArgs(this.CommandName, this.CommandArgument)); }
    public class LinkButton : Button { public bool CausesValidation { get; set; } = true; }
    public class ImageButton : Button { public string ImageUrl { get; set; } public string AlternateText { get; set; } }
    public class HyperLink : WebControl { public string Text { get; set; } public string NavigateUrl { get; set; } public string Target { get; set; } }
    public class TextBox : WebControl { public string Text { get; set; } public TextBoxMode TextMode { get; set; } public int MaxLength { get; set; } public int Rows { get; set; } public bool ReadOnly { get; set; } public bool AutoPostBack { get; set; } public event EventHandler TextChanged; }
    public class HiddenField : Control { public string Value { get; set; } public event EventHandler ValueChanged; }
    public class CheckBox : WebControl { public bool Checked { get; set; } public string Text { get; set; } public bool AutoPostBack { get; set; } public event EventHandler CheckedChanged; }
    public class RadioButton : CheckBox { public string GroupName { get; set; } }
    public class ListItem
    {
        public ListItem() { }
        public ListItem(string text) : this(text, text) { }
        public ListItem(string text, string value) { this.Text = text; this.Value = value; }
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
        public bool Enabled { get; set; } = true;
        public AttributeCollection Attributes { get; } = new AttributeCollection();
    }
    public class ListItemCollection : IList<ListItem>
    {
        private readonly List<ListItem> items = new List<ListItem>();
        public ListItem this[int index] { get => this.items[index]; set => this.items[index] = value; }
        public ListItem this[string value] => this.FindByValue(value);
        public int Count => this.items.Count; public bool IsReadOnly => false;
        public void Add(ListItem item) => this.items.Add(item); public void Add(string item) => this.items.Add(new ListItem(item)); public void Clear() => this.items.Clear(); public bool Contains(ListItem item) => this.items.Contains(item); public void CopyTo(ListItem[] array, int arrayIndex) => this.items.CopyTo(array, arrayIndex); public IEnumerator<ListItem> GetEnumerator() => this.items.GetEnumerator(); public int IndexOf(ListItem item) => this.items.IndexOf(item); public void Insert(int index, ListItem item) => this.items.Insert(index, item); public bool Remove(ListItem item) => this.items.Remove(item); public void RemoveAt(int index) => this.items.RemoveAt(index); IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public ListItem FindByValue(string value) => this.items.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.Ordinal));
        public ListItem FindByText(string text) => this.items.FirstOrDefault(x => string.Equals(x.Text, text, StringComparison.Ordinal));
    }
    public class ListControl : WebControl
    {
        public ListItemCollection Items { get; } = new ListItemCollection();
        public object DataSource { get; set; }
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }
        public bool AutoPostBack { get; set; }
        public int SelectedIndex { get => this.Items.ToList().FindIndex(x => x.Selected); set { for (int i = 0; i < this.Items.Count; i++) this.Items[i].Selected = i == value; } }
        public string SelectedValue { get => this.Items.FirstOrDefault(x => x.Selected)?.Value ?? string.Empty; set { var item = this.Items.FindByValue(value); if (item != null) { foreach (var current in this.Items) current.Selected = false; item.Selected = true; } } }
        public ListItem SelectedItem => this.Items.FirstOrDefault(x => x.Selected);
        public void ClearSelection()
        {
            foreach (var item in this.Items) item.Selected = false;
        }
        public event EventHandler SelectedIndexChanged;
    }
    public class DropDownList : ListControl { }
    public class RadioButtonList : ListControl { public RepeatDirection RepeatDirection { get; set; } public RepeatLayout RepeatLayout { get; set; } }
    public class ListBox : ListControl { public ListSelectionMode SelectionMode { get; set; } public int Rows { get; set; } }
    public class FileUpload : WebControl { public bool HasFile => this.PostedFile != null; public HttpPostedFile PostedFile { get; set; } public string FileName => this.PostedFile?.FileName; public byte[] FileBytes { get { using var ms = new MemoryStream(); this.PostedFile?.InputStream.CopyTo(ms); return ms.ToArray(); } } public void SaveAs(string path) => this.PostedFile?.SaveAs(path); }
    public class Image : WebControl { public string ImageUrl { get; set; } public string AlternateText { get; set; } }
    public class Table : WebControl { public TableRowCollection Rows { get; } = new TableRowCollection(); public int CellPadding { get; set; } public int CellSpacing { get; set; } public GridLines GridLines { get; set; } }
    public class TableRow : WebControl { public TableCellCollection Cells { get; } = new TableCellCollection(); public TableRowSection TableSection { get; set; } public HorizontalAlign HorizontalAlign { get; set; } public VerticalAlign VerticalAlign { get; set; } }
    public class TableHeaderRow : TableRow { }
    public class TableCell : WebControl { public string Text { get; set; } public int ColumnSpan { get; set; } public int RowSpan { get; set; } public HorizontalAlign HorizontalAlign { get; set; } public VerticalAlign VerticalAlign { get; set; } }
    public class TableHeaderCell : TableCell { public string Scope { get; set; } }
    public class TableRowCollection : List<TableRow> { }
    public class TableCellCollection : List<TableCell> { }
    public class GridView : WebControl { public object DataSource { get; set; } public bool AutoGenerateColumns { get; set; } public DataControlFieldCollection Columns { get; } = new DataControlFieldCollection(); public GridViewRowCollection Rows { get; } = new GridViewRowCollection(); public GridViewRow HeaderRow { get; set; } public GridViewRow FooterRow { get; set; } public int PageIndex { get; set; } public int PageSize { get; set; } = 10; public bool AllowPaging { get; set; } public bool AllowSorting { get; set; } public string SortExpression { get; set; } public event GridViewRowEventHandler RowDataBound; public event GridViewCommandEventHandler RowCommand; public event EventHandler<PageEventArgs> PageIndexChanging; }
    public class GridViewRow : TableRow { public GridViewRow(int rowIndex, int dataSourceIndex, DataControlRowType rowType, DataControlRowState rowState) { this.RowIndex = rowIndex; this.DataItemIndex = dataSourceIndex; this.RowType = rowType; this.RowState = rowState; } public int RowIndex { get; } public int DataItemIndex { get; } public DataControlRowType RowType { get; } public DataControlRowState RowState { get; } public object DataItem { get; set; } }
    public class GridViewRowCollection : List<GridViewRow> { }
    public abstract class DataControlField { public string HeaderText { get; set; } public bool Visible { get; set; } public string SortExpression { get; set; } public string ItemStyleCssClass { get; set; } public TableItemStyle ItemStyle { get; } = new TableItemStyle(); public TableItemStyle HeaderStyle { get; } = new TableItemStyle(); }
    public class BoundField : DataControlField { public string DataField { get; set; } public string DataFormatString { get; set; } public bool HtmlEncode { get; set; } = true; public string NullDisplayText { get; set; } }
    public class TemplateField : DataControlField { public ITemplate ItemTemplate { get; set; } public ITemplate HeaderTemplate { get; set; } public ITemplate FooterTemplate { get; set; } }
    public class HyperLinkField : DataControlField { public string DataTextField { get; set; } public string DataNavigateUrlFormatString { get; set; } public string[] DataNavigateUrlFields { get; set; } }
    public class ButtonField : DataControlField { public string Text { get; set; } public string CommandName { get; set; } public ButtonType ButtonType { get; set; } }
    public class DataControlFieldCollection : List<DataControlField> { }
    public class TableItemStyle { public Unit Width { get; set; } public HorizontalAlign HorizontalAlign { get; set; } public VerticalAlign VerticalAlign { get; set; } public string CssClass { get; set; } }
    public interface ITemplate { void InstantiateIn(Control container); }
    public class Repeater : WebControl { public object DataSource { get; set; } public RepeaterItemCollection Items { get; } = new RepeaterItemCollection(); public event RepeaterItemEventHandler ItemDataBound; public event RepeaterCommandEventHandler ItemCommand; }
    public class RepeaterItem : Control { public RepeaterItem(int itemIndex, ListItemType itemType) { this.ItemIndex = itemIndex; this.ItemType = itemType; } public int ItemIndex { get; } public ListItemType ItemType { get; } public object DataItem { get; set; } }
    public class RepeaterItemCollection : List<RepeaterItem> { }
    public class UpdatePanel : Panel { public UpdatePanelUpdateMode UpdateMode { get; set; } public bool ChildrenAsTriggers { get; set; } public void Update() { } }
    public class ValidationSummary : WebControl { public string HeaderText { get; set; } public string ValidationGroup { get; set; } }
    public class BaseValidator : Label { public string ControlToValidate { get; set; } public string ErrorMessage { get; set; } public string ValidationGroup { get; set; } public ValidatorDisplay Display { get; set; } }
    public class RequiredFieldValidator : BaseValidator { public string InitialValue { get; set; } }
    public class CustomValidator : BaseValidator { public bool IsValid { get; set; } public event ServerValidateEventHandler ServerValidate; }

    public delegate void CommandEventHandler(object sender, CommandEventArgs e);
    public delegate void GridViewRowEventHandler(object sender, GridViewRowEventArgs e);
    public delegate void GridViewCommandEventHandler(object sender, GridViewCommandEventArgs e);
    public delegate void RepeaterItemEventHandler(object sender, RepeaterItemEventArgs e);
    public delegate void RepeaterCommandEventHandler(object sender, RepeaterCommandEventArgs e);
    public delegate void ServerValidateEventHandler(object source, ServerValidateEventArgs args);
    public class CommandEventArgs : EventArgs { public CommandEventArgs(object commandName, object commandArgument) { this.CommandName = commandName; this.CommandArgument = commandArgument; } public object CommandName { get; } public object CommandArgument { get; } }
    public class GridViewRowEventArgs : EventArgs { public GridViewRowEventArgs(GridViewRow row) => this.Row = row; public GridViewRow Row { get; } }
    public class GridViewCommandEventArgs : CommandEventArgs { public GridViewCommandEventArgs(object commandSource, CommandEventArgs originalArgs) : base(originalArgs.CommandName, originalArgs.CommandArgument) => this.CommandSource = commandSource; public object CommandSource { get; } }
    public class RepeaterItemEventArgs : EventArgs { public RepeaterItemEventArgs(RepeaterItem item) => this.Item = item; public RepeaterItem Item { get; } }
    public class RepeaterCommandEventArgs : CommandEventArgs { public RepeaterCommandEventArgs(RepeaterItem item, object source, CommandEventArgs e) : base(e.CommandName, e.CommandArgument) { this.Item = item; this.CommandSource = source; } public RepeaterItem Item { get; } public object CommandSource { get; } }
    public class PageEventArgs : EventArgs { public PageEventArgs(int newPageIndex) => this.NewPageIndex = newPageIndex; public int NewPageIndex { get; } }
    public class ServerValidateEventArgs : EventArgs { public ServerValidateEventArgs(string value, bool isValid) { this.Value = value; this.IsValid = isValid; } public string Value { get; } public bool IsValid { get; set; } }

    public enum LiteralMode { Transform, PassThrough, Encode }
    public enum BorderStyle { NotSet, None, Dotted, Dashed, Solid, Double, Groove, Ridge, Inset, Outset }
    public enum TextBoxMode { SingleLine, MultiLine, Password, Color, Date, DateTime, DateTimeLocal, Email, Month, Number, Range, Search, Phone, Time, Url, Week }
    public enum GridLines { None, Horizontal, Vertical, Both }
    public enum HorizontalAlign { NotSet, Left, Center, Right, Justify }
    public enum VerticalAlign { NotSet, Top, Middle, Bottom }
    public enum TableRowSection { TableHeader, TableBody, TableFooter }
    public enum DataControlRowType { Header, Footer, DataRow, Separator, Pager, EmptyDataRow }
    [Flags] public enum DataControlRowState { Normal = 0, Alternate = 1, Selected = 2, Edit = 4, Insert = 8 }
    public enum ListItemType { Header, Footer, Item, AlternatingItem, SelectedItem, EditItem, Separator, Pager }
    public enum RepeatDirection { Horizontal, Vertical }
    public enum RepeatLayout { Table, Flow, OrderedList, UnorderedList }
    public enum ListSelectionMode { Single, Multiple }
    public enum ButtonType { Button, Link, Image }
    public enum UpdatePanelUpdateMode { Always, Conditional }
    public enum ValidatorDisplay { None, Static, Dynamic }
}

namespace MixERP.Net.Framework.Web.Services
{
    using System;
    using MixERP.Net.Framework.Web;
    public class WebService { public HttpContext Context => HttpContext.Current; public HttpServerUtility Server => this.Context?.Server; public SessionState.HttpSessionState Session => this.Context?.Session; }
    [AttributeUsage(AttributeTargets.Method)] public sealed class WebMethodAttribute : Attribute { public bool EnableSession { get; set; } public string Description { get; set; } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class WebServiceAttribute : Attribute { public string Namespace { get; set; } }
    [AttributeUsage(AttributeTargets.Method)] public sealed class SoapHeaderAttribute : Attribute { public SoapHeaderAttribute(string memberName) => this.MemberName = memberName; public string MemberName { get; } }
}

namespace MixERP.Net.Framework.Web.Services.Protocols
{
    public class SoapHeader { }
}

namespace MixERP.Net.Framework.Web.Script.Services
{
    using System;
    [AttributeUsage(AttributeTargets.Class)] public sealed class ScriptServiceAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class ScriptMethodAttribute : Attribute { public bool UseHttpGet { get; set; } public ResponseFormat ResponseFormat { get; set; } }
    public enum ResponseFormat { Json, Xml }
}

namespace MixERP.Net.Framework.Web.Script.Serialization
{
    public sealed class JavaScriptSerializer
    {
        public string Serialize(object value) => JsonSerializer.Serialize(value);
        public T Deserialize<T>(string input) => JsonSerializer.Deserialize<T>(input);
        public object DeserializeObject(string input) => JsonSerializer.Deserialize<object>(input);
        public T ConvertToType<T>(object value) => value is JsonElement element ? element.Deserialize<T>() : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
        public int MaxJsonLength { get; set; } = int.MaxValue;
    }
}
