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

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     MixERP modules (MixERP.Net.Core.Modules.*) are not referenced by the front end; they are built into the
    ///     application folder and discovered at runtime, exactly like the legacy bin-folder plug-in model.
    /// </summary>
    internal static class ModuleLoader
    {
        private const string ModuleAssemblyPattern = "MixERP.Net.Core.Modules.*.dll";

        internal static IReadOnlyList<Assembly> LoadModules()
        {
            // Modules depend on MixERP libraries the front end itself does not reference; resolve them from the
            // application folder like the legacy bin-folder probing did.
            AssemblyLoadContext.Default.Resolving += ResolveFromApplicationFolder;

            List<Assembly> modules = new List<Assembly>();

            foreach (string path in Directory.EnumerateFiles(AppContext.BaseDirectory, ModuleAssemblyPattern))
            {
                AssemblyName name = AssemblyName.GetAssemblyName(path);

                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                                        .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name))
                                    ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

                modules.Add(assembly);
            }

            return modules;
        }

        private static Assembly ResolveFromApplicationFolder(AssemblyLoadContext context, AssemblyName name)
        {
            string path = Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");

            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        }

        /// <summary>
        ///     All MixERP assemblies that may contain pages, controls, handlers, services or hubs.
        /// </summary>
        internal static IReadOnlyList<Assembly> GetApplicationAssemblies(IEnumerable<Assembly> modules)
        {
            return new[] {typeof(ModuleLoader).Assembly}
                .Concat(AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name?.StartsWith("MixERP.", StringComparison.Ordinal) == true))
                .Concat(modules)
                .Distinct()
                .ToList();
        }

        internal static Type FindType(IEnumerable<Assembly> assemblies, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            return assemblies.Select(a => a.GetType(typeName, false)).FirstOrDefault(t => t != null);
        }

        internal static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
