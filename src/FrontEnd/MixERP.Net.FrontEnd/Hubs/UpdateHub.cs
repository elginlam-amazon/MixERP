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

//MIGRATION (checklist tasks 101, 102). Microsoft.AspNet.SignalR -> Microsoft.AspNetCore.SignalR.
//
//  ROUTE:   app.MapHub<UpdateHub>("/hubs/update")   (Program.cs)
//  INVOKED: connection.invoke("InstallUpdates")     (wwwroot/js/Pages/Modules/Update.js)
//  EMITS:   "updateInstallationNotification"(timestamp, description, message) to ALL clients
//           "getNotification"(message) to the CALLER (the authorization refusal)
//           "terminate"(counter, catalog) through BaseHub (eod.js)
//
//  WHAT CHANGED AND WHY
//   * `InstallUpdates` is `async Task`: the legacy `Task.Run(async () => ...).Wait()`
//     sync-over-async around GetLatestRelease() is now a plain `await`.
//   * IsValidRequest() (Thread.Sleep(2000) + globalLoginId/IsAdmin/Elevated + the
//     notify-vs-silent split) moved to Specialist 4's
//     MixERPAuthorization.IsValidHubRequestAsync(principal, notifyCaller) - same order, same
//     2-second delay (now Task.Delay), same notifications.
//   * AppUsers.GetCurrentUserDB() -> BaseHub.GetCatalog(): a hub invocation has no HttpContext,
//     so the HttpContext-based accessor would hand back an EMPTY catalog and the backup path
//     pair would silently lose the attachments directory.
//   * UpdateInstaller.Update() is SYNCHRONOUS and raises Progress on the calling thread. It runs
//     on a worker thread (Task.Run + await) so the hub's dispatch thread is never blocked - this
//     is what keeps the connection's keep-alive pings flowing through a multi-minute install and
//     is the hub-side half of the former
//     `<location path="Modules/Update.aspx"><httpRuntime executionTimeout="40000" />`
//     (the page-side half is Update.razor's WaitAsync(TimeSpan.FromSeconds(40000))).
//   * Because the Progress event can therefore fire OFF the hub's call context (and after the
//     invocation has been unwound), the progress broadcast goes through IHubContext<UpdateHub>
//     (task 102) rather than through `this.Clients`, which is only valid for the lifetime of the
//     hub method. `this.Clients` is still used for the synchronous pre-flight notifications.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.FrontEnd.Services.Authentication;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using MixERP.Net.Updater;
using MixERP.Net.Updater.Api;
using MixERP.Net.Updater.Installation;
using Serilog;

namespace MixERP.Net.FrontEnd.Hubs
{
    public sealed class UpdateHub : BaseHub
    {
        /// <summary>
        ///     The former Modules/Update.aspx executionTimeout="40000" (seconds), applied to the
        ///     release lookup and to the installer run, which is what that location element
        ///     actually protected.
        /// </summary>
        private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(40000);

        private readonly IHubContext<UpdateHub> hubContext;

        public UpdateHub(IHubContext<UpdateHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task InstallUpdates()
        {
            bool authorized = await MixERPAuthorization.IsValidHubRequestAsync(this.Context?.User,
                () => this.NotifyCallerAsync(Warnings.AccessIsDenied)).ConfigureAwait(false);

            if (!authorized)
            {
                await this.NotifyAsync("Cannot Update", Warnings.AccessIsDenied).ConfigureAwait(false);
                return;
            }

            string downloadUrl = string.Empty;

            string[] toBackup =
            {
                DbConfig.GetAttachmentParameter(this.GetCatalog(), "AttachmentsDirectory"),
                ConfigurationHelper.GetDbServerParameter("DatabaseBackupDirectory")
            };

            Release release = new Release();

            try
            {
                UpdateManager updater = new UpdateManager();
                release = await updater.GetLatestRelease().WaitAsync(UpdateTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning("Could not find a release to update to. {Exception}.", ex);
                await this.NotifyAsync("Cannot Update",
                    "Cannot start the update because no release was found.").ConfigureAwait(false);
                return;
            }

            string keyword = Config.UpdateKeyword;

            Asset ass = release?.Assets?.FirstOrDefault(a =>
                a.Name.ToUpperInvariant().Contains(keyword.ToUpperInvariant()));

            if (ass != null)
            {
                downloadUrl = ass.DownloadUrl;
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                await this.NotifyAsync("Cannot Update",
                    "Cannot start the update because no download was found on the release.").ConfigureAwait(false);
                return;
            }

            await this.InstallUpdateAsync(downloadUrl, toBackup).ConfigureAwait(false);
        }

        private async Task InstallUpdateAsync(string downloadUrl, string[] toBackup)
        {
            UpdateInstaller installer = new UpdateInstaller(downloadUrl, toBackup);
            installer.Progress += this.InstallerProgress;

            await this.NotifyAsync("Update Started", "The update process has started.").ConfigureAwait(false);

            try
            {
                //UpdateInstaller.Update() is synchronous (download, backup, extract, restore). It
                //runs off the hub's dispatch thread so the connection stays alive, and is awaited
                //so nothing is fire-and-forget.
                await Task.Run(() => installer.Update()).WaitAsync(UpdateTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred. {Exception}.", ex);
                await this.NotifyAsync(Titles.UnknownError, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                installer.Progress -= this.InstallerProgress;
            }
        }

        /// <summary>
        ///     Was <c>this.Clients.All.updateInstallationNotification(...)</c>. Broadcast through
        ///     IHubContext so the SAME code path works whether the caller is still inside the hub
        ///     invocation or is the installer's progress event on a worker thread.
        /// </summary>
        private Task NotifyAsync(string description, string message)
        {
            return this.BroadcastAsync(DateTime.UtcNow, description, message);
        }

        private Task BroadcastAsync(DateTime timestamp, string description, string message)
        {
            return this.hubContext.Clients.All.SendAsync(UpdateInstallationClientMethod, timestamp,
                description, message);
        }

        /// <summary>
        ///     UpdateInstaller.Progress is a SYNCHRONOUS void delegate, so the broadcast cannot be
        ///     awaited here. Faults are observed and logged instead of being swallowed.
        /// </summary>
        private void InstallerProgress(ProgressInfo progressInfo)
        {
            if (progressInfo == null)
            {
                return;
            }

            this.BroadcastAsync(progressInfo.Timestamp, progressInfo.Description, progressInfo.Message)
                .ContinueWith(t => Log.Warning("Could not broadcast the update progress. {Exception}.",
                        t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
