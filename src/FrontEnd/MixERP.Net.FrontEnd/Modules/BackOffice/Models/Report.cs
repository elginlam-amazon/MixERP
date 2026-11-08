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

using MixERP.Net.Common.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
//MIGRATION: System.Web.Hosting.HostingEnvironment.MapPath has no counterpart on ASP.NET
//Core. MixERPConfiguration.MapPath is the documented equivalent - it resolves an
//application-relative ("~/x" or "x/y") path against the CONTENT ROOT, which is exactly what
//HostingEnvironment.MapPath resolved against, and is the resolver every other MixERP path read
//now uses. Both values resolved here are content-root paths, not wwwroot ones: the report
//container comes from Resource/Configs/ReportParameters.config and License.txt sits at the
//application root, so neither is (or was) served over HTTP.

namespace MixERP.Net.Core.Modules.BackOffice.Models
{
    public sealed class Report
    {
        public string FileName { get; set; }
        public string CreatedOn { get; set; }
        public string LastAccessedOn { get; set; }
        public string LastWrittenOn { get; set; }

        public static IEnumerable<Report> GetReports()
        {
            Collection<Report> models = new Collection<Report>();

            string reportContainer =
                MixERPConfiguration.MapPath(ConfigurationHelper.GetReportParameter("ReportContainer"));

            if (reportContainer != null)
            {
                if (Directory.Exists(reportContainer))
                {
                    DirectoryInfo directory = new DirectoryInfo(reportContainer);

                    foreach (FileInfo fileInfo in directory.GetFiles("*.xml"))
                    {
                        if (fileInfo != null)
                        {
                            Report model = new Report();

                            model.FileName = fileInfo.Name;
                            model.CreatedOn = fileInfo.CreationTime.ToString(CultureInfo.CurrentCulture);
                            model.LastWrittenOn = fileInfo.LastWriteTime.ToString(CultureInfo.CurrentCulture);
                            model.LastAccessedOn = fileInfo.LastAccessTime.ToString(CultureInfo.CurrentCulture);

                            models.Add(model);
                        }
                    }
                }
            }

            return models;
        }
    }
}
