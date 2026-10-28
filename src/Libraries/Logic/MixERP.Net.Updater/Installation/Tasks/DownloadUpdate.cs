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
            // HttpClient replaces the obsolete WebClient; progress is still reported as a percentage.
            try
            {
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(new Uri(downloadFrom), HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (Stream source = await response.Content.ReadAsStreamAsync())
                    using (FileStream target = File.Create(destination))
                    {
                        byte[] buffer = new byte[81920];
                        long receivedBytes = 0;
                        int lastPercentage = -1;
                        int read;

                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await target.WriteAsync(buffer, 0, read);
                            receivedBytes += read;

                            if (totalBytes > 0)
                            {
                                int percentage = (int)(receivedBytes * 100 / totalBytes.Value);

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
                this.OnProgress(new ProgressInfo(this.Description, ex.Message));
                throw;
            }

            this.OnProgress(new ProgressInfo(this.Description, Labels.DownloadSuccessful));
        }

        private void OnDownloadProgress(int progressPercentage)
        {
            string message = string.Format(CultureInfo.DefaultThreadCurrentCulture, Labels.PercentCompleted, progressPercentage);

            this.OnProgress(new ProgressInfo(this.Description, message));
        }
    }
}