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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.BackOffice.Models;
using Newtonsoft.Json;
using Report = MixERP.Net.Core.Modules.BackOffice.Models.Report;

using MixERP.Net.Framework.Security;

namespace MixERP.Net.Core.Modules.BackOffice.Endpoints
{
    /// <summary>
    ///     The minimal-API replacement for the deleted
    ///     <c>Services/Admin/ReportWriter.asmx</c> <c>[ScriptService]</c> web service, called by
    ///     <c>wwwroot/js/admin/ReportWriter.ascx..js</c> (the double dot is the real file name).
    ///     <para>
    ///         THIS IS WHERE THE REPORT-WRITER SCREEN'S FUNCTIONALITY LIVES. The screen itself
    ///         is static markup plus that controller; every server interaction it has is one of
    ///         the four methods below (<c>GetTable</c>, <c>SaveReport</c>, <c>GetReports</c>,
    ///         <c>GetDefinition</c>). Skipping any of them is what would genuinely leave the
    ///         screen "not implemented" - see <c>Modules/migration-summary.md</c> §5 and the
    ///         ported <c>Admin/ReportWriter.razor</c>.
    ///     </para>
    ///     <para>
    ///         ⚠ <c>GetTable</c> EXECUTES USER-SUPPLIED SQL
    ///         (<c>Data.ReportWriter.GetTable(catalog, sql, parameters)</c>) and
    ///         <c>SaveReport</c> WRITES A REPORT DEFINITION FILE TO DISK. In the legacy app the
    ///         only thing gating them was the <c>Admin/ReportWriter.ascx</c> control's
    ///         <c>AccessLevel.AdminOnly</c>, i.e. the SCREEN; the ASMX itself was reachable by
    ///         any signed-in user, so arbitrary SQL could be POSTed straight at the service.
    ///         The independent review of this pass ruled that carrying that hole forward
    ///         unchanged was faithful but must not ship, so all four routes now carry
    ///         <see cref="MixERPPolicies.AdminOnly" /> — the SAME rule
    ///         <c>IMixERPAuthorization</c> applies for <c>AccessLevel.AdminOnly</c> on the
    ///         control (<c>AppUsers.GetCurrent(globalLoginId).View.IsAdmin</c>), registered as a
    ///         policy in the host's <c>Program.cs</c>. The service audience is therefore now
    ///         exactly the screen's audience: administrators, as the designer always intended.
    ///         The admin-only gate ALSO stays on the component, so neither surface relies on the
    ///         other.
    ///     </para>
    ///     <para>
    ///         All four former <c>[WebMethod]</c> bodies are carried over VERBATIM, including:
    ///         <list type="bullet">
    ///             <item>
    ///                 <c>GetTable</c> serializing the <c>DataTable</c> with a bare
    ///                 <c>JsonSerializer</c> into a STRING, which the envelope then nests - so
    ///                 <c>msg.d</c> is a JSON string the caller <c>eval</c>s. That double
    ///                 encoding is the client contract (<c>var result = eval(msg.d);</c>) and is
    ///                 preserved.
    ///             </item>
    ///             <item>
    ///                 <c>SaveReport</c>'s four <c>ArgumentNullException</c> guards, which the
    ///                 original raised with the VALUE as the parameter name (an upstream
    ///                 oddity - the message therefore reads "Value cannot be null. (Parameter
    ///                 '&lt;the blank value&gt;')"). Preserved as found; the caller only
    ///                 <c>alert</c>s <c>xhr.responseText</c>.
    ///             </item>
    ///             <item><c>SaveReport</c> returning the literal <c>"OK"</c>.</item>
    ///         </list>
    ///     </para>
    /// </summary>
    public static class BackOfficeReportWriterEndpoints
    {
        private const string GetTableRoute = "/services/backoffice/admin/reportwriter/gettable";
        private const string SaveReportRoute = "/services/backoffice/admin/reportwriter/savereport";
        private const string GetReportsRoute = "/services/backoffice/admin/reportwriter/getreports";
        private const string GetDefinitionRoute = "/services/backoffice/admin/reportwriter/getdefinition";

