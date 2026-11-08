using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MixERP.Net.Common;
using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;

namespace MixERP.Net.Updater.Installation.Tasks
{
    internal class DownloadUpdate : UpdateTask
    {
        private const int BufferSize = 81920;

        public DownloadUpdate(UpdaterConfig config)
            : base(config)
        {
        }

        public override string Description
        {
            get { return Titles.DownloadingUpdate; }
        }

        public override void Run()
        {
            string temp = PageUtility.MapPath(this.Config.TempPath);
            string downloadFrom = this.Config.DownloadUrl;
            this.Download(temp, downloadFrom, this.Config.GetDownloadDestination());
        }

        private void Download(string temp, string downloadFrom, string destination)
        {
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }

            string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.DownloadingUpdateFrom, downloadFrom);

            this.OnProgress(new ProgressInfo(this.Description, message));

            Task task = Task.Run(async () => { await this.DownloadTask(downloadFrom, destination); });
            task.Wait();
        }

        /// <remarks>
        ///     WebClient, along with its DownloadProgressChanged and DownloadFileCompleted events, is
        ///     obsolete (SYSLIB0014). HttpClient has no progress event, so the response body is copied
        ///     in chunks and the progress is reported from the copy loop. The previous implementation
        ///     also returned the download task from inside the <c>using</c> block, which disposed the
        ///     client while the download was still running; the stream is now consumed before the
        ///     client is released.
        /// </remarks>
        private async Task DownloadTask(string downloadFrom, string destination)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    //The update packages are large, so do not buffer the whole response in memory.
                    using (HttpResponseMessage response = await client.GetAsync(new Uri(downloadFrom), HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        long? total = response.Content.Headers.ContentLength;

                        using (Stream source = await response.Content.ReadAsStreamAsync())
                        {
                            using (FileStream target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                            {
                                await this.Copy(source, target, total);
                            }
                        }
                    }
                }

                this.OnProgress(new ProgressInfo(this.Description, Labels.DownloadSuccessful));
            }
            catch (Exception ex)
            {
                this.OnProgress(new ProgressInfo(this.Description, ex.Message));
                throw;
            }
        }

        private async Task Copy(Stream source, Stream target, long? total)
        {
            byte[] buffer = new byte[BufferSize];
            long downloaded = 0;
            int lastReported = -1;
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await target.WriteAsync(buffer, 0, read);
                downloaded += read;

                if (total == null || total.Value <= 0)
                {
                    continue;
                }

                int percentage = (int)(downloaded * 100 / total.Value);

                //Report only when the whole percentage changes, the way WebClient used to.
                if (percentage == lastReported)
                {
                    continue;
                }

                lastReported = percentage;

                string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.PercentCompleted, percentage);
                this.OnProgress(new ProgressInfo(this.Description, message));
            }
        }
    }
}