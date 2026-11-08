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

namespace MixERP.Net.Framework
{
    /// <summary>
    ///     Deterministic assembly source for the reflection-based discovery used by
    ///     <c>CurrencyConverter.GetAll</c> and <c>FirstStep.GetAll</c>.
    ///     The .NET Framework code scanned <c>AppDomain.CurrentDomain.GetAssemblies()</c>, which only ever
    ///     sees assemblies the runtime has already loaded — on modern .NET loading is lazier still, so a
    ///     plug-in assembly that has not been touched yet would be missed. The host therefore registers the
    ///     assemblies that take part in discovery via <see cref="Register(Assembly)" /> (or
    ///     <see cref="RegisterFrom" /> for a plug-in directory) at startup; when nothing is registered the
    ///     default load context is used, which is the closest modern equivalent of the original behavior.
    /// </summary>
    public static class DiscoveryAssemblies
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<Assembly> Registered = new List<Assembly>();

        /// <summary>
        ///     Adds an assembly to the discovery set. Repeat registrations are ignored.
        /// </summary>
        public static void Register(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            lock (SyncRoot)
            {
                if (!Registered.Contains(assembly))
                {
                    Registered.Add(assembly);
                }
            }
        }

        /// <summary>
        ///     Loads and registers every managed assembly in <paramref name="directory" />.
        ///     Load failures are logged and skipped: on Linux a native or non-managed file throws
        ///     <see cref="BadImageFormatException" />, <see cref="FileNotFoundException" /> or
        ///     <see cref="FileLoadException" /> rather than being silently ignored as on Windows.
        /// </summary>
        public static void RegisterFrom(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*.dll"))
            {
                try
                {
                    Register(AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file)));
                }
                catch (BadImageFormatException ex)
                {
                    Log.Warning(ex, "Skipped {File}: it is not a managed assembly.", file);
                }
                catch (FileNotFoundException ex)
                {
                    Log.Warning(ex, "Skipped {File}: a dependency could not be found.", file);
                }
                catch (FileLoadException ex)
                {
                    Log.Warning(ex, "Skipped {File}: the assembly could not be loaded.", file);
                }
            }
        }

        /// <summary>
        ///     The assemblies discovery runs over.
        /// </summary>
        public static IEnumerable<Assembly> GetAssemblies()
        {
            lock (SyncRoot)
            {
                if (Registered.Count > 0)
                {
                    return Registered.ToList();
                }
            }

            return AssemblyLoadContext.Default.Assemblies
                .Where(a => !a.IsDynamic)
                .ToList();
        }

        /// <summary>
        ///     Every loadable type in the discovery set. A partially loadable assembly contributes the types
        ///     that did load instead of aborting the whole scan.
        /// </summary>
        public static IEnumerable<Type> GetTypes()
        {
            var types = new List<Type>();

            foreach (Assembly assembly in GetAssemblies())
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Log.Warning(ex, "{Assembly} is only partially loadable; discovery continues with the types that did load.", assembly.FullName);
                    types.AddRange(ex.Types.Where(t => t != null));
                }
                catch (FileNotFoundException ex)
                {
                    Log.Warning(ex, "Types of {Assembly} could not be read: a dependency could not be found.", assembly.FullName);
                }
                catch (FileLoadException ex)
                {
                    Log.Warning(ex, "Types of {Assembly} could not be read.", assembly.FullName);
                }
            }

            return types;
        }
    }
}