        public static IEndpointRouteBuilder MapBackOfficeReportWriterService(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods(GetTableRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetTable(
                        parameters.GetString("sql"), parameters.GetString("parameters")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization(MixERPPolicies.AdminOnly);

            endpoints.MapMethods(SaveReportRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(SaveReport(
                        parameters.GetString("title"),
                        parameters.GetString("fileName"),
                        parameters.GetString("menuCode"),
                        parameters.GetString("parentMenuCode"),
                        parameters.GetString("topSection"),
                        parameters.GetString("body"),
                        parameters.GetString("bottomSection"),
                        parameters.GetString("dataSources"),
                        parameters.GetString("gridViews")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization(MixERPPolicies.AdminOnly);

            endpoints.MapMethods(GetReportsRoute, AsmxCompatibility.Verbs, (HttpContext context) =>
            {
                try
                {
                    return AsmxCompatibility.Envelope(GetReports());
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization(MixERPPolicies.AdminOnly);

            endpoints.MapMethods(GetDefinitionRoute, AsmxCompatibility.Verbs, async (HttpContext context) =>
            {
                try
                {
                    AsmxParameters parameters = await AsmxCompatibility.ReadParametersAsync(context)
                        .ConfigureAwait(false);

                    return AsmxCompatibility.Envelope(GetDefinition(parameters.GetString("fileName")));
                }
                catch (Exception ex)
                {
                    return AsmxCompatibility.Error(ex);
                }
            })
                .DisableAntiforgery()
                .RequireAuthorization(MixERPPolicies.AdminOnly);

            return endpoints;
        }

        //-------------------------------------------------------------------------------
        // The four former [WebMethod] bodies, carried over verbatim.
        //
        // AppUsers.GetCurrentUserDB() is KEPT here on purpose: a minimal-API endpoint has a
        // live HttpContext, which is the one place it resolves correctly.
        //-------------------------------------------------------------------------------

        private static string GetTable(string sql, string parameters)
        {
            Collection<Data.ReportWriter.ReportParameter> parameterCollection =
                JsonConvert.DeserializeObject<Collection<Data.ReportWriter.ReportParameter>>(parameters);

            JsonSerializer serializer = new JsonSerializer();
            TextWriter writer = new StringWriter();

            using (DataTable table = Data.ReportWriter.GetTable(AppUsers.GetCurrentUserDB(), sql,
                parameterCollection))
            {
                serializer.Serialize(writer, table);
                return writer.ToString();
            }
        }

        private static string SaveReport(string title, string fileName, string menuCode, string parentMenuCode,
            string topSection, string body, string bottomSection, string dataSources, string gridViews)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentNullException(title);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(fileName);
            }

            if (string.IsNullOrWhiteSpace(menuCode))
            {
                throw new ArgumentNullException(menuCode);
            }

            if (string.IsNullOrWhiteSpace(parentMenuCode))
            {
                throw new ArgumentNullException(parentMenuCode);
            }

            IEnumerable<Data.ReportWriter.DataSource> dataSourceCollection =
                JsonConvert.DeserializeObject<IEnumerable<Data.ReportWriter.DataSource>>(dataSources);
            IEnumerable<Data.ReportWriter.Grid> gridViewCollection =
                JsonConvert.DeserializeObject<IEnumerable<Data.ReportWriter.Grid>>(gridViews);

            SaveReport(title, fileName, menuCode, parentMenuCode, topSection, body, bottomSection,
                dataSourceCollection, gridViewCollection);

            return "OK";
        }

        private static void SaveReport(string title, string fileName, string menuCode, string parentMenuCode,
            string topSection, string body, string bottomSection,
            IEnumerable<Data.ReportWriter.DataSource> dataSources,
            IEnumerable<Data.ReportWriter.Grid> gridViews)
        {
            ReportDefinition definition = new ReportDefinition
            {
                Title = title,
                FileName = fileName,
                MenuCode = menuCode,
                ParentMenuCode = parentMenuCode,
                TopSection = topSection,
                Body = body,
                BottomSection = bottomSection,
                DataSources = dataSources,
                GridViews = gridViews
            };

            definition.Save();
        }

        private static IEnumerable<Report> GetReports()
        {
            return Report.GetReports();
        }

        private static ReportDefinition GetDefinition(string fileName)
        {
            ReportDefinition definition = new ReportDefinition();
            return definition.Get(fileName);
        }
    }
}
