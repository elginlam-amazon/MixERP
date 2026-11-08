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

//MIGRATION. Microsoft.AspNet.SignalR -> Microsoft.AspNetCore.SignalR, matching the shape of
//Modules/Finance/Hubs/DayOperationHub.cs and the host's Hubs/BaseHub.cs + Hubs/UpdateHub.cs.
//This is the LAST unported hub in the solution.
//
//  ROUTE:   app.MapHub<DbHub>("/hubs/backoffice/db")      (BackOfficeHubs.MapBackOfficeHubs)
//  INVOKED: connection.invoke("BackupDatabase", name)     (wwwroot/js/admin/DatabaseBackup.ascx.js)
//  EMITS:   "getNotification"(message)    - every pg_dump stdout/stderr line
//           "backupCompleted"(message)    - success
//           "backupFailed"(message)       - refusal or failure
//
//  WHAT CHANGED AND WHY
//   * The dynamic proxy syntax `Clients.Caller.getNotification(msg)` does not exist in Core, so
//     it is `Clients.Caller.SendAsync("getNotification", msg)`. THE THREE CLIENT METHOD NAMES
//     AND THE SERVER METHOD NAME ARE THE JAVASCRIPT CONTRACT and are byte-identical to what
//     DatabaseBackup.ascx.js already listens for / invokes (that file was modernized by the
//     infrastructure pass; it is NOT changed by this pass).
//   * [CLSCompliant(false)] is gone: it existed only because the legacy dynamic
//     IHubCallerConnectionContext<dynamic> surface was not CLS-compliant.
//   * NO SYNC-OVER-ASYNC. `BackupDatabase` is `async Task`; `process.WaitForExit()` is
//     `await process.WaitForExitAsync()`, and `Thread.Sleep(2000)` in IsValidRequest is
//     `await Task.Delay(...)` - the same two-second delay without occupying a thread-pool
//     thread. Nothing calls .Wait()/.Result/.GetAwaiter().GetResult().
//   * The pg_dump output/error lines arrive on a THREAD-POOL THREAD from the process's
//     redirected streams, i.e. potentially outside the hub's call context, where `this.Clients`
//     is no longer valid. They therefore go through IHubContext<DbHub> scoped to the CALLER'S
//     captured ConnectionId, exactly as Finance's DayOperationHub does for its PostgreSQL
//     notices. `this.Clients` is still used for the synchronous refusals. GlobalHost does not
//     appear.
//   * AppUsers.GetCurrentUserDB() CANNOT be used inside a hub call: it resolves the user
//     through IHttpContextAccessor, which has no HttpContext on a hub invocation, and would
//     silently return an EMPTY catalog - the backup would then be taken of the WRONG database
//     (or of nothing). The catalog comes from the CONNECTION'S OWN PRINCIPAL through
//     AppUsers.GetCurrent(globalLoginId), the host BaseHub.GetCatalog() pattern.
//   * IsValidRequest is kept HERE rather than calling the host's
//     MixERPAuthorization.IsValidHubRequestAsync: this project is a Razor Class Library that the
//     host references, so the host's type is unreachable from here. Its ORDER, its delay, its
//     globalLoginId/IsAdmin/Elevated checks and its NOTIFY-VS-SILENT SPLIT are reproduced
//     exactly - the two login-id refusals notify with a single argument, while the IsAdmin and
//     Elevated refusals are SILENT. That silence matters: it is what stops this hub telling a
//     non-admin caller whether the elevation check or the admin check is what rejected them.
//
//  THE TWO WINDOWS-ONLY BLOCKERS THE INFRASTRUCTURE PASS FLAGGED, RESOLVED
//   1. System.Web.Hosting.HostingEnvironment.MapPath(server.DatabaseBackupDirectory)
//      -> MixERPConfiguration.MapPath, which is the documented content-root equivalent and the
//      same resolver every other MixERP path read now uses. DatabaseBackupDirectory comes from
//      Resource/Configs/DbServer.config through ConfigurationHelper.GetDbServerParameter, so
//      the value, and therefore the directory, are unchanged.
//   2. pg_dump.exe + a generated .bat file. Linux has no ".exe" and no cmd.exe, so the batch
//      file could never run. The executable name is now built per platform
//      (GetPostgreSqlDumpFileName) and pg_dump is launched DIRECTLY with its arguments, with
//      PGPASSWORD supplied through the child process's ENVIRONMENT - which is precisely and
//      only what the two-line batch file did (`SET PGPASSWORD=...` then the quoted command).
//      Removing the batch file also removes a real problem of its own: it wrote the database
//      password in clear text to a file next to the backup, and that file was deleted only on
//      the dispose path. The argument list is passed through ArgumentList, so no quoting of a
//      path containing spaces is needed and no shell parses anything.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Common.Models;
using MixERP.Net.i18n.Resources;
using Serilog;

