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
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Builds the Semantic UI markup fragments shared by the MixERP controls. The Web Forms
    ///     version returned <c>HtmlGenericControl</c>s; this version returns ASP.NET Core
    ///     <see cref="TagBuilder" />s, which are appended to a parent through
    ///     <c>parent.InnerHtml.AppendHtml(child)</c> and rendered with <see cref="Render" />.
    /// </summary>
    public static class HtmlControlHelper
    {
        public static TagBuilder GetElement(string tagName, string cssClass = "", string id = "")
        {
            TagBuilder element = new TagBuilder(tagName);

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                element.MergeAttribute("class", cssClass);
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                element.MergeAttribute("id", id);
            }

            return element;
        }

        public static TagBuilder GetDivider()
        {
            return GetElement("div", "ui divider");
        }

        public static TagBuilder GetModalHeader(string text = "", string icon = "")
        {
            TagBuilder header = GetElement("div", "header");

            if (!string.IsNullOrWhiteSpace(icon))
            {
                header.InnerHtml.AppendHtml(GetIcon(icon));
            }

            header.InnerHtml.AppendHtml(text ?? string.Empty);
            return header;
        }

        public static TagBuilder GetField()
        {
            return GetElement("div", "field");
        }

        public static TagBuilder GetField(string cssClass)
        {
            return GetElement("div", cssClass);
        }

        public static TagBuilder GetFields()
        {
            return GetElement("div", "fields");
        }

        public static TagBuilder GetFields(string cssClass)
        {
            return GetElement("div", cssClass);
        }

        public static TagBuilder GetForm()
        {
            return GetElement("div", "ui form");
        }

        public static TagBuilder GetFormSegment()
        {
            return GetElement("div", "ui form segment");
        }

        public static TagBuilder GetFormSegment(string cssClass)
        {
            return GetElement("div", cssClass);
        }

        public static TagBuilder GetIcon(string cssClass)
        {
            return GetElement("i", cssClass);
        }

        public static TagBuilder GetInlineFields()
        {
            return GetElement("div", "inline fields");
        }

        public static TagBuilder GetLabel(string text)
        {
            TagBuilder label = new TagBuilder("label");
            label.InnerHtml.Append(text ?? string.Empty);
            return label;
        }

        public static TagBuilder GetLabel(string text, string targetControlId)
        {
            TagBuilder label = GetLabel(text);
            label.MergeAttribute("for", targetControlId);
            return label;
        }

        public static TagBuilder GetPageHeader(string text)
        {
            TagBuilder header = new TagBuilder("div");

            TagBuilder h1 = GetElement("h1", "ui purple header");
            h1.InnerHtml.Append(text ?? string.Empty);

            header.InnerHtml.AppendHtml(h1);
            header.InnerHtml.AppendHtml(GetDivider());

            return header;
        }

        public static string GetLabelHtml(string text)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label>{0}</label>", text);
        }

        public static string GetLabelHtml(string text, string targetControlId)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label for='{1}'>{0}</label>", text, targetControlId);
        }

        public static TagBuilder GetLeftIconInput()
        {
            return GetElement("div", "ui left icon input");
        }

        public static TagBuilder GetModal()
        {
            return GetElement("div", "ui modal");
        }

        public static TagBuilder GetModal(string cssClass, string id = "")
        {
            return GetElement("div", cssClass, id);
        }

        public static TagBuilder GetSegment()
        {
            return GetElement("div", "ui segment");
        }

        public static TagBuilder GetSegment(string cssClass)
        {
            return GetElement("div", cssClass);
        }

        public static void AddListItem(TagBuilder container, string key, object value)
        {
            if (container == null)
            {
                return;
            }

            TagBuilder li = new TagBuilder("li");
            li.InnerHtml.Append(key + " : " + value);
            container.InnerHtml.AppendHtml(li);
        }

        public static TagBuilder GetToggleCheckBox()
        {
            return GetElement("div", "ui toggle checkbox");
        }

        /// <summary>
        ///     Creates an input element (text, hidden, button, checkbox, ...).
        /// </summary>
        public static TagBuilder GetInput(string type, string id = "", string value = null, string cssClass = "")
        {
            TagBuilder input = GetElement("input", cssClass, id);
            input.TagRenderMode = TagRenderMode.SelfClosing;
            input.MergeAttribute("type", type);

            if (!string.IsNullOrWhiteSpace(id))
            {
                input.MergeAttribute("name", id);
            }

            if (value != null)
            {
                input.MergeAttribute("value", value);
            }

            return input;
        }

        /// <summary>
        ///     Renders an HTML fragment to a string.
        /// </summary>
        public static string Render(IHtmlContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }
    }
}
