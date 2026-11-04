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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Reads the "Class" attribute of the &lt;%@ WebService %&gt; (.asmx) and &lt;%@ WebHandler %&gt; (.ashx)
    ///     directives, so the existing files keep declaring which type serves which URL.
    /// </summary>
    internal static class WebFormsDirectives
    {
        private static readonly Regex ClassAttribute = new Regex(@"<%@\s*Web(Service|Handler)\b[^>]*\bClass\s*=\s*""(?<class>[^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        ///     Returns the application-relative URL ("/Services/User.asmx") and the declared type of every
        ///     file with the given extension below the content root.
        /// </summary>
        internal static IEnumerable<KeyValuePair<string, Type>> Find(string contentRoot, string extension)
        {
            foreach (string file in Directory.EnumerateFiles(contentRoot, "*" + extension, SearchOption.AllDirectories))
            {
                string relative = "/" + Path.GetRelativePath(contentRoot, file).Replace('\\', '/');

                if (relative.StartsWith("/bin/", StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith("/obj/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Match match = ClassAttribute.Match(File.ReadAllText(file));

                if (!match.Success)
                {
                    continue;
                }

                string typeName = match.Groups["class"].Value.Trim();
                Type type = ResolveType(typeName);

                if (type == null)
                {
                    Log.Warning("Could not find the type {Type} declared by {Path}.", typeName, relative);
                    continue;
                }

                yield return new KeyValuePair<string, Type>(relative, type);
            }
        }

        internal static Type ResolveType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.GetType(typeName, false))
                .FirstOrDefault(t => t != null);
        }
    }
}
