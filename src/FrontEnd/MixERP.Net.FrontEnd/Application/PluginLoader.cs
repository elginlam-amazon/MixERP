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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     ASP.NET loaded every assembly in the bin folder. The modules (MixERP.Net.Core.Modules.*), the
    ///     first steps checklists and the currency converters are not referenced by the front end, they are
    ///     only dropped next to it, so they are loaded explicitly at startup.
    /// </summary>
    internal static class PluginLoader
    {
        internal static void LoadAll(string directory)
        {
            // Dependencies of the plugins that are not part of the front end's deps.json are probed in the same folder.
            AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
            {
                string path = Path.Combine(directory, assemblyName.Name + ".dll");
                return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
            };

            string[] loaded = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToArray();

            foreach (string file in Directory.EnumerateFiles(directory, "MixERP.Net.*.dll"))
            {
                if (loaded.Contains(Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                Assembly.Load(AssemblyLoadContext.GetAssemblyName(file));
            }
        }
    }
}
