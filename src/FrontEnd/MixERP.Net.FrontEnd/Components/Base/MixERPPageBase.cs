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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.Framework.Controls;
using MixERP.Net.FrontEnd.Models;
using MixERP.Net.FrontEnd.Services.Authentication;
using Serilog;

namespace MixERP.Net.FrontEnd.Base
{
    /// <summary>
    ///     Blazor replacement of the WebForms <c>MixERPWebpage</c> (checklist tasks 52, 53, 54, 69).
    ///     Every routable MixERP page inherits it, which is what guarantees that no page can lose a
    ///     login or authorization check by omission.
    ///     <para>
    ///         <c>[Authorize]</c> is NOT declared on this class: <c>AuthorizeRouteView</c> reads
    ///         <c>[Authorize]</c> through the inheritance chain but does NOT honour
    ///         <c>[AllowAnonymous]</c>, so an attribute here would make the anonymous pages (SignIn,
    ///         Install) unreachable. Pages that require a signed-in user inherit
    ///         <see cref="MixERPAuthorizedPageBase" /> (which carries the attribute); the anonymous
    ///         pages inherit this class and set <see cref="SkipLoginCheck" />, which is exactly what the
    ///         legacy flag expressed.
    ///     </para>
    ///     <para>
    ///         WEBFORMS LIFECYCLE: <c>OnInit</c> on a non-postback becomes
    ///         <see cref="OnInitializedAsync" />; <c>InitializeCulture</c> is folded into it (it ran
    ///         immediately before <c>OnInit</c>); <c>IsPostBack</c> has no counterpart because Blazor has
    ///         no postback — the gate therefore runs once per page activation, which is precisely when
    ///         the legacy non-postback gate ran.
    ///     </para>
    /// </summary>
    public class MixERPPageBase : MixERPWebPageBase
    {
        private Menu[] menus;

        /// <summary>
        ///     The pages, having no actual entry on database menu table, can have an OverridePath
        ///     which can be set to an existing page.
        /// </summary>
        public virtual string OverridePath { get; set; }

        /// <summary>
        ///     The pages which do not have actual entry on database menu table, but rather serve as
        ///     placeholders are landing pages.
        /// </summary>
        public virtual bool IsLandingPage { get; set; }

