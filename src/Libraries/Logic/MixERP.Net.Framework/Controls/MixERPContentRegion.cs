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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     A named region of dynamic content — the Blazor replacement for a WebForms
    ///     <c>ContentPlaceHolder</c> / naming container. A region owns an ordered list of
    ///     <see cref="RenderFragment" /> children and may contain nested regions, so the original
    ///     recursive <c>FindControlIterative</c> lookup by id is preserved.
    /// </summary>
    public sealed class MixERPContentRegion
    {
        private readonly List<RenderFragment> children = new List<RenderFragment>();
        private readonly List<MixERPContentRegion> regions = new List<MixERPContentRegion>();

        public MixERPContentRegion(string id)
        {
            this.Id = id;
        }

        /// <summary>
        ///     The region identifier — the WebForms <c>Control.ID</c> equivalent.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        ///     Content added to this region, in insertion order.
        /// </summary>
        public IReadOnlyList<RenderFragment> Children
        {
            get { return this.children; }
        }

        /// <summary>
        ///     Regions nested directly inside this one.
        /// </summary>
        public IReadOnlyList<MixERPContentRegion> Regions
        {
            get { return this.regions; }
        }

        /// <summary>
        ///     Raised whenever content is added, so the hosting component can re-render.
        /// </summary>
        public event Action Changed;

        /// <summary>
        ///     Adds content to this region. Equivalent of <c>Controls.Add(control)</c>.
        /// </summary>
        public void Add(RenderFragment content)
        {
            if (content == null)
            {
                return;
            }

            this.children.Add(content);
            this.OnChanged();
        }

        /// <summary>
        ///     Gets the nested region with the given id, creating it when absent.
        ///     Equivalent of declaring a <c>ContentPlaceHolder</c> / naming container in markup.
        /// </summary>
        public MixERPContentRegion GetOrAddRegion(string id)
        {
            MixERPContentRegion existing = this.FindRegion(id);

            if (existing != null)
            {
                return existing;
            }

            var region = new MixERPContentRegion(id);
            region.Changed += this.OnChanged;
            this.regions.Add(region);
            return region;
        }

        /// <summary>
        ///     Depth-first lookup of a region by id, matching this region first and then recursing into
        ///     nested regions in declaration order — the same order as the original
        ///     <c>FindControlIterative</c>. Returns null when not found.
        /// </summary>
        public MixERPContentRegion FindRegion(string id)
        {
            if (string.Equals(this.Id, id, StringComparison.Ordinal))
            {
                return this;
            }

            foreach (MixERPContentRegion region in this.regions)
            {
                MixERPContentRegion match = region.FindRegion(id);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        ///     All content of this region as a single fragment, for rendering from markup:
        ///     <c>@Region.Content</c>.
        /// </summary>
        public RenderFragment Content
        {
            get
            {
                return builder =>
                {
                    foreach (RenderFragment child in this.children)
                    {
                        child(builder);
                    }
                };
            }
        }

        /// <summary>
        ///     Renders this region's content into an existing render tree.
        /// </summary>
        public void Render(RenderTreeBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            this.Content(builder);
        }

        private void OnChanged()
        {
            Action handler = this.Changed;

            if (handler != null)
            {
                handler();
            }
        }
    }
}
