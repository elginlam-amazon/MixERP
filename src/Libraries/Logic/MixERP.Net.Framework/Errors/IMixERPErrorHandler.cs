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

namespace MixERP.Net.Framework.Errors
{
    /// <summary>
    ///     Framework-agnostic replacement for the WebForms
    ///     <c>Session["ex"] = ex; Server.TransferRequest("~/Site/RuntimeError.aspx")</c>
    ///     flow. The host application provides the implementation (in Blazor Server it stores the
    ///     exception in circuit-scoped state and navigates to the <c>/site/runtime-error</c> route).
    /// </summary>
    public interface IMixERPErrorHandler
    {
        /// <summary>
        ///     Records the exception for display and routes the user to the runtime-error page.
        /// </summary>
        void Handle(MixERPException exception);
    }
}
