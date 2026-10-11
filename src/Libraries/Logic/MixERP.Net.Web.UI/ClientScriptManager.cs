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
using System.Linq;
using System.Text;

namespace MixERP.Net.Web.UI
{
    /// <summary>
    ///     Collects client script blocks, includes and startup scripts registered during the page lifecycle.
    ///     Blocks and includes are written right after the opening form tag, startup scripts right before the closing tag.
    /// </summary>
    public sealed class ClientScriptManager
    {
        public const string WebResourceHandlerPath = "/WebResource.axd";

        private readonly List<KeyValuePair<string, string>> blocks = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> startupScripts = new List<KeyValuePair<string, string>>();
        private readonly Dictionary<string, string> hiddenFields = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> submitStatements = new List<string>();

        internal ClientScriptManager(Page owner)
        {
            this.Owner = owner;
        }

        private Page Owner { get; }

        internal bool RequiresPostBackScript { get; private set; }

        private static string Key(string category, Type type, string key)
        {
            return category + "|" + (type?.FullName ?? string.Empty) + "|" + key;
        }

        public bool IsClientScriptBlockRegistered(string key)
        {
            return this.IsClientScriptBlockRegistered(typeof(Page), key);
        }

        public bool IsClientScriptBlockRegistered(Type type, string key)
        {
            return this.keys.Contains(Key("block", type, key));
        }

        public bool IsStartupScriptRegistered(string key)
        {
            return this.IsStartupScriptRegistered(typeof(Page), key);
        }

        public bool IsStartupScriptRegistered(Type type, string key)
        {
            return this.keys.Contains(Key("startup", type, key));
        }

        public bool IsClientScriptIncludeRegistered(string key)
        {
            return this.IsClientScriptIncludeRegistered(typeof(Page), key);
        }

        public bool IsClientScriptIncludeRegistered(Type type, string key)
        {
            return this.keys.Contains(Key("include", type, key));
        }

        public void RegisterClientScriptBlock(Type type, string key, string script)
        {
            this.RegisterClientScriptBlock(type, key, script, false);
        }

        public void RegisterClientScriptBlock(Type type, string key, string script, bool addScriptTags)
        {
            if (this.keys.Add(Key("block", type, key)))
            {
                this.blocks.Add(new KeyValuePair<string, string>(key, addScriptTags ? WrapScript(script) : script));
            }
        }

        public void RegisterStartupScript(Type type, string key, string script)
        {
            this.RegisterStartupScript(type, key, script, false);
        }

        public void RegisterStartupScript(Type type, string key, string script, bool addScriptTags)
        {
            if (this.keys.Add(Key("startup", type, key)))
            {
                this.startupScripts.Add(new KeyValuePair<string, string>(key, addScriptTags ? WrapScript(script) : script));
            }
        }

        public void RegisterClientScriptInclude(string key, string url)
        {
            this.RegisterClientScriptInclude(typeof(Page), key, url);
        }

        public void RegisterClientScriptInclude(Type type, string key, string url)
        {
            if (this.keys.Add(Key("include", type, key)))
            {
                this.blocks.Add(new KeyValuePair<string, string>(key,
                    "<script src=\"" + Encoder.AttributeEncode(UrlHelper.ResolveUrl(url)) + "\" type=\"text/javascript\"></script>"));
            }
        }

        public void RegisterClientScriptResource(Type type, string resourceName)
        {
            this.RegisterClientScriptInclude(type, resourceName, this.GetWebResourceUrl(type, resourceName));
        }

        public void RegisterHiddenField(string hiddenFieldName, string hiddenFieldInitialValue)
        {
            this.hiddenFields[hiddenFieldName] = hiddenFieldInitialValue;
        }

        public void RegisterOnSubmitStatement(Type type, string key, string script)
        {
            if (this.keys.Add(Key("submit", type, key)))
            {
                this.submitStatements.Add(script);
            }
        }

        /// <summary>
        ///     Returns the URL of an embedded resource declared with <see cref="WebResourceAttribute" /> in the assembly of
        ///     <paramref name="type" />. The host serves it from /WebResource.axd.
        /// </summary>
        public string GetWebResourceUrl(Type type, string resourceName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return WebResourceHandlerPath + "?a=" + Uri.EscapeDataString(type.Assembly.GetName().Name) + "&r=" + Uri.EscapeDataString(resourceName);
        }

