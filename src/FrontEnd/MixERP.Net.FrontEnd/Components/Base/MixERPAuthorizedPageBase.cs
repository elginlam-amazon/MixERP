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

using Microsoft.AspNetCore.Authorization;

namespace MixERP.Net.FrontEnd.Base
{
    /// <summary>
    ///     The page base EVERY authenticated MixERP page inherits (checklist task 52): it adds the
    ///     router-level <c>[Authorize]</c> gate on top of the ported
    ///     <see cref="MixERPPageBase" /> activation sequence (login gate, forced log-off, policy check).
    ///     <para>
    ///         TWO LAYERS ON PURPOSE. <c>[Authorize]</c> + <c>AuthorizeRouteView</c> challenges an
    ///         anonymous visitor before the page is even instantiated, which is the modern equivalent of
    ///         <c>&lt;deny users="?"&gt;</c>. The <see cref="MixERPPageBase" /> gate still runs because
    ///         the legacy check did more than "is authenticated": it rebuilt the login cache entry when
    ///         <c>LoginId</c> was 0 and signed the user off when it stayed 0.
    ///     </para>
    ///     <para>
    ///         The ONLY pages that must NOT inherit this class are the anonymous ones — <c>SignIn</c>
    ///         and <c>Install</c> — which inherit <see cref="MixERPPageBase" /> and set
    ///         <c>SkipLoginCheck = true</c> (the legacy flag). <c>AuthorizeRouteView</c> does not honour
    ///         <c>[AllowAnonymous]</c>, so opting out means NOT inheriting this type.
    ///     </para>
    /// </summary>
    [Authorize]
    public class MixERPAuthorizedPageBase : MixERPPageBase
    {
    }
}
