using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MixERP.Net.ReportManager
{
    public class Repository
    {
        /// <summary>
        ///     Downloads every report published by the report API, installs the definition on
        ///     disk and registers its menu entry in each configured catalog.
        ///     <para>
        ///         Was <c>async void</c>: changed to <c>async Task</c> so faults surface to the
        ///         caller instead of crashing the process on an unobserved exception. The sole
        ///         call site invokes it without assignment, so this stays source-compatible.
        ///     </para>
        ///     <para>
        ///         NOTE for the MixERP.Net.FrontEnd port: the current unassigned call in
        ///         <c>Global.asax.cs</c> leaves the returned task unobserved, so a failure here is
        ///         no longer surfaced. When the FrontEnd moves to ASP.NET Core this call belongs in
        ///         an <c>IHostedService.StartAsync</c> (awaited), or at minimum needs a faulted
        ///         continuation that logs the exception.
        ///     </para>
        /// </summary>
        public static async Task DownloadAndInstallReports()
        {
            Client client = new Client(Config.ApiUrl);
            IEnumerable<Report> reports = await client.FetchReports().ConfigureAwait(false);
            Collection<ReportMenu> menus = new Collection<ReportMenu>();

            if (reports == null)
            {
                return;
            }

            foreach (Report report in reports)
            {
                client.Url = report.DownloadUrl;
                string content = await client.GetContent().ConfigureAwait(false);

                MenuParser parser = new MenuParser(content, report.FileName);
                menus.Add(parser.Parse());

                Serializer serializer = new Serializer(report.FileName, content);
                serializer.Serialize();
            }

            DataLayer.AddMenus(menus);
        }
    }
}
