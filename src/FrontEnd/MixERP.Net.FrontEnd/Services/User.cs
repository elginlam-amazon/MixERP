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

using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using Serilog;
using System.Threading;
using MixERP.Net.FrontEnd.Application;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MixERP.Net.Framework.Web;

namespace MixERP.Net.FrontEnd.Services
{
    /// <summary>
    ///     Replaces the /Services/User.asmx ASP.NET AJAX script service (same url and JSON contract).
    /// </summary>
    [Route("Services/User.asmx")]
    public class User : ScriptServiceController
    {
        [HttpPost("Authenticate")]
        public string Authenticate(string catalog, string username, string password, bool rememberMe, string language,
            int branchId)
        {
            Thread.Sleep(this.GetDelay());

            string challenge = this.HttpContext.Session.GetString("Challenge");

            if (string.IsNullOrWhiteSpace(challenge))
            {
                return Titles.AccessIsDenied;
            }

            return this.Login(catalog, branchId, username, password, language, rememberMe, challenge, this.HttpContext);
        }

        private int GetDelay()
        {
            int attempts = PageUtility.InvalidPasswordAttempts(this.HttpContext.Session);

            if (attempts > 3)
            {
                return attempts * 10000;
            }

            return 1000;
        }

        private string Login(string catalog, int officeId, string userName, string password, string culture,
            bool rememberMe, string challenge, HttpContext context)
        {
            try
            {
                long globalLoginId = Data.Office.User.SignIn(catalog, officeId, userName, password, culture, rememberMe,
                    challenge, context);

                Log.Information("{UserName} signed in to office : #{OfficeId} from {IP}.", userName, officeId,
                    context.Connection.RemoteIpAddress);

                if (globalLoginId > 0)
                {
                    AppUsers.SetCurrentLogin(globalLoginId);
                    AuthenticationTicket.SignIn(context, globalLoginId, rememberMe);

                    context.Session.SetString("FirstStepsPending", FirstSteps.CheckIfPending(globalLoginId).ToString());


                    return "OK";
                }

                this.LogInvalidSignIn();
                return Warnings.UserIdOrPasswordIncorrect;
            }
            catch (MixERPException ex)
            {
                Log.Warning("{UserName} could not sign in to office : #{OfficeId} from {IP}.", userName, officeId,
                    context.Connection.RemoteIpAddress);

                this.LogInvalidSignIn();
                return ex.Message;
            }
        }

        private void LogInvalidSignIn()
        {
            PageUtility.InvalidPasswordAttempts(this.HttpContext.Session, 1);
        }
    };
} ;
