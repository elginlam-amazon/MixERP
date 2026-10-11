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
using System.Globalization;

namespace MixERP.Net.Web.UI.WebControls
{
    public enum UnitType
    {
        Pixel = 1,
        Point = 2,
        Pica = 3,
        Inch = 4,
        Mm = 5,
        Cm = 6,
        Percentage = 7,
        Em = 8,
        Ex = 9
    }

    [Serializable]
    public struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Empty = new Unit();

        public Unit(int value) : this(value, UnitType.Pixel)
        {
        }

        public Unit(double value) : this(value, UnitType.Pixel)
        {
        }

        public Unit(double value, UnitType type)
        {
            this.Value = value;
            this.Type = type;
            this.hasValue = true;
        }

        public Unit(string value)
        {
            this = Parse(value);
        }

        private readonly bool hasValue;

        public double Value { get; }
        public UnitType Type { get; }

        /// <summary>
        ///     True for the default (unset) unit, which is not rendered.
        /// </summary>
        public bool IsEmpty => !this.hasValue;

        public static Unit Pixel(int n)
        {
            return new Unit(n, UnitType.Pixel);
        }

        public static Unit Percentage(double n)
        {
            return new Unit(n, UnitType.Percentage);
        }

        public static Unit Point(int n)
        {
            return new Unit(n, UnitType.Point);
        }

        public static Unit Parse(string s, CultureInfo culture)
        {
            return Parse(s);
        }

