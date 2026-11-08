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
using System.Net;
using System.Text;

namespace MixERP.Net.Common.Helpers
{
    /// <summary>
    ///     Semantic-UI markup factory.
    ///     <para>
    ///         On .NET Framework these members produced <c>HtmlGenericControl</c> /
    ///         <c>Literal</c> instances that were added into a WebForms control tree. That tree
    ///         does not exist on modern .NET, so every member now returns the SAME rendered
    ///         HTML as a <see cref="string" />: the tag, the Semantic-UI CSS class and the
    ///         nesting are byte-for-byte what WebForms rendered.
    ///     </para>
    ///     <para>
    ///         In a Razor component render the result with
    ///         <c>@((MarkupString)HtmlControlHelper.GetSegment())</c>. Container-producing
    ///         members (segment, field, fields, form, modal, …) have an additional overload
    ///         taking the inner HTML so a control tree built by <c>Controls.Add</c> becomes
    ///         nested composition instead; the original parameter lists are unchanged.
    ///     </para>
    ///     <para>
    ///         Text values are HTML-encoded, exactly as <c>HtmlGenericControl.InnerText</c> and
    ///         attribute values were encoded by WebForms.
    ///     </para>
    /// </summary>
    public static class HtmlControlHelper
    {
        #region Markup primitives

        /// <summary>
        ///     Renders an element with a class attribute and the supplied (already-HTML) inner markup,
        ///     reproducing <c>HtmlGenericControl</c> rendering.
        /// </summary>
        private static string GetElement(string tag, string cssClass, string innerHtml, string id)
        {
            StringBuilder markup = new StringBuilder();

            markup.Append("<").Append(tag);

            if (!string.IsNullOrWhiteSpace(id))
            {
                markup.Append(" id=\"").Append(WebUtility.HtmlEncode(id)).Append("\"");
            }

            if (cssClass != null)
            {
                markup.Append(" class=\"").Append(WebUtility.HtmlEncode(cssClass)).Append("\"");
            }

            markup.Append(">");

            if (!string.IsNullOrEmpty(innerHtml))
            {
                markup.Append(innerHtml);
            }

            markup.Append("</").Append(tag).Append(">");

            return markup.ToString();
        }

        /// <summary>
        ///     Encodes a text value the way <c>HtmlGenericControl.InnerText</c> did.
        /// </summary>
        private static string EncodeText(string text)
        {
            return text == null ? string.Empty : WebUtility.HtmlEncode(text);
        }

        #endregion Markup primitives

        public static string GetDivider()
        {
            return GetElement("div", "ui divider", null, null);
        }

        public static string GetModalHeader(string text = "", string icon = "")
        {
            StringBuilder inner = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(icon))
            {
                inner.Append(GetIcon(icon));
            }

            //The original added a Literal carrying the header text after the icon. A Literal
            //renders its Text RAW, so encoding here is a deliberate behaviour change: it is the
            //correct XSS posture for the text overload. Callers that genuinely passed markup
            //(&nbsp;, <b>..</b>, a nested icon span) must move to GetModalHeaderHtml below.
            inner.Append(EncodeText(text));

            return GetElement("div", "header", inner.ToString(), null);
        }

        /// <summary>
        ///     Same markup as <see cref="GetModalHeader" /> but emits <paramref name="innerHtml" />
        ///     VERBATIM, reproducing the raw rendering of the original
        ///     <c>System.Web.UI.WebControls.Literal</c>. Use this only for trusted markup —
        ///     prefer <see cref="GetModalHeader" />, which encodes.
        /// </summary>
        public static string GetModalHeaderHtml(string innerHtml = "", string icon = "")
        {
            StringBuilder inner = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(icon))
            {
                inner.Append(GetIcon(icon));
            }

            inner.Append(innerHtml);

