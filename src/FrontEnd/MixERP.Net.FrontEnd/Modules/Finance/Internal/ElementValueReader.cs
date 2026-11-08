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

using System.Threading.Tasks;
using Microsoft.JSInterop;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Internal
{
    /// <summary>
    ///     Reads the value of a DOM element the module's own javascript owns.
    ///     <para>
    ///         This is the replacement for WebForms POSTBACK VALUE RECOVERY. Several Finance
    ///         report screens have a server-side Show/Flag handler whose inputs are written
    ///         client-side by their own script (or by jQuery UI) and never bound - the three
    ///         toggle hidden fields of <c>Reports/TrialBalance</c>
    ///         (<c>#IsCompactHidden</c>, <c>#ChangeSideWhenNegativeHidden</c>,
    ///         <c>#IncludeZeroBalanceAccountHidden</c>), <c>Reports/ProfitAndLossAccount</c>'s
    ///         <c>#IsCompactHidden</c>, <c>Reports/AccountStatement</c>'s
    ///         <c>#SelectedValuesHidden</c>, and every <c>DateTextBox</c> whose value the
    ///         jQuery UI datepicker writes. Under WebForms those arrived on the POSTBACK because
    ///         they were form fields; a value jQuery writes into an input raises no input event,
    ///         so a Blazor component never sees it. Reading the element at click time is the
    ///         faithful equivalent of the postback read.
    ///     </para>
    ///     <para>
    ///         Backed by <c>wwwroot/js/finance-interop.js</c>, whose header records the same
    ///         reasoning. The module reference is cached per component instance, so repeated
    ///         clicks do not re-import it.
    ///     </para>
    /// </summary>
    internal sealed class ElementValueReader
    {
        private const string ModulePath =
            "_content/MixERP.Net.Core.Modules.Finance/js/finance-interop.js";

        private readonly IJSRuntime js;
        private IJSObjectReference module;

        internal ElementValueReader(IJSRuntime js)
        {
            this.js = js;
        }

        /// <summary>
        ///     The current value of the element, or <c>null</c> when the element does not exist
        ///     (which is the same "nothing was posted" outcome the WebForms read had).
        /// </summary>
        internal async Task<string> GetValueAsync(string elementId)
        {
            IJSObjectReference reference = await this.GetModuleAsync().ConfigureAwait(true);

            if (reference == null)
            {
                return null;
            }

            try
            {
                return await reference.InvokeAsync<string>("getValue", elementId).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                Log.Warning("Could not read the value of the element {ElementId}. {Exception}.", elementId, ex);
                return null;
            }
        }

        /// <summary>Writes a value back into a javascript-owned element.</summary>
        internal async Task SetValueAsync(string elementId, string value)
        {
            IJSObjectReference reference = await this.GetModuleAsync().ConfigureAwait(true);

            if (reference == null)
            {
                return;
            }

            try
            {
                await reference.InvokeVoidAsync("setValue", elementId, value).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                Log.Warning("Could not write the value of the element {ElementId}. {Exception}.", elementId, ex);
            }
        }

        private async Task<IJSObjectReference> GetModuleAsync()
        {
            if (this.module != null)
            {
                return this.module;
            }

            if (this.js == null)
            {
                return null;
            }

            try
            {
                this.module = await this.js
                    .InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                Log.Warning("Could not import the Finance interop module. {Exception}.", ex);
            }

            return this.module;
        }
    }
}
