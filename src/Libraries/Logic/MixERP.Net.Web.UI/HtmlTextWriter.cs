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
using System.IO;
using System.Text;

namespace MixERP.Net.Web.UI
{
    /// <summary>
    ///     HTML writer compatible with System.Web.UI.HtmlTextWriter: attributes are queued with AddAttribute /
    ///     AddStyleAttribute and emitted by RenderBeginTag; RenderEndTag closes the most recently opened tag.
    /// </summary>
    public class HtmlTextWriter : TextWriter
    {
        public const string DefaultTabString = "\t";
        public const char DoubleQuoteChar = '"';
        public const string EndTagLeftChars = "</";
        public const char EqualsChar = '=';
        public const string EqualsDoubleQuoteString = "=\"";
        public const string SelfClosingChars = " /";
        public const string SelfClosingTagEnd = " />";
        public const char SemicolonChar = ';';
        public const char SingleQuoteChar = '\'';
        public const char SlashChar = '/';
        public const char SpaceChar = ' ';
        public const char StyleEqualsChar = ':';
        public const char TagLeftChar = '<';
        public const char TagRightChar = '>';

        private static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
        };

        private readonly List<KeyValuePair<string, string>> pendingAttributes = new List<KeyValuePair<string, string>>();
        private readonly List<KeyValuePair<string, string>> pendingStyles = new List<KeyValuePair<string, string>>();
        private readonly Stack<string> openTags = new Stack<string>();

        public HtmlTextWriter(TextWriter writer) : this(writer, DefaultTabString)
        {
        }