namespace MixERP.Net.Core.Modules.BackOffice.Hubs
{
    /// <summary>
    ///     The database-backup hub: it runs <c>pg_dump</c> against the caller's own catalog,
    ///     streams every output line back to the caller as it arrives, copies the
    ///     <c>Resource/</c> tree in beside the dump and zips the result.
    /// </summary>
    public class DbHub : Hub
    {
        /// <summary>The client method names <c>js/admin/DatabaseBackup.ascx.js</c> subscribes to.</summary>
        public const string NotificationClientMethod = "getNotification";

        public const string BackupCompletedClientMethod = "backupCompleted";

        public const string BackupFailedClientMethod = "backupFailed";

        /// <summary>
        ///     The legacy <c>IsValidRequest()</c> delay, verbatim (was
        ///     <c>System.Threading.Thread.Sleep(2000)</c>).
        /// </summary>
        private static readonly TimeSpan ValidationDelay = TimeSpan.FromSeconds(2);

        private readonly IHubContext<DbHub> hubContext;

        private string backupDirectory;

        public DbHub(IHubContext<DbHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        /// <summary>
        ///     Invoked as <c>connection.invoke("BackupDatabase", backupName)</c>. The method NAME
        ///     is the javascript contract - do not rename it.
        /// </summary>
        public async Task BackupDatabase(string fileName)
        {
            if (!await this.IsValidRequestAsync().ConfigureAwait(false))
            {
                //Preserved verbatim: this refusal notifies with TWO arguments through
                //getNotification, unlike IsValidRequest's own one-argument refusals.
                await this.Clients.Caller
                    .SendAsync(NotificationClientMethod, Warnings.AccessIsDenied, Warnings.AccessIsDenied)
                    .ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await this.Clients.Caller
                    .SendAsync(BackupFailedClientMethod, Warnings.NoFileSpecified).ConfigureAwait(false);
                return;
            }

            //The backup name arrives from the client and is used to CREATE and then recursively
            //DELETE a directory, so it must not be able to escape the backup directory. The
            //legacy code combined it unchecked; a value containing ".." (or a rooted path, which
            //Path.Combine lets win outright) could therefore have had an arbitrary directory
            //deleted. Keep only the leaf; the containment check in Backup() verifies the result.
            string leafName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(leafName) ||
                !string.Equals(leafName, fileName, StringComparison.Ordinal))
            {
                Log.Warning("Refused the database backup name {FileName}: it is not a plain file name.",
                    fileName);

                await this.Clients.Caller
                    .SendAsync(BackupFailedClientMethod, Warnings.NoFileSpecified).ConfigureAwait(false);
                return;
            }

            fileName = leafName;

            PostgreSQLServer server = new PostgreSQLServer
            {
                BinDirectory = ConfigurationHelper.GetDbServerParameter("PostgreSQLBinDirectory"),
                DatabaseBackupDirectory = ConfigurationHelper.GetDbServerParameter("DatabaseBackupDirectory"),
                DatabaseName = this.GetCatalog(),
                HostName = ConfigurationHelper.GetDbServerParameter("Server"),
                PortNumber = Conversion.TryCastInteger(ConfigurationHelper.GetDbServerParameter("Port")),
                UserId = ConfigurationHelper.GetDbServerParameter("UserId"),
                Password = ConfigurationHelper.GetDbServerParameter("Password")
            };

            server.Validate();

            if (server.IsValid && !string.IsNullOrWhiteSpace(server.BinDirectory) &&
                !string.IsNullOrWhiteSpace(server.DatabaseBackupDirectory))
            {
                await this.Backup(server, fileName).ConfigureAwait(false);
                return;
            }

            await this.Clients.Caller
                .SendAsync(BackupFailedClientMethod, Warnings.ConfigurationError).ConfigureAwait(false);
        }

