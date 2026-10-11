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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.SessionState;
using MixERP.Net.Web.UI.HtmlControls;
using MixERP.Net.Web.UI.WebControls;

namespace MixERP.Net.Web.UI
{
    /// <summary>
    ///     A server page (replacement for System.Web.UI.Page). <see cref="ProcessRequest" /> runs the Web Forms
    ///     compatible lifecycle: FrameworkInitialize, InitializeCulture, PreInit, master page composition, Init,
    ///     post data loading, Load, post-back events, PreRender, Render and Unload.
    /// </summary>
    public class Page : TemplateControl, IHttpHandler
    {
        public const string PostBackEventTargetKey = "__EVENTTARGET";
        public const string PostBackEventArgumentKey = "__EVENTARGUMENT";

        private readonly HashSet<Control> postDataLoaded = new HashSet<Control>();
        private readonly List<IPostBackDataHandler> changedPostDataHandlers = new List<IPostBackDataHandler>();
        private HttpContext context;
        private int autoId;
        private bool ended;
        private string masterPageFile;
        private string title;
        private bool? isValid;

        public Page()
        {
            this.ClientScript = new ClientScriptManager(this);
            this.Validators = new ValidatorCollection();
            this.RouteData = new RouteData();
        }

        public event EventHandler PreInit;
        public event EventHandler InitComplete;
        public event EventHandler PreLoad;
        public event EventHandler LoadComplete;
        public event EventHandler PreRenderComplete;

        [CLSCompliant(false)]

        public override HttpContext Context => this.context ?? HttpContext.Current;

        [CLSCompliant(false)]

        public HttpRequest Request => this.Context.Request;
        [CLSCompliant(false)]
        public HttpResponse Response => this.Context.Response;
        [CLSCompliant(false)]
        public HttpServerUtility Server => this.Context.Server;
        [CLSCompliant(false)]
        public HttpSessionState Session => this.Context.Session;
        [CLSCompliant(false)]
        public System.Web.Caching.Cache Cache => this.Context.Cache;
        [CLSCompliant(false)]
        public HttpApplicationState Application => this.Context.Application;
        public IDictionary Items => this.Context.Items;
        public System.Security.Principal.IPrincipal User => this.Context.User;

        public bool IsPostBack { get; private set; }
        public bool IsCallback => false;
        public bool IsPostBackEventControlRegistered => false;

        public ClientScriptManager ClientScript { get; }
        public ValidatorCollection Validators { get; }
        public RouteData RouteData { get; }

        public HtmlHead Header { get; internal set; }
        public HtmlForm Form { get; internal set; }
        public MasterPage Master { get; private set; }

        public virtual string MasterPageFile
        {
            get { return this.masterPageFile; }
            set { this.masterPageFile = value; }
        }

