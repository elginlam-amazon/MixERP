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
using System.Data.Common;
using Npgsql;

namespace PetaPoco
{
    /// <summary>
    /// Holds the <see cref="PetaPocoOptions" /> supplied by the host at startup.
    /// </summary>
    /// <remarks>
    /// The public surface of this library (<see cref="Factory" />, <see cref="Database" />) is
    /// static or directly constructed by every MixERP module, so the configuration it needs is
    /// supplied once by the host through
    /// <see cref="PetaPocoServiceCollectionExtensions.AddPetaPoco" /> instead of being read
    /// ambiently from the legacy configuration and hosting-environment statics as it was on
    /// .NET Framework.
    /// </remarks>
    public static class PetaPocoRuntime
    {
        private static readonly object SyncRoot = new object();
        private static PetaPocoOptions current = new PetaPocoOptions();
        private static bool providerFactoriesRegistered;

        /// <summary>
        /// The options currently in effect. Never null — defaults apply until the host
        /// configures it.
        /// </summary>
        public static PetaPocoOptions Options => current;

        /// <summary>
        /// Called by the host during startup to supply configuration and the content root.
        /// Also registers the ADO.NET provider factories this library resolves by invariant
        /// name, which modern .NET no longer discovers from machine configuration.
        /// </summary>
        public static void Configure(PetaPocoOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            current = options;
            RegisterProviderFactories();
        }

        /// <summary>
        /// Registers the Npgsql provider factory under the invariant names this library passes
        /// to <see cref="DbProviderFactories.GetFactory(string)" />. On .NET Framework these
        /// came from machine.config; on modern .NET each factory must be registered explicitly.
        /// Idempotent and safe to call from any entry point.
        /// </summary>
        public static void RegisterProviderFactories()
        {
            if (providerFactoriesRegistered)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (providerFactoriesRegistered)
                {
                    return;
                }

                DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

                providerFactoriesRegistered = true;
            }
        }
    }
}