        public string GetPostBackEventReference(Control control, string argument)
        {
            this.RequiresPostBackScript = true;
            return "__doPostBack('" + EscapeJs(control.UniqueID) + "','" + EscapeJs(argument) + "')";
        }

        public string GetPostBackClientHyperlink(Control control, string argument)
        {
            return "javascript:" + this.GetPostBackEventReference(control, argument);
        }

        internal void RenderFormBegin(HtmlTextWriter writer)
        {
            writer.Write("<input type=\"hidden\" name=\"" + Page.PostBackEventTargetKey + "\" id=\"" + Page.PostBackEventTargetKey + "\" value=\"\" />");
            writer.Write("<input type=\"hidden\" name=\"" + Page.PostBackEventArgumentKey + "\" id=\"" + Page.PostBackEventArgumentKey + "\" value=\"\" />");

            foreach (KeyValuePair<string, string> field in this.hiddenFields)
            {
                writer.Write("<input type=\"hidden\" name=\"" + Encoder.AttributeEncode(field.Key) + "\" id=\"" + Encoder.AttributeEncode(field.Key) + "\" value=\"" +
                             Encoder.AttributeEncode(field.Value) + "\" />");
            }

            writer.WriteLine();
            writer.Write("<script type=\"text/javascript\">");
            writer.Write("function __doPostBack(eventTarget, eventArgument){var f=document.forms[0];if(!f){return;}");
            writer.Write("if(!f.onsubmit||(f.onsubmit()!==false)){f.__EVENTTARGET.value=eventTarget;f.__EVENTARGUMENT.value=eventArgument;f.submit();}}");
            writer.WriteLine("</script>");

            foreach (KeyValuePair<string, string> block in this.blocks)
            {
                writer.WriteLine(block.Value);
            }
        }

        internal string GetOnSubmitStatement()
        {
            return this.submitStatements.Count == 0 ? null : string.Join(";", this.submitStatements) + ";";
        }

        internal void RenderFormEnd(HtmlTextWriter writer)
        {
            foreach (KeyValuePair<string, string> script in this.startupScripts)
            {
                writer.WriteLine(script.Value);
            }
        }

