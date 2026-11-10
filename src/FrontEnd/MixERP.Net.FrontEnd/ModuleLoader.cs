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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     Loads the MixERP.Net.Core.Modules.* assemblies that sit next to the host and exposes
    ///     their controllers and SignalR hubs.
    /// </summary>
    internal static class ModuleLoader
    {
        private const string ModuleAssemblyPattern = "MixERP.Net.Core.Modules.*.dll";

        internal static Assembly[] LoadModules(string directory)
        {
            List<Assembly> assemblies = new List<Assembly>();

            if (!Directory.Exists(directory))
            {
                return assemblies.ToArray();
            }

            // The modules and the control libraries they bring along are not in the host's
            // deps.json, so their dependencies are resolved from the same folder.
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string candidate = Path.Combine(directory, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };

            foreach (string file in Directory.GetFiles(directory, ModuleAssemblyPattern))
            {
                try
                {
                    AssemblyName name = AssemblyName.GetAssemblyName(file);
                    Assembly loaded = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name));

                    assemblies.Add(loaded ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
                }
                catch (BadImageFormatException ex)
                {
                    Log.Warning("Could not load module {File}. {Exception}", file, ex);
                }
                catch (FileLoadException ex)
                {
                    Log.Warning("Could not load module {File}. {Exception}", file, ex);
                }
            }

            return assemblies.ToArray();
        }

        internal static IEnumerable<Assembly> GetControllerAssemblies(IEnumerable<Assembly> modules)
        {
            return modules.Where(a => GetLoadableTypes(a).Any(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract));
        }

        /// <summary>
        ///     Maps every hub of every module at /signalr/{HubName}.
        /// </summary>
        internal static void MapModuleHubs(IEndpointRouteBuilder endpoints, IEnumerable<Assembly> modules)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.GetParameters().Length == 2);

            foreach (Type hub in modules.SelectMany(GetLoadableTypes).Where(t => typeof(Hub).IsAssignableFrom(t) && !t.IsAbstract))
            {
                mapHub.MakeGenericMethod(hub).Invoke(null, new object[] { endpoints, "/signalr/" + hub.Name });
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
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
