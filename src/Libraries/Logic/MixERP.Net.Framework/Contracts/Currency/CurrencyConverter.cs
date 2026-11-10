using MixERP.Net.Framework.Contracts.Checklist;

namespace MixERP.Net.Framework.Contracts.Currency
{
    public class CurrencyConverter
    {
        public string Name { get; set; }
        public string AssemblyQualifiedName { get; set; }
        public bool Enabled { get; set; }

        public static IEnumerable<CurrencyConverter> GetEnabled()
        {
            return GetAll().Where(p => p.Enabled);
        }

        public static IEnumerable<CurrencyConverter> GetAll()
        {
            Type type = typeof(ICurrencyConverter);

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(FirstStep.GetLoadableTypes)
                .Where(p => type.IsAssignableFrom(p))
                .Where(p => p.Name != type.Name)
                // The interface itself, other interfaces deriving from it and abstract
                // implementations cannot be instantiated — excluding them keeps
                // Activator.CreateInstance from throwing and aborting the scan.
                .Where(p => !p.IsInterface && !p.IsAbstract && !p.IsGenericTypeDefinition)
                .Select(p => new
                {
                    Type = p,
                    Instance = Activator.CreateInstance(p)
                })
                .Select(p => new CurrencyConverter
                {
                    Name = GetPropValue(p.Instance, "ConverterName"),
                    AssemblyQualifiedName = p.Type.AssemblyQualifiedName,
                    Enabled = bool.Parse(GetPropValue(p.Instance, "Enabled"))
                });
        }

        private static string GetPropValue(object src, string propName)
        {
            object value = src.GetType().GetProperty(propName).GetValue(src, null);
            return value.ToString();
        }
    }
}
