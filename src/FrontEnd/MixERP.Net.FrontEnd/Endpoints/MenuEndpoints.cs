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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Entities.Core;
using MixERP.Net.FrontEnd.Models;
using Newtonsoft.Json;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     Replaces <c>Services/Menu.asmx</c> (<c>GetMenus</c>) — checklist task 77.
    ///     <para>
    ///         The payload is a JSON STRING (the legacy method returned
    ///         <c>JsonConvert.SerializeObject(menus)</c>), so the caller sees
    ///         <c>{"d":"[...escaped json...]"}</c> and does
    ///         <c>menus = JSON.parse(msg.d)</c> — that double encoding is part of the contract of
    ///         <c>wwwroot/js/mixerp/master-page/menu.js</c> and <c>js-tree.js</c> and is preserved
    ///         exactly, Newtonsoft included (System.Text.Json would camelCase <c>Menu.MenuCode</c>
    ///         etc. and silently break the tree).
    ///     </para>
    ///     <para>
    ///         This stays a real HTTP endpoint: <c>AppUsers.GetCurrent()</c> resolves the user from
    ///         <c>HttpContext.User</c>, which is exactly what an HTTP request has (and a circuit does
    ///         not).
    ///     </para>
    /// </summary>
    public static class MenuEndpoints
    {
        public const string GetMenusRoute = "/services/menu/getmenus";

        public static IEndpointRouteBuilder MapMenuServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetMenusRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(GetMenus());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                //AJAX POST from the layout's menu.js, no antiforgery token. The menu set is scoped to
                //the signed-in user, so authorization is required (an anonymous caller got an empty
                //login and therefore an empty menu from the legacy service).
                .DisableAntiforgery()
                .RequireAuthorization();

            return endpoints;
        }

        /// <summary>
        ///     Verbatim port of <c>Menu.GetMenus()</c>.
        /// </summary>
        public static string GetMenus()
        {
            LoginView view = AppUsers.GetCurrent().View;

            string catalog = AppUsers.GetCurrentUserDB();
            int userId = view.UserId.ToInt();
            int officeId = view.OfficeId.ToInt();
            string culture = view.Culture;

            Navigation nav = new Navigation(catalog, userId, officeId, culture);
            var menus = nav.GetMenus();

            return JsonConvert.SerializeObject(menus);
        }
    }
}
