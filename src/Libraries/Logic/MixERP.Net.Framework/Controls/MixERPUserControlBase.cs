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
using System.Net;
using Microsoft.AspNetCore.Components;

namespace MixERP.Net.Framework.Controls
{
    /// <summary>
    ///     Base class for every MixERP user control. Formerly derived from
    ///     <c>System.Web.UI.UserControl</c>; now a Blazor <see cref="ComponentBase" />.
    ///     <see cref="OnControlLoad" /> replaces the WebForms <c>Load</c> handler and is invoked once from
    ///     <see cref="OnInitialized" /> (the Blazor equivalent of <c>Page_Load</c> on a non-postback).
    /// </summary>
    public abstract class MixERPUserControlBase : ComponentBase, IControl
    {
        private MixERPValidateRequestMode validDateRequest = MixERPValidateRequestMode.Inherit;
        private bool loaded;

        public abstract AccessLevel AccessLevel { get; }

        /// <summary>
        ///     Request-validation mode for this control. Replaces the WebForms
        ///     <c>System.Web.UI.ValidateRequestMode</c> property; the default is still
        ///     <see cref="MixERPValidateRequestMode.Inherit" />.
        /// </summary>
        [Parameter]
        public MixERPValidateRequestMode ValidDateRequest
        {
            get { return this.validDateRequest; }
            set { this.validDateRequest = value; }
        }

        [Parameter]
        public string MasterPageId { get; set; }

        [Parameter]
        public bool RemoveTheme { get; set; }

        [Parameter]
        public string OverridePath { get; set; }

        /// <summary>
        ///     Renders user-supplied text that the control wants to emit as markup, honouring
        ///     <see cref="ValidDateRequest" />. ASP.NET Core performs no implicit request validation, so the
        ///     WebForms protection only survives if it is applied where the value is consumed: unless the
        ///     control explicitly opted out with <see cref="MixERPValidateRequestMode.Disabled" />, the
        ///     value is HTML-encoded instead of being trusted as raw markup.
        /// </summary>
        protected MarkupString RenderUserMarkup(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new MarkupString(string.Empty);
            }

            if (this.validDateRequest == MixERPValidateRequestMode.Disabled)
            {
                return new MarkupString(value);
            }

            return new MarkupString(WebUtility.HtmlEncode(value));
        }

        /// <summary>
        ///     Control initialization hook. The original signature is preserved so existing overrides
        ///     continue to work; <paramref name="sender" /> is this component and <paramref name="e" /> is
        ///     <see cref="EventArgs.Empty" />.
        /// </summary>
        public abstract void OnControlLoad(object sender, EventArgs e);

        /// <summary>
        ///     Re-runs <see cref="OnControlLoad" /> and re-renders. The WebForms <c>Load</c> event fired on
        ///     every postback, which is how these controls refreshed their data; Blazor has no postback, so
        ///     an interactive handler that changes what the control should display calls this explicitly.
        /// </summary>
        public void Reload()
        {
            this.OnControlLoad(this, EventArgs.Empty);
            this.StateHasChanged();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.OnControlLoad(this, EventArgs.Empty);
            this.loaded = true;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // WebForms raised Load on every postback. OnInitialized runs only once per component, so the
            // control is reloaded whenever its parameters change too — otherwise it would keep rendering
            // the data it fetched when it was first created.
            if (this.loaded)
            {
                this.OnControlLoad(this, EventArgs.Empty);
            }
        }
    }
}