        internal static string WrapScript(string script)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<script type=\"text/javascript\">").AppendLine();
            builder.Append("//<![CDATA[").AppendLine();
            builder.Append(script).AppendLine();
            builder.Append("//]]>").AppendLine();
            builder.Append("</script>");
            return builder.ToString();
        }

        internal static string EscapeJs(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");
        }

        internal bool HasRegistrations => this.blocks.Any() || this.startupScripts.Any();
    }

    public enum UpdatePanelUpdateMode
    {
        Always = 0,
        Conditional = 1
    }

    /// <summary>
    ///     Replacement for the ASP.NET AJAX ScriptManager. Partial-page (async) postbacks are not available on
    ///     ASP.NET Core, so UpdatePanels perform regular postbacks. The control renders a tiny client-side shim of
    ///     Sys.WebForms.PageRequestManager and Sys.Application (including the global pageLoad() callback) so that
    ///     existing scripts which rely on them keep working.
    /// </summary>
    public class ScriptManager : Control
    {
        private const string ItemKey = "MixERP.Net.Web.UI.ScriptManager";

        public bool EnablePartialRendering { get; set; }
        public bool EnablePageMethods { get; set; }
        public bool EnableCdn { get; set; }
        public bool EnableScriptGlobalization { get; set; }
        public bool EnableScriptLocalization { get; set; }
        public int AsyncPostBackTimeout { get; set; }
        public bool IsInAsyncPostBack => false;
        public string AsyncPostBackSourceElementID => string.Empty;

        public static ScriptManager GetCurrent(Page page)
        {
            if (page == null || page.Context == null)
            {
                return null;
            }

            return page.Items[ItemKey] as ScriptManager;
        }

        public void RegisterAsyncPostBackControl(Control control)
        {
        }

        public void RegisterPostBackControl(Control control)
        {
        }

        public void SetFocus(string clientId)
        {
            this.Page?.SetFocus(clientId);
        }

        public static void RegisterStartupScript(Control control, Type type, string key, string script, bool addScriptTags)
        {
            control?.Page?.ClientScript.RegisterStartupScript(type, key, script, addScriptTags);
        }

        public static void RegisterStartupScript(Page page, Type type, string key, string script, bool addScriptTags)
        {
            page?.ClientScript.RegisterStartupScript(type, key, script, addScriptTags);
        }

        public static void RegisterClientScriptBlock(Control control, Type type, string key, string script, bool addScriptTags)
        {
            control?.Page?.ClientScript.RegisterClientScriptBlock(type, key, script, addScriptTags);
        }

        public static void RegisterClientScriptBlock(Page page, Type type, string key, string script, bool addScriptTags)
        {
            page?.ClientScript.RegisterClientScriptBlock(type, key, script, addScriptTags);
        }

        public static void RegisterClientScriptInclude(Control control, Type type, string key, string url)
        {
            control?.Page?.ClientScript.RegisterClientScriptInclude(type, key, url);
        }

        protected override void OnInit(EventArgs e)
        {
            if (this.Page != null && this.Page.Context != null)
            {
                this.Page.Items[ItemKey] = this;
            }

            base.OnInit(e);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.Write("<script type=\"text/javascript\">");
            writer.Write("window.Sys=window.Sys||{};Sys.WebForms=Sys.WebForms||{};");
            writer.Write("Sys.WebForms.PageRequestManager=Sys.WebForms.PageRequestManager||(function(){var i={");
            writer.Write("add_beginRequest:function(){},remove_beginRequest:function(){},add_endRequest:function(){},remove_endRequest:function(){},");
            writer.Write("add_pageLoaded:function(){},remove_pageLoaded:function(){},add_initializeRequest:function(){},remove_initializeRequest:function(){},");
            writer.Write("get_isInAsyncPostBack:function(){return false;}};return{getInstance:function(){return i;}};})();");

            //Sys.Application raised "load" (and called a global pageLoad function) once the page was initialized.
            writer.Write("Sys.Application=Sys.Application||(function(){var h=[];return{add_load:function(f){h.push(f);},remove_load:function(f){h=h.filter(function(x){return x!==f;});},");
            writer.Write("raiseLoad:function(){var a={get_isPartialLoad:function(){return false;}};h.forEach(function(f){f(Sys.Application,a);});if(typeof window.pageLoad==='function'){window.pageLoad(Sys.Application,a);}}};})();");
            writer.Write("window.addEventListener('load',function(){Sys.Application.raiseLoad();});");
            writer.WriteLine("</script>");
        }
    }

    public class ScriptManagerProxy : Control
    {
        protected internal override void Render(HtmlTextWriter writer)
        {
        }
    }

    /// <summary>
    ///     Container formerly used for partial rendering; renders its content inside a div (or span) with its client id.
    /// </summary>
    public class UpdatePanel : Control
    {
        private Control contentTemplateContainer;

        public UpdatePanelUpdateMode UpdateMode { get; set; }
        public bool ChildrenAsTriggers { get; set; } = true;
        public UpdatePanelRenderMode RenderMode { get; set; }
        public ITemplate ContentTemplate { get; set; }

        public Control ContentTemplateContainer
        {
            get
            {
                if (this.contentTemplateContainer == null)
                {
                    this.contentTemplateContainer = new Control();
                    this.Controls.Add(this.contentTemplateContainer);
                    this.ContentTemplate?.InstantiateIn(this.contentTemplateContainer);
                }

                return this.contentTemplateContainer;
            }
        }

        public void Update()
        {
        }

        protected internal override void CreateChildControls()
        {
            Control unused = this.ContentTemplateContainer;
            base.CreateChildControls();
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            string tag = this.RenderMode == UpdatePanelRenderMode.Inline ? "span" : "div";

            if (!string.IsNullOrEmpty(this.ClientID))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            }

            writer.RenderBeginTag(tag);
            this.RenderChildren(writer);
            writer.RenderEndTag();
        }
    }

    public enum UpdatePanelRenderMode
    {
        Block = 0,
        Inline = 1
    }

    public class UpdateProgress : Control
    {
        public string AssociatedUpdatePanelID { get; set; }
        public int DisplayAfter { get; set; }
        public bool DynamicLayout { get; set; } = true;
        public ITemplate ProgressTemplate { get; set; }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID ?? this.UniqueID);
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            if (this.ProgressTemplate != null)
            {
                Control container = new Control();
                this.ProgressTemplate.InstantiateIn(container);
                container.RenderControl(writer);
            }

            writer.RenderEndTag();
        }
    }
}
