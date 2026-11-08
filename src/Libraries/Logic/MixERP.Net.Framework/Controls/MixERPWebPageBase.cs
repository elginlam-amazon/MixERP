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
using Microsoft.AspNetCore.Components;
using Serilog;

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     Base class for every MixERP page. Formerly derived from <c>System.Web.UI.Page</c>; now a Blazor
    ///     <see cref="ComponentBase" />.
    ///     WebForms ViewState had been switched OFF for these pages by a no-op
    ///     <c>PageStatePersister</c> whose <c>Load</c>/<c>Save</c> did nothing, so nothing was persisted.
    ///     Blazor keeps component state on the server for the lifetime of the circuit and has no ViewState
    ///     at all, so the no-op persister has no counterpart and its removal changes no behavior.
    /// </summary>
    // Derives from ASP.NET Core ComponentBase, which is not CLS-compliant.
    [System.CLSCompliant(false)]
    public class MixERPWebPageBase : ComponentBase
    {
        private readonly MixERPContentRegion masterPageRegion = new MixERPContentRegion("Master");
        private readonly MixERPContentRegion pageRegion = new MixERPContentRegion("Page");

        protected MixERPWebPageBase()
        {
            this.masterPageRegion.Changed += this.OnContentChanged;
            this.pageRegion.Changed += this.OnContentChanged;
        }

        /// <summary>
        ///     Root of the layout's named content regions — the replacement for the master page's
        ///     <c>ContentPlaceHolder</c> tree. The layout renders a region with
        ///     <c>@MasterPageRegion.FindRegion("id")?.Content</c>.
        /// </summary>
        public MixERPContentRegion MasterPageRegion
        {
            get { return this.masterPageRegion; }
        }

        /// <summary>
        ///     Root of this page's named content regions — the replacement for the page's naming
        ///     containers. Page markup renders a region with <c>@PageRegion.FindRegion("id")?.Content</c>.
        /// </summary>
        public MixERPContentRegion PageRegion
        {
            get { return this.pageRegion; }
        }

        /// <summary>
        ///     Adds content to the layout's named placeholder.
        ///     Replacement of <c>AddToMasterPage(string placeHolderId, Control control)</c>, which looked up
        ///     a <c>ContentPlaceHolder</c> on <c>Page.Master</c> and did
        ///     <c>placeHolder.Controls.Add(control)</c>. When the placeholder has not been declared yet it is
        ///     created, so content added before the layout renders is not lost (the WebForms version
        ///     silently dropped the content when the placeholder was missing — that silent drop is
        ///     preserved only for a null fragment).
        /// </summary>
        public void AddToMasterPage(string placeHolderId, RenderFragment content)
        {
            this.AddToRegion(this.masterPageRegion, placeHolderId, content);
        }

        /// <summary>
        ///     Adds content to one of this page's named containers.
        ///     Replacement of <c>AddToPage(string namingContainerId, Control control)</c>. In WebForms the
        ///     master page was a child of the page, so this lookup could also resolve an id declared by the
        ///     master; that single-tree search is preserved by falling back to
        ///     <see cref="MasterPageRegion" /> before creating a new region.
        /// </summary>
        public void AddToPage(string namingContainerId, RenderFragment content)
        {
            this.AddToRegion(this.pageRegion, namingContainerId, content, this.masterPageRegion);
        }

        /// <summary>
        ///     Depth-first lookup of a named region, preserving the match-by-id-then-recurse order and the
        ///     null return of the original <c>FindControlIterative(Control root, string id)</c>.
        /// </summary>
        public static MixERPContentRegion FindControlIterative(MixERPContentRegion root, string id)
        {
            if (root == null)
            {
                return null;
            }

            return root.FindRegion(id);
        }

        private void AddToRegion(MixERPContentRegion root, string regionId, RenderFragment content,
            MixERPContentRegion fallbackRoot = null)
        {
            if (content == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(regionId))
            {
                Log.Warning("Content was not added because no region id was supplied.");
                return;
            }

            MixERPContentRegion region = FindControlIterative(root, regionId)
                                         ?? FindControlIterative(fallbackRoot, regionId);

            if (region == null)
            {
                // The region has not been declared (yet). Create it so content added before the layout
                // renders is not lost, but say so: a mistyped id would otherwise put the content in a
                // region nothing renders, and it would disappear with no error at all.
                Log.Warning(
                    "Region {RegionId} was not found under {RootId}; it has been created on demand. Ensure the layout or page renders it, otherwise this content is not displayed.",
                    regionId, root.Id);
                region = root.GetOrAddRegion(regionId);
            }

            region.Add(content);
        }

        private void OnContentChanged()
        {
            this.InvokeAsync(this.StateHasChanged);
        }
    }
}
