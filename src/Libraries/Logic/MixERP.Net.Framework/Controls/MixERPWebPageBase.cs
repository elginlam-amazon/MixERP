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

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     Base class for every MixERP page. Previously derived from
    ///     <c>System.Web.UI.Page</c>; it is now a Blazor component base class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The WebForms version injected controls into the master page's
    ///         <c>ContentPlaceHolder</c>s and into arbitrary naming containers by walking the
    ///         control tree. Blazor has no server-side control tree, so the equivalent
    ///         mechanism is a set of NAMED RENDER-FRAGMENT SLOTS: a page (or any code running
    ///         on it) contributes content to a slot by name through
    ///         <see cref="AddToMasterPage" /> / <see cref="AddToPage" />, and the layout or
    ///         markup renders that slot through
    ///         <see cref="GetMasterPageSection" /> / <see cref="GetPageSection" />, e.g.
    ///         <c>@GetMasterPageSection("TitlePlaceHolder")</c>.
    ///     </para>
    ///     <para>
    ///         The <c>MixERPPageStatePersister</c> override is not carried over: ViewState does
    ///         not exist in Blazor (component fields survive re-renders on their own), and the
    ///         original persister's <c>Load</c>/<c>Save</c> were both empty, so no behaviour is
    ///         lost.
    ///     </para>
    /// </remarks>
    public class MixERPWebPageBase : ComponentBase
    {
        private readonly Dictionary<string, List<RenderFragment>> masterPageSections =
            new Dictionary<string, List<RenderFragment>>(StringComparer.Ordinal);

        private readonly Dictionary<string, List<RenderFragment>> pageSections =
            new Dictionary<string, List<RenderFragment>>(StringComparer.Ordinal);

        /// <summary>
        ///     Adds content to the named slot on the layout (the Blazor replacement for a
        ///     master page's <c>ContentPlaceHolder</c>). The layout renders the slot with
        ///     <see cref="GetMasterPageSection" />.
        /// </summary>
        /// <param name="placeHolderId">Name of the layout slot.</param>
        /// <param name="content">The content to add. Ignored when null.</param>
        public void AddToMasterPage(string placeHolderId, RenderFragment content)
        {
            Add(this.masterPageSections, placeHolderId, content);
        }

        /// <summary>
        ///     Adds content to the named slot on this page (the Blazor replacement for adding
        ///     a control to a naming container). The page's markup renders the slot with
        ///     <see cref="GetPageSection" />.
        /// </summary>
        /// <param name="namingContainerId">Name of the page slot.</param>
        /// <param name="content">The content to add. Ignored when null.</param>
        public void AddToPage(string namingContainerId, RenderFragment content)
        {
            Add(this.pageSections, namingContainerId, content);
        }

        /// <summary>
        ///     Adds a child component of the given type, with the supplied parameters, to the
        ///     named layout slot. This is the direct counterpart of the WebForms
        ///     <c>placeHolder.Controls.Add(control)</c> call.
        /// </summary>
        public void AddToMasterPage(string placeHolderId, Type componentType,
            IDictionary<string, object> parameters = null)
        {
            this.AddToMasterPage(placeHolderId, BuildComponentFragment(componentType, parameters));
        }

        /// <summary>
        ///     Adds a child component of the given type, with the supplied parameters, to the
        ///     named page slot.
        /// </summary>
        public void AddToPage(string namingContainerId, Type componentType,
            IDictionary<string, object> parameters = null)
        {
            this.AddToPage(namingContainerId, BuildComponentFragment(componentType, parameters));
        }

        /// <summary>
        ///     Renders everything contributed to the named layout slot, or null when nothing
        ///     was contributed (so <c>@GetMasterPageSection("x")</c> emits nothing, exactly as
        ///     an empty <c>ContentPlaceHolder</c> did).
        /// </summary>
        public RenderFragment GetMasterPageSection(string placeHolderId)
        {
            return Combine(this.masterPageSections, placeHolderId);
        }

        /// <summary>
        ///     Renders everything contributed to the named page slot, or null when nothing was
        ///     contributed.
        /// </summary>
        public RenderFragment GetPageSection(string namingContainerId)
        {
            return Combine(this.pageSections, namingContainerId);
        }

        /// <summary>
        ///     True when content has been contributed to the named layout slot.
        /// </summary>
        public bool HasMasterPageSection(string placeHolderId)
        {
            return !string.IsNullOrWhiteSpace(placeHolderId) &&
                   this.masterPageSections.ContainsKey(placeHolderId);
        }

        /// <summary>
        ///     True when content has been contributed to the named page slot.
        /// </summary>
        public bool HasPageSection(string namingContainerId)
        {
            return !string.IsNullOrWhiteSpace(namingContainerId) &&
                   this.pageSections.ContainsKey(namingContainerId);
        }

        private static void Add(IDictionary<string, List<RenderFragment>> sections, string id,
            RenderFragment content)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (content == null)
            {
                return;
            }

            if (!sections.TryGetValue(id, out List<RenderFragment> fragments))
            {
                fragments = new List<RenderFragment>();
                sections[id] = fragments;
            }

            fragments.Add(content);
        }

        private static RenderFragment Combine(IDictionary<string, List<RenderFragment>> sections,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (!sections.TryGetValue(id, out List<RenderFragment> fragments) || fragments.Count == 0)
            {
                return null;
            }

            return builder =>
            {
                foreach (RenderFragment fragment in fragments)
                {
                    fragment(builder);
                }
            };
        }

        private static RenderFragment BuildComponentFragment(Type componentType,
            IDictionary<string, object> parameters)
        {
            if (componentType == null)
            {
                return null;
            }

            return builder =>
            {
                builder.OpenComponent(0, componentType);

                if (parameters != null)
                {
                    // Parameters added in a loop share one sequence number, as recommended for
                    // generated render trees (ASP0006).
                    foreach (KeyValuePair<string, object> parameter in parameters)
                    {
                        builder.AddComponentParameter(1, parameter.Key, parameter.Value);
                    }
                }

                builder.CloseComponent();
            };
        }
    }
}
