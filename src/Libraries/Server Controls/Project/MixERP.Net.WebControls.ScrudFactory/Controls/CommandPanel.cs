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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MixERP.Net.i18n.Resources;
using System;

namespace MixERP.Net.WebControls.ScrudFactory.Controls
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     The scrud grid's command toolbar — select / show-compact / show-all / add / edit / delete /
    ///     print.
    ///     <para>
    ///         This was never a WebForms <c>Control</c>: it was a plain class that assembled an
    ///         <c>HtmlGenericControl("div")</c> tree of <c>HtmlButton</c>s (the visible affordances,
    ///         which only run client script) plus two real <c>Button</c>s kept in
    ///         <c>class="hidden"</c> whose postback carried the edit/delete command to the server.
    ///         <c>HtmlGenericControl</c>, <c>HtmlButton</c> and <c>Button</c> are removed on net10.0,
    ///         so <see cref="GetCommandPanel" /> now returns a <see cref="RenderFragment" /> — the
    ///         same resolution the already-migrated <c>ScrudPager</c> used. Every button, its order,
    ///         its css classes, its title (the keyboard-shortcut hint), its <c>onclick</c> script and
    ///         — critically — the two load-bearing ids are unchanged.
    ///     </para>
    ///     <para>
    ///         DOM CONTRACT. <c>Scrud.js</c> HARDCODES <c>#EditButtontop</c> and
    ///         <c>#DeleteButtontop</c>, and each visible button clicks its hidden sibling by
    ///         <c>#EditButton{suffix}</c> / <c>#DeleteButton{suffix}</c>. Renaming either id compiles
    ///         green and silently kills editing and deleting.
    ///     </para>
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    internal sealed class CommandPanel
    {
        /// <summary>
        ///     Former <c>event EventHandler DeleteButtonClick</c>. <c>ScrudForm</c> subscribed both
        ///     panels to its <c>DeleteButton_Click</c>; an <see cref="EventCallback" /> is the Blazor
        ///     equivalent and additionally re-renders the owning component afterwards.
        /// </summary>
        public EventCallback DeleteButtonClick { get; set; }

        /// <summary>Former <c>event EventHandler EditButtonClick</c>.</summary>
        public EventCallback EditButtonClick { get; set; }

        public string AddButtonIconCssClass { get; set; }
        public string AllButtonIconCssClass { get; set; }
        public string ButtonCssClass { get; set; }
        public string CompactButtonIconCssClass { get; set; }
        public string CssClass { get; set; }
        public string DeleteButtonIconCssClass { get; set; }
        public string EditButtonIconCssClass { get; set; }
        public string PrintButtonIconCssClass { get; set; }
        public string SelectButtonIconCssClass { get; set; }

        /// <summary>
        ///     Replaces the former <c>public Button DeleteButton { get; private set; }</c> /
        ///     <c>EditButton</c> pair as far as any surviving reader is concerned: the only consumer
        ///     of those two properties was <c>ScrudForm.AddScriptManager()</c>, which registered them
        ///     as async-postback controls — and that whole mechanism is dropped (see the ScrudForm
        ///     summary's deliberate-drop table). The buttons themselves are still rendered, with
        ///     their ids.
        /// </summary>
        public string ControlSuffix { get; private set; }

        /// <summary>
        ///     <c>ScrudForm.DenyAdd</c>/<c>DenyEdit</c>/<c>DenyDelete</c>. The original rendered all
        ///     three affordances unconditionally and enforced the policy only inside the event
        ///     handlers; the affordance is now suppressed as well (checklist tasks 112-114), while the
        ///     HIDDEN buttons still render so the load-bearing ids never disappear from the DOM.
        /// </summary>
        public bool DenyAdd { get; set; }

        public bool DenyEdit { get; set; }
        public bool DenyDelete { get; set; }

        /// <summary>
        ///     Supplies the request's query string in place of
        ///     <c>HttpContext.Current.CurrentHandler</c>. <c>ScrudForm</c> assigns its own injected
        ///     <c>NavigationManager</c>; this type is not a component, so it cannot inject one.
        /// </summary>
        public NavigationManager Navigation { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetCommandPanel(string controlSuffix)</c>: the toolbar <c>div</c> and its
        ///     nine buttons, in the original order — select, show-compact, show-all, add, hidden
        ///     edit, visible edit, hidden delete, visible delete, print.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public RenderFragment GetCommandPanel(string controlSuffix)
        {
            this.ControlSuffix = controlSuffix;

            return builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "role", "toolbar");
                builder.AddAttribute(2, "class", this.CssClass);

                builder.AddContent(3, this.AddSelectButton());
                builder.AddContent(4, this.AddShowCompactButton());
                builder.AddContent(5, this.AddShowAllButton());
                builder.AddContent(6, this.AddAddButton());
                builder.AddContent(7, this.AddEditButtonHidden(controlSuffix));
                builder.AddContent(8, this.AddEditButtonVisible(controlSuffix));
                builder.AddContent(9, this.AddDeleteButtonHidden(controlSuffix));
                builder.AddContent(10, this.AddDeleteButtonVisible(controlSuffix));
                builder.AddContent(11, this.AddPrintButton());

                builder.CloseElement();
            };
        }

        /// <summary>Port of <c>AddAddButton</c> — <c>Titles.AddNew</c>, <c>scrudAddNew()</c>.</summary>
        private RenderFragment AddAddButton()
        {
            if (this.DenyAdd)
            {
                return null;
            }

            return this.GetInputButton("CTRL + SHIFT + A", "return(scrudAddNew());", Titles.AddNew, this.ButtonCssClass, this.AddButtonIconCssClass);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>AddDeleteButtonHidden</c>: the real command button, kept
        ///     <c>class="hidden"</c>, that the visible button and <c>Scrud.js</c> both click by id.
        ///     Its former <c>Click</c> postback is now the <see cref="DeleteButtonClick" /> callback.
        ///     <para>
        ///         DIVERGENCE (recorded): the original put the confirmation
        ///         (<c>return(scrudConfirmAction());</c>) on the HIDDEN button's <c>OnClientClick</c>,
        ///         where returning false cancelled the postback. A Blazor <c>@onclick</c> listener is
        ///         not cancellable by an inline handler, so the confirmation moved onto the VISIBLE
        ///         button, which now only clicks its hidden sibling once the user confirms. The
        ///         user-visible behaviour (confirm, then delete) is identical, and
        ///         <c>Scrud.js</c>'s own <c>#DeleteButtontop</c> click still deletes — as it did
        ///         before, since it ran its own confirm first.
        ///     </para>
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private RenderFragment AddDeleteButtonHidden(string controlSuffix)
        {
            return builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "id", "DeleteButton" + controlSuffix);
                builder.AddAttribute(3, "class", "hidden");
                builder.AddAttribute(4, "title", "CTRL + SHIFT + D");
                builder.AddAttribute(5, "onclick", this.DeleteButtonClick);
                builder.AddContent(6, Titles.DeleteSelected);
                builder.CloseElement();
            };
        }

        /// <summary>Port of <c>AddDeleteButtonVisible</c> — clicks the hidden button by id.</summary>
        private RenderFragment AddDeleteButtonVisible(string controlSuffix)
        {
            if (this.DenyDelete)
            {
                return null;
            }

            string onclick = "if(scrudConfirmAction()){$('#DeleteButton" + controlSuffix + "').click();}return false;";

            return this.GetInputButton("CTRL + SHIFT + D", onclick, Titles.DeleteSelected, this.ButtonCssClass, this.DeleteButtonIconCssClass);
        }

        /// <summary>
        ///     Port of <c>AddEditButtonHidden</c>, including its <c>role="edit"</c> attribute
        ///     (<c>Scrud.js</c> selects on it).
        /// </summary>
        private RenderFragment AddEditButtonHidden(string controlSuffix)
        {
            return builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "role", "edit");
                builder.AddAttribute(3, "id", "EditButton" + controlSuffix);
                builder.AddAttribute(4, "class", "hidden");
                builder.AddAttribute(5, "title", "CTRL + SHIFT + E");
                builder.AddAttribute(6, "onclick", this.EditButtonClick);
                builder.AddContent(7, Titles.EditSelected);
                builder.CloseElement();
            };
        }

        /// <summary>Port of <c>AddEditButtonVisible</c> — clicks the hidden button by id.</summary>
        private RenderFragment AddEditButtonVisible(string controlSuffix)
        {
            if (this.DenyEdit)
            {
                return null;
            }

            string onclick = "if(scrudConfirmAction()){$('#EditButton" + controlSuffix + "').click();}return false;";

            return this.GetInputButton("CTRL + SHIFT + E", onclick, Titles.EditSelected, this.ButtonCssClass, this.EditButtonIconCssClass);
        }

        /// <summary>Port of <c>AddPrintButton</c> — <c>Titles.Print</c>, <c>scrudPrintGridView()</c>.</summary>
        private RenderFragment AddPrintButton()
        {
            return this.GetInputButton("CTRL + SHIFT + P", "scrudPrintGridView();", Titles.Print, this.ButtonCssClass, this.PrintButtonIconCssClass);
        }

        /// <summary>Port of <c>AddSelectButton</c> — modal-only, <c>Titles.Select</c>.</summary>
        private RenderFragment AddSelectButton()
        {
            if (this.IsModal())
            {
                return this.GetInputButton("RETURN", "scrudSelectAndClose();", Titles.Select, this.ButtonCssClass, this.SelectButtonIconCssClass);
            }

            return null;
        }

        /// <summary>Port of <c>AddShowAllButton</c> — <c>Titles.ShowAll</c>, <c>scrudShowAll()</c>.</summary>
        private RenderFragment AddShowAllButton()
        {
            return this.GetInputButton("CTRL + SHIFT + S", "scrudShowAll();", Titles.ShowAll, this.ButtonCssClass, this.AllButtonIconCssClass);
        }

        /// <summary>Port of <c>AddShowCompactButton</c> — <c>Titles.ShowCompact</c>.</summary>
        private RenderFragment AddShowCompactButton()
        {
            return this.GetInputButton("CTRL + SHIFT + C", "scrudShowCompact();", Titles.ShowCompact, this.ButtonCssClass, this.CompactButtonIconCssClass);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>GetInputButton(title, onclick, value, cssClass, iconCssClass)</c>: an
        ///     <c>HtmlButton</c> whose <c>InnerHtml</c> was an optional <c>&lt;i&gt;</c> icon followed
        ///     by the label. The icon markup is emitted as its own element rather than through a
        ///     markup string, which renders the identical DOM.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private RenderFragment GetInputButton(string title, string onclick, string value, string cssClass, string iconCssClass)
        {
            return builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", cssClass);
                builder.AddAttribute(2, "type", "button");
                builder.AddAttribute(3, "title", title);
                builder.AddAttribute(4, "onclick", onclick);

                if (!string.IsNullOrWhiteSpace(iconCssClass))
                {
                    builder.OpenElement(5, "i");
                    builder.AddAttribute(6, "class", iconCssClass);
                    builder.CloseElement();

                    builder.AddContent(7, " " + value);
                }
                else
                {
                    builder.AddContent(8, value);
                }

                builder.CloseElement();
            };
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Port of <c>IsModal()</c>. <c>HttpContext.Current.CurrentHandler as Page</c> +
        ///     <c>page.Request.QueryString["modal"]</c> becomes a query-string read off the
        ///     <see cref="Navigation" /> the owning component injects. The truthiness semantics are
        ///     unchanged: ONLY the literal "1" is modal.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool IsModal()
        {
            if (this.Navigation == null)
            {
                return false;
            }

            string query = new Uri(this.Navigation.Uri).Query;

            if (string.IsNullOrEmpty(query))
            {
                return false;
            }

            StringValues modal;

            if (QueryHelpers.ParseQuery(query).TryGetValue("modal", out modal))
            {
                return modal.ToString().Equals("1");
            }

            return false;
        }
    }
}
