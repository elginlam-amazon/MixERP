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

//MIGRATION. Microsoft.AspNet.SignalR -> Microsoft.AspNetCore.SignalR, matching the shape of the
//host's already-ported Hubs/BaseHub.cs + Hubs/UpdateHub.cs.
//
//  ROUTE:   app.MapHub<DayOperationHub>("/hubs/finance/dayoperation")   (FinanceHubs.MapFinanceHubs)
//  INVOKED: connection.invoke("PerformEOD")        (wwwroot/js/EODOperation.js)
//  EMITS:   "getNotification"(message, condition)  to the CALLER
//
//  WHAT CHANGED AND WHY
//   * `Clients.Caller.getNotification(a, b)` dynamic proxy syntax does not exist in Core, so it
//     is `Clients.Caller.SendAsync("getNotification", a, b)`. THE CLIENT METHOD NAME IS PART OF
//     THE CONTRACT and is kept byte-identical to what EODOperation.js listens for with
//     `dayOperationConnection.on("getNotification", function (msg, condition) {...})`; so is the
//     SERVER method name `PerformEOD`, which the same file invokes by string.
//   * [HubName] and [CLSCompliant(false)] are gone: Core has no hub-name attribute (the mapped
//     route is the hub's identity), and the attribute existed only because the legacy dynamic
//     `IHubCallerConnectionContext<dynamic>` surface was not CLS-compliant.
//   * `PerformEOD` is `async Task`. Data.EODOperation.Perform is SYNCHRONOUS and raises
//     NotificationReceived on the calling thread, so it runs on a worker thread (Task.Run +
//     await) and is awaited - nothing is fire-and-forget and the hub's dispatch thread is never
//     blocked, which is what keeps the connection's keep-alive pings flowing through a
//     multi-minute end-of-day run. This mirrors UpdateHub's handling of UpdateInstaller.Update().
//   * Because the NotificationReceived event therefore fires OFF the hub's call context, the
//     notifications it raises go through IHubContext<DayOperationHub> scoped to the CALLER'S
//     connection id, not through `this.Clients` - which is only valid for the lifetime of the
//     hub method. `this.Clients` is still used for the synchronous authorization refusal.
//   * NO SYNC-OVER-ASYNC: there is no .Wait()/.Result/.GetAwaiter().GetResult() anywhere, and
//     the legacy `Thread.Sleep(2000)` of IsValidRequest is now `await Task.Delay(...)` - the
//     same two-second delay, without occupying a thread-pool thread.
//   * AppUsers.GetCurrentUserDB() CANNOT be used inside a hub call: it resolves the user through
//     IHttpContextAccessor, which has no HttpContext on a hub invocation, and would silently
//     return an EMPTY catalog - the end-of-day run would then execute against the wrong
//     database. The catalog and the login id come from the CONNECTION'S OWN PRINCIPAL through
//     AppUsers.GetCurrent(globalLoginId), which is the host BaseHub.GetCatalog() pattern.
//   * IsValidRequest is kept HERE rather than calling the host's
//     MixERPAuthorization.IsValidHubRequestAsync: this project is a Razor Class Library that the
//     host references, so the host's type is unreachable from here (referencing it back is the
//     inverted direction the infrastructure pass removed). The ORDER, the delay, the
//     globalLoginId/IsAdmin/Elevated checks and the notify-vs-silent split are reproduced
//     exactly as the original had them - including the fact that the IsAdmin and Elevated
//     refusals are SILENT (no notification) while the two login-id refusals notify with a
//     SINGLE argument.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Events;
using MixERP.Net.Common.Extensions;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Hubs
{
    /// <summary>
    ///     The end-of-day operation hub: it runs <c>Data.EODOperation.Perform</c> and streams the
    ///     PostgreSQL notices it raises back to the caller as they arrive.
    /// </summary>
    public class DayOperationHub : Hub
    {
        /// <summary>
        ///     The client method name <c>wwwroot/js/EODOperation.js</c> subscribes to.
        /// </summary>
        public const string NotificationClientMethod = "getNotification";

        /// <summary>
        ///     The legacy <c>IsValidRequest()</c> delay, verbatim (was <c>Thread.Sleep(2000)</c>).
        /// </summary>
        private static readonly TimeSpan ValidationDelay = TimeSpan.FromSeconds(2);

        private readonly IHubContext<DayOperationHub> hubContext;

        public DayOperationHub(IHubContext<DayOperationHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        /// <summary>
        ///     Invoked as <c>connection.invoke("PerformEOD")</c>. The method NAME is the
        ///     javascript contract - do not rename it.
        /// </summary>
        public async Task PerformEOD()
        {
            if (!await this.IsValidRequestAsync().ConfigureAwait(false))
            {
                await this.Clients.Caller
                    .SendAsync(NotificationClientMethod, Warnings.AccessIsDenied, Warnings.AccessIsDenied)
                    .ConfigureAwait(false);
                return;
            }

            long globalLoginId = this.GetGlobalLoginId();
            string catalog = this.GetCatalog();
            long loginId = AppUsers.GetCurrent(globalLoginId).View.LoginId.ToLong();

            //The caller's connection id is captured here, while the hub call context is still
            //alive, because the notifications below are raised from a worker thread.
            string connectionId = this.Context == null ? null : this.Context.ConnectionId;

            Data.EODOperation operation = new Data.EODOperation();
            EventHandler<MixERPPGEventArgs> handler =
                (sender, e) => this.NotifyCaller(connectionId, e);

            operation.NotificationReceived += handler;

            try
            {
                //Perform() is synchronous and raises NotificationReceived on the calling thread.
                await Task.Run(() => operation.Perform(catalog, loginId)).ConfigureAwait(false);
            }
            finally
            {
                operation.NotificationReceived -= handler;
            }
        }

        /// <summary>
        ///     Was <c>this.Clients.Caller.getNotification(e.AdditionalInformation, e.Condition)</c>.
        ///     <para>
        ///         <c>NotificationReceived</c> is a SYNCHRONOUS void event, so the send cannot be
        ///         awaited here; faults are observed and logged rather than swallowed, exactly as
        ///         the host's <c>UpdateHub.InstallerProgress</c> does.
        ///     </para>
        /// </summary>
        private void NotifyCaller(string connectionId, MixERPPGEventArgs e)
        {
            if (e == null || string.IsNullOrEmpty(connectionId))
            {
                return;
            }

            this.hubContext.Clients.Client(connectionId)
                .SendAsync(NotificationClientMethod, e.AdditionalInformation, e.Condition)
                .ContinueWith(t => Log.Warning("Could not send the end-of-day notification. {Exception}.",
                        t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        ///     Port of the private <c>IsValidRequest()</c>: the 2-second delay, the
        ///     <c>globalLoginId &gt; 0</c> check and the <c>IsAdmin</c> + <c>Elevated</c> checks,
        ///     in the original order and with the original notify-vs-silent split.
        /// </summary>
        private async Task<bool> IsValidRequestAsync()
        {
            await Task.Delay(ValidationDelay).ConfigureAwait(false);

            if (this.Context == null)
            {
                await this.NotifyAccessDeniedAsync().ConfigureAwait(false);
                return false;
            }

            long globalLoginId = this.GetGlobalLoginId();

            if (globalLoginId <= 0)
            {
                await this.NotifyAccessDeniedAsync().ConfigureAwait(false);
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.IsAdmin.ToBool())
            {
                return false;
            }

            if (!AppUsers.GetCurrent(globalLoginId).View.Elevated.ToBool())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     The original notified with a SINGLE argument here
        ///     (<c>getNotification(Warnings.AccessIsDenied)</c>) while the <c>PerformEOD</c>
        ///     refusal used two. Both are preserved as found - the client callback reads
        ///     <c>condition.toLowerCase()</c>, so the one-argument form left it undefined before
        ///     and does so now.
        /// </summary>
        private Task NotifyAccessDeniedAsync()
        {
            return this.Clients.Caller.SendAsync(NotificationClientMethod, Warnings.AccessIsDenied);
        }

        /// <summary>
        ///     The global login id of the connection's principal (the host BaseHub pattern).
        /// </summary>
        private long GetGlobalLoginId()
        {
            return Conversion.TryCastLong(this.Context == null || this.Context.User == null ||
                                          this.Context.User.Identity == null
                ? null
                : this.Context.User.Identity.Name);
        }

        /// <summary>
        ///     The catalog of the connection's principal - the hub-safe replacement of
        ///     <c>AppUsers.GetCurrentUserDB()</c>.
        /// </summary>
        private string GetCatalog()
        {
            return AppUsers.GetCurrent(this.GetGlobalLoginId()).Catalog;
        }
    }
}
