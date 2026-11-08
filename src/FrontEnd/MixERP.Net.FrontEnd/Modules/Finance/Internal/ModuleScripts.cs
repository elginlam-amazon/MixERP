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
using MixERP.Net.Common;
using Serilog;

namespace MixERP.Net.Core.Modules.Finance.Internal
{
    /// <summary>
    ///     Loads the per-control javascript files the legacy <c>.ascx</c> markup carried as
    ///     <c>&lt;script src="..."&gt;</c> elements.
    ///     <para>
    ///         A <c>.razor</c> file may not contain a <c>&lt;script&gt;</c> element (Blazor strips
    ///         it), so the files are fetched after the first render through the host's
    ///         <c>wwwroot/js/mixerp/page-interop.js</c> <c>loadScripts</c> helper, which appends
    ///         them SEQUENTIALLY - preserving the original document order. For this module the
    ///         only such file is <c>js/Reorder.js</c>, whose top-level block reads
    ///         <c>#ReorderGrid</c> out of the DOM, so it must run AFTER the grid has rendered -
    ///         which is exactly what loading it from <c>OnAfterRenderAsync</c> guarantees.
    ///     </para>
    /// </summary>
    internal static class ModuleScripts
    {
        private const string PageInteropModule = "/js/mixerp/page-interop.js";

        internal static async Task LoadAsync(IJSRuntime js, params string[] urls)
        {
            if (js == null || urls == null || urls.Length.Equals(0))
            {
                return;
            }

            try
            {
                IJSObjectReference module = await js
                    .InvokeAsync<IJSObjectReference>("import", PageInteropModule).ConfigureAwait(true);

                await module.InvokeVoidAsync("loadScripts", new object[] { urls }).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                Log.Warning("Could not load the page scripts. {Exception}.", ex);
            }
        }

        /// <summary>
        ///     Runs an inline script block ONCE per document, keyed exactly as
        ///     <c>PageUtility.RegisterJavascript(key, script, page, true)</c> keyed it.
        ///     <para>
        ///         This is the replacement for the two legacy controls that registered
        ///         javascript VARIABLES rather than a file: <c>Reports/TrialBalance</c>'s
        ///         <c>"TrialBalance_Vars"</c> block, which declares
        ///         <c>perviousPeriodLocalized</c> / <c>currentPeriodLocalized</c> /
        ///         <c>closingLocalized</c> that <c>js/reports/TrialBalance.js</c> reads when it
        ///         builds the grouped header. It is AWAITED (unlike
        ///         <c>PageUtility.RegisterJavascript</c>, which is a fire-and-forget void API)
        ///         so the variables always exist before the dependent file is appended.
        ///     </para>
        /// </summary>
        internal static async Task RunAsync(IJSRuntime js, string key, string script)
        {
            if (js == null || string.IsNullOrWhiteSpace(script))
            {
                return;
            }

            try
            {
                IJSObjectReference module = await js
                    .InvokeAsync<IJSObjectReference>("import", PageUtility.InteropModulePath)
                    .ConfigureAwait(true);

                await module.InvokeVoidAsync("runScript", key, script).ConfigureAwait(true);
            }
            catch (JSException ex)
            {
                Log.Warning("Could not run the page script {Key}. {Exception}.", key, ex);
            }
        }
    }
}
