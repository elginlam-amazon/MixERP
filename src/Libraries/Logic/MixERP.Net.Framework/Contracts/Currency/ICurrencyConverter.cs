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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MixERP.Net.Framework.Contracts.Currency
{
    public interface ICurrencyConverter
    {
        bool Enabled { get; }
        string ConfigFileName { get; }
        string ConverterName { get; }
        string BaseCurrency { get; set; }
        List<string> CurrencyCodes { get; set; }

        /// <summary>
        ///     Fetches the current exchange rates from the provider's remote API.
        ///     <para>
        ///         This replaces the former synchronous <c>GetResult()</c>. The providers call an
        ///         external HTTP endpoint, which on modern .NET is an asynchronous operation:
        ///         keeping the method synchronous would force every implementation to block on
        ///         the HTTP call (<c>.Result</c>), which starves the ASP.NET Core thread pool and
        ///         can deadlock under load. Callers should await this method.
        ///     </para>
        /// </summary>
        Task<IEnumerable<CurrencyConversionResult>> GetResultAsync(
            CancellationToken cancellationToken);
    }
}