        /// <summary>
        ///     Set this to true for pages where you do not need users to be logged in.
        /// </summary>
        public bool SkipLoginCheck { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected CurrentLoginProvider CurrentLogin { get; set; }

        [Inject]
        protected MixERPAuthorization Authorization { get; set; }

        [Inject]
        protected PageAuthorizationState AuthorizationState { get; set; }

        /// <summary>
        ///     The menu set of the current user, loaded on demand. In the legacy class the equivalent
        ///     private field was never populated; it is populated here so the policy check of
        ///     <see cref="MixERPAuthorization.VerifyAccess" /> and the JSTree markup of
        ///     <see cref="GetMenu" /> have real data.
        /// </summary>
        public Menu[] Menus
        {
            get { return this.menus ?? new Menu[0]; }
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync().ConfigureAwait(true);

            //A denial is per page activation, never inherited from the previously rendered page.
            this.AuthorizationState.Reset();

            await this.ActivateAsync().ConfigureAwait(true);
        }

        /// <summary>
        ///     The whole legacy activation sequence in its original order:
        ///     InitializeCulture -> OnInit login gate -> CheckForceLogOffFlags, followed by the policy
        ///     check of <c>VerifyAccess</c>.
        /// </summary>
        protected async Task ActivateAsync()
        {
            await this.SetCultureAsync().ConfigureAwait(true);

            if (!await this.CheckLoginAsync().ConfigureAwait(true))
            {
                return;
            }

            //Legacy: CheckForceLogOffFlags() ran OUTSIDE the non-postback branch, i.e. on every
            //page activation.
            if (await this.Authorization.MustForceLogOffAsync().ConfigureAwait(true))
            {
                await this.RequestLoginPageAsync().ConfigureAwait(true);
                return;
            }

            //VerifyAccess is deliberately NOT invoked here. In the legacy application
            //MixERPWebpage.VerifyAccess() was never called and its `menus` field was never
            //populated (a call would have thrown NullReferenceException), so no page was ever
            //gated by it. Invoking it on every activation is a behaviour CHANGE, not parity:
            //the gate compares a menu Url against OverridePath, and OverridePath is null on
            //every page that does not set one (/General/ItemSelector, /Reports/ReportViewer,
            ///Reports/Assets/Header and most /Modules/** entry controls), so those pages would
            //be denied for any user with a populated menu set. The ported method is kept below
            //as an opt-in check a page may call explicitly, preserving the original logic
            //without changing which pages are reachable.
        }

        /// <summary>
        ///     Port of the <c>OnInit</c> login gate (checklist task 52). Returns false when the login
        ///     page was requested, i.e. when the rest of the activation must not run.
        /// </summary>
        private async Task<bool> CheckLoginAsync()
        {
            if (await this.CurrentLogin.IsAuthenticatedAsync().ConfigureAwait(true))
            {
                MetaLogin login = await this.CurrentLogin.GetCurrentAsync().ConfigureAwait(true);

                if (login.View.LoginId.ToLong().Equals(0))
                {
                    //The login cache entry is missing (a fresh circuit, or the cache was evicted):
                    //rebuild it from the subject claim, then re-check.
                    await this.CurrentLogin.SetCurrentLoginAsync().ConfigureAwait(true);

                    login = await this.CurrentLogin.GetCurrentAsync().ConfigureAwait(true);

                    if (login.View.LoginId.ToLong().Equals(0))
                    {
                        await this.RequestLoginPageAsync().ConfigureAwait(true);
                        return false;
                    }
                }

                return true;
            }

            if (!this.SkipLoginCheck)
            {
                await this.RequestLoginPageAsync().ConfigureAwait(true);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.RequestLoginPage()</c> (checklist task 51).
        ///     <para>
        ///         BLAZOR SERVER SEAM: signing the cookie out and deleting cookies needs an open HTTP
        ///         response, which a circuit does not have, so the work is performed by
        ///         <see cref="AuthEndpoints.RequestLoginRoute" /> and this method force-loads it. The
        ///         "only when not already on the login url" guard is kept here so a visitor sitting on
        ///         the sign-in page is not bounced in a loop.
        ///     </para>
        /// </summary>
        public Task RequestLoginPageAsync()
        {
            string currentPage = PageUtility.GetCurrentPageUrl(this.NavigationManager);

            Log.Verbose("Current page is {CurrentPage}.", currentPage);

            if (MixERPAuthentication.IsLoginUrl(currentPage))
            {
                return Task.CompletedTask;
            }

            this.NavigationManager.NavigateTo(AuthEndpoints.GetRequestLoginUrl(currentPage), true);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.VerifyAccess()</c> (checklist task 54). The denial is RENDERED
        ///     IN PLACE by <c>AuthorizationOutlet</c> (Server.Transfer semantics: the url does not
        ///     change), never redirected to.
        /// </summary>
        protected async Task<bool> VerifyAccessAsync()
        {
            if (this.IsLandingPage)
            {
                return true;
            }

            await this.LoadMenusAsync().ConfigureAwait(true);

            return this.Authorization.VerifyAccess(this.IsLandingPage, this.OverridePath, this.menus);
        }

        /// <summary>
        ///     Loads (once) the flat menu set of the current user through
        ///     <see cref="Navigation" /> — the same <c>policy.get_menu</c> data the navigation tree is
        ///     built from.
        /// </summary>
        protected async Task LoadMenusAsync()
        {
            if (this.menus != null)
            {
                return;
            }

            try
            {
                MetaLogin login = await this.CurrentLogin.GetCurrentAsync().ConfigureAwait(true);

                if (login == null || login.View == null || login.View.UserId.ToInt().Equals(0))
                {
                    this.menus = new Menu[0];
                    return;
                }

                Navigation navigation = new Navigation(login.Catalog, login.View.UserId.ToInt(),
                    login.View.OfficeId.ToInt(), login.View.Culture);

                List<NavigationMenu> tree = navigation.GetMenus();
                List<Menu> flat = new List<Menu>();

                Flatten(tree, flat);

                this.menus = flat.ToArray();
            }
            catch (Exception ex)
            {
                Log.Warning("Could not load the menu set of the current user. {Exception}.", ex);
                this.menus = new Menu[0];
            }
        }

        private static void Flatten(IEnumerable<NavigationMenu> source, List<Menu> target)
        {
            if (source == null)
            {
                return;
            }

            foreach (NavigationMenu item in source)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Menu != null)
                {
                    target.Add(item.Menu);
                }

                Flatten(item.Children, target);
            }
        }

        /// <summary>
        ///     Port of the recursive JSTree markup builder (checklist task 74). The attribute output is
        ///     byte-identical to the original because <c>menu.js</c> / <c>js-tree.js</c> read
        ///     <c>data-jstree</c>, <c>data-selected</c>, <c>data-menucode</c> and the
        ///     <c>class='expanded'</c> marker.
        /// </summary>
        public string GetMenu(int menuId)
        {
            string menu = string.Empty;

            if (menuId.Equals(0))
            {
                return menu;
            }

            Menu[] items = this.Menus.Where(m => m.ParentMenuId.Equals(menuId)).ToArray();

            if (items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (i == 0)
                    {
                        menu += "<ul>";
                    }

                    //If this menu has any children, fetch them.
                    string childMenu = this.GetMenu(items[i].MenuId);
                    string id = items[i].MenuId.ToString(CultureInfo.InvariantCulture);
                    string url = string.IsNullOrWhiteSpace(items[i].Url)
                        ? "javascript:void(0);"
                        : PageUtility.ResolveUrl(items[i].Url);

                    string cssClass = string.Empty;
                    string dataJSTree = string.Empty;
                    string dataSelected = string.Empty;

                    if (string.IsNullOrWhiteSpace(childMenu))
                    {
                        dataJSTree = "data-jstree='{\"type\":\"file\"}'";
                    }

                    if (url.Equals(this.OverridePath))
                    {
                        dataSelected = "data-selected='true'";
                        cssClass = "class='expanded'";
                        dataJSTree = "data-jstree='{\"type\":\"active\"}'";
                    }

                    string anchor = "<a href='" + url + "'>" + items[i].MenuText + "</a>";

                    menu += string.Format(CultureInfo.CurrentCulture,
                        "<li id='node{0}' {1} {2} data-menucode='{3}' {4}>",
                        id, cssClass, dataSelected, items[i].MenuCode, dataJSTree);

                    menu += anchor;
                    menu += childMenu;
                    menu += "</li>";

                    if (i == items.Length - 1)
                    {
                        menu += "</ul>";
                    }
                }
            }

            return menu;
        }

        /// <summary>
        ///     Port of <c>MixERPWebpage.RedirectToRootPage()</c>. No longer static: the navigation now
        ///     goes through the injected <see cref="NavigationManager" /> instead of
        ///     <c>HttpContext.Current.Response</c>.
        /// </summary>
        public void RedirectToRootPage()
        {
            Log.Debug("Redirecting to '/' root page.");
            this.NavigationManager.NavigateTo("/");
        }

        /// <summary>
        ///     Port of <c>InitializeCulture</c> / <c>SetCulture</c>: the per-user
        ///     <c>logins.View.Culture</c> override. Request localization (Program.cs task 42) already
        ///     applies it per request through <c>MixERPUserCultureProvider</c>; this keeps the circuit's
        ///     thread culture aligned for pages rendered over the circuit, where no request-localization
        ///     middleware runs.
        /// </summary>
        protected async Task SetCultureAsync()
        {
            MetaLogin login = await this.CurrentLogin.GetCurrentAsync().ConfigureAwait(true);

            if (login == null || login.View == null)
            {
                return;
            }

            string cultureName = login.View.Culture;

            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return;
            }

            try
            {
                CultureInfo culture = new CultureInfo(cultureName);

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException ex)
            {
                Log.Warning("The culture {Culture} of the current user is not available. {Exception}.",
                    cultureName, ex);
            }
        }
    }
}
