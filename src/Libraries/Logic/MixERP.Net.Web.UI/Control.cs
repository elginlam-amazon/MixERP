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
using System.Collections.Generic;
using System.Globalization;
using System.Web;

namespace MixERP.Net.Web.UI
{
    /// <summary>
    ///     Base class of the MixERP server-side control tree (replacement for System.Web.UI.Control).
    ///     Supports the Web Forms lifecycle (Init, Load, PreRender, Render, Unload), child-control creation,
    ///     naming-container aware FindControl and static client IDs (MixERP always used clientIDMode="Static").
    /// </summary>
    public class Control : IDisposable
    {
        private ControlCollection controls;
        private string id;
        private Page page;
        private StateBag viewState;
        private bool visible = true;

        internal ControlLifecycleStage Stage { get; set; }

        public event EventHandler Init;
        public event EventHandler Load;
        public event EventHandler PreRender;
        public event EventHandler Unload;
        public event EventHandler DataBinding;
        public event EventHandler Disposed;

        public virtual string ID
        {
            get { return this.id; }
            set { this.id = value; }
        }

        public virtual ClientIDMode ClientIDMode { get; set; } = ClientIDMode.Inherit;

        /// <summary>
        ///     The client id. Static and Inherit modes (the MixERP default) render the ID as-is.
        ///     Predictable and AutoID prefix the client id of the naming container.
        /// </summary>
        public virtual string ClientID
        {
            get
            {
                if (string.IsNullOrEmpty(this.ID))
                {
                    return null;
                }

                ClientIDMode mode = this.GetEffectiveClientIdMode();

                if (mode == ClientIDMode.Predictable || mode == ClientIDMode.AutoID)
                {
                    Control container = this.NamingContainer;

                    if (container != null && !(container is Page) && !string.IsNullOrEmpty(container.ClientID))
                    {
                        return container.ClientID + "_" + this.ID;
                    }
                }

                return this.ID;
            }
        }

        /// <summary>
        ///     The name used for posted form fields. Equal to the ID (static ids), or an auto generated id when no ID is set.
        /// </summary>
        public virtual string UniqueID
        {
            get
            {
                if (string.IsNullOrEmpty(this.ID))
                {
                    Page p = this.Page;
                    this.ID = p != null ? p.GetNextAutoId() : "ctl" + Guid.NewGuid().ToString("N");
                }

                return this.ClientID;
            }
        }

        public virtual ControlCollection Controls
        {
            get { return this.controls ?? (this.controls = this.CreateControlCollection()); }
        }

        public bool HasControls()
        {
            return this.controls != null && this.controls.Count > 0;
        }

        public Control Parent { get; internal set; }

        public virtual Page Page
        {
            get
            {
                if (this.page != null)
                {
                    return this.page;
                }

                if (this is Page self)
                {
                    return self;
                }

                return this.Parent?.Page;
            }
            set { this.page = value; }
        }

        public virtual Control NamingContainer
        {
            get
            {
                Control current = this.Parent;

                while (current != null)
                {
                    if (current is INamingContainer)
                    {
                        return current;
                    }

                    current = current.Parent;
                }

                return null;
            }
        }

        public virtual bool Visible
        {
            get { return this.visible && (this.Parent == null || this.Parent.Visible); }
            set { this.visible = value; }
        }

        public virtual bool EnableViewState { get; set; }

        /// <summary>
        ///     Kept for source compatibility; ASP.NET themes (App_Themes skins) are not applied to controls.
        /// </summary>
        public virtual bool EnableTheming { get; set; } = true;
        public virtual ValidateRequestMode ValidateRequestMode { get; set; } = ValidateRequestMode.Inherit;
        public string AppRelativeTemplateSourceDirectory { get; set; } = "~/";

        /// <summary>
        ///     Request scoped state bag. View state is not round-tripped (MixERP disabled view state globally).
        /// </summary>
        protected virtual StateBag ViewState
        {
            get { return this.viewState ?? (this.viewState = new StateBag()); }
        }

        protected bool ChildControlsCreated { get; set; }

        [CLSCompliant(false)]

        public virtual HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        [CLSCompliant(false)]

        protected virtual HttpContext ControlContext => this.Context;

        public object DataItemContainerValue { get; set; }

        public virtual void Dispose()
        {
            this.Disposed?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
        }

        public virtual void Focus()
        {
        }

