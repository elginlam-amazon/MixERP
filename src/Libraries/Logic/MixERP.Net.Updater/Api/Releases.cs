using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.Updater.Api
{
    /// <summary>
    ///     Reads release metadata from the GitHub releases API.
    ///     <para>
    ///         The release-metadata transport moved off <c>Microsoft.AspNet.WebApi.Client</c>
    ///         (<c>ReadAsAsync&lt;dynamic&gt;</c>, which has no modern equivalent) onto
    ///         <see cref="HttpClient" /> + <c>Newtonsoft.Json.Linq.JToken</c>, which keeps the
    ///         string-indexer <c>dynamic</c> semantics <see cref="Release.Parse" /> and
    ///         <see cref="Asset.Parse" /> depend on.
    ///     </para>
    /// </summary>
    public class Releases
    {
        //A process-lifetime HttpClient avoids the socket exhaustion of "new HttpClient()" per
        //call, while PooledConnectionLifetime keeps DNS fresh for a long-running host.
        private static readonly HttpClient SharedClient = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        private readonly HttpClient client;

        /// <summary>
        ///     Uses the shared process-wide client.
        /// </summary>
        public Releases()
            : this(null)
        {
        }

        /// <summary>
        ///     Uses a caller supplied client, e.g. one obtained from
        ///     <c>IHttpClientFactory.CreateClient()</c> in a DI enabled host.
        /// </summary>
        public Releases(HttpClient client)
        {
            this.client = client ?? SharedClient;
        }

        /// <summary>
        ///     Fetches every published release. Returns null when the request failed or the
        ///     endpoint answered with a non-success status code (unchanged failure contract).
        /// </summary>
        public static Task<IEnumerable<Release>> GetReleases()
        {
            return new Releases().GetReleasesAsync(CancellationToken.None);
        }

        public Task<IEnumerable<Release>> GetReleasesAsync()
        {
            return this.GetReleasesAsync(CancellationToken.None);
        }

        public async Task<IEnumerable<Release>> GetReleasesAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Config.ApiUrl))
                {
                    //Per-request headers rather than DefaultRequestHeaders: the client instance is
                    //shared, so mutating its defaults would race with concurrent callers.
                    request.Headers.Accept.Clear();

                    if (!string.IsNullOrWhiteSpace(Config.MediaType))
                    {
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Config.MediaType));
                    }

                    //GitHub rejects requests without a User-Agent.
                    if (!string.IsNullOrWhiteSpace(Config.UserAgent))
                    {
                        request.Headers.Add("User-Agent", Config.UserAgent);
                    }

                    using (HttpResponseMessage response = await this.client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                            dynamic content = JToken.Parse(json);

                            List<Release> releases = new List<Release>();

                            foreach (dynamic release in content)
                            {
                                releases.Add(Release.Parse(release, Config.AssetsKey));
                            }

                            return releases;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not read the release metadata from {ApiUrl}.", Config.ApiUrl);
            }

            return null;
        }
    }
}
