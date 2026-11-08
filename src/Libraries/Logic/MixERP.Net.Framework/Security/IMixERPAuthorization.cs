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
using MixERP.Net.Framework.Controls;

namespace MixERP.Net.Framework.Security
{
    /// <summary>
    ///     The authorization rules the legacy WebForms surfaces enforced, as seen by any library or
    ///     Razor Class Library that cannot reference the web host.
    ///     <para>
    ///         The single implementation is
    ///         <c>MixERP.Net.FrontEnd.Services.Authentication.MixERPAuthorization</c>, registered
    ///         against this interface in the host's <c>Program.cs</c>. There is no second
    ///         implementation and no shim.
    ///     </para>
    ///     <para>
    ///         A denial does NOT navigate: the implementation records it on the host's scoped
    ///         <c>PageAuthorizationState</c> and the layout's <c>AuthorizationOutlet</c> substitutes
    ///         the denial content for the page body. That reproduces <c>Server.Transfer</c> semantics
    ///         — the response body is replaced, the url stays exactly where the user put it.
    ///     </para>
    ///     <para>
    ///         <c>VerifyAccess</c> (the menu-policy check) is deliberately NOT on this interface: its
    ///         signature takes <c>MixERP.Net.Entities.Core.Menu</c>, and
    ///         <c>MixERP.Net.Entities</c> references THIS assembly, so naming it here would close a
    ///         project reference cycle. It is a page-level check that only the host performs, and it
    ///         remains a public member of the concrete host implementation.
    ///     </para>
    /// </summary>
    public interface IMixERPAuthorization
    {
        /// <summary>
        ///     Port of <c>MixERPWebpage.CheckForceLogOffFlags()</c>. True when the user must be signed
        ///     off because a forced log-off timestamp for their office falls between their login time
        ///     and now.
        /// </summary>
        Task<bool> MustForceLogOffAsync();

        /// <summary>
        ///     Port of <c>MixERPUserControl.CheckAccessLevel()</c>. Returns false — and records an
        ///     AccessIsDenied denial — when the surface's <paramref name="accessLevel" /> is not
        ///     satisfied by the current login. <paramref name="control" /> is only used for the log
        ///     entry, exactly as the original passed <c>this</c>.
        /// </summary>
        Task<bool> CheckAccessLevelAsync(AccessLevel accessLevel, object control);

        /// <summary>
        ///     Port of <c>MixERPUserControl.CheckTransactionPostingStatus()</c>.
        ///     <para>
        ///         The original test was <c>this is ITransaction</c>. <c>ITransaction</c> lives in
        ///         <c>MixERP.Net.Entities.Contracts</c>, and <c>MixERP.Net.Entities</c> references
        ///         THIS assembly, so the test cannot be written here. The CONTROL is therefore passed
        ///         and the implementation — which is in the host and does reference
        ///         <c>MixERP.Net.Entities</c> — performs the identical
        ///         <c>control is ITransaction</c> test. Behaviour is unchanged; only the side of the
        ///         boundary the type test runs on moved.
        ///     </para>
        /// </summary>
        Task<bool> CheckTransactionPostingStatusAsync(object control);

        /// <summary>
        ///     Port of the <c>Modules/Update.aspx</c> page-level authorization: admin AND (not
        ///     development mode OR (development mode AND localhost)).
        /// </summary>
        Task<bool> CanAccessUpdatePageAsync(object page);
    }
}
