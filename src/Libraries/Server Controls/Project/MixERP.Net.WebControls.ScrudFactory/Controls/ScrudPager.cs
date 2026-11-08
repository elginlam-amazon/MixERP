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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MixERP.Net.i18n.Resources;
using System;
using System.Globalization;
using System.Threading;

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The scrud grid's pagination menu.
    ///     <para>
    ///         This was never a WebForms <c>Control</c> — it was a plain class that assembled a
    ///         <c>System.Web.UI.HtmlControls.HtmlGenericControl</c> tree of
    ///         <c>HtmlAnchor</c>/<c>&lt;i&gt;</c> pairs and handed it to the caller to add to its own
    ///         control collection. <c>HtmlGenericControl</c> and <c>HtmlAnchor</c> are removed on
    ///         net10.0, so <see cref="GetPager" /> now returns a
    ///         <see cref="RenderFragment" /> instead. Every one of the ten members, all of the page
    ///         arithmetic and the complete anchor markup are unchanged — the render calls below map
    ///         one-for-one onto the original's <c>Controls.Add</c> / <c>Attributes.Add</c> calls, in
    ///         the same order, so the emitted DOM is identical.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal class ScrudPager
    {
        /// <summary>
        ///     The special query string the party item-selector popup runs under. The original
        ///     compared against this literal in BOTH anchor builders to decide whether to carry the
        ///     modal callback parameters into every page link; it is hoisted to a constant so the two
        ///     comparisons cannot drift, but the string itself is byte-identical.
        /// </summary>
        private const string ModalPartyQueryString = "modal=1&CallBackFunctionName=processCallBackActions&AssociatedControlId=PartyIdHidden";

        private int TotalPages;
        internal int TotalRecords { get; set; }
        internal int CurrentPage { get; set; }
        internal int PageSize { get; set; }
        internal string CssClass { get; set; }
        internal string ActiveCssClass { get; set; }
        internal string ItemCssClass { get; set; }
        internal string CurrentPageUrl { get; set; }
        internal string QueryString { get; set; }
        public int BlockCount { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetPager()</c>. The page arithmetic is VERBATIM, including the
        ///     <c>PageSize == 0</c> fallback to <see cref="BlockCount" /> and the
        ///     <c>Decimal.Ceiling(Decimal.Divide(...))</c> rounding (which is not the same as integer
        ///     division — it rounds UP, so a partial last page still gets a link).
        ///     <para>
        ///         Item order, unchanged: first, previous, page 1, the "previous block" ellipsis, the
        ///         pages of the current block (page 1 and the last page skipped, because they are
        ///         emitted explicitly), the "next block" ellipsis, the last page, next, last.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        internal RenderFragment GetPager()
        {
            if (this.PageSize.Equals(0))
            {
                this.PageSize = this.BlockCount;
            }

            this.TotalPages = (int)Decimal.Ceiling(Decimal.Divide(this.TotalRecords, this.PageSize));

            int start = 1 + (int)Decimal.Ceiling(Decimal.Divide(this.CurrentPage, this.BlockCount) - 1) * this.BlockCount;
            int end = start + this.BlockCount - 1;

            if (end > this.TotalPages)
            {
                end = this.TotalPages;
            }

            return builder =>
            {
                int sequence = 0;

                //HtmlGenericControl("div") with Attributes.Add("class", this.CssClass)
                builder.OpenElement(sequence++, "div");
                builder.AddAttribute(sequence++, "class", this.CssClass);

                this.FirstItem(builder, ref sequence);
                this.PreviousItem(builder, ref sequence);
                this.GetAnchor(builder, ref sequence, 1);

                //The previous page block
                string title;
                if (start - this.BlockCount > 0)
                {
                    title = string.Format(Thread.CurrentThread.CurrentCulture, Titles.PageN, start - 1);
                    this.GetEllipsisAnchor(builder, ref sequence, start - 1, title);
                }

                //Paged items
                for (int i = start; i <= end; i++)
                {
                    //Do not create the first and last page
                    //because we will create them explicitly
                    //which will be followed/led
                    //by next/previous page blocks
                    if (i.Equals(1) || i.Equals(this.TotalPages))
                    {
                        continue;
                    }

                    this.GetAnchor(builder, ref sequence, i);
                }

                //The next page block
                if (start + this.BlockCount < this.TotalPages)
                {
                    title = string.Format(Thread.CurrentThread.CurrentCulture, Titles.PageN, end + 1);
                    this.GetEllipsisAnchor(builder, ref sequence, end + 1, title);
                }

                this.GetAnchor(builder, ref sequence, this.TotalPages);
                this.NextItem(builder, ref sequence);
                this.LastItem(builder, ref sequence);

                builder.CloseElement();
            };
        }

        /// <summary>Port of <c>FirstItem()</c> — <c>Titles.FirstPage</c>, inert on page 1.</summary>
        private void FirstItem(RenderTreeBuilder builder, ref int sequence)
        {
            int page = 0;

            if (this.CurrentPage > 1)
            {
                page = 1;
            }

            this.GetIconAnchor(builder, ref sequence, "icon item", "chevron circle left icon", page, Titles.FirstPage);
        }

        /// <summary>Port of <c>PreviousItem()</c> — <c>Titles.PreviousPage</c>, inert on page 1.</summary>
        private void PreviousItem(RenderTreeBuilder builder, ref int sequence)
        {
            int page = 0;
            if (this.CurrentPage > 1)
            {
                page = this.CurrentPage - 1;
            }

            this.GetIconAnchor(builder, ref sequence, "icon item", "arrow circle left icon", page, Titles.PreviousPage);
        }

        /// <summary>Port of <c>NextItem()</c> — <c>Titles.NextPage</c>, inert on the last page.</summary>
        private void NextItem(RenderTreeBuilder builder, ref int sequence)
        {
            int page = 0;

            if (this.CurrentPage < this.TotalPages)
            {
                page = this.CurrentPage + 1;
            }

            this.GetIconAnchor(builder, ref sequence, "icon item", "arrow circle right icon", page, Titles.NextPage);
        }

        /// <summary>Port of <c>LastItem()</c> — <c>Titles.LastPage</c>, inert on the last page.</summary>
        private void LastItem(RenderTreeBuilder builder, ref int sequence)
        {
            int page = 0;

            if (this.CurrentPage < this.TotalPages)
            {
                page = this.TotalPages;
            }

            this.GetIconAnchor(builder, ref sequence, "icon item", "chevron circle right icon", page, Titles.LastPage);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetAnchor(int pageNumber)</c>: a numbered page link, classed
        ///     <c>"active item"</c> when it is the current page and <c>"item"</c> otherwise.
        ///     <para>
        ///         NOTE: the original used those two LITERALS and never read
        ///         <see cref="ActiveCssClass" /> / <see cref="ItemCssClass" />. The literals are kept
        ///         (changing them would restyle every pager) and the two properties are kept as well,
        ///         because they are part of the type's member set and a caller still assigns them.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void GetAnchor(RenderTreeBuilder builder, ref int sequence, int pageNumber)
        {
            builder.OpenElement(sequence++, "a");

            if (pageNumber.Equals(this.CurrentPage))
            {
                builder.AddAttribute(sequence++, "class", "active item");
            }
            else
            {
                builder.AddAttribute(sequence++, "class", "item");
            }

            builder.AddAttribute(sequence++, "href", this.GetPageUrl(pageNumber));

            string title = string.Format(Thread.CurrentThread.CurrentCulture, Titles.PageN, pageNumber);
            builder.AddAttribute(sequence++, "title", title);

            builder.AddContent(sequence++, pageNumber.ToString(Thread.CurrentThread.CurrentCulture));

            builder.CloseElement();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetIconAnchor(cssClass, iconCssClass, pageNumber)</c> plus the
        ///     <c>title</c> its four callers added: an anchor wrapping an <c>&lt;i&gt;</c> icon.
        ///     <para>
        ///         The <c>href</c> is emitted only when there is a page to go to — the original left
        ///         <c>HtmlAnchor.HRef</c> unset for <c>pageNumber == 0</c>, which rendered no
        ///         attribute at all and is what makes the first/previous links inert on page 1.
        ///         The two modal-query-string branches, however, assigned the href
        ///         UNCONDITIONALLY — so under the party item-selector popup these links always carry
        ///         a href, even the inert ones. That asymmetry is preserved.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void GetIconAnchor(RenderTreeBuilder builder, ref int sequence, string cssClass, string iconCssClass, int pageNumber, string title)
        {
            builder.OpenElement(sequence++, "a");
            builder.AddAttribute(sequence++, "class", cssClass);

            string href = this.GetIconAnchorUrl(pageNumber);

            if (href != null)
            {
                builder.AddAttribute(sequence++, "href", href);
            }

            builder.AddAttribute(sequence++, "title", title);

            //HtmlGenericControl("i") with Attributes.Add("class", iconCssClass)
            builder.OpenElement(sequence++, "i");
            builder.AddAttribute(sequence++, "class", iconCssClass);
            builder.CloseElement();

            builder.CloseElement();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of the two "previous/next block" ellipsis anchors.
        ///     <para>
        ///         Both call sites built the anchor through <c>GetIconAnchor("icon item", "", page)</c>
        ///         — which added an <c>&lt;i class=""&gt;</c> child — and then assigned
        ///         <c>anchor.InnerText = "..."</c>. <c>HtmlAnchor.InnerText</c> REPLACES the control's
        ///         children, so that icon was discarded and the rendered anchor contained only the
        ///         ellipsis text. That is reproduced here (no <c>&lt;i&gt;</c> element) rather than
        ///         emitting an empty icon the original never produced.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private void GetEllipsisAnchor(RenderTreeBuilder builder, ref int sequence, int pageNumber, string title)
        {
            builder.OpenElement(sequence++, "a");
            builder.AddAttribute(sequence++, "class", "icon item");

            string href = this.GetIconAnchorUrl(pageNumber);

            if (href != null)
            {
                builder.AddAttribute(sequence++, "href", href);
            }

            builder.AddAttribute(sequence++, "title", title);
            builder.AddContent(sequence++, "...");

            builder.CloseElement();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of the href composition in <c>GetAnchor</c>: either the plain
        ///     <c>{url}?page={n}</c> or, under the party item-selector popup, the same link with the
        ///     modal callback parameters appended so the popup survives paging.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private string GetPageUrl(int pageNumber)
        {
            if (this.IsModalPartyQueryString())
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}?page={1}&" + ModalPartyQueryString, this.CurrentPageUrl, pageNumber);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}?page={1}", this.CurrentPageUrl, pageNumber);
        }

        /// <summary>
        ///     Port of the href composition in <c>GetIconAnchor</c>, which — unlike
        ///     <c>GetAnchor</c> — emitted NO href for page 0 outside the modal branches.
        /// </summary>
        /// <returns>The href, or null when none is to be emitted.</returns>
        private string GetIconAnchorUrl(int pageNumber)
        {
            if (this.IsModalPartyQueryString())
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}?page={1}&" + ModalPartyQueryString, this.CurrentPageUrl, pageNumber);
            }

            if (pageNumber > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}?page={1}", this.CurrentPageUrl, pageNumber);
            }

            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of the two identical query-string comparisons both anchor builders performed: the
        ///     request is the party item-selector popup either when the query string is exactly the
        ///     modal parameter set, or when it is that set prefixed with the current page.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool IsModalPartyQueryString()
        {
            if (this.QueryString == ModalPartyQueryString)
            {
                return true;
            }

            return this.QueryString == string.Format(CultureInfo.InvariantCulture, "page={0}&" + ModalPartyQueryString, this.CurrentPage);
        }
    }
}
