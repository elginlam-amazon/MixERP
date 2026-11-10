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
    ///     Base class for every MixERP user control. Previously derived from
    ///     <c>System.Web.UI.UserControl</c>; it is now a Blazor component, so the controls
    ///     that derive from it become <c>.razor</c> components.
    /// </summary>
    /// <remarks>
    ///     The WebForms-only <c>ValidateRequestMode ValidDateRequest</c> property has no
    ///     modern counterpart — request validation is middleware concern in ASP.NET Core —
    ///     and is therefore not carried over.
    /// </remarks>
    public abstract class MixERPUserControlBase : ComponentBase, IControl
    {
        public abstract AccessLevel AccessLevel { get; }

        [Parameter]
        public string MasterPageId { get; set; }

        [Parameter]
        public bool RemoveTheme { get; set; }

        [Parameter]
        public string OverridePath { get; set; }

        /// <summary>
        ///     True once <see cref="OnControlLoad" /> has run for this component instance.
        /// </summary>
        protected bool ControlLoaded { get; private set; }

        /// <summary>
        ///     The control's own initialization logic, equivalent to the WebForms
        ///     <c>Load</c> event handler that each <c>.ascx</c> wired up.
        /// </summary>
        public abstract void OnControlLoad(object sender, EventArgs e);

        /// <summary>
        ///     Runs <see cref="OnControlLoad" /> exactly once per component instance. Call
        ///     this instead of invoking <see cref="OnControlLoad" /> directly so that the
        ///     control's load logic cannot run twice for a single render tree.
        /// </summary>
        public void LoadControl()
        {
            if (this.ControlLoaded)
            {
                return;
            }

            this.ControlLoaded = true;
            this.OnControlLoad(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Blazor's equivalent of the WebForms control <c>Load</c> phase.
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.LoadControl();
        }
    }
}
