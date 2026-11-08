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
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.Entities.Office;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     Replaces <c>Services/Office.asmx</c> (<c>GetOffices(catalog)</c>) — checklist task 78.
    ///     Consumed by <c>wwwroot/js/Pages/SignIn.aspx.js</c>'s company-select change handler, which
    ///     binds <c>msg.d</c> as <c>item.OfficeId</c> / <c>item.OfficeName</c> options and, on
    ///     failure, reloads the sign-in page with <c>?Message=&lt;Message&gt;</c>.
    /// </summary>
    public static class OfficeEndpoints
    {
        public const string GetOfficesRoute = "/services/office/getoffices";

        public static IEndpointRouteBuilder MapOfficeServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetOfficesRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters =
                        await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetOffices(parameters.GetString("catalog")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                //Called by the sign-in page before anybody is authenticated, as an AJAX POST with no
                //antiforgery token (exactly as the legacy ASMX was called).
                .DisableAntiforgery()
                .AllowAnonymous();

            return endpoints;
        }

        /// <summary>
        ///     Verbatim port of <c>Office.GetOffices(catalog)</c>: any failure is logged and
        ///     translated to <c>MixERPException(Titles.AccessIsDenied)</c>, so a probe cannot tell a
        ///     non-existent catalog from an unreadable one.
        /// </summary>
        public static IEnumerable<DbGetOfficesResult> GetOffices(string catalog)
        {
            IEnumerable<DbGetOfficesResult> offices;

            try
            {
                offices = Data.Office.Offices.GetOffices(catalog);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw new MixERPException(Titles.AccessIsDenied);
            }

            return offices;
        }
    }
}
