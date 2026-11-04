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

using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Common.Models;
using MixERP.Net.i18n.Resources;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using MixERP.Net.Common.Extensions;

namespace MixERP.Net.Core.Modules.BackOffice.Hubs
{
    /// <remarks>
    ///     <para>
    ///         Migrated from ASP.NET SignalR 2. ASP.NET Core SignalR dropped the dynamic client
    ///         proxy, so client methods are invoked by name with <c>SendAsync</c>, and hub methods
    ///         are asynchronous. See knowledge base doc 32.
    ///     </para>
    ///     <para>
    ///         The backup itself still drives <c>pg_dump.exe</c> through a generated <c>.bat</c>
    ///         file, which only works on Windows. That was already true of the .NET Framework
    ///         version; making the pipeline cross platform is tracked in summary.md.
    ///     </para>
    /// </remarks>
    [CLSCompliant(false)]
    public class DbHub : Hub
    {
        private string backupDirectory;
        private string batchFile;

        public async Task BackupDatabase(string fileName)
        {
            if (!await this.IsValidRequest())
            {
                await this.Clients.Caller.SendAsync("getNotification", Warnings.AccessIsDenied,
                    Warnings.AccessIsDenied);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await this.Clients.Caller.SendAsync("backupFailed", Warnings.NoFileSpecified);
                return;
            }

            PostgreSQLServer server = new PostgreSQLServer
            {
                BinDirectory = ConfigurationHelper.GetDbServerParameter("PostgreSQLBinDirectory"),
                DatabaseBackupDirectory = ConfigurationHelper.GetDbServerParameter("DatabaseBackupDirectory"),
                DatabaseName = AppUsers.GetCurrentUserDB(),
                HostName = ConfigurationHelper.GetDbServerParameter("Server"),
                PortNumber = Conversion.TryCastInteger(ConfigurationHelper.GetDbServerParameter("Port")),
                UserId = ConfigurationHelper.GetDbServerParameter("UserId"),
                Password = ConfigurationHelper.GetDbServerParameter("Password")
            };

            server.Validate();

            if (server.IsValid && !string.IsNullOrWhiteSpace(server.BinDirectory) &&
                !string.IsNullOrWhiteSpace(server.DatabaseBackupDirectory))
            {
                await this.Backup(server, fileName);
                return;
            }

            await this.Clients.Caller.SendAsync("backupFailed", Warnings.ConfigurationError);
        }

        private async Task Backup(PostgreSQLServer server, string fileName)
        {
            string pgdumpPath = Path.Combine(server.BinDirectory, "pg_dump.exe");
            this.backupDirectory = HostingEnvironment.MapPath(server.DatabaseBackupDirectory);

            if (this.backupDirectory != null)
            {
                this.backupDirectory = Path.Combine(this.backupDirectory, fileName);
                Directory.CreateDirectory(this.backupDirectory);

                string path = Path.Combine(this.backupDirectory, "db.backup");

                bool result = this.BackupDatabase(pgdumpPath, server, path);


                if (result)
                {
                    StringBuilder message = new StringBuilder();
                    message.Append(Labels.DatabaseBackupSuccessful);
                    message.Append("&nbsp;");
                    message.Append("<a href='");
                    message.Append(
                        PageUtility.ResolveUrl(Path.Combine(server.DatabaseBackupDirectory, fileName + ".zip")));
                    message.Append("'");
                    message.Append(" target='_blank'>");
                    message.Append(Labels.ClickHereToDownload);
                    message.Append("</a>");

                    await this.Clients.Caller.SendAsync("backupCompleted", message.ToString());
                    return;
                }

                await this.Clients.Caller.SendAsync("backupFailed", Warnings.CannotCreateABackup);
            }
        }

        private void CompressBackupDirectory(bool removeSource)
        {
            ZipFile.CreateFromDirectory(this.backupDirectory, this.backupDirectory + ".zip");

            if (removeSource)
            {
                Directory.Delete(this.backupDirectory, true);
            }
        }

        private bool BackupDatabase(string pgDumpPath, PostgreSQLServer server, string fileName)
        {
            this.CreateBatchFile(server, pgDumpPath, fileName);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = this.batchFile;

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.ErrorDataReceived += this.Data_Received;
                process.OutputDataReceived += this.Data_Received;
                process.Disposed += this.Completed;

                process.Start();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();


                return true;
            }
        }

        /// <remarks>
        ///     <see cref="Process.Disposed" /> is a void returning event, so this handler cannot
        ///     become asynchronous. Blocking is safe here: ASP.NET Core has no synchronization
        ///     context, so there is no deadlock risk the way there was under System.Web.
        /// </remarks>
        private void Completed(object sender, EventArgs e)
        {
            this.RemoveFile(this.batchFile);
            this.CopyResource();
            this.CompressBackupDirectory(true);
            this.Clients.Caller.SendAsync("backupCompleted", string.Empty).GetAwaiter().GetResult();
        }

        /// <remarks>
        ///     <c>Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory</c> is Windows only on
        ///     modern .NET; MixERP already ships an equivalent recursive copy in
        ///     <see cref="DirectoryHelper.CopyDirectory" />.
        /// </remarks>
        private void CopyResource()
        {
            string source = ConfigurationHelper.GetResourceDirectory();
            string destination = Path.Combine(this.backupDirectory, new DirectoryInfo(source).Name);

            DirectoryHelper.CopyDirectory(source, destination);
        }

        private void CreateBatchFile(PostgreSQLServer server, string pgDumpPath, string fileName)
        {
            Collection<string> commands = new Collection<string>();
            commands.Add("@echo off");
            commands.Add("SET PGPASSWORD=" + server.Password);
            string command =
                @"""{0}"" --host ""{1}"" --port {2} --username ""{3}"" --format custom --blobs --verbose --file ""{4}"" ""{5}""";
            command = string.Format(CultureInfo.InvariantCulture, command, pgDumpPath, server.HostName,
                server.PortNumber, server.UserId, fileName, server.DatabaseName);
            commands.Add(command);
            commands.Add("exit");

            this.batchFile = fileName + ".bat";

            File.WriteAllText(this.batchFile, string.Join(Environment.NewLine, commands));
        }

        /// <remarks>
        ///     <see cref="Process.OutputDataReceived" /> is a void returning event; see
        ///     <see cref="Completed" /> for why blocking on the send is safe.
        /// </remarks>
        private void Data_Received(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                this.Clients.Caller.SendAsync("getNotification", e.Data).GetAwaiter().GetResult();
            }
        }

        private void RemoveFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch
            {
                Log.Warning("Could not delete file: {FileName}.", fileName);
            }
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

    }
}
