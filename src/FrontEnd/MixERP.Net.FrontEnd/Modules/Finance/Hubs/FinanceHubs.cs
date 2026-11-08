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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace MixERP.Net.Core.Modules.Finance.Hubs
{
    /// <summary>
    ///     The single entry point the HOST calls to map this module's hub:
    ///     <code>app.MapFinanceHubs();</code>
    ///     <para>
    ///         THE ROUTE IS A JAVASCRIPT CONTRACT. <c>wwwroot/js/EODOperation.js</c> builds its
    ///         connection with <c>.withUrl("/hubs/finance/dayoperation")</c>, so this path must
    ///         not change without changing that file in the same commit.
    ///     </para>
    ///     <para>
    ///         NOTE FOR THE HOST on long-running tolerance: the end-of-day run can take minutes,
    ///         but it does NOT need a raised server timeout, because
    ///         <see cref="DayOperationHub.PerformEOD" /> never blocks the hub's dispatch thread -
    ///         it offloads the synchronous <c>Data.EODOperation.Perform</c> to a worker thread,
    ///         so the connection's keep-alive pings keep flowing and the CLIENT keeps receiving
    ///         <c>getNotification</c> messages throughout. If a future change ever makes the hub
    ///         itself wait, the PER-HUB registration to add alongside the host's existing
    ///         <c>AddHubOptions&lt;UpdateHub&gt;</c> is:
    ///     </para>
    ///     <code>
    ///         builder.Services.AddSignalR()
    ///                .AddHubOptions&lt;MixERP.Net.Core.Modules.Finance.Hubs.DayOperationHub&gt;(o =&gt;
    ///                {
    ///                    o.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
    ///                    o.KeepAliveInterval = TimeSpan.FromSeconds(15);
    ///                });
    ///     </code>
    ///     <para>
    ///         It must stay PER-HUB: setting these on the global <c>HubOptions</c> would also
    ///         apply to the Blazor circuit hub and break circuit reaping.
    ///     </para>
    /// </summary>
    public static class FinanceHubs
    {
        /// <summary>
        ///     The route <c>wwwroot/js/EODOperation.js</c> connects to.
        /// </summary>
        public const string DayOperationHubRoute = "/hubs/finance/dayoperation";

        public static IEndpointRouteBuilder MapFinanceHubs(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                return endpoints;
            }

            endpoints.MapHub<DayOperationHub>(DayOperationHubRoute);

            return endpoints;
        }
    }
}
