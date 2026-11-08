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

using System.Threading.Tasks;

namespace MixERP.Net.Framework.Security
{
    /// <summary>
    ///     The circuit-safe current-login accessor, as seen by any library or Razor Class Library that
    ///     cannot reference the web host.
    ///     <para>
    ///         The single implementation is
    ///         <c>MixERP.Net.FrontEnd.Services.Authentication.CurrentLoginProvider</c>, registered
    ///         against this interface in the host's <c>Program.cs</c>. There is no second
    ///         implementation and no shim.
    ///     </para>
    ///     <para>
    ///         ⚠ USE THIS, NOT <c>AppUsers.GetCurrent()</c>, FROM COMPONENT CODE.
    ///         <c>AppUsers.GetCurrent()</c> resolves the user through
    ///         <c>IHttpContextAccessor.HttpContext.User</c>, which is NULL over the Blazor SignalR
    ///         circuit: called from interactive component code it silently returns an EMPTY login
    ///         (<c>UserId == 0</c>, <c>IsAdmin == false</c>, <c>AllowTransactionPosting == false</c>,
    ///         no catalog) with no exception — the form then queries the wrong database or renders a
    ///         permanent "restricted transaction mode". The implementation of this interface resolves
    ///         the <c>globalLoginId</c> from the circuit's authentication state instead, and falls back
    ///         to <c>HttpContext.User</c> on a plain HTTP request.
    ///     </para>
    /// </summary>
    public interface ICurrentLogin
    {
        /// <summary>
        ///     The remote address of the request that created this scope — the legacy
        ///     <c>Request.ServerVariables["REMOTE_ADDR"]</c>. Captured once, during the initial HTTP
        ///     request, because it is unreadable at interactive time.
        /// </summary>
        string RemoteIpAddress { get; }

        /// <summary>
        ///     The legacy <c>Request.IsAuthenticated</c>.
        /// </summary>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        ///     The <c>globalLoginId</c> carried by the subject claim, or 0.
        /// </summary>
        Task<long> GetGlobalLoginIdAsync();

        /// <summary>
        ///     The current login as primitives. NEVER null — an unauthenticated or unresolvable user
        ///     yields <see cref="LoginInfo.Empty" />, which is the same "empty login" shape
        ///     <c>AppUsers.GetCurrent(globalLoginId)</c> guarantees.
        /// </summary>
        Task<LoginInfo> GetLoginAsync();

        /// <summary>
        ///     The circuit-safe equivalent of <c>AppUsers.GetCurrentUserDB()</c>: the catalog
        ///     (database) of the signed-in user, or an empty string.
        /// </summary>
        Task<string> GetCatalogAsync();

        /// <summary>
        ///     Loads the login cache entry for the current subject — <c>AppUsers.SetCurrentLogin()</c>
        ///     without the <c>HttpContext</c> dependency.
        /// </summary>
        Task SetCurrentLoginAsync();
    }
}
