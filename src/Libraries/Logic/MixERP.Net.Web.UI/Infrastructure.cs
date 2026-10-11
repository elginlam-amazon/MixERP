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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace MixERP.Net.Web.UI
{
    public class ControlCollection : IEnumerable<Control>, ICollection
    {
        private readonly List<Control> items = new List<Control>();

        public ControlCollection(Control owner)
        {
            this.Owner = owner;
        }

        protected Control Owner { get; }

        public int Count => this.items.Count;
        public bool IsReadOnly => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public virtual Control this[int index] => this.items[index];

        public virtual void Add(Control child)
        {
            this.AddAt(this.items.Count, child);
        }

        public virtual void AddAt(int index, Control child)
        {
            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            child.Parent?.Controls.Remove(child);

            if (index < 0 || index > this.items.Count)
            {
                index = this.items.Count;
            }

            this.items.Insert(index, child);
            child.Parent = this.Owner;
            child.OnAddedToParent();
            child.CatchUp(this.Owner.Stage);
        }

        public virtual void Remove(Control value)
        {
            if (value != null && this.items.Remove(value))
            {
                value.Parent = null;
            }
        }

        public virtual void RemoveAt(int index)
        {
            this.Remove(this.items[index]);
        }

        public virtual void Clear()
        {
            foreach (Control control in this.items)
            {
                control.Parent = null;
            }

            this.items.Clear();
        }

        public virtual bool Contains(Control c)
        {
            return this.items.Contains(c);
        }

        public virtual int IndexOf(Control value)
        {
            return this.items.IndexOf(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.items).CopyTo(array, index);
        }

        public Control[] ToArray()
        {
            return this.items.ToArray();
        }

        public IEnumerator<Control> GetEnumerator()
        {
            return this.items.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    ///     Request scoped key/value state (view state is not persisted across requests).
    /// </summary>
    public sealed class StateBag : IEnumerable
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public object this[string key]
        {
            get { return this.values.TryGetValue(key, out object value) ? value : null; }
            set { this.values[key] = value; }
        }

        public int Count => this.values.Count;

        public void Add(string key, object value)
        {
            this.values[key] = value;
        }

        public void Remove(string key)
        {
            this.values.Remove(key);
        }

        public void Clear()
        {
            this.values.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return this.values.GetEnumerator();
        }
    }

    public sealed class CssStyleCollection
    {
        private readonly Dictionary<string, string> styles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> order = new List<string>();

        public string this[string key]
        {
            get { return this.styles.TryGetValue(key, out string value) ? value : null; }
            set
            {
                if (value == null)
                {
                    this.Remove(key);
                    return;
                }

                if (!this.styles.ContainsKey(key))
                {
                    this.order.Add(key);
                }

                this.styles[key] = value;
            }
        }

        public string this[HtmlTextWriterStyle key]
        {
            get { return this[HtmlTextWriter.GetStyleName(key)]; }
            set { this[HtmlTextWriter.GetStyleName(key)] = value; }
        }

        public int Count => this.styles.Count;
        public ICollection Keys => this.order.ToArray();

        public string Value
        {
            get
            {
                if (this.styles.Count == 0)
                {
                    return null;
                }

                StringBuilder builder = new StringBuilder();

                foreach (string key in this.order)
                {
                    builder.Append(key).Append(':').Append(this.styles[key]).Append(';');
                }

                return builder.ToString();
            }
            set
            {
                this.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                foreach (string declaration in value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = declaration.IndexOf(':');

                    if (colon > 0)
                    {
                        this[declaration.Substring(0, colon).Trim()] = declaration.Substring(colon + 1).Trim();
                    }
                }
            }
        }

        public void Add(string key, string value)
        {
            this[key] = value;
        }

        public void Add(HtmlTextWriterStyle key, string value)
        {
            this[key] = value;
        }

        public void Remove(string key)
        {
            if (this.styles.Remove(key))
            {
                this.order.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Clear()
        {
            this.styles.Clear();
            this.order.Clear();
        }
    }

    /// <summary>
    ///     Ordered collection of HTML attributes. The "style" attribute is backed by <see cref="CssStyle" />.
    /// </summary>
    public sealed class AttributeCollection
    {
        private readonly List<string> order = new List<string>();
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AttributeCollection()
        {
            this.CssStyle = new CssStyleCollection();
        }

        public CssStyleCollection CssStyle { get; }

        public string this[string key]
        {
            get
            {
                if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    return this.CssStyle.Value;
                }

                return this.values.TryGetValue(key, out string value) ? value : null;
            }
            set
            {
                if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    this.CssStyle.Value = value;
                    return;
                }

                if (value == null)
                {
                    this.Remove(key);
                    return;
                }

                if (!this.values.ContainsKey(key))
                {
                    this.order.Add(key);
                }

                this.values[key] = value;
            }
        }

        public int Count => this.values.Count + (this.CssStyle.Count > 0 ? 1 : 0);

        public ICollection Keys
        {
            get
            {
                List<string> keys = new List<string>(this.order);

                if (this.CssStyle.Count > 0)
                {
                    keys.Add("style");
                }

                return keys;
            }
        }

        public void Add(string key, string value)
        {
            this[key] = value;
        }

        public void Remove(string key)
        {
            if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
            {
                this.CssStyle.Clear();
                return;
            }

            if (this.values.Remove(key))
            {
                this.order.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Clear()
        {
            this.values.Clear();
            this.order.Clear();
            this.CssStyle.Clear();
        }

        public void AddAttributes(HtmlTextWriter writer)
        {
            foreach (string key in this.order)
            {
                writer.AddAttribute(key, this.values[key]);
            }

            string style = this.CssStyle.Value;

            if (!string.IsNullOrEmpty(style))
            {
                writer.AddAttribute("style", style);
            }
        }

        public void Render(HtmlTextWriter writer)
        {
            foreach (string key in this.order)
            {
                writer.WriteAttribute(key, this.values[key]);
            }

            string style = this.CssStyle.Value;

            if (!string.IsNullOrEmpty(style))
            {
                writer.WriteAttribute("style", style);
            }
        }
    }

    public class LiteralControl : Control
    {
        public LiteralControl()
        {
        }

        public LiteralControl(string text)
        {
            this.Text = text;
        }

        public virtual string Text { get; set; }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.Write(this.Text);
        }
    }

    /// <summary>
    ///     A control whose output is produced by a delegate at render time. Used for compiled markup
    ///     expressions (the former &lt;%= ... %&gt; code blocks).
    /// </summary>
    public sealed class RenderDelegateControl : Control
    {
        private readonly Action<HtmlTextWriter> renderer;

        public RenderDelegateControl(Action<HtmlTextWriter> renderer)
        {
            this.renderer = renderer;
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.renderer?.Invoke(writer);
        }
    }

    public static class UrlHelper
    {
        public static string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
            {
                return relativeUrl;
            }

            if (relativeUrl.StartsWith("~/", StringComparison.Ordinal) || relativeUrl == "~")
            {
                string appPath = HttpRuntime.AppDomainAppVirtualPath;

                if (string.IsNullOrEmpty(appPath))
                {
                    appPath = "/";
                }

                appPath = appPath.TrimEnd('/');
                return appPath + relativeUrl.Substring(1);
            }

            return relativeUrl;
        }
    }

    public interface INamingContainer
    {
    }

    public interface IPostBackDataHandler
    {
        bool LoadPostData(string postDataKey, System.Collections.Specialized.NameValueCollection postCollection);
        void RaisePostDataChangedEvent();
    }

    public interface IPostBackEventHandler
    {
        void RaisePostBackEvent(string eventArgument);
    }

    public interface ITemplate
    {
        void InstantiateIn(Control container);
    }

    public interface IValidator
    {
        bool IsValid { get; set; }
        string ErrorMessage { get; set; }
        void Validate();
    }

    public interface IDataItemContainer : INamingContainer
    {
        object DataItem { get; }
        int DataItemIndex { get; }
        int DisplayIndex { get; }
    }

    public enum ClientIDMode
    {
        Inherit = 0,
        AutoID = 1,
        Predictable = 2,
        Static = 3
    }

    public enum ValidateRequestMode
    {
        Inherit = 0,
        Disabled = 1,
        Enabled = 2
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ToolboxDataAttribute : Attribute
    {
        public ToolboxDataAttribute(string data)
        {
            this.Data = data;
        }

        public string Data { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ParseChildrenAttribute : Attribute
    {
        public ParseChildrenAttribute(bool childrenAsProperties)
        {
            this.ChildrenAsProperties = childrenAsProperties;
        }

        public bool ChildrenAsProperties { get; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PersistenceModeAttribute : Attribute
    {
        public PersistenceModeAttribute(PersistenceMode mode)
        {
            this.Mode = mode;
        }

        public PersistenceMode Mode { get; }
    }

    public enum PersistenceMode
    {
        Attribute = 0,
        InnerProperty = 1,
        InnerDefaultProperty = 2,
        EncodedInnerDefaultProperty = 3
    }

    /// <summary>
    ///     Declares an embedded resource that can be served to the browser (replacement for System.Web.UI.WebResourceAttribute).
    ///     The host serves these resources from /WebResource.axd.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WebResourceAttribute : Attribute
    {
        public WebResourceAttribute(string webResource, string contentType)
        {
            this.WebResource = webResource;
            this.ContentType = contentType;
        }

        public string WebResource { get; }
        public string ContentType { get; }
        public bool PerformSubstitution { get; set; }
    }

    /// <summary>
    ///     Associates a page, master page or user control class with the virtual path of its former markup file
    ///     (e.g. "~/Modules/Sales/Setup/BonusSlabs.ascx"), so that LoadControl, MasterPageFile and routing keep working.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class VirtualPathAttribute : Attribute
    {
        public VirtualPathAttribute(string virtualPath)
        {
            this.VirtualPath = virtualPath;
        }

        public string VirtualPath { get; }
    }

    internal static class Encoder
    {
        public static string HtmlEncode(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value);
        }

        public static string AttributeEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        public static string ToInvariant(object value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
