using System;
using System.Net.Http;

namespace MixERP.Net.CurrencyConversion.Currencylayer
{
    /// <summary>
    /// Provides the single <see cref="HttpClient"/> used when this converter is created by the
    /// plugin-discovery path (<c>Activator.CreateInstance</c>), which cannot inject an
    /// <see cref="IHttpClientFactory"/>. One shared, handler-pooling client is used instead of a
    /// per-call <c>new HttpClient()</c> (which exhausts sockets); the handler is rotated every two
    /// minutes exactly like <see cref="IHttpClientFactory"/> does, so DNS changes are picked up.
    /// Hosts that register the converter through <c>AddCurrencylayer</c> never use this client.
    /// </summary>
    internal static class CurrencylayerHttpClient
    {
        internal const string ClientName = "Currencylayer";

        private static readonly Lazy<HttpClient> SharedClient = new Lazy<HttpClient>(Create);

        internal static HttpClient Shared
        {
            get { return SharedClient.Value; }
        }

        private static HttpClient Create()
        {
            SocketsHttpHandler handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            };

            return new HttpClient(handler, true);
        }
    }
}
