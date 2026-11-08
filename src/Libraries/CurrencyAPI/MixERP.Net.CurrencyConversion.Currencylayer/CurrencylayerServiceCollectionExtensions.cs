using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MixERP.Net.Framework.Contracts.Currency;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    /// <summary>
    /// Host-side wiring for the Currencylayer converter. One call registers the named
    /// <c>Currencylayer</c> HTTP client, the per-request options (DB values overridden by the
    /// <c>Currencylayer</c> configuration section) and the converter itself.
    /// </summary>
    public static class CurrencylayerServiceCollectionExtensions
    {
        public static IServiceCollection AddCurrencylayer(this IServiceCollection services)
        {
            services.AddHttpClient(CurrencylayerHttpClient.ClientName);

            services.AddOptions<CurrencylayerOptions>();

            // Registered as a singleton, as the Options pattern requires (the singleton
            // IOptionsFactory<T> consumes IConfigureOptions<T> and cannot depend on a scoped
            // service). Per-tenant freshness comes from IOptionsSnapshot<CurrencylayerOptions>,
            // which is scoped and re-runs Configure — and therefore the per-request database
            // read — once per request, instead of once at type load as before.
            services.TryAddSingleton<IConfigureOptions<CurrencylayerOptions>, CurrencylayerOptionsSetup>();

            services.AddScoped<Converter>();
            services.AddScoped<ICurrencyConverter>(sp => sp.GetRequiredService<Converter>());

            return services;
        }
    }
}
