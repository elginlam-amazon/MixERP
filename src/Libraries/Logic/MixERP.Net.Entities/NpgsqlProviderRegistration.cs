using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using Npgsql;

namespace MixERP.Net.Entities
{
    /// <summary>
    /// Replaces the legacy App.config &lt;system.data&gt;/&lt;DbProviderFactories&gt; registration of the
    /// Npgsql provider, which modern .NET no longer reads from configuration files. Registering the
    /// factory in a module initializer keeps `DbProviderFactories.GetFactory("Npgsql")` working for
    /// every consumer of this assembly, exactly as it did on .NET Framework.
    /// </summary>
    internal static class NpgsqlProviderRegistration
    {
        // Intentional library module initializer: every host (web app, utilities) that loads this
        // assembly needs the provider registered before PetaPoco resolves it by invariant name.
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void Register()
        {
            if (!DbProviderFactories.GetProviderInvariantNames().Contains("Npgsql"))
            {
                DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
            }
        }
    }
}
