using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace MixERP.Net.ReportManager
{
    internal class Client
    {
        internal Client(string url)
        {
            this.Url = url;
        }

        internal string Url { get; set; }

        internal async Task<IEnumerable<Report>> FetchReports()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Config.MediaType));
                    client.DefaultRequestHeaders.Add("User-Agent", Config.UserAgent);

                    HttpResponseMessage response = await client.GetAsync(this.Url);

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic content = response.Content.ReadAsAsync<dynamic>();

                        Parser parser = new Parser(content.Result);
                        IEnumerable<Report> reports = parser.Parse();

                        return reports;
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

        /// <remarks>
        ///     WebClient is obsolete (SYSLIB0014). HttpClient has no synchronous string download, so
        ///     the asynchronous call is awaited inline; this member is called from synchronous code
        ///     and ASP.NET Core has no synchronization context to deadlock against.
        /// </remarks>
        internal string GetContent()
        {
            using (HttpClient client = new HttpClient())
            {
                string content = client.GetStringAsync(this.Url).GetAwaiter().GetResult();
                Console.WriteLine("{0}", content);
                return content;
            }
        }
    }
}