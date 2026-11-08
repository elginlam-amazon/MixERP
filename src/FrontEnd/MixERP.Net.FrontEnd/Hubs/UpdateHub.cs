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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using MixERP.Net.Updater;
using MixERP.Net.Updater.Api;
using MixERP.Net.Updater.Installation;
using Serilog;

namespace MixERP.Net.FrontEnd.Hubs
{
    /// <remarks>
    ///     Migrated from ASP.NET SignalR 2. ASP.NET Core SignalR dropped the dynamic client proxy,
    ///     so client methods are invoked by name with <c>SendAsync</c>, and hub methods are
    ///     asynchronous. See knowledge base doc 32.
    /// </remarks>
    [CLSCompliant(false)]
    public class UpdateHub : Hub
    {
        public async Task InstallUpdates()
        {
            if (!await this.IsValidRequest())
            {
                await this.Notify("Cannot Update", Warnings.AccessIsDenied);
                return;
            }

            string downloadUrl = string.Empty;
            string[] toBackup =
            {
                DbConfig.GetAttachmentParameter(AppUsers.GetCurrentUserDB(), "AttachmentsDirectory"),
                ConfigurationHelper.GetDbServerParameter("DatabaseBackupDirectory")
            };

            Release release = new Release();

            try
            {
                UpdateManager updater = new UpdateManager();
                Task task = Task.Run(async () => { release = await updater.GetLatestRelease(); });
                task.Wait();
            }
            catch
            {
                await this.Notify("Cannot Update", "Cannot start the update because no release was found.");
                return;
            }


            string keyword = Config.UpdateKeyword;

            Asset ass = release.Assets.FirstOrDefault(a => a.Name.ToUpperInvariant().Contains(keyword.ToUpperInvariant()));

            if (ass != null)
            {
                downloadUrl = ass.DownloadUrl;
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                await this.Notify("Cannot Update",
                    "Cannot start the update because no download was found on the release.");
                return;
            }

            await this.InstallUpdate(downloadUrl, toBackup);
        }

        private async Task<bool> IsValidRequest()
        {
            await Task.Delay(2000);

            if (this.Context == null)
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied);
                return false;
            }

            long globalLoginId = Conversion.TryCastLong(this.Context.User?.Identity?.Name);

            if (globalLoginId <= 0)
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied);
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

        private async Task InstallUpdate(string downloadUrl, string[] toBackup)
        {
            UpdateInstaller installer = new UpdateInstaller(downloadUrl, toBackup);
            installer.Progress += this.installer_Progress;


            await this.Notify("Update Started", "The update process has started.");
            try
            {
                installer.Update();
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred. {Exception}.", ex);
                await this.Notify(Titles.UnknownError, ex.Message);
            }
        }

        private Task Notify(string description, string message)
        {
            return this.Clients.All.SendAsync("updateInstallationNotification", DateTime.UtcNow, description, message);
        }

        /// <remarks>
        ///     <see cref="UpdateInstaller.UpdateProgressHandler" /> is a void returning delegate, so this
        ///     handler cannot become asynchronous. Blocking here is safe: ASP.NET Core has no
        ///     synchronization context, so there is no deadlock risk the way there was under
        ///     System.Web.
        /// </remarks>
        private void installer_Progress(ProgressInfo progressInfo)
        {
            this.Clients.All.SendAsync("updateInstallationNotification", progressInfo.Timestamp,
                progressInfo.Description, progressInfo.Message).GetAwaiter().GetResult();
        }
    }
}