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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using MixERP.Net.Web.UI;
using Serilog;

namespace MixERP.Net.FrontEnd.Hosting
{
    /// <summary>
    ///     Loads the MixERP assemblies deployed in the application folder (the former "bin" folder which ASP.NET
    ///     loaded automatically), so that modules, checklists and currency converters are discovered by reflection.
    /// </summary>
    internal static class ModuleLoader
    {
        private static readonly List<Assembly> Loaded = new List<Assembly>();

        internal static IReadOnlyList<Assembly> Assemblies => Loaded;

        internal static void LoadModules(string contentRootPath)
        {
            string directory = AppContext.BaseDirectory;

            foreach (string path in Directory.GetFiles(directory, "MixERP.Net.*.dll"))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                try
                {
                    Assembly assembly;

                    try
                    {
                        //Assemblies referenced by the application are resolved from its dependency manifest.
                        assembly = Assembly.Load(new AssemblyName(name));
                    }
                    catch (FileNotFoundException)
                    {
                        //Module assemblies are deployed next to the application without being referenced by it.
                        assembly = Assembly.LoadFrom(path);
                    }

                    Loaded.Add(assembly);
                    ControlRegistry.Register(assembly);
                }
                catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException || ex is FileNotFoundException)
                {
                    Log.Error("Could not load the assembly {Assembly}. {Exception}.", path, ex);
                }
            }

            ControlRegistry.Register(typeof(ModuleLoader).Assembly);
        }

        /// <summary>
        ///     Maps the SignalR hubs declared by modules (formerly discovered by OWIN's MapSignalR()) to /signalr/{hubName}.
        /// </summary>
        internal static void MapModuleHubs(IEndpointRouteBuilder endpoints)
        {
            MethodInfo mapHub = typeof(HubEndpointRouteBuilderExtensions).GetMethods()
                .Single(m => m.Name == nameof(HubEndpointRouteBuilderExtensions.MapHub) && m.IsGenericMethod && m.GetParameters().Length == 2);

            foreach (Assembly assembly in Loaded.Where(a => a != typeof(ModuleLoader).Assembly))
            {
                foreach (Type hub in GetTypes(assembly).Where(t => typeof(Hub).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    string pattern = "/signalr/" + char.ToLowerInvariant(hub.Name[0]) + hub.Name.Substring(1);
                    mapHub.MakeGenericMethod(hub).Invoke(null, new object[] {endpoints, pattern});
                }
            }
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
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