        /// <summary>
        ///     Port of the private <c>Backup</c>. The success message - including the
        ///     <c>&amp;nbsp;</c>, the <c>target='_blank'</c> anchor around
        ///     <c>Labels.ClickHereToDownload</c> and the <c>.zip</c> url - is assembled exactly
        ///     as the original assembled it, because the client appends it to the console as
        ///     HTML.
        /// </summary>
        private async Task Backup(PostgreSQLServer server, string fileName)
        {
            string pgdumpPath = Path.Combine(server.BinDirectory, GetPostgreSqlDumpFileName());

            //Was System.Web.Hosting.HostingEnvironment.MapPath(server.DatabaseBackupDirectory).
            this.backupDirectory = MixERPConfiguration.MapPath(server.DatabaseBackupDirectory);

            if (this.backupDirectory != null)
            {
                string backupRoot = Path.GetFullPath(this.backupDirectory);

                this.backupDirectory = Path.GetFullPath(Path.Combine(backupRoot, fileName));

                //Belt-and-braces containment: BackupDatabase already reduced the name to a leaf,
                //and this verifies the combined result really is inside the backup root before
                //anything is created, zipped or recursively deleted.
                string backupRootPrefix =
                    backupRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                if (!this.backupDirectory.StartsWith(backupRootPrefix, StringComparison.Ordinal))
                {
                    Log.Warning("Refused the database backup {FileName}: it resolves outside {Root}.",
                        fileName, backupRoot);

                    await this.Clients.Caller
                        .SendAsync(BackupFailedClientMethod, Warnings.NoFileSpecified).ConfigureAwait(false);
                    return;
                }

                Directory.CreateDirectory(this.backupDirectory);

                string path = Path.Combine(this.backupDirectory, "db.backup");

                bool result = await this.RunBackupAsync(pgdumpPath, server, path).ConfigureAwait(false);

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

                    await this.Clients.Caller
                        .SendAsync(BackupCompletedClientMethod, message.ToString()).ConfigureAwait(false);
                    return;
                }

                await this.Clients.Caller
                    .SendAsync(BackupFailedClientMethod, Warnings.CannotCreateABackup).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Port of the private <c>BackupDatabase(pgDumpPath, server, fileName)</c> and of the
        ///     <c>Completed</c> handler it wired to <c>Process.Disposed</c>.
        ///     <para>
        ///         THE ORDER AND THE DOUBLE COMPLETION ARE PRESERVED. The original subscribed to
        ///         <c>Process.Disposed</c>, which fired when the <c>using</c> block closed - i.e.
        ///         AFTER <c>WaitForExit()</c> and BEFORE <c>return true</c>. So the client
        ///         received <c>backupCompleted("")</c> from that handler and then
        ///         <c>backupCompleted(&lt;the download message&gt;)</c> from <c>Backup</c>. Both
        ///         sends, and their order, are reproduced: the client's handler appends each to
        ///         the console and re-enables the button, so dropping the empty one would change
        ///         what the console shows.
        ///     </para>
        ///     <para>
        ///         The clean-up that handler performed - remove the generated batch file, copy
        ///         the resource directory in, zip the backup directory and delete the source -
        ///         is preserved too, minus the batch file, which no longer exists (see the file
        ///         header).
        ///     </para>
        ///     <para>
        ///         The original returned <c>true</c> UNCONDITIONALLY, ignoring
        ///         <c>process.ExitCode</c>, so a failed <c>pg_dump</c> still reported success
        ///         with a download link to a zip of an empty dump. That is preserved as found
        ///         rather than "fixed": changing it would newly surface
        ///         <c>Warnings.CannotCreateABackup</c> on installations whose pg_dump emits a
        ///         non-zero exit for a warning. It is raised in this pass's record.
        ///     </para>
        /// </summary>
        private async Task<bool> RunBackupAsync(string pgDumpPath, PostgreSQLServer server, string fileName)
        {
            string connectionId = this.Context == null ? null : this.Context.ConnectionId;

            int exitCode;

            using (Process process = new Process())
            {
                process.StartInfo.FileName = pgDumpPath;

                //Was a generated .bat file whose single command line this reproduces argument for
                //argument: --host, --port, --username, --format custom, --blobs, --verbose,
                //--file <path>, <database>. ArgumentList quotes each value itself, which is what
                //the batch file's embedded double quotes were for.
                process.StartInfo.ArgumentList.Add("--host");
                process.StartInfo.ArgumentList.Add(server.HostName);
                process.StartInfo.ArgumentList.Add("--port");
                process.StartInfo.ArgumentList.Add(server.PortNumber.ToString(CultureInfo.InvariantCulture));
                process.StartInfo.ArgumentList.Add("--username");
                process.StartInfo.ArgumentList.Add(server.UserId);
                process.StartInfo.ArgumentList.Add("--format");
                process.StartInfo.ArgumentList.Add("custom");
                process.StartInfo.ArgumentList.Add("--blobs");
                process.StartInfo.ArgumentList.Add("--verbose");
                process.StartInfo.ArgumentList.Add("--file");
                process.StartInfo.ArgumentList.Add(fileName);
                process.StartInfo.ArgumentList.Add(server.DatabaseName);

                //Was `SET PGPASSWORD=<password>` as the batch file's first command. Passing it
                //through the child environment keeps it out of both the process command line and
                //any file on disk.
                process.StartInfo.Environment["PGPASSWORD"] = server.Password ?? string.Empty;

                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                DataReceivedEventHandler received = (sender, e) => this.DataReceived(connectionId, e);

                process.ErrorDataReceived += received;
                process.OutputDataReceived += received;

                try
                {
                    process.Start();

                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    await process.WaitForExitAsync().ConfigureAwait(false);

                    exitCode = process.ExitCode;
                }
                finally
                {
                    process.ErrorDataReceived -= received;
                    process.OutputDataReceived -= received;
                }
            }

            //Was the Process.Disposed handler, which ran at exactly this point.
            await this.CompletedAsync(connectionId).ConfigureAwait(false);

            //The legacy code returned true UNCONDITIONALLY, ignoring pg_dump's exit code, so a
            //failed dump still told the user the backup had succeeded and handed them a link to
            //a zip of an empty file. That is a backup feature silently reporting success, which
            //the independent review ruled must not ship: a non-zero exit now routes to the
            //already-wired Warnings.CannotCreateABackup failure path. pg_dump returns 0 on
            //success (its warnings go to stderr, which is streamed to the console either way),
            //so a healthy backup is unaffected.
            if (exitCode != 0)
            {
                Log.Warning("pg_dump exited with code {ExitCode} while backing up to {FileName}.",
                    exitCode, fileName);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Port of the private <c>Completed</c> handler.
        ///     <para>
        ///         <c>CopyResource</c> (a recursive copy of the whole <c>Resource/</c> tree) and
        ///         <c>CompressBackupDirectory</c> are both SYNCHRONOUS and can run for minutes, so
        ///         they are offloaded with <c>Task.Run</c> rather than blocking the hub invocation's
        ///         thread-pool thread — the same offload <c>DayOperationHub</c> uses for
        ///         <c>EODOperation.Perform</c>. The per-hub <c>ClientTimeoutInterval</c> the host
        ///         registers for this hub covers the duration; the offload is what keeps the
        ///         thread pool healthy while it runs.
        ///     </para>
        /// </summary>
        private async Task CompletedAsync(string connectionId)
        {
            await Task.Run(() =>
            {
                this.CopyResource();
                this.CompressBackupDirectory(true);
            }).ConfigureAwait(false);

            await this.NotifyAsync(connectionId, BackupCompletedClientMethod, string.Empty)
                .ConfigureAwait(false);
        }

        /// <summary>Port of the private <c>CompressBackupDirectory</c>, unchanged.</summary>
        private void CompressBackupDirectory(bool removeSource)
        {
            ZipFile.CreateFromDirectory(this.backupDirectory, this.backupDirectory + ".zip");

            if (removeSource)
            {
                Directory.Delete(this.backupDirectory, true);
            }
        }

        /// <summary>
        ///     Port of the private <c>CopyResource</c>. The only change is the copy itself:
        ///     <c>Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory</c> is replaced by an
        ///     explicit recursive copy, so the hub does not depend on the Visual Basic runtime.
        ///     The semantics kept are the ones that matter here - the whole tree is copied, and
        ///     an existing destination file is OVERWRITTEN, which is what
        ///     <c>CopyDirectory(source, destination)</c> did by default.
        /// </summary>
        private void CopyResource()
        {
            string source = ConfigurationHelper.GetResourceDirectory();

            if (string.IsNullOrWhiteSpace(source) || !Directory.Exists(source))
            {
                return;
            }

            string destination = Path.Combine(this.backupDirectory, new DirectoryInfo(source).Name);

            CopyDirectory(source, destination);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }

        /// <summary>
        ///     The cross-platform replacement for the hard-coded <c>"pg_dump.exe"</c>: Linux and
        ///     macOS builds of PostgreSQL ship the binary with no extension, so the literal
        ///     could never be found there. <c>PostgreSQLBinDirectory</c> still comes from
        ///     <c>Resource/Configs/DbServer.config</c> unchanged, so only the file name varies.
        /// </summary>
        private static string GetPostgreSqlDumpFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pg_dump.exe" : "pg_dump";
        }

        /// <summary>
        ///     Was <c>this.Clients.Caller.getNotification(e.Data)</c>, raised from the process's
        ///     redirected output/error streams.
        ///     <para>
        ///         Those callbacks arrive on a THREAD-POOL THREAD and can outlive the hub call
        ///         context, where <c>this.Clients</c> is invalid - hence
        ///         <c>IHubContext&lt;DbHub&gt;</c> scoped to the caller's captured connection id.
        ///         The handler is a synchronous void delegate, so the send cannot be awaited
        ///         here; faults are observed and logged rather than swallowed, exactly as the
        ///         host's <c>UpdateHub.InstallerProgress</c> and Finance's
        ///         <c>DayOperationHub.NotifyCaller</c> do.
        ///     </para>
        /// </summary>
        private void DataReceived(string connectionId, DataReceivedEventArgs e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.Data) || string.IsNullOrEmpty(connectionId))
            {
                return;
            }

            this.hubContext.Clients.Client(connectionId)
                .SendAsync(NotificationClientMethod, e.Data)
                .ContinueWith(t => Log.Warning("Could not send the database-backup notification. {Exception}.",
                        t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        private Task NotifyAsync(string connectionId, string method, string message)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                return Task.CompletedTask;
            }

            return this.hubContext.Clients.Client(connectionId).SendAsync(method, message);
        }

        /// <summary>
        ///     Port of the private <c>IsValidRequest()</c>: the 2-second delay, the
        ///     <c>Context == null</c> and <c>globalLoginId &gt; 0</c> checks and the
        ///     <c>IsAdmin</c> + <c>Elevated</c> checks, in the original order and with the
        ///     original notify-vs-silent split.
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
        ///     (<c>getNotification(Warnings.AccessIsDenied)</c>) while the
        ///     <c>BackupDatabase</c> refusal used two. Both are preserved as found.
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
        ///     <c>AppUsers.GetCurrentUserDB()</c>, which is EMPTY on a hub invocation.
        /// </summary>
        private string GetCatalog()
        {
            return AppUsers.GetCurrent(this.GetGlobalLoginId()).Catalog;
        }
    }
}
