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

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     What the legacy code reached by <c>Server.Transfer</c> / <c>Server.Execute</c>.
    /// </summary>
    public enum AuthorizationDenial
    {
        /// <summary>Nothing is denied; the page renders.</summary>
        None,

        /// <summary><c>Server.Transfer("~/Site/AccessIsDenied.aspx")</c>.</summary>
        AccessIsDenied,

        /// <summary><c>Server.Transfer("~/Site/Exceptions/RestrictedTransactionMode.aspx")</c>.</summary>
        RestrictedTransactionMode
    }

    /// <summary>
    ///     Circuit-scoped carrier of the current page's authorization denial.
    ///     <para>
    ///         SERVER.TRANSFER SEMANTICS: <c>Server.Transfer</c> / <c>Server.Execute</c> replaced the
    ///         RESPONSE BODY with another page while LEAVING THE URL UNCHANGED — it was not a redirect.
    ///         A Blazor navigation would change the url, so instead the page base / control base records
    ///         the denial here and the layout renders
    ///         <c>Components/Base/AuthorizationOutlet.razor</c>, which substitutes the denial content
    ///         for <c>@Body</c>. The url the user requested stays exactly where it was.
    ///     </para>
    /// </summary>
    public sealed class PageAuthorizationState
    {
        private AuthorizationDenial denial;

        /// <summary>
        ///     Raised when <see cref="Denial" /> changes so the outlet can re-render.
        /// </summary>
        public event Action Changed;

        public AuthorizationDenial Denial
        {
            get { return this.denial; }
        }

        public bool IsDenied
        {
            get { return this.denial != AuthorizationDenial.None; }
        }

        public void Deny(AuthorizationDenial reason)
        {
            if (this.denial == reason)
            {
                return;
            }

            this.denial = reason;
            this.OnChanged();
        }

        /// <summary>
        ///     Called at the start of every page activation: the legacy checks ran per request, so a
        ///     denial must never survive into the next page the user navigates to.
        /// </summary>
        public void Reset()
        {
            if (this.denial == AuthorizationDenial.None)
            {
                return;
            }

            this.denial = AuthorizationDenial.None;
            this.OnChanged();
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
