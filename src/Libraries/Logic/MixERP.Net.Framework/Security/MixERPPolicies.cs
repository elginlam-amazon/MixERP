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

namespace MixERP.Net.Framework.Security
{
    /// <summary>
    ///     The names of the authorization policies the host registers, so that a library or a Razor
    ///     Class Library can reference a policy on its own endpoints without referencing the host.
    /// </summary>
    public static class MixERPPolicies
    {
        /// <summary>
        ///     Requires the signed-in user to be a MixERP administrator — the same rule
        ///     <see cref="IMixERPAuthorization" /> applies for
        ///     <see cref="MixERP.Net.Framework.Controls.AccessLevel.AdminOnly" /> on a control
        ///     (<c>AppUsers.GetCurrent(globalLoginId).View.IsAdmin</c>).
        ///     <para>
        ///         Registered in the host's <c>Program.cs</c> with an assertion handler, because admin
        ///         status is NOT carried as a claim: the sign-in principal holds only the
        ///         <c>globalLoginId</c> subject, exactly as the legacy
        ///         <c>FormsAuthenticationTicket</c> did, and the admin flag is read from the login
        ///         cache. Any endpoint that was reachable only through an
        ///         <c>AccessLevel.AdminOnly</c> control should carry this policy.
        ///     </para>
        /// </summary>
        public const string AdminOnly = "MixERPAdminOnly";
    }
}
