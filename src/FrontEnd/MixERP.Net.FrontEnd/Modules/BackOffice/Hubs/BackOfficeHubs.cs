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

namespace MixERP.Net.Core.Modules.BackOffice.Hubs
{
    /// <summary>
    ///     The single entry point the HOST calls to map this module's hub:
    ///     <code>app.MapBackOfficeHubs();</code>
    ///     <para>
    ///         THE ROUTE IS A JAVASCRIPT CONTRACT. <c>wwwroot/js/admin/DatabaseBackup.ascx.js</c>
    ///         declares <c>var dbHubRoute = "/hubs/backoffice/db";</c> and builds its connection
    ///         with <c>.withUrl(dbHubRoute)</c>, so this path must not change without changing
    ///         that file in the same commit.
    ///     </para>
    ///     <para>
    ///         ⚠ NOTE FOR THE HOST on long-running tolerance - THIS HUB DOES NEED IT, unlike
    ///         Finance's. A full <c>pg_dump</c> of a production catalog, plus the
    ///         <c>Resource/</c> copy and the zip, routinely exceeds the default 30-second
    ///         <c>ClientTimeoutInterval</c>, and although <see cref="DbHub.BackupDatabase" />
    ///         never blocks its dispatch thread (it awaits
    ///         <c>Process.WaitForExitAsync</c>), <c>ZipFile.CreateFromDirectory</c> and the
    ///         recursive resource copy inside <c>CompletedAsync</c> ARE synchronous and can hold
    ///         the invocation for a long time on a large install. Register the tolerance
    ///         PER HUB, alongside the host's existing <c>AddHubOptions&lt;UpdateHub&gt;</c>:
    ///     </para>
    ///     <code>
    ///         builder.Services.AddSignalR()
    ///                .AddHubOptions&lt;MixERP.Net.Core.Modules.BackOffice.Hubs.DbHub&gt;(o =&gt;
    ///                {
    ///                    o.ClientTimeoutInterval = TimeSpan.FromMinutes(30);
    ///                    o.KeepAliveInterval = TimeSpan.FromSeconds(15);
    ///                });
    ///     </code>
    ///     <para>
    ///         It MUST stay PER-HUB. Setting these on the global <c>HubOptions</c> would also
    ///         apply to Blazor Server's own component hub and break circuit reaping - the exact
    ///         regression the host's post-review fix F3 removed.
    ///     </para>
    /// </summary>
    public static class BackOfficeHubs
    {
        /// <summary>
        ///     The route <c>wwwroot/js/admin/DatabaseBackup.ascx.js</c> connects to.
        /// </summary>
        public const string DbHubRoute = "/hubs/backoffice/db";

        public static IEndpointRouteBuilder MapBackOfficeHubs(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                return endpoints;
            }

            endpoints.MapHub<DbHub>(DbHubRoute);

            return endpoints;
        }
    }
}
