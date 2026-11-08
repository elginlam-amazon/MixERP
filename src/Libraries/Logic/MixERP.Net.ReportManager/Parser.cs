using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MixERP.Net.ReportManager
{
    internal class Parser
    {
        internal Parser(dynamic content)
        {
            this.Content = content;
        }

        internal dynamic Content { get; set; }

        internal IEnumerable<Report> Parse()
        {
            Collection<Report> reports = new Collection<Report>();
            foreach (dynamic item in this.Content)
            {
                //The content is now a Newtonsoft.Json.Linq.JToken (see Client.FetchReports).
                //The casts are explicit because JToken exposes string conversion as an explicit
                //operator, which the dynamic binder does not apply on a plain assignment.
                Report report = new Report
                {
                    FileName = (string)item[Config.FileNameKey],
                    DownloadUrl = (string)item[Config.DownloadUrlKey]
                };
                reports.Add(report);
            }

            return reports;
        }
    }
}