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
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Port of <c>Application/Routes.RegisterRoutes(RouteCollection)</c>.
    ///     <para>
    ///         WebForms needed an imperative <c>RouteTable</c> because a url and a physical
    ///         <c>.aspx</c> file were two different things. In Blazor a routable component
    ///         DECLARES its own url with <c>@page</c>, so the three
    ///         <c>routes.MapPageRoute(...)</c> calls become <c>@page</c> directives on the
    ///         migrated pages. The three route templates are declared here as the single source
    ///         of truth, and <see cref="Verify" /> asserts at startup that a routable component
    ///         actually claims each one — otherwise the route silently 404s, which is exactly the
    ///         failure an imperative registration used to make impossible.
    ///     </para>
    ///     <para>
    ///         <c>routes.Ignore("{resource}.axd")</c> is DROPPED and has no equivalent:
    ///         <c>.axd</c> handlers (<c>ScriptResource.axd</c>, <c>WebResource.axd</c>) do not
    ///         exist on ASP.NET Core, so there is no handler path to exclude from routing. Their
    ///         replacement is the <c>JSUtility.WebResourceRoutePrefix</c> endpoint
    ///         (checklist task 41).
    ///     </para>
    /// </summary>
    internal static class Routes
    {
        /// <summary>Legacy "DefaultRoute": <c>""</c> → <c>~/SignIn.aspx</c>.</summary>
        internal const string DefaultRoute = "/";

        /// <summary>Legacy "Reporting": <c>Reports/{path}</c> → <c>~/Reports/ReportMaster.aspx</c>.</summary>
        internal const string ReportingRoute = "/Reports/{path}";

        /// <summary>Legacy "Modules": <c>Modules/{*path}</c> → <c>~/Modules/Default.aspx</c>.</summary>
        internal const string ModulesRoute = "/Modules/{*path}";

        /// <summary>
        ///     Logs the route set (the original logged "Registering routes.") and warns about any
        ///     template that no routable component in <paramref name="appAssembly" /> claims.
        /// </summary>
        internal static void Verify(Assembly appAssembly)
        {
            Log.Information("Registering routes.");

            if (appAssembly == null)
            {
                return;
            }

            string[] declared;

            try
            {
                declared = appAssembly.GetTypes()
                    .Where(t => typeof(IComponent).IsAssignableFrom(t))
                    .SelectMany(t => t.GetCustomAttributes<RouteAttribute>(false))
                    .Select(a => a.Template)
                    .ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Warning("Could not enumerate routable components. {Exception}.", ex);
                return;
            }

            foreach (string template in new[] { DefaultRoute, ReportingRoute, ModulesRoute })
            {
                bool claimed = declared.Any(d =>
                    string.Equals(Normalize(d), Normalize(template), StringComparison.OrdinalIgnoreCase));

                if (!claimed)
                {
                    Log.Warning(
                        "No routable component declares the legacy route template {Template}; requests to it will return 404.",
                        template);
                }
            }
        }

        /// <summary>
        ///     Compares templates ignoring route-parameter names and constraints, so
        ///     <c>/Reports/{path}</c> matches <c>/Reports/{path:...}</c>.
        /// </summary>
        private static string Normalize(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return string.Empty;
            }

            string value = template.Trim();

            if (!value.StartsWith("/", StringComparison.Ordinal))
            {
                value = "/" + value;
            }

            int constraint = value.IndexOf(':');

            while (constraint > -1)
            {
                int close = value.IndexOf('}', constraint);

                if (close < 0)
                {
                    break;
                }

                value = value.Remove(constraint, close - constraint);
                constraint = value.IndexOf(':');
            }

            return value.TrimEnd('/') == string.Empty ? "/" : value.TrimEnd('/');
        }
    }
}
