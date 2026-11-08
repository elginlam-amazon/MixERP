using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MixERP.Net.Common;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;

namespace MixERP.Net.Updater.Installation.Tasks
{
    internal class DownloadUpdate : UpdateTask
    {
        //A process-lifetime client avoids the socket exhaustion of a per-call HttpClient;
        //PooledConnectionLifetime keeps DNS fresh for a long-running host.
        private static readonly HttpClient SharedClient = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
        {
            //An update archive can be large and the body is streamed, so no overall timeout.
            Timeout = Timeout.InfiniteTimeSpan
        };

        public DownloadUpdate(UpdaterConfig config)
            : base(config)
        {
        }

        public override string Description
        {
            get { return Titles.DownloadingUpdate; }
        }

        /// <summary>
        ///     Synchronous pipeline entry point (<c>IUpdateTask.Run</c>). Uses the synchronous
        ///     <see cref="HttpClient.Send(HttpRequestMessage, HttpCompletionOption)" /> API, so
        ///     there is no sync-over-async blocking and no deadlock risk.
        /// </summary>
        public override void Run()
        {
            string temp = PageUtility.MapPath(this.Config.TempPath);
            string downloadFrom = this.Config.DownloadUrl;
            this.Download(temp, downloadFrom, this.Config.GetDownloadDestination());
        }

        /// <summary>
        ///     Asynchronous entry point, for hosts that can await the update pipeline.
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            string temp = PageUtility.MapPath(this.Config.TempPath);
            string downloadFrom = this.Config.DownloadUrl;
            string destination = this.Config.GetDownloadDestination();

            this.BeginDownload(temp, downloadFrom);

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(downloadFrom)))
                using (HttpResponseMessage response = await SharedClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    long? total = response.Content.Headers.ContentLength;

                    using (Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (FileStream target = CreateTargetFile(destination))
                    {
                        byte[] buffer = new byte[81920];
                        long received = 0;
                        int lastReportedPercentage = -1;
                        int read;

                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            await target.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                            received += read;
                            this.ReportProgress(received, total, ref lastReportedPercentage);
                        }

                        await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                this.OnDownloadComplete(null);
            }
            catch (Exception ex) when (IsDownloadFailure(ex))
            {
                //Report the failure (progress parity), discard the partial archive so a retry
                //cannot resume onto a corrupt file, then rethrow: the original task.Wait()
                //propagated the failure and aborted UpdateInstaller.Update() BEFORE
                //RemoveApplication/CopyUpdate ran. Swallowing it here would delete the live
                //application and copy a truncated update over it.
                this.OnDownloadComplete(ex);
                DiscardPartialDownload(destination);
                throw;
            }
        }

        private void Download(string temp, string downloadFrom, string destination)
        {
            this.BeginDownload(temp, downloadFrom);

            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(downloadFrom)))
                using (HttpResponseMessage response = SharedClient.Send(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? total = response.Content.Headers.ContentLength;

                    using (Stream source = response.Content.ReadAsStream())
                    using (FileStream target = CreateTargetFile(destination))
                    {
                        byte[] buffer = new byte[81920];
                        long received = 0;
                        int lastReportedPercentage = -1;
                        int read;

                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            target.Write(buffer, 0, read);
                            received += read;
                            this.ReportProgress(received, total, ref lastReportedPercentage);
                        }

                        target.Flush();
                    }
                }

                this.OnDownloadComplete(null);
            }
            catch (Exception ex) when (IsDownloadFailure(ex))
            {
                //See RunAsync: the failure must abort the update pipeline, not be downgraded
                //to a progress message.
                this.OnDownloadComplete(ex);
                DiscardPartialDownload(destination);
                throw;
            }
        }

        private static void DiscardPartialDownload(string destination)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(destination) && File.Exists(destination))
                {
                    File.Delete(destination);
                }
            }
            catch (IOException)
            {
                //Best effort: the original failure is the one that matters and is rethrown.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void BeginDownload(string temp, string downloadFrom)
        {
            if (!string.IsNullOrWhiteSpace(temp) && !Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }

            string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.DownloadingUpdateFrom, downloadFrom);

            this.OnProgress(new ProgressInfo(this.Description, message));
        }

        private static FileStream CreateTargetFile(string destination)
        {
            return new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        }

        private static bool IsDownloadFailure(Exception ex)
        {
            return ex is HttpRequestException
                   || ex is IOException
                   || ex is OperationCanceledException
                   || ex is UriFormatException
                   || ex is UnauthorizedAccessException;
        }

        /// <summary>
        ///     Reports one message per whole percent completed, matching the granularity of the
        ///     former <c>WebClient.DownloadProgressChanged</c> event.
        /// </summary>
        private void ReportProgress(long received, long? total, ref int lastReportedPercentage)
        {
            if (!total.HasValue || total.Value <= 0)
            {
                return;
            }

            int percentage = (int)(received * 100 / total.Value);

            if (percentage == lastReportedPercentage)
            {
                return;
            }

            lastReportedPercentage = percentage;
            this.OnDownloadProgress(percentage);
        }

        private void OnDownloadComplete(Exception error)
        {
            //The original read e.Error.Message whenever the operation was cancelled OR faulted,
            //which threw a NullReferenceException on cancellation (Error is null then). The
            //cancellation exception now carries the message itself, so there is no null deref.
            string message = error == null ? Labels.DownloadSuccessful : error.Message;

            this.OnProgress(new ProgressInfo(this.Description, message));
        }

        private void OnDownloadProgress(int progressPercentage)
        {
            string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.PercentCompleted, progressPercentage);

            this.OnProgress(new ProgressInfo(this.Description, message));
        }
    }
}
