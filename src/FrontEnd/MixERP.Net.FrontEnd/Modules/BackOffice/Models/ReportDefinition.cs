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
using MixERP.Net.Framework;
using System;
using System.Collections.Generic;
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
    public sealed class ReportDefinition
    {
        private static readonly string reportContainer =
            MixERPConfiguration.MapPath(ConfigurationHelper.GetReportParameter("ReportContainer"));

        public string Title { get; set; }
        public string FileName { get; set; }
        public string MenuCode { get; set; }
        public string ParentMenuCode { get; set; }
        public string TopSection { get; set; }
        public string Body { get; set; }
        public string BottomSection { get; set; }
        public IEnumerable<Data.ReportWriter.DataSource> DataSources { get; set; }
        public IEnumerable<Data.ReportWriter.Grid> GridViews { get; set; }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(this.FileName))
            {
                throw new MixERPException("Invalid file name \"" + this.FileName + "\".");
            }

            if (string.IsNullOrWhiteSpace(reportContainer) || !Directory.Exists(reportContainer))
            {
                throw new DirectoryNotFoundException(reportContainer);
            }

            if (!this.FileName.EndsWith(".xml"))
            {
                this.FileName += ".xml";
            }

            string path = ResolveContainedPath(this.FileName);

            ReportBuilder builder = new ReportBuilder(this);

            string contents = builder.Build();

            File.WriteAllText(path, contents);
        }

        public ReportDefinition Get(string fileName)
        {
            string file = ResolveContainedPath(fileName);

            if (File.Exists(file))
            {
                ReportParser parser = new ReportParser(fileName);

                return parser.Definition;
            }

            return new ReportDefinition();
        }

        /// <summary>
        ///     Resolves a caller-supplied report file name INSIDE the report container and refuses
        ///     anything that escapes it.
        ///     <para>
        ///         The legacy code did a bare <c>Path.Combine(reportContainer, fileName)</c>. The
        ///         file name arrives from the ReportWriter service, so a value containing
        ///         <c>..</c> (or a rooted path, which <c>Path.Combine</c> would let win outright)
        ///         wrote a caller-controlled XML document anywhere the process could reach. The
        ///         four routes are now administrator-only, but the containment check is the actual
        ///         fix and matches the one the attachment endpoints use.
        ///     </para>
        /// </summary>
        private static string ResolveContainedPath(string fileName)
        {
            //Discard any directory component: a report definition has never legitimately lived in
            //a subdirectory of the container.
            string leaf = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(leaf))
            {
                throw new MixERPException("Invalid file name \"" + fileName + "\".");
            }

            string root = Path.GetFullPath(reportContainer);

            string resolved;

            try
            {
                resolved = Path.GetFullPath(Path.Combine(root, leaf));
            }
            catch (ArgumentException)
            {
                throw new MixERPException("Invalid file name \"" + fileName + "\".");
            }

            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;

            if (!resolved.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new MixERPException("Invalid file name \"" + fileName + "\".");
            }

            return resolved;
        }
    }
}
