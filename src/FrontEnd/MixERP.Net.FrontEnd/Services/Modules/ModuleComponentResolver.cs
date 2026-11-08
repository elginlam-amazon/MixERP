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
using System.Linq;
using MixERP.Net.Framework;
using MixERP.Net.Framework.Controls;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Modules
{
    /// <summary>
    ///     Replacement for the two <c>LoadControl</c> call sites of the host (checklist tasks 85 and
    ///     87):
    ///     <list type="bullet">
    ///         <item>
    ///             <c>Dashboard/Index.aspx.cs</c>: <c>page.LoadControl(item.WidgetSource)</c> where
    ///             <c>widget_source</c> holds a virtual path such as
    ///             <c>/Modules/Sales/Widgets/SalesByGeographyWidget.ascx</c>.
    ///         </item>
    ///         <item>
    ///             <c>Modules/Default.aspx.cs</c>:
    ///             <c>LoadControl("~/Modules/" + path + ".ascx")</c> guarded by
    ///             <c>File.Exists(Server.MapPath(path))</c>.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         There is no <c>.ascx</c> file and no virtual-path provider on modern .NET: a control is
    ///         a COMPONENT TYPE, rendered through <c>DynamicComponent</c>. The virtual path is
    ///         therefore resolved to a type over
    ///         <see cref="DiscoveryAssemblies" /> — the registered discovery set that replaced
    ///         <c>AppDomain.CurrentDomain.GetAssemblies()</c> (Specialist 3's note; every referenced
    ///         assembly plus everything in the application's base directory, which is where the eight
    ///         <c>Modules/*</c> assemblies live).
    ///     </para>
    ///     <para>
    ///         The RESOLUTION RULE mirrors what the virtual path meant: the file name is the control's
    ///         type name and the directories are its namespace segments. A type whose namespace
    ///         matches the directory segments wins; a unique type-name match is accepted otherwise
    ///         (the module assemblies' namespaces are not strictly path-shaped). Resolving to nothing
    ///         is NOT an error — the legacy <c>File.Exists</c> guard simply rendered nothing — but it
    ///         IS logged, so a missing module cannot fail silently.
    ///     </para>
    /// </summary>
    public static class ModuleComponentResolver
    {
        /// <summary>
        ///     The component type of a <c>core.widgets.widget_source</c> virtual path, or null.
        /// </summary>
        public static Type ResolveWidget(string widgetSource)
        {
            return Resolve(widgetSource, typeof(MixERPWidget), "widget");
        }

        /// <summary>
        ///     The component type of a <c>/Modules/{*path}</c> route value (the <c>.mix</c> extension
        ///     already stripped by the caller, exactly as <c>Modules/Default.aspx.cs</c> did), or null.
        /// </summary>
        public static Type ResolveModule(string path)
        {
            return Resolve(path, typeof(MixERPUserControlBase), "module control");
        }

        private static Type Resolve(string virtualPath, Type baseType, string kind)
        {
            string[] segments = GetSegments(virtualPath);

            if (segments.Length == 0)
            {
                return null;
            }

            string typeName = segments[segments.Length - 1];

            List<Type> candidates = DiscoveryAssemblies.GetTypes()
                .Where(t => t != null && !t.IsAbstract && t.IsClass && baseType.IsAssignableFrom(t) &&
                            string.Equals(t.Name, typeName, StringComparison.Ordinal))
                .ToList();

            if (candidates.Count == 0)
            {
                Log.Warning("No {Kind} type named {TypeName} was found for the path {Path}.", kind,
                    typeName, virtualPath);
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            //More than one assembly declares this name: the directory segments disambiguate, as they
            //did when they were a real path.
            string[] namespaceSegments = segments.Take(segments.Length - 1).ToArray();

            Type matched = candidates.FirstOrDefault(t => MatchesNamespace(t, namespaceSegments));

            if (matched != null)
            {
                return matched;
            }

            Log.Warning(
                "{Count} {Kind} types are named {TypeName} and none of them matches the path {Path}; nothing is rendered.",
                candidates.Count, kind, typeName, virtualPath);

            //Deliberately NOT candidates[0]. The original resolved an EXACT virtual path
            //(LoadControl + File.Exists), so a path that identifies no single control rendered
            //nothing. Picking an arbitrary same-named control from a different module would render
            //the WRONG screen — strictly worse than rendering nothing.
            return null;
        }

        /// <summary>
        ///     The directory segments of the former virtual path must match the TRAILING namespace
        ///     segments of the candidate type, segment for segment. A substring test would let
        ///     unrelated namespaces match (e.g. the segment "Sales" matching
        ///     <c>...Modules.Purchase.SalesReturn</c>).
        /// </summary>
        private static bool MatchesNamespace(Type type, IList<string> namespaceSegments)
        {
            if (namespaceSegments == null || namespaceSegments.Count == 0)
            {
                return false;
            }

            string[] typeSegments = (type.Namespace ?? string.Empty)
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (typeSegments.Length < namespaceSegments.Count)
            {
                return false;
            }

            int offset = typeSegments.Length - namespaceSegments.Count;

            for (int i = 0; i < namespaceSegments.Count; i++)
            {
                if (!string.Equals(typeSegments[offset + i], namespaceSegments[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Turns <c>~/Modules/Sales/Widgets/SalesByGeographyWidget.ascx</c>,
        ///     <c>/Modules/Sales/Widgets/SalesByGeographyWidget.ascx</c> and
        ///     <c>Sales/Widgets/SalesByGeographyWidget</c> into the same segment list.
        /// </summary>
        private static string[] GetSegments(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                return new string[0];
            }

            string path = virtualPath.Trim().Replace('\\', '/').TrimStart('~');

            if (path.EndsWith(".ascx", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 5);
            }

            if (path.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.Length - 4);
            }

            return path.Split('/')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) &&
                            !string.Equals(s, "Modules", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
