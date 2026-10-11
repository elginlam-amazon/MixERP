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
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using MixERP.Net.Web.UI.WebControls;

namespace MixERP.Net.Web.UI.HtmlControls
{
    public abstract class HtmlControl : Control
    {
        protected HtmlControl() : this("span")
        {
        }

        protected HtmlControl(string tag)
        {
            this.TagName = tag;
            this.Attributes = new AttributeCollection();
        }

        public AttributeCollection Attributes { get; }
        public CssStyleCollection Style => this.Attributes.CssStyle;
        public virtual string TagName { get; protected set; }

        public bool Disabled
        {
            get { return this.Attributes["disabled"] != null; }
            set
            {
                if (value)
                {
                    this.Attributes["disabled"] = "disabled";
                }
                else
                {
                    this.Attributes.Remove("disabled");
                }
            }
        }

        protected virtual bool IsVoidElement => false;

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.RenderBeginTag(writer);

            if (this.IsVoidElement)
            {
                return;
            }

            this.RenderChildren(writer);
            this.RenderEndTag(writer);
        }

        protected virtual void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.WriteBeginTag(this.TagName);
            this.RenderAttributes(writer);
            writer.Write(this.IsVoidElement ? HtmlTextWriter.SelfClosingTagEnd : ">");
        }

        protected virtual void RenderEndTag(HtmlTextWriter writer)
        {
            writer.WriteEndTag(this.TagName);
        }

        protected virtual void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("id", this.ClientID);
            }

            this.Attributes.Render(writer);
        }
    }

    public abstract class HtmlContainerControl : HtmlControl
    {
        protected HtmlContainerControl() : this("span")
        {
        }

        protected HtmlContainerControl(string tag) : base(tag)
        {
        }

        public virtual string InnerHtml
        {
            get
            {
                if (this.HasControls() && this.Controls.Count == 1 && this.Controls[0] is LiteralControl literal)
                {
                    return literal.Text;
                }

                return string.Empty;
            }
            set
            {
                this.Controls.Clear();
                this.Controls.Add(new LiteralControl(value));
            }
        }

        public virtual string InnerText
        {
            get { return System.Net.WebUtility.HtmlDecode(this.InnerHtml); }
            set { this.InnerHtml = Encoder.HtmlEncode(value); }
        }
    }

    public class HtmlGenericControl : HtmlContainerControl
    {
        public HtmlGenericControl() : this("span")
        {
        }

        public HtmlGenericControl(string tag) : base(tag)
        {
        }

        public new string TagName
        {
            get { return base.TagName; }
            set { base.TagName = value; }
        }

        protected override bool IsVoidElement => string.Equals(this.TagName, "br", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(this.TagName, "hr", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(this.TagName, "img", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(this.TagName, "input", StringComparison.OrdinalIgnoreCase);
    }

    public class HtmlAnchor : HtmlContainerControl, IPostBackEventHandler
    {
        public HtmlAnchor() : base("a")
        {
        }

        public event EventHandler ServerClick;

        public string HRef
        {
            get { return this.Attributes["href"]; }
            set { this.Attributes["href"] = value; }
        }

        public string Target
        {
            get { return this.Attributes["target"]; }
            set { this.Attributes["target"] = value; }
        }

        public string Title
        {
            get { return this.Attributes["title"]; }
            set { this.Attributes["title"] = value; }
        }

        public string Name
        {
            get { return this.Attributes["name"]; }
            set { this.Attributes["name"] = value; }
        }

        public void RaisePostBackEvent(string eventArgument)
        {
            this.ServerClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (this.ServerClick != null && this.Page != null && string.IsNullOrEmpty(this.HRef))
            {
                this.Attributes["href"] = this.Page.ClientScript.GetPostBackClientHyperlink(this, string.Empty);
            }

            if (!string.IsNullOrEmpty(this.HRef))
            {
                this.Attributes["href"] = this.ResolveUrl(this.HRef);
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlButton : HtmlContainerControl, IPostBackEventHandler
    {
        public HtmlButton() : base("button")
        {
        }

        public event EventHandler ServerClick;

        public bool CausesValidation { get; set; } = true;
        public string ValidationGroup { get; set; }

        public void RaisePostBackEvent(string eventArgument)
        {
            if (this.CausesValidation)
            {
                this.Page?.Validate(this.ValidationGroup);
            }

            this.ServerClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (this.ServerClick != null)
            {
                writer.WriteAttribute("name", this.UniqueID);
                writer.WriteAttribute("value", "1");
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlHead : HtmlGenericControl
    {
        private string title;

        public HtmlHead() : base("head")
        {
        }

        public string Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        protected override void OnInit(EventArgs e)
        {
            if (this.Page != null)
            {
                string pageTitle = this.Page.Title;
                this.Page.Header = this;

                if (string.IsNullOrEmpty(this.title) && !string.IsNullOrEmpty(pageTitle))
                {
                    this.title = pageTitle;
                }
            }

            base.OnInit(e);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.RenderBeginTag(writer);

            //A literal <title> element in the head markup acts as the page title (it was an HtmlTitle in Web Forms).
            LiteralControl literalTitle = this.Controls.OfType<LiteralControl>()
                .FirstOrDefault(l => l.Text != null && TitleExpression.IsMatch(l.Text));

            foreach (Control child in this.Controls.ToArray())
            {
                if (ReferenceEquals(child, literalTitle) && !string.IsNullOrEmpty(this.title))
                {
                    writer.Write(TitleExpression.Replace(literalTitle.Text, "<title>" + Encoder.HtmlEncode(this.title) + "</title>", 1));
                    continue;
                }

                child.RenderControl(writer);
            }

            if (literalTitle == null && !this.Controls.OfType<HtmlTitle>().Any())
            {
                writer.Write("<title>");
                writer.WriteEncodedText(this.title ?? string.Empty);
                writer.Write("</title>");
            }

            this.RenderEndTag(writer);
        }

        private static readonly System.Text.RegularExpressions.Regex TitleExpression =
            new System.Text.RegularExpressions.Regex("<title>.*?</title>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    public class HtmlTitle : HtmlGenericControl
    {
        public HtmlTitle() : base("title")
        {
        }

        public string Text { get; set; }

        protected internal override void Render(HtmlTextWriter writer)
        {
            string text = this.Page?.Header?.Title ?? this.Text;
            writer.Write("<title>");

            if (!string.IsNullOrEmpty(text))
            {
                writer.WriteEncodedText(text);
            }
            else
            {
                this.RenderChildren(writer);
            }

            writer.Write("</title>");
        }
    }

    public class HtmlMeta : HtmlControl
    {
        public HtmlMeta() : base("meta")
        {
        }

        public string Name
        {
            get { return this.Attributes["name"]; }
            set { this.Attributes["name"] = value; }
        }

        public string Content
        {
            get { return this.Attributes["content"]; }
            set { this.Attributes["content"] = value; }
        }

        public string HttpEquiv
        {
            get { return this.Attributes["http-equiv"]; }
            set { this.Attributes["http-equiv"] = value; }
        }

        protected override bool IsVoidElement => true;
    }

    public class HtmlLink : HtmlControl
    {
        public HtmlLink() : base("link")
        {
        }

        public string Href
        {
            get { return this.Attributes["href"]; }
            set { this.Attributes["href"] = value; }
        }

        protected override bool IsVoidElement => true;

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.Href))
            {
                this.Attributes["href"] = this.ResolveUrl(this.Href);
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlImage : HtmlControl
    {
        public HtmlImage() : base("img")
        {
        }

        public string Src
        {
            get { return this.Attributes["src"]; }
            set { this.Attributes["src"] = value; }
        }

        public string Alt
        {
            get { return this.Attributes["alt"]; }
            set { this.Attributes["alt"] = value; }
        }

        protected override bool IsVoidElement => true;

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.Src))
            {
                this.Attributes["src"] = this.ResolveUrl(this.Src);
            }

            base.RenderAttributes(writer);
        }
    }

    /// <summary>
    ///     The server form. Writes the post-back infrastructure and registered client scripts.
    /// </summary>
    public class HtmlForm : HtmlContainerControl
    {
        public HtmlForm() : base("form")
        {
        }

        public string Action { get; set; }
        public string Method { get; set; } = "post";
        public string Enctype { get; set; }
        public string DefaultButton { get; set; }
        public string DefaultFocus { get; set; }

        protected override void OnInit(EventArgs e)
        {
            if (this.Page != null)
            {
                this.Page.Form = this;
            }

            base.OnInit(e);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.WriteBeginTag("form");

            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("id", this.ClientID);
                writer.WriteAttribute("name", this.ClientID);
            }

            writer.WriteAttribute("method", this.Method);
            writer.WriteAttribute("action", this.Action ?? this.GetDefaultAction());

            if (!string.IsNullOrEmpty(this.Enctype))
            {
                writer.WriteAttribute("enctype", this.Enctype);
            }

            string onSubmit = this.Page?.ClientScript.GetOnSubmitStatement();

            if (!string.IsNullOrEmpty(onSubmit))
            {
                writer.WriteAttribute("onsubmit", onSubmit);
            }

            this.Attributes.Render(writer);
            writer.Write(HtmlTextWriter.TagRightChar);

            this.Page?.ClientScript.RenderFormBegin(writer);
            this.RenderChildren(writer);
            this.Page?.ClientScript.RenderFormEnd(writer);

            writer.WriteEndTag("form");
        }

        private string GetDefaultAction()
        {
            System.Web.HttpContext current = this.Context;

            if (current == null)
            {
                return string.Empty;
            }

            string rawUrl = current.Request.RawUrl;
            return string.IsNullOrEmpty(rawUrl) ? current.Request.Url.PathAndQuery : rawUrl;
        }
    }

    public abstract class HtmlInputControl : HtmlControl
    {
        protected HtmlInputControl(string type) : base("input")
        {
            this.Type = type;
        }

        public string Type
        {
            get { return this.Attributes["type"]; }
            set { this.Attributes["type"] = value; }
        }

        public virtual string Name
        {
            get { return this.UniqueID; }
            set { }
        }

        public virtual string Value
        {
            get { return this.Attributes["value"]; }
            set { this.Attributes["value"] = value; }
        }

        protected override bool IsVoidElement => true;

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("name", this.UniqueID);
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlInputText : HtmlInputControl, IPostBackDataHandler
    {
        public HtmlInputText() : this("text")
        {
        }

        public HtmlInputText(string type) : base(type)
        {
        }

        public event EventHandler ServerChange;

        public int MaxLength
        {
            get
            {
                string value = this.Attributes["maxlength"];
                return string.IsNullOrEmpty(value) ? -1 : int.Parse(value, CultureInfo.InvariantCulture);
            }
            set { this.Attributes["maxlength"] = value < 0 ? null : value.ToString(CultureInfo.InvariantCulture); }
        }

        public int Size
        {
            get
            {
                string value = this.Attributes["size"];
                return string.IsNullOrEmpty(value) ? -1 : int.Parse(value, CultureInfo.InvariantCulture);
            }
            set { this.Attributes["size"] = value < 0 ? null : value.ToString(CultureInfo.InvariantCulture); }
        }

        public bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string posted = postCollection[postDataKey];

            if (posted == null || string.Equals(posted, this.Value, StringComparison.Ordinal))
            {
                return false;
            }

            this.Value = posted;
            return true;
        }

        public void RaisePostDataChangedEvent()
        {
            this.ServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public class HtmlInputPassword : HtmlInputText
    {
        public HtmlInputPassword() : base("password")
        {
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            string value = this.Value;
            this.Attributes.Remove("value");
            base.RenderAttributes(writer);
            this.Attributes["value"] = value;
        }
    }

    public class HtmlInputHidden : HtmlInputText
    {
        public HtmlInputHidden() : base("hidden")
        {
        }
    }

    public class HtmlInputFile : HtmlInputControl
    {
        public HtmlInputFile() : base("file")
        {
        }

        public string Accept
        {
            get { return this.Attributes["accept"]; }
            set { this.Attributes["accept"] = value; }
        }

        [CLSCompliant(false)]

        public System.Web.HttpPostedFile PostedFile => this.Context?.Request.Files[this.UniqueID];
    }

    public class HtmlInputButton : HtmlInputControl, IPostBackEventHandler
    {
        public HtmlInputButton() : this("button")
        {
        }

        public HtmlInputButton(string type) : base(type)
        {
        }

        public event EventHandler ServerClick;

        public bool CausesValidation { get; set; } = true;
        public string ValidationGroup { get; set; }

        public void RaisePostBackEvent(string eventArgument)
        {
            if (this.CausesValidation)
            {
                this.Page?.Validate(this.ValidationGroup);
            }

            this.ServerClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (this.ServerClick != null && this.Page != null && string.Equals(this.Type, "button", StringComparison.OrdinalIgnoreCase))
            {
                string onclick = this.Attributes["onclick"];
                string postBack = this.Page.ClientScript.GetPostBackEventReference(this, string.Empty);
                this.Attributes["onclick"] = string.IsNullOrEmpty(onclick) ? postBack : onclick.TrimEnd(';') + ";" + postBack;
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlInputSubmit : HtmlInputButton
    {
        public HtmlInputSubmit() : base("submit")
        {
        }
    }

    public class HtmlInputReset : HtmlInputButton
    {
        public HtmlInputReset() : base("reset")
        {
        }
    }

    public class HtmlInputCheckBox : HtmlInputControl, IPostBackDataHandler
    {
        public HtmlInputCheckBox() : this("checkbox")
        {
        }

        protected HtmlInputCheckBox(string type) : base(type)
        {
        }

        public event EventHandler ServerChange;

        public bool Checked
        {
            get { return this.Attributes["checked"] != null; }
            set
            {
                if (value)
                {
                    this.Attributes["checked"] = "checked";
                }
                else
                {
                    this.Attributes.Remove("checked");
                }
            }
        }

        public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            bool posted = postCollection[postDataKey] != null;
            bool changed = posted != this.Checked;
            this.Checked = posted;
            return changed;
        }

        public void RaisePostDataChangedEvent()
        {
            this.ServerChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public class HtmlInputRadioButton : HtmlInputCheckBox
    {
        public HtmlInputRadioButton() : base("radio")
        {
        }

        public override string Name
        {
            get { return this.Attributes["name"] ?? this.UniqueID; }
            set { this.Attributes["name"] = value; }
        }

        public override bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string posted = postCollection[this.Name];
            bool isChecked = posted != null && string.Equals(posted, this.Value ?? this.UniqueID, StringComparison.Ordinal);
            bool changed = isChecked != this.Checked;
            this.Checked = isChecked;
            return changed;
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("id", this.ClientID);
            }

            if (this.Attributes["name"] == null)
            {
                writer.WriteAttribute("name", this.UniqueID);
            }

            this.Attributes.Render(writer);
        }
    }

    public class HtmlTextArea : HtmlContainerControl, IPostBackDataHandler
    {
        public HtmlTextArea() : base("textarea")
        {
        }

        public event EventHandler ServerChange;

        public string Value
        {
            get { return this.InnerText; }
            set { this.InnerText = value; }
        }

        public int Rows
        {
            get { return Convert.ToInt32(this.Attributes["rows"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["rows"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public int Cols
        {
            get { return Convert.ToInt32(this.Attributes["cols"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["cols"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string posted = postCollection[postDataKey];

            if (posted == null || string.Equals(posted, this.Value, StringComparison.Ordinal))
            {
                return false;
            }

            this.Value = posted;
            return true;
        }

        public void RaisePostDataChangedEvent()
        {
            this.ServerChange?.Invoke(this, EventArgs.Empty);
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("name", this.UniqueID);
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlSelect : HtmlContainerControl, IPostBackDataHandler
    {
        public HtmlSelect() : base("select")
        {
            this.Items = new ListItemCollection();
        }

        public event EventHandler ServerChange;

        public ListItemCollection Items { get; }
        public object DataSource { get; set; }
        public string DataMember { get; set; }
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }

        public bool Multiple
        {
            get { return this.Attributes["multiple"] != null; }
            set
            {
                if (value)
                {
                    this.Attributes["multiple"] = "multiple";
                }
                else
                {
                    this.Attributes.Remove("multiple");
                }
            }
        }

        public int Size
        {
            get { return Convert.ToInt32(this.Attributes["size"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["size"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public int SelectedIndex
        {
            get
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    if (this.Items[i].Selected)
                    {
                        return i;
                    }
                }

                return this.Items.Count > 0 && !this.Multiple ? 0 : -1;
            }
            set
            {
                for (int i = 0; i < this.Items.Count; i++)
                {
                    this.Items[i].Selected = i == value;
                }
            }
        }

        public string Value
        {
            get
            {
                int index = this.SelectedIndex;
                return index >= 0 ? this.Items[index].Value : string.Empty;
            }
            set
            {
                foreach (ListItem item in this.Items)
                {
                    item.Selected = string.Equals(item.Value, value, StringComparison.Ordinal);
                }
            }
        }

        public override void DataBind()
        {
            this.OnDataBinding(EventArgs.Empty);
            IEnumerable source = DataSourceAccess.GetEnumerable(this.DataSource, this.DataMember);

            if (source == null)
            {
                return;
            }

            this.Items.Clear();

            foreach (object item in source)
            {
                string text = DataSourceAccess.GetValue(item, this.DataTextField);
                string value = string.IsNullOrEmpty(this.DataValueField) ? text : DataSourceAccess.GetValue(item, this.DataValueField);
                this.Items.Add(new ListItem(text, value));
            }
        }

        public bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string[] values = postCollection.GetValues(postDataKey);

            if (values == null)
            {
                return false;
            }

            bool changed = false;

            foreach (ListItem item in this.Items)
            {
                bool selected = values.Contains(item.Value);
                changed |= selected != item.Selected;
                item.Selected = selected;
            }

            return changed;
        }

        public void RaisePostDataChangedEvent()
        {
            this.ServerChange?.Invoke(this, EventArgs.Empty);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.RenderBeginTag(writer);

            foreach (ListItem item in this.Items)
            {
                item.RenderOption(writer);
            }

            this.RenderChildren(writer);
            this.RenderEndTag(writer);
        }

        protected override void RenderAttributes(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.WriteAttribute("name", this.UniqueID);
            }

            base.RenderAttributes(writer);
        }
    }

    public class HtmlTable : HtmlContainerControl
    {
        public HtmlTable() : base("table")
        {
        }

        public HtmlTableRowCollection Rows => new HtmlTableRowCollection(this);

        public int Border
        {
            get { return Convert.ToInt32(this.Attributes["border"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["border"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public int CellPadding
        {
            get { return Convert.ToInt32(this.Attributes["cellpadding"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["cellpadding"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public int CellSpacing
        {
            get { return Convert.ToInt32(this.Attributes["cellspacing"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["cellspacing"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public string Width
        {
            get { return this.Attributes["width"]; }
            set { this.Attributes["width"] = value; }
        }
    }

    public sealed class HtmlTableRowCollection : IEnumerable
    {
        private readonly HtmlTable owner;

        internal HtmlTableRowCollection(HtmlTable owner)
        {
            this.owner = owner;
        }

        public int Count => this.owner.Controls.OfType<HtmlTableRow>().Count();
        public HtmlTableRow this[int index] => this.owner.Controls.OfType<HtmlTableRow>().ElementAt(index);

        public void Add(HtmlTableRow row)
        {
            this.owner.Controls.Add(row);
        }

        public void Insert(int index, HtmlTableRow row)
        {
            this.owner.Controls.AddAt(index, row);
        }

        public void Remove(HtmlTableRow row)
        {
            this.owner.Controls.Remove(row);
        }

        public void Clear()
        {
            this.owner.Controls.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return this.owner.Controls.OfType<HtmlTableRow>().ToList().GetEnumerator();
        }
    }

    public class HtmlTableRow : HtmlContainerControl
    {
        public HtmlTableRow() : base("tr")
        {
        }

        public HtmlTableCellCollection Cells => new HtmlTableCellCollection(this);
    }

    public sealed class HtmlTableCellCollection : IEnumerable
    {
        private readonly HtmlTableRow owner;

        internal HtmlTableCellCollection(HtmlTableRow owner)
        {
            this.owner = owner;
        }

        public int Count => this.owner.Controls.OfType<HtmlTableCell>().Count();
        public HtmlTableCell this[int index] => this.owner.Controls.OfType<HtmlTableCell>().ElementAt(index);

        public void Add(HtmlTableCell cell)
        {
            this.owner.Controls.Add(cell);
        }

        public void Insert(int index, HtmlTableCell cell)
        {
            this.owner.Controls.AddAt(index, cell);
        }

        public void Remove(HtmlTableCell cell)
        {
            this.owner.Controls.Remove(cell);
        }

        public void Clear()
        {
            this.owner.Controls.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return this.owner.Controls.OfType<HtmlTableCell>().ToList().GetEnumerator();
        }
    }

    public class HtmlTableCell : HtmlContainerControl
    {
        public HtmlTableCell() : this("td")
        {
        }

        public HtmlTableCell(string tagName) : base(tagName)
        {
        }

        public int ColSpan
        {
            get { return Convert.ToInt32(this.Attributes["colspan"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["colspan"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public int RowSpan
        {
            get { return Convert.ToInt32(this.Attributes["rowspan"] ?? "-1", CultureInfo.InvariantCulture); }
            set { this.Attributes["rowspan"] = value.ToString(CultureInfo.InvariantCulture); }
        }

        public string Align
        {
            get { return this.Attributes["align"]; }
            set { this.Attributes["align"] = value; }
        }

        public string Width
        {
            get { return this.Attributes["width"]; }
            set { this.Attributes["width"] = value; }
        }
    }

    public static class DataSourceAccess
    {
        public static IEnumerable GetEnumerable(object dataSource, string dataMember)
        {
            switch (dataSource)
            {
                case null:
                    return null;
                case DataSet set:
                    DataTable table = string.IsNullOrEmpty(dataMember) ? set.Tables.Count > 0 ? set.Tables[0] : null : set.Tables[dataMember];
                    return table?.DefaultView;
                case DataTable dataTable:
                    return dataTable.DefaultView;
                case System.ComponentModel.IListSource listSource:
                    return listSource.GetList();
                case IEnumerable enumerable:
                    return enumerable;
                default:
                    return new[] {dataSource};
            }
        }

        public static string GetValue(object item, string field)
        {
            return Convert.ToString(GetObject(item, field), CultureInfo.CurrentCulture);
        }

        public static object GetObject(object item, string field)
        {
            if (item == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(field))
            {
                return item;
            }

            switch (item)
            {
                case DataRowView rowView:
                    return rowView.Row.Table.Columns.Contains(field) ? rowView[field] : null;
                case DataRow row:
                    return row.Table.Columns.Contains(field) ? row[field] : null;
                case IDictionary dictionary:
                    return dictionary[field];
            }

            System.ComponentModel.PropertyDescriptor property = System.ComponentModel.TypeDescriptor.GetProperties(item).Find(field, true);
            return property?.GetValue(item);
        }
    }
}
