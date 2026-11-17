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
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Authenticates ASP.NET Core requests (the SignalR hubs) with the Forms Authentication
    ///     ticket issued by the Web Forms sign-in page, so Hub.Context.User.Identity.Name is the
    ///     global login id, exactly as it was when the hubs ran in the System.Web pipeline.
    /// </summary>
    internal sealed class FormsTicketAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        internal const string SchemeName = "FormsTicket";

        public FormsTicketAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string cookie = this.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (string.IsNullOrWhiteSpace(cookie))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            FormsAuthenticationTicket ticket;

            try
            {
                ticket = FormsAuthentication.Decrypt(cookie);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(AuthenticateResult.Fail(ex));
            }
            catch (System.Web.HttpException ex)
            {
                return Task.FromResult(AuthenticateResult.Fail(ex));
            }

            if (ticket == null || ticket.Expired || string.IsNullOrWhiteSpace(ticket.Name))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ClaimsIdentity identity = new ClaimsIdentity(new[] {new Claim(ClaimTypes.Name, ticket.Name)}, SchemeName);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