        public static Unit Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return Empty;
            }

            s = s.Trim().ToLowerInvariant();
            string[] suffixes = {"px", "pt", "pc", "in", "mm", "cm", "%", "em", "ex"};
            UnitType[] types = {UnitType.Pixel, UnitType.Point, UnitType.Pica, UnitType.Inch, UnitType.Mm, UnitType.Cm, UnitType.Percentage, UnitType.Em, UnitType.Ex};

            for (int i = 0; i < suffixes.Length; i++)
            {
                if (s.EndsWith(suffixes[i], StringComparison.Ordinal))
                {
                    return new Unit(double.Parse(s.Substring(0, s.Length - suffixes[i].Length), CultureInfo.InvariantCulture), types[i]);
                }
            }

            return new Unit(double.Parse(s, CultureInfo.InvariantCulture), UnitType.Pixel);
        }

        public static implicit operator Unit(int n)
        {
            return Pixel(n);
        }

        public static bool operator ==(Unit left, Unit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Unit left, Unit right)
        {
            return !left.Equals(right);
        }

        public bool Equals(Unit other)
        {
            return this.IsEmpty == other.IsEmpty && this.Type == other.Type && this.Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is Unit other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode() ^ (int)this.Type;
        }

        public override string ToString()
        {
            if (this.IsEmpty)
            {
                return string.Empty;
            }

            string number = this.Value.ToString(CultureInfo.InvariantCulture);

            switch (this.Type)
            {
                case UnitType.Percentage:
                    return number + "%";
                case UnitType.Point:
                    return number + "pt";
                case UnitType.Pica:
                    return number + "pc";
                case UnitType.Inch:
                    return number + "in";
                case UnitType.Mm:
                    return number + "mm";
                case UnitType.Cm:
                    return number + "cm";
                case UnitType.Em:
                    return number + "em";
                case UnitType.Ex:
                    return number + "ex";
                default:
                    return number + "px";
            }
        }
    }

    public enum BorderStyle
    {
        NotSet = 0,
        None = 1,
        Dotted = 2,
        Dashed = 3,
        Solid = 4,
        Double = 5,
        Groove = 6,
        Ridge = 7,
        Inset = 8,
        Outset = 9
    }

    public enum HorizontalAlign
    {
        NotSet = 0,
        Left = 1,
        Center = 2,
        Right = 3,
        Justify = 4
    }

    public enum VerticalAlign
    {
        NotSet = 0,
        Top = 1,
        Middle = 2,
        Bottom = 3
    }

    public sealed class FontInfo
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public string Name { get; set; }

        internal void AddTo(CssStyleCollection style)
        {
            if (this.Bold)
            {
                style["font-weight"] = "bold";
            }

            if (this.Italic)
            {
                style["font-style"] = "italic";
            }

            if (this.Underline)
            {
                style["text-decoration"] = "underline";
            }

            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                style["font-family"] = this.Name;
            }
        }
    }

    public class Style
    {
        public Style()
        {
            this.Font = new FontInfo();
        }

        public string CssClass { get; set; }
        public Unit Width { get; set; }
        public Unit Height { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public Unit BorderWidth { get; set; }
        public FontInfo Font { get; }

        public virtual void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(this.CssClass))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, this.CssClass);
            }

            CssStyleCollection style = new CssStyleCollection();
            this.FillStyleAttributes(style);

            if (!string.IsNullOrEmpty(style.Value))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Style, style.Value);
            }
        }

        protected internal virtual void FillStyleAttributes(CssStyleCollection style)
        {
            if (!this.Width.IsEmpty)
            {
                style["width"] = this.Width.ToString();
            }

            if (!this.Height.IsEmpty)
            {
                style["height"] = this.Height.ToString();
            }

            if (this.BorderStyle != BorderStyle.NotSet)
            {
                style["border-style"] = this.BorderStyle.ToString().ToLowerInvariant();
            }

            if (!this.BorderWidth.IsEmpty)
            {
                style["border-width"] = this.BorderWidth.ToString();
            }

            this.Font.AddTo(style);
        }
    }

    public class TableItemStyle : Style
    {
        public HorizontalAlign HorizontalAlign { get; set; }
        public VerticalAlign VerticalAlign { get; set; }
        public bool Wrap { get; set; } = true;

        protected internal override void FillStyleAttributes(CssStyleCollection style)
        {
            base.FillStyleAttributes(style);

            if (this.HorizontalAlign != HorizontalAlign.NotSet)
            {
                style["text-align"] = this.HorizontalAlign.ToString().ToLowerInvariant();
            }

            if (this.VerticalAlign != VerticalAlign.NotSet)
            {
                style["vertical-align"] = this.VerticalAlign.ToString().ToLowerInvariant();
            }

            if (!this.Wrap)
            {
                style["white-space"] = "nowrap";
            }
        }
    }

    /// <summary>
    ///     Base class of rich server controls (replacement for System.Web.UI.WebControls.WebControl).
    /// </summary>
    public class WebControl : Control
    {
        private readonly string tagName;

        protected WebControl() : this(HtmlTextWriterTag.Span)
        {
        }

        public WebControl(HtmlTextWriterTag tag)
        {
            this.TagKey = tag;
            this.tagName = HtmlTextWriter.GetTagName(tag);
            this.Attributes = new AttributeCollection();
            this.ControlStyle = this.CreateControlStyle();
        }

        protected WebControl(string tag)
        {
            this.TagKey = HtmlTextWriterTag.Unknown;
            this.tagName = tag;
            this.Attributes = new AttributeCollection();
            this.ControlStyle = this.CreateControlStyle();
        }

        protected virtual HtmlTextWriterTag TagKey { get; }
        protected virtual string TagName => this.TagKey == HtmlTextWriterTag.Unknown ? this.tagName : HtmlTextWriter.GetTagName(this.TagKey);

        public AttributeCollection Attributes { get; }
        public CssStyleCollection Style => this.Attributes.CssStyle;
        public Style ControlStyle { get; }

        public virtual string CssClass
        {
            get { return this.ControlStyle.CssClass ?? string.Empty; }
            set { this.ControlStyle.CssClass = value; }
        }

        public virtual Unit Width
        {
            get { return this.ControlStyle.Width; }
            set { this.ControlStyle.Width = value; }
        }

        public virtual Unit Height
        {
            get { return this.ControlStyle.Height; }
            set { this.ControlStyle.Height = value; }
        }

        public virtual BorderStyle BorderStyle
        {
            get { return this.ControlStyle.BorderStyle; }
            set { this.ControlStyle.BorderStyle = value; }
        }

        public virtual Unit BorderWidth
        {
            get { return this.ControlStyle.BorderWidth; }
            set { this.ControlStyle.BorderWidth = value; }
        }

        public FontInfo Font => this.ControlStyle.Font;

        public virtual bool Enabled { get; set; } = true;
        public virtual string ToolTip { get; set; }
        public virtual string AccessKey { get; set; }
        public virtual short TabIndex { get; set; }

        protected virtual Style CreateControlStyle()
        {
            return new Style();
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.RenderBeginTag(writer);
            this.RenderContents(writer);
            this.RenderEndTag(writer);
        }

        public virtual void RenderBeginTag(HtmlTextWriter writer)
        {
            this.AddAttributesToRender(writer);
            writer.RenderBeginTag(this.TagName);
        }

        public virtual void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag();
        }

        protected internal virtual void RenderContents(HtmlTextWriter writer)
        {
            this.RenderChildren(writer);
        }

        protected virtual void AddAttributesToRender(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(this.ID))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, this.ClientID);
            }

            if (!string.IsNullOrWhiteSpace(this.CssClass))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, this.CssClass);
            }

            if (!this.Enabled)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "disabled");
            }

            if (!string.IsNullOrEmpty(this.ToolTip))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Title, this.ToolTip);
            }

            if (!string.IsNullOrEmpty(this.AccessKey))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Accesskey, this.AccessKey);
            }

            if (this.TabIndex != 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Tabindex, this.TabIndex.ToString(CultureInfo.InvariantCulture));
            }

            foreach (string key in this.Attributes.Keys)
            {
                if (!string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
                {
                    writer.AddAttribute(key, this.Attributes[key]);
                }
            }

            CssStyleCollection style = new CssStyleCollection();
            this.ControlStyle.FillStyleAttributes(style);

            foreach (string key in this.Attributes.CssStyle.Keys)
            {
                style[key] = this.Attributes.CssStyle[key];
            }

            if (!string.IsNullOrEmpty(style.Value))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Style, style.Value);
            }
        }
    }

    /// <summary>
    ///     A control which creates its child controls on demand and acts as a naming container.
    /// </summary>
    public abstract class CompositeControl : WebControl, INamingContainer
    {
        protected CompositeControl() : base(HtmlTextWriterTag.Span)
        {
        }

        public override ControlCollection Controls
        {
            get
            {
                this.EnsureChildControls();
                return base.Controls;
            }
        }

        protected internal override void Render(HtmlTextWriter writer)
        {
            this.EnsureChildControls();
            base.Render(writer);
        }

        protected override void RecreateChildControls()
        {
            this.ChildControlsCreated = false;
            this.EnsureChildControls();
        }
    }
}
