using System;
using MixERP.Net.Framework;

namespace MixERP.Net.CurrencyConversion.OpenExchangeRates
{
    public class OpenExchangeRatesException : MixERPException
    {
        public OpenExchangeRatesException(string message)
            : base(message)
        {
        }

        public OpenExchangeRatesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
