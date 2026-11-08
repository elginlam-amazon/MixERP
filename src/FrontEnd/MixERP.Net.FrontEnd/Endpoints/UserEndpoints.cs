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
using MixERP.Net.FrontEnd.Services.Authentication;

namespace MixERP.Net.FrontEnd.Endpoints
{
    /// <summary>
    ///     Replaces <c>Services/User.asmx</c> (<c>Authenticate</c>) — checklist task 79.
    ///     <para>
    ///         This endpoint is PURE TRANSPORT. Every piece of behaviour — the escalating delay
    ///         (<c>attempts &gt; 3 ? attempts * 10000 : 1000</c> ms), the session <c>Challenge</c>
    ///         guard, <c>Data.Office.User.SignIn</c>, <c>AppUsers.SetCurrentLogin</c>, the 30 minute
    ///         authentication ticket with <c>IsPersistent = rememberMe</c>, the
    ///         <c>Session["FirstStepsPending"]</c> write, the invalid-attempt counter and the two log
    ///         entries carrying the remote IP (the former
    ///         <c>ServerVariables["REMOTE_ADDR"]</c>, now
    ///         <c>HttpContext.Connection.RemoteIpAddress</c>) — lives in
    ///         <see cref="UserAuthenticationService.AuthenticateAsync" />, which returns the exact
    ///         four outcomes the javascript branches on: <c>"OK"</c>,
    ///         <c>Warnings.UserIdOrPasswordIncorrect</c>, <c>Titles.AccessIsDenied</c> or the
    ///         <c>MixERPException</c> message.
    ///     </para>
    ///     <para>
    ///         IT MUST STAY A REAL HTTP ENDPOINT: it writes the authentication cookie and reads and
    ///         writes session, neither of which is possible inside a Blazor circuit (there is no open
    ///         response). The sign-in form therefore posts here instead of invoking a component
    ///         handler.
    ///     </para>
    /// </summary>
    public static class UserEndpoints
    {
        public const string AuthenticateRoute = "/services/user/authenticate";

        public static IEndpointRouteBuilder MapUserServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(AuthenticateRoute, AsmxCompatibility.Verbs,
                async (HttpContext context, UserAuthenticationService authentication) =>
                {
                    try
                    {
                        AsmxParameters parameters =
                            await AsmxCompatibility.ReadParametersAsync(context).ConfigureAwait(false);

                        string result = await authentication.AuthenticateAsync(context,
                            parameters.GetString("catalog"),
                            parameters.GetString("username"),
                            parameters.GetString("password"),
                            parameters.GetBoolean("rememberMe"),
                            parameters.GetString("language"),
                            parameters.GetInt32("branchId")).ConfigureAwait(false);

                        return AsmxCompatibility.Envelope(result);
                    }
                    catch (Exception ex)
                    {
                        //AuthenticateAsync already turns a MixERPException into a message string, so
                        //reaching here means an infrastructure failure; the caller's
                        //getAjaxErrorMessage reads .Message out of this payload.
                        return AsmxCompatibility.Error(ex);
                    }
                })
                //The sign-in page posts this without an antiforgery token (as the legacy ASMX was
                //called), and of course before the caller is authenticated.
                .DisableAntiforgery()
                .AllowAnonymous();

            return endpoints;
        }
    }
}
