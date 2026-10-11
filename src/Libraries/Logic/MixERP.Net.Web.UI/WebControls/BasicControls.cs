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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Web;

namespace MixERP.Net.Web.UI.WebControls
{
    public enum LiteralMode
    {
        Transform = 0,
        PassThrough = 1,
        Encode = 2
    }

    public class Literal : Control
    {
        public string Text { get; set; }
        public LiteralMode Mode { get; set; }

        protected internal override void Render(HtmlTextWriter writer)
        {
            if (this.Mode == LiteralMode.Encode)
            {
                writer.WriteEncodedText(this.Text);
                return;
            }

            writer.Write(this.Text);
        }
    }

    public class PlaceHolder : Control
    {
    }

    /// <summary>
    ///     A region of a master page which is filled by the matching <see cref="Content" /> of the page.
    /// </summary>
    public class ContentPlaceHolder : Control, INamingContainer
    {
    }

    public class Content : Control, INamingContainer
    {
        public string ContentPlaceHolderID { get; set; }
        internal MasterPage OwnerMaster { get; set; }
    }

    public class Label : WebControl
    {
        public Label() : base(HtmlTextWriterTag.Span)
        {
        }

        public virtual string Text { get; set; }
        public string AssociatedControlID { get; set; }

        protected override string TagName => string.IsNullOrEmpty(this.AssociatedControlID) ? "span" : "label";

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.AssociatedControlID))
            {
                Control target = this.NamingContainer?.FindControl(this.AssociatedControlID);
                writer.AddAttribute(HtmlTextWriterAttribute.For, target?.ClientID ?? this.AssociatedControlID);
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (this.HasControls())
            {
                base.RenderContents(writer);
                return;
            }

            writer.Write(this.Text);
        }
    }

    public enum TextBoxMode
    {
        SingleLine = 0,
        MultiLine = 1,
        Password = 2,
        Color = 3,
        Date = 4,
        DateTime = 5,
        DateTimeLocal = 6,
        Email = 7,
        Month = 8,
        Number = 9,
        Range = 10,
        Search = 11,
        Phone = 12,
        Time = 13,
        Url = 14,
        Week = 15
    }

    public class TextBox : WebControl, IPostBackDataHandler
    {
        public TextBox() : base(HtmlTextWriterTag.Input)
        {
        }

        public event EventHandler TextChanged;

        public virtual string Text { get; set; } = string.Empty;
        public virtual TextBoxMode TextMode { get; set; }
        public virtual bool ReadOnly { get; set; }
        public virtual int MaxLength { get; set; }
        public virtual int Rows { get; set; }
        public virtual int Columns { get; set; }
        public virtual bool AutoPostBack { get; set; }
        public virtual bool CausesValidation { get; set; }
        public virtual string ValidationGroup { get; set; }

        protected override string TagName => this.TextMode == TextBoxMode.MultiLine ? "textarea" : "input";

        public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            string posted = postCollection[postDataKey];

            if (posted == null || this.ReadOnly || string.Equals(posted, this.Text, StringComparison.Ordinal))
            {
                return false;
            }

            this.Text = posted;
            return true;
        }

        public virtual void RaisePostDataChangedEvent()
        {
            this.TextChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);

            if (this.TextMode != TextBoxMode.MultiLine)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Type, GetInputType(this.TextMode));

                if (this.TextMode != TextBoxMode.Password && !string.IsNullOrEmpty(this.Text))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, this.Text);
                }

                if (this.MaxLength > 0)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Maxlength, this.MaxLength.ToString(CultureInfo.InvariantCulture));
                }

                if (this.Columns > 0)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Size, this.Columns.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                if (this.Rows > 0)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Rows, this.Rows.ToString(CultureInfo.InvariantCulture));
                }

                if (this.Columns > 0)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Cols, this.Columns.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (this.ReadOnly)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
            }

            if (this.AutoPostBack && this.Page != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onchange, this.Page.ClientScript.GetPostBackEventReference(this, string.Empty));
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (this.TextMode == TextBoxMode.MultiLine)
            {
                writer.WriteEncodedText(this.Text);
            }
        }

        private static string GetInputType(TextBoxMode mode)
        {
            switch (mode)
            {
                case TextBoxMode.Password:
                    return "password";
                case TextBoxMode.Phone:
                    return "tel";
                case TextBoxMode.DateTimeLocal:
                    return "datetime-local";
                case TextBoxMode.SingleLine:
                    return "text";
                default:
                    return mode.ToString().ToLowerInvariant();
            }
        }
    }

    public class HiddenField : Control, IPostBackDataHandler
    {
        public event EventHandler ValueChanged;

        public virtual string Value { get; set; } = string.Empty;

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
            this.ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);

            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Value, this.Value);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag();
        }
    }

    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(string commandName, object argument)
        {
            this.CommandName = commandName;
            this.CommandArgument = argument;
        }

        public CommandEventArgs(CommandEventArgs e) : this(e.CommandName, e.CommandArgument)
        {
        }

        public string CommandName { get; }
        public object CommandArgument { get; }
    }

    public delegate void CommandEventHandler(object sender, CommandEventArgs e);

    public sealed class ImageClickEventArgs : EventArgs
    {
        public ImageClickEventArgs(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public delegate void ImageClickEventHandler(object sender, ImageClickEventArgs e);

    public interface IButtonControl
    {
        bool CausesValidation { get; set; }
        string CommandArgument { get; set; }
        string CommandName { get; set; }
        string Text { get; set; }
        string ValidationGroup { get; set; }
        event EventHandler Click;
        event CommandEventHandler Command;
    }

    public class Button : WebControl, IPostBackEventHandler, IButtonControl
    {
        public Button() : base(HtmlTextWriterTag.Input)
        {
        }

        public event EventHandler Click;
        public event CommandEventHandler Command;

        public virtual string Text { get; set; } = string.Empty;
        public virtual string CommandName { get; set; } = string.Empty;
        public virtual string CommandArgument { get; set; } = string.Empty;
        public virtual string OnClientClick { get; set; }
        public virtual bool UseSubmitBehavior { get; set; } = true;
        public virtual bool CausesValidation { get; set; } = true;
        public virtual string ValidationGroup { get; set; } = string.Empty;
        public virtual string PostBackUrl { get; set; }

        public void RaisePostBackEvent(string eventArgument)
        {
            if (this.CausesValidation)
            {
                this.Page?.Validate(this.ValidationGroup);
            }

            this.OnClick(EventArgs.Empty);
            this.OnCommand(new CommandEventArgs(this.CommandName, this.CommandArgument));
        }

        protected virtual void OnClick(EventArgs e)
        {
            this.Click?.Invoke(this, e);
        }

        protected virtual void OnCommand(CommandEventArgs e)
        {
            this.Command?.Invoke(this, e);
            this.RaiseBubbleEvent(this, e);
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, this.UseSubmitBehavior ? "submit" : "button");
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
            writer.AddAttribute(HtmlTextWriterAttribute.Value, this.Text);

            string onClick = this.OnClientClick;

            if (!this.UseSubmitBehavior && this.Page != null)
            {
                string postBack = this.Page.ClientScript.GetPostBackEventReference(this, string.Empty);
                onClick = string.IsNullOrEmpty(onClick) ? postBack : onClick.TrimEnd(';') + ";" + postBack;
            }

            if (!string.IsNullOrEmpty(onClick))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, onClick);
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
        }
    }

    public class LinkButton : WebControl, IPostBackEventHandler, IButtonControl
    {
        public LinkButton() : base(HtmlTextWriterTag.A)
        {
        }

        public event EventHandler Click;
        public event CommandEventHandler Command;

        public virtual string Text { get; set; } = string.Empty;
        public virtual string CommandName { get; set; } = string.Empty;
        public virtual string CommandArgument { get; set; } = string.Empty;
        public virtual string OnClientClick { get; set; }
        public virtual bool CausesValidation { get; set; } = true;
        public virtual string ValidationGroup { get; set; } = string.Empty;

        public void RaisePostBackEvent(string eventArgument)
        {
            if (this.CausesValidation)
            {
                this.Page?.Validate(this.ValidationGroup);
            }

            this.Click?.Invoke(this, EventArgs.Empty);
            CommandEventArgs args = new CommandEventArgs(this.CommandName, this.CommandArgument);
            this.Command?.Invoke(this, args);
            this.RaiseBubbleEvent(this, args);
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.OnClientClick))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, this.OnClientClick);
            }

            if (this.Page != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Href, this.Page.ClientScript.GetPostBackClientHyperlink(this, string.Empty));
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (this.HasControls())
            {
                base.RenderContents(writer);
                return;
            }

            writer.Write(this.Text);
        }
    }

    public class Image : WebControl
    {
        public Image() : base(HtmlTextWriterTag.Img)
        {
        }

        public virtual string ImageUrl { get; set; }
        public virtual string AlternateText { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ImageUrl))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Src, this.ResolveUrl(this.ImageUrl));
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Alt, this.AlternateText ?? string.Empty);
            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
        }
    }

    public class ImageButton : Image, IPostBackEventHandler, IButtonControl
    {
        public event ImageClickEventHandler Click;
        public event CommandEventHandler Command;

        event EventHandler IButtonControl.Click
        {
            add { }
            remove { }
        }

        public virtual string Text
        {
            get { return this.AlternateText; }
            set { this.AlternateText = value; }
        }

        public virtual string CommandName { get; set; } = string.Empty;
        public virtual string CommandArgument { get; set; } = string.Empty;
        public virtual string OnClientClick { get; set; }
        public virtual bool CausesValidation { get; set; } = true;
        public virtual string ValidationGroup { get; set; } = string.Empty;

        public void RaisePostBackEvent(string eventArgument)
        {
            if (this.CausesValidation)
            {
                this.Page?.Validate(this.ValidationGroup);
            }

            int x = 0;
            int y = 0;
            HttpContext context = this.Context;

            if (context != null)
            {
                int.TryParse(context.Request.Form[this.UniqueID + ".x"], NumberStyles.Integer, CultureInfo.InvariantCulture, out x);
                int.TryParse(context.Request.Form[this.UniqueID + ".y"], NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
            }

            this.Click?.Invoke(this, new ImageClickEventArgs(x, y));
            CommandEventArgs args = new CommandEventArgs(this.CommandName, this.CommandArgument);
            this.Command?.Invoke(this, args);
            this.RaiseBubbleEvent(this, args);
        }

        protected override string TagName => "input";

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "image");
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);

            if (!string.IsNullOrEmpty(this.OnClientClick))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, this.OnClientClick);
            }

            base.AddAttributesToRender(writer);
        }
    }

    public class HyperLink : WebControl
    {
        public HyperLink() : base(HtmlTextWriterTag.A)
        {
        }

        public virtual string NavigateUrl { get; set; }
        public virtual string Target { get; set; }
        public virtual string Text { get; set; }
        public virtual string ImageUrl { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.NavigateUrl) && this.Enabled)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Href, this.ResolveUrl(this.NavigateUrl));
            }

            if (!string.IsNullOrEmpty(this.Target))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Target, this.Target);
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ImageUrl))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Src, this.ResolveUrl(this.ImageUrl));
                writer.AddAttribute(HtmlTextWriterAttribute.Alt, this.Text ?? string.Empty);
                writer.RenderBeginTag(HtmlTextWriterTag.Img);
                writer.RenderEndTag();
                return;
            }

            if (this.HasControls())
            {
                base.RenderContents(writer);
                return;
            }

            writer.Write(this.Text);
        }
    }

    public class Panel : WebControl
    {
        public Panel() : base(HtmlTextWriterTag.Div)
        {
        }

        public virtual string DefaultButton { get; set; }
        public virtual string GroupingText { get; set; }
        public virtual ScrollBars ScrollBars { get; set; }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            switch (this.ScrollBars)
            {
                case ScrollBars.Horizontal:
                    this.Style["overflow-x"] = "scroll";
                    break;
                case ScrollBars.Vertical:
                    this.Style["overflow-y"] = "scroll";
                    break;
                case ScrollBars.Both:
                    this.Style["overflow"] = "scroll";
                    break;
                case ScrollBars.Auto:
                    this.Style["overflow"] = "auto";
                    break;
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
            if (string.IsNullOrEmpty(this.GroupingText))
            {
                base.RenderContents(writer);
                return;
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Fieldset);
            writer.RenderBeginTag(HtmlTextWriterTag.Legend);
            writer.WriteEncodedText(this.GroupingText);
            writer.RenderEndTag();
            base.RenderContents(writer);
            writer.RenderEndTag();
        }
    }

    public enum ScrollBars
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = 3,
        Auto = 4
    }

    public class CheckBox : WebControl, IPostBackDataHandler
    {
        public CheckBox() : base(HtmlTextWriterTag.Input)
        {
        }

        public event EventHandler CheckedChanged;

        public virtual bool Checked { get; set; }
        public virtual string Text { get; set; }
        public virtual bool AutoPostBack { get; set; }

        protected virtual string InputType => "checkbox";

        protected virtual string InputName => this.UniqueID;

        protected virtual string InputValue => null;

        public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            bool posted = postCollection[postDataKey] != null;
            bool changed = posted != this.Checked;
            this.Checked = posted;
            return changed;
        }

        public void RaisePostDataChangedEvent()
        {
            this.CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, this.InputType);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.InputName);

            if (this.InputValue != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Value, this.InputValue);
            }

            if (this.Checked)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
            }

            if (this.AutoPostBack && this.Page != null)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Onclick, this.Page.ClientScript.GetPostBackEventReference(this, string.Empty));
            }

            this.AddAttributesToRender(writer);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag();

            if (!string.IsNullOrEmpty(this.Text))
            {
                if (!string.IsNullOrEmpty(this.ID))
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.For, this.ClientID);
                }

                writer.RenderBeginTag(HtmlTextWriterTag.Label);
                writer.Write(this.Text);
                writer.RenderEndTag();
            }
        }
    }

    public class RadioButton : CheckBox
    {
        public virtual string GroupName { get; set; }

        protected override string InputType => "radio";

        protected override string InputName => string.IsNullOrEmpty(this.GroupName) ? this.UniqueID : this.GroupName;

        protected override string InputValue => this.UniqueID;

        public override bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            bool isChecked = string.Equals(postCollection[this.InputName], this.UniqueID, StringComparison.Ordinal);
            bool changed = isChecked != this.Checked;
            this.Checked = isChecked;
            return changed;
        }
    }

    public class FileUpload : WebControl
    {
        public FileUpload() : base(HtmlTextWriterTag.Input)
        {
        }

        public bool AllowMultiple { get; set; }

        [CLSCompliant(false)]

        public HttpPostedFile PostedFile => this.Context?.Request.Files[this.UniqueID];

        public bool HasFile
        {
            get
            {
                HttpPostedFile file = this.PostedFile;
                return file != null && file.ContentLength > 0;
            }
        }

        public string FileName => this.PostedFile == null ? string.Empty : Path.GetFileName(this.PostedFile.FileName);

        public Stream FileContent => this.PostedFile?.InputStream ?? Stream.Null;

        public byte[] FileBytes
        {
            get
            {
                HttpPostedFile file = this.PostedFile;

                if (file == null)
                {
                    return new byte[0];
                }

                using (MemoryStream memory = new MemoryStream())
                {
                    file.InputStream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        public void SaveAs(string filename)
        {
            this.PostedFile?.SaveAs(filename);
        }

        protected override void AddAttributesToRender(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "file");
            writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);

            if (this.AllowMultiple)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Multiple, "multiple");
            }

            base.AddAttributesToRender(writer);
        }

        protected internal override void RenderContents(HtmlTextWriter writer)
        {
        }
    }
}