        public virtual string Theme { get; set; }
        public virtual string StyleSheetTheme { get; set; }
        public string Culture { get; set; }
        public string UICulture { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public bool EnableEventValidation { get; set; }
        public bool MaintainScrollPositionOnPostBack { get; set; }

        public string Title
        {
            get { return this.Header?.Title ?? this.title; }
            set
            {
                this.title = value;

                if (this.Header != null)
                {
                    this.Header.Title = value;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                if (this.isValid.HasValue)
                {
                    return this.isValid.Value;
                }

                return this.Validators.Cast<IValidator>().All(v => v.IsValid);
            }
        }

        public bool IsReusable => false;

        /// <summary>
        ///     Indicates that the response has been completed (redirect, transfer or end) and the lifecycle must stop.
        /// </summary>
        public bool IsEnded => this.ended || (this.context != null && this.Response.IsRequestBeingRedirected);

        public virtual void Validate()
        {
            this.Validate(null);
        }

        public virtual void Validate(string validationGroup)
        {
            bool valid = true;

            foreach (BaseValidator validator in this.Validators.OfType<BaseValidator>())
            {
                if (validationGroup == null || string.Equals(validator.ValidationGroup ?? string.Empty, validationGroup, StringComparison.Ordinal))
                {
                    validator.Validate();
                    valid &= validator.IsValid;
                }
            }

            this.isValid = valid;
        }

        public virtual void VerifyRenderingInServerForm(Control control)
        {
        }

        public void RegisterRequiresPostBack(Control control)
        {
        }

        public void SetFocus(string clientId)
        {
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                this.ClientScript.RegisterStartupScript(typeof(Page), "focus:" + clientId,
                    "(function(){var el=document.getElementById('" + clientId.Replace("'", "\\'") + "');if(el){el.focus();}})();", true);
            }
        }

        public void SetFocus(Control control)
        {
            this.SetFocus(control?.ClientID);
        }

        /// <summary>
        ///     Stops the current page lifecycle (equivalent to the ThreadAbort raised by Response.End in Web Forms).
        /// </summary>
        public void EndResponse()
        {
            this.ended = true;
            throw new PageEndException();
        }

        /// <summary>
        ///     Executes the page registered for <paramref name="virtualPath" /> in the current request and ends this
        ///     page (replacement for Server.Transfer).
        /// </summary>
        public void Transfer(string virtualPath)
        {
            string path = virtualPath;
            string query = null;
            int queryIndex = path.IndexOf('?');

            if (queryIndex >= 0)
            {
                query = path.Substring(queryIndex + 1);
                path = path.Substring(0, queryIndex);
            }

            Type type = ControlRegistry.Resolve(path);

            if (type == null || !typeof(Page).IsAssignableFrom(type))
            {
                throw new InvalidOperationException("No page is registered for the virtual path \"" + virtualPath + "\".");
            }

            Page target = (Page)Activator.CreateInstance(type);
            target.TransferQueryString = query;
            this.Response.Clear();
            target.ProcessRequest(this.Context);
            this.EndResponse();
        }

        public string TransferQueryString { get; private set; }

        [CLSCompliant(false)]

        public virtual void ProcessRequest(HttpContext httpContext)
        {
            this.context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            this.context.Items[typeof(Page)] = this;

            try
            {
                this.ProcessRequestMain();
            }
            catch (PageEndException)
            {
                //The response was completed by a redirect, a transfer or an explicit end.
            }
            finally
            {
                try
                {
                    this.UnloadRecursive();
                }
                catch (PageEndException)
                {
                }
            }
        }

        /// <summary>
        ///     The page currently being executed for this request, if any.
        /// </summary>
        [CLSCompliant(false)]
        public static Page GetCurrent(HttpContext httpContext)
        {
            return httpContext?.Items[typeof(Page)] as Page;
        }

        private void ProcessRequestMain()
        {
            this.IsPostBack = string.Equals(this.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                              && this.Request.Form[PostBackEventTargetKey] != null;

            this.EnsureFrameworkInitialized();
            this.InitializeCulture();
            this.ApplyCulture();
            this.OnPreInit(EventArgs.Empty);

            if (this.Stop())
            {
                return;
            }

            this.ApplyMasterPage();

            this.InitRecursive();
            this.OnInitComplete(EventArgs.Empty);

            if (this.Stop())
            {
                return;
            }

            if (this.IsPostBack)
            {
                this.ProcessPostData();
            }

            this.OnPreLoad(EventArgs.Empty);
            this.LoadRecursive();

            if (this.Stop())
            {
                return;
            }

            if (this.IsPostBack)
            {
                this.EnsureChildControlsRecursive();
                this.ProcessPostData();
                this.RaiseChangedEvents();

                if (this.Stop())
                {
                    return;
                }

                this.RaisePostBackEvent();
            }

            this.OnLoadComplete(EventArgs.Empty);

            if (this.Stop())
            {
                return;
            }

            this.PreRenderRecursive();
            this.OnPreRenderComplete(EventArgs.Empty);

            if (this.Stop())
            {
                return;
            }

            using (StringWriter buffer = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(buffer))
                {
                    this.RenderControl(writer);
                }

                if (string.IsNullOrWhiteSpace(this.Response.ContentType))
                {
                    this.Response.ContentType = "text/html";
                }

                this.Response.Write(buffer.ToString());
            }
        }

        private bool Stop()
        {
            return this.IsEnded;
        }

        private void ApplyCulture()
        {
            if (!string.IsNullOrWhiteSpace(this.Culture) && !string.Equals(this.Culture, "auto", StringComparison.OrdinalIgnoreCase))
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(this.Culture);
            }

            if (!string.IsNullOrWhiteSpace(this.UICulture) && !string.Equals(this.UICulture, "auto", StringComparison.OrdinalIgnoreCase))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(this.UICulture);
            }
        }

        private void ApplyMasterPage()
        {
            if (string.IsNullOrWhiteSpace(this.MasterPageFile))
            {
                return;
            }

            MasterPage master = this.LoadControl(this.MasterPageFile) as MasterPage;

            if (master == null)
            {
                throw new InvalidOperationException("The master page \"" + this.MasterPageFile + "\" could not be loaded.");
            }

            this.Master = master;
            List<Content> contents = this.Controls.OfType<Content>().ToList();
            this.Controls.Clear();

            Control root = ComposeMaster(master, contents);
            this.Controls.Add(root);
        }

