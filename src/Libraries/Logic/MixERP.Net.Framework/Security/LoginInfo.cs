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

namespace MixERP.Net.Framework.Security
{
    /// <summary>
    ///     The signed-in user, expressed in PRIMITIVES so that this library — which sits at the very
    ///     bottom of the dependency graph — can describe the current login without referencing the
    ///     projects that model it.
    ///     <para>
    ///         WHY THIS TYPE EXISTS INSTEAD OF <c>MetaLogin</c>: the natural choice would have been to
    ///         move <c>MixERP.Net.FrontEnd.Services.Authentication.CurrentLoginProvider</c> into this
    ///         assembly and keep returning <c>MixERP.Net.ApplicationState.Cache.MetaLogin</c>. That is
    ///         IMPOSSIBLE: <c>MixERP.Net.ApplicationState</c> references
    ///         <c>MixERP.Net.Framework</c> (<c>Cache/AppUsers.cs</c> and <c>Dates.cs</c> both
    ///         <c>using MixERP.Net.Framework</c> for the cache factory), and so do
    ///         <c>MixERP.Net.Common</c>, <c>MixERP.Net.Entities</c> and <c>PetaPoco</c>. Referencing
    ///         any of them from here would close a project reference CYCLE.
    ///     </para>
    ///     <para>
    ///         Every member is a 1:1 projection of <c>MetaLogin</c> + <c>MetaLogin.View</c>
    ///         (<c>office.sign_in_view</c>). The nullable database columns are projected to
    ///         non-nullable CLR values using exactly the conversion the legacy code applied at each
    ///         call site (<c>.ToInt()</c> / <c>.ToBool()</c>, i.e. null becomes 0 / false), so a
    ///         consumer reading <see cref="OfficeId" /> here gets the same value the WebForms code got
    ///         from <c>AppUsers.GetCurrent().View.OfficeId.ToInt()</c>.
    ///     </para>
    ///     <para>
    ///         An unauthenticated or unresolvable login is represented by <see cref="Empty" /> — never
    ///         by null — which matches <c>AppUsers.GetCurrent(globalLoginId)</c>'s guarantee of a
    ///         non-null login with a non-null <c>View</c>.
    ///     </para>
    /// </summary>
    public sealed class LoginInfo
    {
        /// <summary>
        ///     The "no signed-in user" login: <see cref="GlobalLoginId" /> 0, no catalog, no rights.
        ///     This is the shape every legacy authorization rule treated as a denial.
        /// </summary>
        public static readonly LoginInfo Empty = new LoginInfo();

        //MetaLogin
        public long GlobalLoginId { get; set; }

        public string Catalog { get; set; }

        public long LoginId { get; set; }

        //MetaLogin.View (office.sign_in_view)
        public int UserId { get; set; }

        public int RoleId { get; set; }

        public string Role { get; set; }

        public string RoleCode { get; set; }

        public string RoleName { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsSystem { get; set; }

        public bool Elevated { get; set; }

        public bool AllowTransactionPosting { get; set; }

        public string Browser { get; set; }

        public string IpAddress { get; set; }

        public DateTime LoginDateTime { get; set; }

        public string RemoteUser { get; set; }

        public string Culture { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public string NickName { get; set; }

        public string Office { get; set; }

        public int OfficeId { get; set; }

        public string OfficeCode { get; set; }

        public string OfficeName { get; set; }

        public DateTime RegistrationDate { get; set; }

        public string CurrencyCode { get; set; }

        public string PoBox { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string ZipCode { get; set; }

        public string Country { get; set; }

        public string Phone { get; set; }

        public string Fax { get; set; }

        public string Email { get; set; }

        public string Url { get; set; }

        public string RegistrationNumber { get; set; }

        public string PanNumber { get; set; }

        /// <summary>
        ///     True when this instance describes an actually signed-in user, i.e. the legacy
        ///     <c>login != null &amp;&amp; login.View != null</c> test.
        /// </summary>
        public bool IsSignedIn
        {
            get { return this.GlobalLoginId > 0; }
        }
    }
}
