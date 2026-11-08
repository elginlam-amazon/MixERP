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

using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Core.Modules.Finance.Data.Core;
using MixERP.Net.Core.Modules.Finance.Data.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.Framework.Contracts.Currency;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace MixERP.Net.Core.Modules.Finance.Services
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    [ScriptService]
    public class CurrencyData : WebService
    {
        [WebMethod]
        public IEnumerable<Currency> GetExchangeCurrencies()
        {
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            return Currencies.GetExchangeCurrencies(AppUsers.GetCurrentUserDB(), officeId);
        }

        /// <summary>
        ///     Fetches the exchange rates from the named provider.
        ///     <para>
        ///         <c>ICurrencyConverter.GetResult()</c> became
        ///         <c>GetResultAsync(CancellationToken)</c> because the providers call an
        ///         external HTTP API. Classic ASMX predates the task-based pattern and does NOT
        ///         support a <c>Task</c>-returning <c>[WebMethod]</c> — it would serialize the
        ///         Task object itself instead of awaiting it, silently breaking
        ///         UpdateExchangeRates.js. The Begin/End pair below is the asynchronous shape
        ///         ASMX does understand, so the request thread is released while the outbound
        ///         call is in flight and nothing blocks on the task.
        ///     </para>
        ///     <para>
        ///         When this project is migrated off WebForms/ASMX, this pair collapses back
        ///         into a single <c>async Task&lt;IEnumerable&lt;CurrencyConversionResult&gt;&gt;</c>
        ///         controller action on the same route.
        ///     </para>
        /// </summary>
        [WebMethod]
        public IAsyncResult BeginGetExchangeRates(string moduleName, AsyncCallback callback,
            object state)
        {
            return ToAsyncResult(this.GetExchangeRatesAsync(moduleName), callback, state);
        }

        [WebMethod]
        public IEnumerable<CurrencyConversionResult> EndGetExchangeRates(IAsyncResult asyncResult)
        {
            Task<IEnumerable<CurrencyConversionResult>> task =
                (Task<IEnumerable<CurrencyConversionResult>>) asyncResult;

            // The APM contract guarantees ASMX only calls End* once the IAsyncResult has
            // completed, so reading the result here does not block a thread. The awaiter is
            // used rather than .Result so a failure rethrows the provider's original
            // exception instead of wrapping it in an AggregateException.
            return task.GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<CurrencyConversionResult>> GetExchangeRatesAsync(
            string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return null;
            }

            Type type = Type.GetType(moduleName);

            if (type == null)
            {
                return null;
            }

            object instance = Activator.CreateInstance(type);
            ICurrencyConverter converter = instance as ICurrencyConverter;

            if (converter == null)
            {
                return null;
            }

            CurrencyData currencyData = new CurrencyData();
            IEnumerable<string> currencies = currencyData.GetExchangeCurrencies().Select(c => c.CurrencyCode);

            converter.BaseCurrency = AppUsers.GetCurrent().View.CurrencyCode;
            converter.CurrencyCodes = currencies.ToList();

            return await converter.GetResultAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        ///     Bridges a <see cref="Task{TResult}" /> onto the APM
        ///     <see cref="IAsyncResult" /> contract ASMX requires, preserving the caller's
        ///     <paramref name="state" /> and invoking <paramref name="callback" /> on
        ///     completion.
        /// </summary>
        private static IAsyncResult ToAsyncResult<T>(Task<T> task, AsyncCallback callback,
            object state)
        {
            TaskCompletionSource<T> source = new TaskCompletionSource<T>(state);

            task.ContinueWith(completed =>
            {
                if (completed.IsFaulted)
                {
                    source.TrySetException(completed.Exception.InnerExceptions);
                }
                else if (completed.IsCanceled)
                {
                    source.TrySetCanceled();
                }
                else
                {
                    source.TrySetResult(completed.Result);
                }

                if (callback != null)
                {
                    callback(source.Task);
                }
            }, TaskScheduler.Default);

            return source.Task;
        }

        [WebMethod]
        public Collection<ListItem> GetModules()
        {
            Collection<ListItem> items = new Collection<ListItem>();

            foreach (CurrencyConverter module in CurrencyConverter.GetEnabled())
            {
                items.Add(new ListItem(module.Name, module.AssemblyQualifiedName));
            }

            return items;
        }

        [WebMethod]
        public bool SaveExchangeRates(List<Data.Models.ExchangeRate> exchangeRates)
        {
            if (exchangeRates == null || exchangeRates.Count().Equals(0))
            {
                return false;
            }

            string catalog = AppUsers.GetCurrentUserDB();
            int officeId = AppUsers.GetCurrent().View.OfficeId.ToInt();
            string baseCurrency = AppUsers.GetCurrent().View.CurrencyCode;


            ExchangeRates.SaveExchangeRates(catalog, officeId, baseCurrency, exchangeRates);
            return true;
        }
    }
}