        private Control ComposeMaster(MasterPage master, List<Content> contents)
        {
            master.Page = this;
            master.EnsureFrameworkInitialized();
            FillPlaceHolders(master, contents);

            if (string.IsNullOrWhiteSpace(master.MasterPageFile))
            {
                return master;
            }

            MasterPage parent = this.LoadControl(master.MasterPageFile) as MasterPage;

            if (parent == null)
            {
                throw new InvalidOperationException("The master page \"" + master.MasterPageFile + "\" could not be loaded.");
            }

            master.Master = parent;
            List<Content> masterContents = master.Controls.OfType<Content>().ToList();
            master.Controls.Clear();

            foreach (Content content in masterContents)
            {
                content.OwnerMaster = master;
            }

            return this.ComposeMaster(parent, masterContents);
        }

        private static void FillPlaceHolders(MasterPage master, List<Content> contents)
        {
            Dictionary<string, ContentPlaceHolder> placeHolders = master.GetContentPlaceHolders();

            foreach (Content content in contents)
            {
                if (string.IsNullOrWhiteSpace(content.ContentPlaceHolderID) ||
                    !placeHolders.TryGetValue(content.ContentPlaceHolderID, out ContentPlaceHolder placeHolder))
                {
                    continue;
                }

                placeHolder.Controls.Clear();
                placeHolder.Controls.Add(content);
            }
        }

        private void ProcessPostData()
        {
            this.LoadPostDataFor(this);
        }

        internal void LoadPostDataFor(Control root)
        {
            if (!this.IsPostBack || this.context == null)
            {
                return;
            }

            NameValueCollection form = this.Request.Form;

            foreach (Control control in root.DescendantsAndSelf().ToList())
            {
                if (!(control is IPostBackDataHandler handler) || this.postDataLoaded.Contains(control))
                {
                    continue;
                }

                this.postDataLoaded.Add(control);

                if (handler.LoadPostData(control.UniqueID, form))
                {
                    this.changedPostDataHandlers.Add(handler);
                }
            }
        }

        private void RaiseChangedEvents()
        {
            foreach (IPostBackDataHandler handler in this.changedPostDataHandlers.ToList())
            {
                handler.RaisePostDataChangedEvent();
            }

            this.changedPostDataHandlers.Clear();
        }

        private void RaisePostBackEvent()
        {
            NameValueCollection form = this.Request.Form;
            string eventTarget = form[PostBackEventTargetKey];
            string eventArgument = form[PostBackEventArgumentKey];
            List<Control> all = this.DescendantsAndSelf().ToList();

            if (!string.IsNullOrEmpty(eventTarget))
            {
                IPostBackEventHandler target = all.Where(c => c is IPostBackEventHandler)
                    .FirstOrDefault(c => string.Equals(c.UniqueID, eventTarget, StringComparison.Ordinal)) as IPostBackEventHandler;

                target?.RaisePostBackEvent(eventArgument);
                return;
            }

            foreach (Control control in all.Where(c => c is IPostBackEventHandler))
            {
                string key = control.UniqueID;

                if (form[key] != null || form[key + ".x"] != null)
                {
                    ((IPostBackEventHandler)control).RaisePostBackEvent(eventArgument);
                    return;
                }
            }
        }

        internal string GetNextAutoId()
        {
            this.autoId++;
            return "ctl" + this.autoId.ToString("00", CultureInfo.InvariantCulture);
        }

        protected virtual PageStatePersister PageStatePersister => null;

        protected virtual void InitializeCulture()
        {
        }

        protected virtual void OnPreInit(EventArgs e)
        {
            this.PreInit?.Invoke(this, e);
        }

        protected virtual void OnInitComplete(EventArgs e)
        {
            this.InitComplete?.Invoke(this, e);
        }

        protected virtual void OnPreLoad(EventArgs e)
        {
            this.PreLoad?.Invoke(this, e);
        }

        protected virtual void OnLoadComplete(EventArgs e)
        {
            this.LoadComplete?.Invoke(this, e);
        }

        protected virtual void OnPreRenderComplete(EventArgs e)
        {
            this.PreRenderComplete?.Invoke(this, e);
        }
    }

    /// <summary>
    ///     Thrown to unwind the page lifecycle after the response has been completed.
    /// </summary>
    [Serializable]
    public sealed class PageEndException : Exception
    {
        public PageEndException() : base("The page response has ended.")
        {
        }
    }

    public sealed class ValidatorCollection : ICollection
    {
        private readonly List<IValidator> validators = new List<IValidator>();

        public int Count => this.validators.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        public IValidator this[int index] => this.validators[index];

        public void Add(IValidator validator)
        {
            if (!this.validators.Contains(validator))
            {
                this.validators.Add(validator);
            }
        }

        public void Remove(IValidator validator)
        {
            this.validators.Remove(validator);
        }

        public bool Contains(IValidator validator)
        {
            return this.validators.Contains(validator);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.validators).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return this.validators.ToList().GetEnumerator();
        }
    }
}