        public virtual void DataBind()
        {
            this.OnDataBinding(EventArgs.Empty);

            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.DataBind();
                }
            }
        }

        public virtual Control FindControl(string controlId)
        {
            if (string.IsNullOrEmpty(controlId))
            {
                return null;
            }

            this.EnsureChildControls();
            return FindControlInternal(this, controlId);
        }

        private static Control FindControlInternal(Control root, string controlId)
        {
            if (!root.HasControls())
            {
                return null;
            }

            foreach (Control child in root.Controls.ToArray())
            {
                if (string.Equals(child.ID, controlId, StringComparison.Ordinal))
                {
                    return child;
                }

                if (child is INamingContainer)
                {
                    continue;
                }

                Control found = FindControlInternal(child, controlId);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public string ResolveUrl(string relativeUrl)
        {
            return UrlHelper.ResolveUrl(relativeUrl);
        }

        public string ResolveClientUrl(string relativeUrl)
        {
            return UrlHelper.ResolveUrl(relativeUrl);
        }

        public virtual void RenderControl(HtmlTextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (this.visible)
            {
                this.Render(writer);
            }
        }

        protected internal virtual void Render(HtmlTextWriter writer)
        {
            this.RenderChildren(writer);
        }

        protected internal virtual void RenderChildren(HtmlTextWriter writer)
        {
            if (this.controls == null)
            {
                return;
            }

            foreach (Control child in this.controls.ToArray())
            {
                child.RenderControl(writer);
            }
        }

        protected virtual ControlCollection CreateControlCollection()
        {
            return new ControlCollection(this);
        }

        protected virtual void EnsureChildControls()
        {
            if (this.ChildControlsCreated)
            {
                return;
            }

            this.ChildControlsCreated = true;
            this.CreateChildControls();
        }

        protected internal virtual void CreateChildControls()
        {
        }

        protected virtual void RecreateChildControls()
        {
            this.ChildControlsCreated = false;
            this.EnsureChildControls();
        }

        protected virtual void OnInit(EventArgs e)
        {
            this.Init?.Invoke(this, e);
        }

        protected virtual void OnLoad(EventArgs e)
        {
            this.Load?.Invoke(this, e);
        }

        protected virtual void OnPreRender(EventArgs e)
        {
            this.PreRender?.Invoke(this, e);
        }

        protected virtual void OnUnload(EventArgs e)
        {
            this.Unload?.Invoke(this, e);
        }

        protected virtual void OnDataBinding(EventArgs e)
        {
            this.DataBinding?.Invoke(this, e);
        }

        protected virtual bool OnBubbleEvent(object source, EventArgs args)
        {
            return false;
        }

        protected void RaiseBubbleEvent(object source, EventArgs args)
        {
            Control current = this.Parent;

            while (current != null)
            {
                if (current.OnBubbleEvent(source, args))
                {
                    return;
                }

                current = current.Parent;
            }
        }

        internal virtual void OnAddedToParent()
        {
        }

        internal ClientIDMode GetEffectiveClientIdMode()
        {
            Control current = this;

            while (current != null)
            {
                if (current.ClientIDMode != ClientIDMode.Inherit)
                {
                    return current.ClientIDMode;
                }

                current = current.Parent;
            }

            return ClientIDMode.Static;
        }

        internal void InitRecursive()
        {
            if (this.Stage >= ControlLifecycleStage.Initialized)
            {
                return;
            }

            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.InitRecursive();
                }
            }

            this.Stage = ControlLifecycleStage.Initialized;
            this.OnInit(EventArgs.Empty);
        }

        internal void LoadRecursive()
        {
            if (this.Stage >= ControlLifecycleStage.Loaded)
            {
                return;
            }

            this.Stage = ControlLifecycleStage.Loaded;
            this.OnLoad(EventArgs.Empty);

            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.LoadRecursive();
                }
            }
        }

        internal void PreRenderRecursive()
        {
            if (this.Stage >= ControlLifecycleStage.PreRendered)
            {
                return;
            }

            if (this.visible)
            {
                this.EnsureChildControls();
            }

            this.Stage = ControlLifecycleStage.PreRendered;

            if (this.visible)
            {
                this.OnPreRender(EventArgs.Empty);
            }

            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.PreRenderRecursive();
                }
            }
        }

        internal void UnloadRecursive()
        {
            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.UnloadRecursive();
                }
            }

            this.OnUnload(EventArgs.Empty);
        }

        internal void EnsureChildControlsRecursive()
        {
            this.EnsureChildControls();

            if (this.controls != null)
            {
                foreach (Control child in this.controls.ToArray())
                {
                    child.EnsureChildControlsRecursive();
                }
            }
        }

        /// <summary>
        ///     Brings a control which was added late (e.g. during Load) up to the lifecycle stage of its new parent.
        /// </summary>
        internal void CatchUp(ControlLifecycleStage parentStage)
        {
            if (parentStage >= ControlLifecycleStage.Initialized)
            {
                this.InitRecursive();
            }

            if (parentStage >= ControlLifecycleStage.Loaded)
            {
                Page p = this.Page;
                p?.LoadPostDataFor(this);
                this.LoadRecursive();
            }

            if (parentStage >= ControlLifecycleStage.PreRendered)
            {
                this.PreRenderRecursive();
            }
        }

        internal IEnumerable<Control> DescendantsAndSelf()
        {
            yield return this;

            if (this.controls == null)
            {
                yield break;
            }

            foreach (Control child in this.controls.ToArray())
            {
                foreach (Control descendant in child.DescendantsAndSelf())
                {
                    yield return descendant;
                }
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", this.GetType().Name, this.ID);
        }
    }

    internal enum ControlLifecycleStage
    {
        Constructed = 0,
        Initialized = 1,
        Loaded = 2,
        PreRendered = 3
    }
}