            return GetElement("div", "header", inner.ToString(), null);
        }

        public static string GetField()
        {
            return GetElement("div", "field", null, null);
        }

        public static string GetField(string cssClass)
        {
            return GetElement("div", cssClass, null, null);
        }

        /// <summary>
        ///     Composing overload: a "field" div wrapping the supplied inner markup.
        /// </summary>
        public static string GetField(string cssClass, string innerHtml)
        {
            return GetElement("div", cssClass, innerHtml, null);
        }

        public static string GetFields()
        {
            return GetElement("div", "fields", null, null);
        }

        public static string GetFields(string cssClass)
        {
            return GetElement("div", cssClass, null, null);
        }

        /// <summary>
        ///     Composing overload: a "fields" div wrapping the supplied inner markup.
        /// </summary>
        public static string GetFields(string cssClass, string innerHtml)
        {
            return GetElement("div", cssClass, innerHtml, null);
        }

        public static string GetForm()
        {
            return GetElement("div", "ui form", null, null);
        }

        /// <summary>
        ///     Composing overload: a "ui form" div wrapping the supplied inner markup.
        /// </summary>
        public static string GetForm(string innerHtml)
        {
            return GetElement("div", "ui form", innerHtml, null);
        }

        public static string GetFormSegment()
        {
            return GetElement("div", "ui form segment", null, null);
        }

        public static string GetFormSegment(string cssClass)
        {
            return GetElement("div", cssClass, null, null);
        }

        /// <summary>
        ///     Composing overload: a form-segment div wrapping the supplied inner markup.
        /// </summary>
        public static string GetFormSegment(string cssClass, string innerHtml)
        {
            return GetElement("div", cssClass, innerHtml, null);
        }

        public static string GetIcon(string cssClass)
        {
            return GetElement("i", cssClass, null, null);
        }

        public static string GetInlineFields()
        {
            return GetElement("div", "inline fields", null, null);
        }

        /// <summary>
        ///     Composing overload: an "inline fields" div wrapping the supplied inner markup.
        /// </summary>
        public static string GetInlineFields(string innerHtml)
        {
            return GetElement("div", "inline fields", innerHtml, null);
        }

        public static string GetLabel(string text)
        {
            return GetElement("label", null, EncodeText(text), null);
        }

        public static string GetLabel(string text, string targetControlId)
        {
            StringBuilder markup = new StringBuilder();

            markup.Append("<label for=\"").Append(WebUtility.HtmlEncode(targetControlId)).Append("\">");
            markup.Append(EncodeText(text));
            markup.Append("</label>");

            return markup.ToString();
        }

        public static string GetPageHeader(string text)
        {
            StringBuilder inner = new StringBuilder();

            inner.Append(GetElement("h1", "ui purple header", EncodeText(text), null));
            inner.Append(GetDivider());

            return GetElement("div", null, inner.ToString(), null);
        }

        public static string GetLabelHtml(string text)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label>{0}</label>", text);
        }

        public static string GetLabelHtml(string text, string targetControlId)
        {
            return String.Format(CultureInfo.InvariantCulture, "<label for='{1}'>{0}</label>", text, targetControlId);
        }

        public static string GetLeftIconInput()
        {
            return GetElement("div", "ui left icon input", null, null);
        }

        /// <summary>
        ///     Composing overload: the Semantic-UI left-icon input wrapper around the supplied
        ///     icon + input markup.
        /// </summary>
        public static string GetLeftIconInput(string innerHtml)
        {
            return GetElement("div", "ui left icon input", innerHtml, null);
        }

        public static string GetModal()
        {
            return GetElement("div", "ui modal", null, null);
        }

        public static string GetModal(string cssClass, string id = "")
        {
            return GetElement("div", cssClass, null, id);
        }

        /// <summary>
        ///     Composing overload: a modal div wrapping the supplied inner markup.
        /// </summary>
        public static string GetModal(string cssClass, string id, string innerHtml)
        {
            return GetElement("div", cssClass, innerHtml, id);
        }

        public static string GetSegment()
        {
            return GetElement("div", "ui segment", null, null);
        }

        public static string GetSegment(string cssClass)
        {
            return GetElement("div", cssClass, null, null);
        }

        /// <summary>
        ///     Composing overload: a segment div wrapping the supplied inner markup.
        /// </summary>
        public static string GetSegment(string cssClass, string innerHtml)
        {
            return GetElement("div", cssClass, innerHtml, null);
        }

        /// <summary>
        ///     Appends a <c>&lt;li&gt;{key} : {value}&lt;/li&gt;</c> item to <paramref name="container" />
        ///     and returns that item's markup.
        ///     <para>
        ///         SIGNATURE CHANGE: the first parameter was an <c>HtmlGenericControl</c> whose
        ///         <c>Controls</c> collection was mutated. A markup-producing library cannot mutate a
        ///         control, so the accumulator is now a <see cref="StringBuilder" /> — the call site
        ///         keeps its shape (<c>AddListItem(list, key, value)</c> in a loop) and the rendered
        ///         <c>&lt;li&gt;</c> text is unchanged. Passing null returns the markup without
        ///         accumulating.
        ///     </para>
        /// </summary>
        public static string AddListItem(StringBuilder container, string key, object value)
        {
            string item = GetElement("li", null, EncodeText(key + " : " + value), null);

            if (container != null)
            {
                container.Append(item);
            }

            return item;
        }

        public static string GetToggleCheckBox()
        {
            return GetElement("div", "ui toggle checkbox", null, null);
        }

        /// <summary>
        ///     Composing overload: the Semantic-UI toggle-checkbox wrapper around the supplied
        ///     checkbox + label markup.
        /// </summary>
        public static string GetToggleCheckBox(string innerHtml)
        {
            return GetElement("div", "ui toggle checkbox", innerHtml, null);
        }
    }
}
