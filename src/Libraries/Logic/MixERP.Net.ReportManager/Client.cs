using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MixERP.Net.ReportManager
{
    /// <summary>
    ///     Reads the report catalog from the report API and downloads individual report
    ///     definitions.
    ///     <para>
    ///         The transport moved off <c>Microsoft.AspNet.WebApi.Client</c>
    ///         (<c>ReadAsAsync&lt;dynamic&gt;</c>) and <see cref="System.Net.WebClient" />, neither
    ///         of which exists on modern .NET, onto <see cref="HttpClient" /> plus
    ///         <c>Newtonsoft.Json.Linq.JToken</c> — which keeps the string-indexer
    ///         <c>dynamic</c> semantics <see cref="Parser.Parse" /> depends on. This mirrors
    ///         <c>MixERP.Net.Updater/Api/Releases.cs</c>.
    ///     </para>
    /// </summary>
    internal class Client
    {
        //A process-lifetime HttpClient avoids the socket exhaustion of "new HttpClient()" per
        //call, while PooledConnectionLifetime keeps DNS fresh for a long-running host.
        //The 100 second timeout is the default both the framework HttpClient and WebClient
        //used, so a slow report API behaves exactly as it did before the migration.
        private static readonly HttpClient SharedClient = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
        {
            Timeout = TimeSpan.FromSeconds(100)
        };

        private readonly HttpClient client;

        internal Client(string url)
            : this(url, null)
        {
        }

        /// <summary>
        ///     Uses a caller supplied client, e.g. one obtained from
        ///     <c>IHttpClientFactory.CreateClient()</c> in a DI enabled host.
        /// </summary>
        internal Client(string url, HttpClient client)
        {
            this.Url = url;
            this.client = client ?? SharedClient;
        }

        internal string Url { get; set; }

        internal Task<IEnumerable<Report>> FetchReports()
        {
            return this.FetchReports(CancellationToken.None);
        }

        internal async Task<IEnumerable<Report>> FetchReports(CancellationToken cancellationToken)
        {
            try
            {
                using (HttpRequestMessage request = this.CreateRequest(this.Url))
                {
                    using (HttpResponseMessage response = await this.client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                            dynamic content = JToken.Parse(json);

                            Parser parser = new Parser(content);
                            IEnumerable<Report> reports = parser.Parse();

                            return reports;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                //Swallow
            }

            return null;
        }

        internal Task<string> GetContent()
        {
            return this.GetContent(CancellationToken.None);
        }

        internal async Task<string> GetContent(CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = this.CreateRequest(this.Url))
            {
                using (HttpResponseMessage response = await this.client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    Console.WriteLine("{0}", content);
                    return content;
                }
            }
        }

        /// <summary>
        ///     Builds the request with per-request headers rather than DefaultRequestHeaders: the
        ///     client instance is shared, so mutating its defaults would race with concurrent
        ///     callers. The header set is the same one the framework code sent.
        /// </summary>
        private HttpRequestMessage CreateRequest(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Accept.Clear();

            if (!string.IsNullOrWhiteSpace(Config.MediaType))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Config.MediaType));
            }

            if (!string.IsNullOrWhiteSpace(Config.UserAgent))
            {
                request.Headers.Add("User-Agent", Config.UserAgent);
            }

            return request;
        }
    }
}
