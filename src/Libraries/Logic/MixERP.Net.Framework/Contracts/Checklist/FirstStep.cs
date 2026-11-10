using System.ComponentModel;
using System.Reflection;

namespace MixERP.Net.Framework.Contracts.Checklist
{
    public abstract class FirstStep
    {
        [Localizable(true)]
        public string Name { get; set; }
        public string Icon { get; set; }
        [Localizable(true)]
        public string Category { get; set; }
        public string CategoryAlias { get; set; }
        public int Order { get; set; }
        [Localizable(true)]
        public string Description { get; set; }
        public bool Status { get; set; }
        [Localizable(true)]
        public string Message { get; set; }
        public string NavigateUrl { get; set; }

        public static IEnumerable<FirstStep> GetAll()
        {
            Type type = typeof(FirstStep);

            var items = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(p => p.IsSubclassOf(type))
                .Where(p => !p.IsAbstract && !p.IsGenericTypeDefinition);

            return items.Select(t => (FirstStep)Activator.CreateInstance(t));
        }

        /// <summary>
        ///     Enumerates the types of an assembly, tolerating assemblies whose dependencies
        ///     cannot be resolved. On modern .NET, assemblies are loaded lazily and the
        ///     process may contain assemblies with unresolvable references, so a bare
        ///     <c>Assembly.GetTypes()</c> can throw and abort the whole scan.
        /// </summary>
        internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                return Array.Empty<Type>();
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<Type>();
            }
            catch (FileLoadException)
            {
                return Array.Empty<Type>();
            }
            catch (BadImageFormatException)
            {
                return Array.Empty<Type>();
            }
        }
    }
}
