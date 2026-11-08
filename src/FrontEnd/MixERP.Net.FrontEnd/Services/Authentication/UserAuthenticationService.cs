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
using Microsoft.AspNetCore.Http;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Framework;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Authentication
{
    /// <summary>
    ///     The authentication half of the former <c>Services/User.asmx</c>
    ///     (<c>Authenticate</c> / <c>GetDelay</c> / <c>Login</c> / <c>LogInvalidSignIn</c>) —
    ///     checklist tasks 50, 59 and 60.
    ///     <para>
    ///         Specialist 6 maps <c>POST /services/user/authenticate</c> onto
    ///         <see cref="AuthenticateAsync" /> and wraps the returned string in the ASMX
    ///         <c>{ "d": ... }</c> envelope. All of the security behaviour lives here so the endpoint is
    ///         pure transport.
    ///     </para>
    ///     <para>
    ///         BLAZOR SERVER SEAM: this MUST run on a real HTTP endpoint, never inside a circuit — it
    ///         reads session (challenge, attempt counter) and WRITES the authentication cookie, both of
    ///         which require an open response. That is why the sign-in form posts to an endpoint instead
    ///         of calling a C# handler over the circuit.
    ///     </para>
    /// </summary>
    public sealed class UserAuthenticationService
    {
        /// <summary>
        ///     Port of <c>User.Authenticate(catalog, username, password, rememberMe, language, branchId)</c>.
        ///     Returns <c>"OK"</c>, <c>Warnings.UserIdOrPasswordIncorrect</c>,
        ///     <c>Titles.AccessIsDenied</c> or the <c>MixERPException</c> message — the exact four
        ///     outcomes the javascript caller branches on.
        /// </summary>
        public async Task<string> AuthenticateAsync(HttpContext context, string catalog, string username,
            string password, bool rememberMe, string language, int branchId)
        {
            ISession session = context == null ? null : context.Session;

            //Escalating delay BEFORE anything else, exactly as Thread.Sleep(this.GetDelay()) did.
            //Task.Delay keeps the request thread free; the wall-clock friction is identical.
            await Task.Delay(SignInSession.GetDelay(session)).ConfigureAwait(false);

            string challenge = SignInSession.GetChallenge(session);

            if (string.IsNullOrWhiteSpace(challenge))
            {
                //No challenge in session means the caller never loaded the sign-in page.
                return Titles.AccessIsDenied;
            }

            return await this.LoginAsync(context, catalog, branchId, username, password, language, rememberMe,
                challenge).ConfigureAwait(false);
        }

        private async Task<string> LoginAsync(HttpContext context, string catalog, int officeId, string userName,
            string password, string culture, bool rememberMe, string challenge)
        {
            ISession session = context == null ? null : context.Session;

            //Legacy: context.Request.ServerVariables["REMOTE_ADDR"].
            string remoteAddress = context == null || context.Connection.RemoteIpAddress == null
                ? string.Empty
                : context.Connection.RemoteIpAddress.ToString();

            try
            {
                long globalLoginId = Data.Office.User.SignIn(catalog, officeId, userName, password, culture,
                    rememberMe, challenge, context);

                Log.Information("{UserName} signed in to office : #{OfficeId} from {IP}.", userName, officeId,
                    remoteAddress);

                if (globalLoginId > 0)
                {
                    AppUsers.SetCurrentLogin(globalLoginId);

                    //Task 50: the 30 minute ticket, persistent when rememberMe is set.
                    await MixERPAuthentication.SetAuthenticationTicketAsync(context, globalLoginId, rememberMe)
                        .ConfigureAwait(false);

                    if (session != null)
                    {
                        session.SetString("FirstStepsPending",
                            FirstSteps.CheckIfPending(globalLoginId).ToString());
                    }

                    return "OK";
                }

                SignInSession.LogInvalidSignIn(session);
                return Warnings.UserIdOrPasswordIncorrect;
            }
            catch (MixERPException ex)
            {
                Log.Warning("{UserName} could not sign in to office : #{OfficeId} from {IP}.", userName, officeId,
                    remoteAddress);

                SignInSession.LogInvalidSignIn(session);
                return ex.Message;
            }
        }
    }
}
