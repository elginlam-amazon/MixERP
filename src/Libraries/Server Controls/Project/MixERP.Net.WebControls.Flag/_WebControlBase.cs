// Auto-generated stub
#pragma warning disable CS0067, CS0108, CS0109, CS0114, CS0628, CS0649, CS0169, CS3021
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MixERP.Net.WebControls.Flag
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

    public interface IHttpHandler { bool IsReusable { get; } void ProcessRequest(object context); }
    
    public interface INamingContainer { }
    public interface IPostBackEventHandler { void RaisePostBackEvent(string eventArgument); }
    public interface IPostBackDataHandler
    {
        bool LoadPostData(string postDataKey, NameValueCollection postCollection);
        void RaisePostDataChangedEvent();
    }
}
