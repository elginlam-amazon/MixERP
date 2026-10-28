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
using System.Runtime.Loader;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    /// The core modules (MixERP.Net.Core.Modules.*) reference this web application, so they cannot be project
    /// references of it. As on .NET Framework (where they were built into the site's bin folder), their assemblies
    /// are discovered at startup from the application directory.
    /// </summary>
    internal static class ModuleLoader
    {
        private const string ModuleAssemblyPattern = "MixERP.Net.Core.Modules.*.dll";

        internal static IReadOnlyList<Assembly> LoadModules()
        {
            string baseDirectory = AppContext.BaseDirectory;

            // Module assemblies (and their *.Data dependencies) are not part of the host's deps.json.
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string candidate = Path.Combine(baseDirectory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };

            List<Assembly> modules = new List<Assembly>();

            foreach (string path in Directory.EnumerateFiles(baseDirectory, ModuleAssemblyPattern))
            {
                try
                {
                    AssemblyName name = AssemblyName.GetAssemblyName(path);
                    Assembly loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name));
                    modules.Add(loaded ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(path));
                }
                catch (BadImageFormatException ex)
                {
                    Log.Warning("Could not load module assembly {Path}. {Exception}", path, ex);
                }
            }

            return modules;
        }

        internal static Type FindType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(type => type != null);
        }

        internal static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }
    }
}
