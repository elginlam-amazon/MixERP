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
        private static readonly HttpClient Client = new HttpClient();

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

        private async Task DownloadTask(string downloadFrom, string destination)
        {
            // WebClient (SYSLIB0014) replaced by HttpClient; progress and completion are reported as before.
            try
            {
                using (HttpResponseMessage response = await Client.GetAsync(new Uri(downloadFrom), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    long? total = response.Content.Headers.ContentLength;

                    using (Stream source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (FileStream target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                    {
                        byte[] buffer = new byte[BufferSize];
                        long received = 0;
                        int lastPercentage = -1;
                        int read;

                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            await target.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                            received += read;

                            if (total > 0)
                            {
                                int percentage = (int)(received * 100 / total.Value);

                                if (percentage != lastPercentage)
                                {
                                    lastPercentage = percentage;
                                    this.OnDownloadProgress(percentage);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is TaskCanceledException)
            {
                this.OnDownloadComplete(ex);
                throw;
            }

            this.OnDownloadComplete(null);
        }

        private void OnDownloadComplete(Exception error)
        {
            string message = Labels.DownloadSuccessful;

            if (error != null)
            {
                message = error.Message;
            }

            this.OnProgress(new ProgressInfo(this.Description, message));
        }

        private void OnDownloadProgress(int progressPercentage)
        {
            string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.PercentCompleted, progressPercentage);

            this.OnProgress(new ProgressInfo(this.Description, message));
        }
    }
}