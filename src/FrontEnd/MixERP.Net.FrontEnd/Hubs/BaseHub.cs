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

//MIGRATION (checklist task 100). Microsoft.AspNet.SignalR -> Microsoft.AspNetCore.SignalR.
//
//  * `Hub` is now the ASP.NET Core hub base; there is no [HubName] and no dynamic client proxy,
//    so every server -> client call is `Clients.X.SendAsync("<clientMethodName>", args)`.
//  * The CLIENT METHOD NAMES ARE PART OF THE CONTRACT and are kept byte-identical to the names
//    the javascript listens for:
//       "terminate"                       - wwwroot/js/mixerp/master-page/eod.js
//       "getNotification"                 - wwwroot/js/mixerp/master-page/notification.js
//       "updateInstallationNotification"  - notification.js -> wwwroot/js/Pages/Modules/Update.js
//  * AppUsers.GetCurrentUserDB() CANNOT be used inside a hub call: it resolves the user through
//    IHttpContextAccessor, which has no HttpContext on a hub invocation, and would silently
//    return an EMPTY catalog. The catalog is therefore resolved from the connection's own
//    principal with AppUsers.GetCurrent(globalLoginId) (Specialist 4's rule).
//  * The class is abstract: the only mapped route is /hubs/update, served by UpdateHub, which
//    derives from this type - so `Terminate` stays invokable on that single connection (that is
//    what eod.js listens on since the jQuery `$.connection.baseHub` proxy no longer exists).

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;

namespace MixERP.Net.FrontEnd.Hubs
{
    public abstract class BaseHub : Hub
    {
        /// <summary>
        ///     The client method name eod.js subscribes to.
        /// </summary>
        public const string TerminateClientMethod = "terminate";

        /// <summary>
        ///     The client method name notification.js subscribes to.
        /// </summary>
        public const string NotificationClientMethod = "getNotification";

        /// <summary>
        ///     The client method name notification.js forwards to the Update page.
        /// </summary>
        public const string UpdateInstallationClientMethod = "updateInstallationNotification";

        /// <summary>
        ///     End-of-day progress broadcast. Was <c>this.Clients.All.terminate(counter, catalog)</c>.
        /// </summary>
        public Task Terminate(int counter)
        {
            string catalog = this.GetCatalog();
            return this.Clients.All.SendAsync(TerminateClientMethod, counter, catalog);
        }

        /// <summary>
        ///     Was <c>this.Clients.Caller.getNotification(message)</c>.
        /// </summary>
        protected Task NotifyCallerAsync(string message)
        {
            return this.Clients.Caller.SendAsync(NotificationClientMethod, message);
        }

        /// <summary>
        ///     Was <c>this.Clients.All.updateInstallationNotification(timestamp, description, message)</c>.
        /// </summary>
        protected Task NotifyAllAsync(DateTime timestamp, string description, string message)
        {
            return this.Clients.All.SendAsync(UpdateInstallationClientMethod, timestamp, description, message);
        }

        /// <summary>
        ///     The global login id of the connection's principal.
        /// </summary>
        protected long GetGlobalLoginId()
        {
            return Conversion.TryCastLong(this.Context?.User?.Identity?.Name);
        }

        /// <summary>
        ///     The catalog of the connection's principal (the hub-safe replacement of
        ///     <c>AppUsers.GetCurrentUserDB()</c>).
        /// </summary>
        protected string GetCatalog()
        {
            return AppUsers.GetCurrent(this.GetGlobalLoginId()).Catalog;
        }
    }
}