        public HtmlTextWriter(TextWriter writer, string tabString)
        {
            this.InnerWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public TextWriter InnerWriter { get; set; }
        public int Indent { get; set; }
        public override Encoding Encoding => this.InnerWriter.Encoding;

        public override string NewLine
        {
            get { return this.InnerWriter.NewLine; }
            set { this.InnerWriter.NewLine = value; }
        }

        public override void Write(char value)
        {
            this.InnerWriter.Write(value);
        }

        public override void Write(string s)
        {
            this.InnerWriter.Write(s);
        }

        public override void WriteLine(string s)
        {
            this.InnerWriter.WriteLine(s);
        }

        public override void WriteLine()
        {
            this.InnerWriter.WriteLine();
        }

        public override void Flush()
        {
            this.InnerWriter.Flush();
        }

        public override string ToString()
        {
            return this.InnerWriter.ToString();
        }

        public virtual void WriteLineNoTabs(string s)
        {
            this.InnerWriter.WriteLine(s);
        }

        public virtual void AddAttribute(string name, string value)
        {
            this.AddAttribute(name, value, true);
        }

        public virtual void AddAttribute(string name, string value, bool fEncode)
        {
            this.pendingAttributes.Add(new KeyValuePair<string, string>(name, fEncode ? Encoder.AttributeEncode(value) : value));
        }

        public virtual void AddAttribute(HtmlTextWriterAttribute key, string value)
        {
            this.AddAttribute(GetAttributeName(key), value, true);
        }

        public virtual void AddAttribute(HtmlTextWriterAttribute key, string value, bool fEncode)
        {
            this.AddAttribute(GetAttributeName(key), value, fEncode);
        }

        public virtual void AddStyleAttribute(string name, string value)
        {
            this.pendingStyles.Add(new KeyValuePair<string, string>(name, value));
        }

        public virtual void AddStyleAttribute(HtmlTextWriterStyle key, string value)
        {
            this.AddStyleAttribute(GetStyleName(key), value);
        }

        public virtual void RenderBeginTag(HtmlTextWriterTag tagKey)
        {
            this.RenderBeginTag(GetTagName(tagKey));
        }

        public virtual void RenderBeginTag(string tagName)
        {
            this.InnerWriter.Write(TagLeftChar);
            this.InnerWriter.Write(tagName);
            this.WritePendingAttributes();

            if (VoidElements.Contains(tagName))
            {
                this.InnerWriter.Write(SelfClosingTagEnd);
                this.openTags.Push(null);
                return;
            }

            this.InnerWriter.Write(TagRightChar);
            this.openTags.Push(tagName);
        }

        public virtual void RenderEndTag()
        {
            if (this.openTags.Count == 0)
            {
                return;
            }

            string tagName = this.openTags.Pop();

            if (tagName == null)
            {
                return;
            }

            this.WriteEndTag(tagName);
        }

        public virtual void WriteBeginTag(string tagName)
        {
            this.InnerWriter.Write(TagLeftChar);
            this.InnerWriter.Write(tagName);
        }

        public virtual void WriteFullBeginTag(string tagName)
        {
            this.InnerWriter.Write(TagLeftChar);
            this.InnerWriter.Write(tagName);
            this.InnerWriter.Write(TagRightChar);
        }

        public virtual void WriteEndTag(string tagName)
        {
            this.InnerWriter.Write(EndTagLeftChars);
            this.InnerWriter.Write(tagName);
            this.InnerWriter.Write(TagRightChar);
        }

        public virtual void WriteAttribute(string name, string value)
        {
            this.WriteAttribute(name, value, true);
        }

        public virtual void WriteAttribute(string name, string value, bool fEncode)
        {
            this.InnerWriter.Write(SpaceChar);
            this.InnerWriter.Write(name);

            if (value == null)
            {
                return;
            }

            this.InnerWriter.Write(EqualsDoubleQuoteString);
            this.InnerWriter.Write(fEncode ? Encoder.AttributeEncode(value) : value);
            this.InnerWriter.Write(DoubleQuoteChar);
        }

        public virtual void WriteStyleAttribute(string name, string value)
        {
            this.InnerWriter.Write(name);
            this.InnerWriter.Write(StyleEqualsChar);
            this.InnerWriter.Write(value);
            this.InnerWriter.Write(SemicolonChar);
        }

        public virtual void WriteEncodedText(string text)
        {
            this.InnerWriter.Write(Encoder.HtmlEncode(text));
        }

        public virtual void WriteBreak()
        {
            this.InnerWriter.Write("<br />");
        }

        private void WritePendingAttributes()
        {
            foreach (KeyValuePair<string, string> attribute in this.pendingAttributes)
            {
                this.InnerWriter.Write(SpaceChar);
                this.InnerWriter.Write(attribute.Key);

                if (attribute.Value != null)
                {
                    this.InnerWriter.Write(EqualsDoubleQuoteString);
                    this.InnerWriter.Write(attribute.Value);
                    this.InnerWriter.Write(DoubleQuoteChar);
                }
            }

            if (this.pendingStyles.Count > 0)
            {
                StringBuilder style = new StringBuilder();

                foreach (KeyValuePair<string, string> pair in this.pendingStyles)
                {
                    style.Append(pair.Key).Append(StyleEqualsChar).Append(pair.Value).Append(SemicolonChar);
                }

                this.InnerWriter.Write(" style=\"");
                this.InnerWriter.Write(Encoder.AttributeEncode(style.ToString()));
                this.InnerWriter.Write(DoubleQuoteChar);
            }

            this.pendingAttributes.Clear();
            this.pendingStyles.Clear();
        }

        public static string GetTagName(HtmlTextWriterTag tagKey)
        {
            return tagKey.ToString().ToLowerInvariant();
        }

        public static string GetAttributeName(HtmlTextWriterAttribute attrKey)
        {
            return attrKey.ToString().ToLowerInvariant();
        }

        public static string GetStyleName(HtmlTextWriterStyle styleKey)
        {
            string name = styleKey.ToString();
            StringBuilder builder = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (char.IsUpper(c) && i > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLower(c, CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }

    public enum HtmlTextWriterTag
    {
        Unknown,
        A,
        Abbr,
        Address,
        B,
        Blockquote,
        Body,
        Br,
        Button,
        Caption,
        Center,
        Code,
        Col,
        Colgroup,
        Dd,
        Div,
        Dl,
        Dt,
        Em,
        Fieldset,
        Font,
        Form,
        H1,
        H2,
        H3,
        H4,
        H5,
        H6,
        Head,
        Hr,
        Html,
        I,
        Iframe,
        Img,
        Input,
        Label,
        Legend,
        Li,
        Link,
        Meta,
        Ol,
        Option,
        P,
        Pre,
        Script,
        Select,
        Small,
        Span,
        Strong,
        Style,
        Sub,
        Sup,
        Table,
        Tbody,
        Td,
        Textarea,
        Tfoot,
        Th,
        Thead,
        Title,
        Tr,
        U,
        Ul
    }

    public enum HtmlTextWriterAttribute
    {
        Accesskey,
        Align,
        Alt,
        Background,
        Bgcolor,
        Border,
        Bordercolor,
        Cellpadding,
        Cellspacing,
        Checked,
        Class,
        Cols,
        Colspan,
        Content,
        Disabled,
        Dir,
        For,
        Height,
        Href,
        Id,
        Lang,
        Maxlength,
        Multiple,
        Name,
        Nowrap,
        Onchange,
        Onclick,
        ReadOnly,
        Rel,
        Rows,
        Rowspan,
        Scope,
        Selected,
        Size,
        Src,
        Style,
        Summary,
        Tabindex,
        Target,
        Title,
        Type,
        Valign,
        Value,
        Width,
        Wrap
    }

    public enum HtmlTextWriterStyle
    {
        BackgroundColor,
        BackgroundImage,
        BorderCollapse,
        BorderColor,
        BorderStyle,
        BorderWidth,
        Color,
        Cursor,
        Direction,
        Display,
        FontFamily,
        FontSize,
        FontStyle,
        FontVariant,
        FontWeight,
        Height,
        Left,
        ListStyleImage,
        ListStyleType,
        Margin,
        MarginBottom,
        MarginLeft,
        MarginRight,
        MarginTop,
        Overflow,
        OverflowX,
        OverflowY,
        Padding,
        PaddingBottom,
        PaddingLeft,
        PaddingRight,
        PaddingTop,
        Position,
        TextAlign,
        TextDecoration,
        TextOverflow,
        Top,
        VerticalAlign,
        Visibility,
        WhiteSpace,
        Width,
        ZIndex
    }
}
