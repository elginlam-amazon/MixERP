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

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common;
using MixERP.Net.Common.Extensions;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Entities.Core;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.FrontEnd.Services.Authentication;
using MixERP.Net.i18n;
using Serilog;

namespace MixERP.Net.FrontEnd.Services.Layout
{
    /// <summary>
    ///     The shared layout service that replaces
    ///     <c>Base/MixERPMasterPage.RegisterJavascript()</c> (checklist tasks 65 and 66).
    ///     <para>
    ///         The WebForms master page emitted these globals from <c>OnLoad</c>, so EVERY page served
    ///         through a master page got them. The Blazor equivalent is called once per layout render
    ///         (<c>OnAfterRenderAsync(firstRender: true)</c> of <c>MixERPLayout</c>, <c>BackendLayout</c>,
    ///         <c>BlankLayout</c> and <c>ReportLayout</c>), which is the same coverage.
    ///     </para>
    ///     <para>
    ///         THE EMITTED SCRIPT IS BYTE-FOR-BYTE THE ORIGINAL ONE, including the duplicate
    ///         re-emissions of <c>today</c>, <c>shortDateFormat</c>, <c>thousandSeparator</c>,
    ///         <c>decimalSeparator</c> and <c>currencyDecimalPlaces</c>: the variable names and value
    ///         formats are read by <c>wwwroot/js/mixerp/**</c> (<c>locale.js</c>, <c>notification.js</c>,
    ///         <c>declaration.js</c>, ...), so any rename or reformat silently breaks the client.
    ///         De-duplication is by REGISTRATION KEY (<see cref="RegistrationKey" />), never by
    ///         variable name, so the duplicates must stay.
    ///     </para>
    ///     <para>
    ///         Registered SCOPED (one instance per circuit / per request): the
    ///         <c>FirstStepsPending</c> value lives in the session, which is only readable while an
    ///         <c>HttpContext</c> exists — i.e. during the initial render of the circuit — so it is
    ///         captured in the constructor and reused for the rest of the circuit, exactly like
    ///         <c>CurrentLoginProvider.RemoteIpAddress</c>.
    ///     </para>
    /// </summary>
    public sealed class MasterPageScriptService
    {
        /// <summary>
        ///     The original WebForms client-script registration key. Preserved verbatim: the script is
        ///     injected once per key, so re-rendering a layout cannot emit the globals twice.
        /// </summary>
        public const string RegistrationKey = "MixERPMasterPage";

        /// <summary>
        ///     The legacy session key written by <c>UserAuthenticationService</c> / <c>User.asmx</c>
        ///     (as the string "True"/"False").
        /// </summary>
        public const string FirstStepsPendingSessionKey = "FirstStepsPending";

        private readonly CurrentLoginProvider currentLogin;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUpdateAvailabilityState updateAvailability;

        public MasterPageScriptService(CurrentLoginProvider currentLogin,
            IUpdateAvailabilityState updateAvailability, IHttpContextAccessor httpContextAccessor)
        {
            this.currentLogin = currentLogin;
            this.updateAvailability = updateAvailability;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        ///     Builds the global-variable script. Kept public so it can be asserted on directly.
        /// </summary>
        public async Task<string> GetScriptAsync()
        {
            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);
            string catalog = await this.currentLogin.GetCatalogAsync().ConfigureAwait(false);

            string userName = login == null || login.View == null ? string.Empty : login.View.UserName;
            string officeName = login == null || login.View == null ? string.Empty : login.View.OfficeName;
            string currencyCode = login == null || login.View == null ? string.Empty : login.View.CurrencyCode;

            string script = JSUtility.GetVar("culture", CultureInfo.CurrentCulture.Name);
            script += JSUtility.GetVar("language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

            //Legacy: this.Page.ResolveUrl("/js/jquery-ui/i18n/"). An already-rooted url is returned
            //unchanged by ResolveUrl, so the emitted value is identical.
            script += JSUtility.GetVar("jqueryUIi18nPath", PageUtility.ResolveUrl("/js/jquery-ui/i18n/"));

            script += JSUtility.GetVar("today", DateTime.Now.ToShortDateString());
            script += JSUtility.GetVar("now", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            script += JSUtility.GetVar("user", userName);
            script += JSUtility.GetVar("office", officeName);

            script += JSUtility.GetVar("shortDateFormat", CurrentCulture.GetShortDateFormat());
            script += JSUtility.GetVar("longDateFormat", CurrentCulture.GetLongDateFormat());

            script += JSUtility.GetVar("thousandSeparator", CurrentCulture.GetThousandSeparator());
            script += JSUtility.GetVar("decimalSeparator", CurrentCulture.GetDecimalSeparator());
            script += JSUtility.GetVar("currencyDecimalPlaces", CurrentCulture.GetCurrencyDecimalPlaces());
            script += JSUtility.GetVar("currencySymbol", CurrentCulture.GetCurrencySymbol());

            //The duplicate re-emissions of the original. DO NOT REMOVE: see the class remarks.
            script += JSUtility.GetVar("today", DateTime.Now.ToShortDateString());
            script += JSUtility.GetVar("shortDateFormat", CurrentCulture.GetShortDateFormat());
            script += JSUtility.GetVar("thousandSeparator", CurrentCulture.GetThousandSeparator());
            script += JSUtility.GetVar("decimalSeparator", CurrentCulture.GetDecimalSeparator());
            script += JSUtility.GetVar("currencyDecimalPlaces", CurrentCulture.GetCurrencyDecimalPlaces());
            script += JSUtility.GetVar("baseCurrencyCode", currencyCode);

            script += JSUtility.GetVar("catalog", catalog);

            script += JSUtility.GetVar("update", await this.UpdateAsync().ConfigureAwait(false));

            script += JSUtility.GetVar("firstStepsPending",
                await this.GetFirstStepsPendingAsync().ConfigureAwait(false));

            return script;
        }

        /// <summary>
        ///     Emits the globals to the browser under <see cref="RegistrationKey" />, the exact
        ///     <c>PageUtility.RegisterJavascript("MixERPMasterPage", script, page, true)</c> call of the
        ///     original (<c>addScriptTags: true</c> — bare javascript to execute).
        /// </summary>
        public async Task RegisterAsync(IJSRuntime jsRuntime)
        {
            if (jsRuntime == null)
            {
                return;
            }

            string script = await this.GetScriptAsync().ConfigureAwait(false);
            PageUtility.RegisterJavascript(RegistrationKey, script, jsRuntime, true);
        }

        /// <summary>
        ///     Port of <c>MixERPMasterPage.Update()</c> (checklist task 66): 1 when an update is
        ///     available AND the current user is an administrator, otherwise 0. The flag source is the
        ///     <see cref="IUpdateAvailabilityState" /> singleton that replaced
        ///     <c>Application["UpdateAvailable"]</c>.
        /// </summary>
        public async Task<int> UpdateAsync()
        {
            MetaLogin login = await this.currentLogin.GetCurrentAsync().ConfigureAwait(false);

            bool isAdmin = login != null && login.View != null && login.View.IsAdmin.ToBool();

            if (this.updateAvailability.UpdateAvailable && isAdmin)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        ///     The legacy value of <c>Session["FirstStepsPending"]</c> as the original emitted it
        ///     ("True"/"False" — notification.js tests <c>substring(0, 1) === "T"</c>).
        ///     <para>
        ///         The session is readable only while an <c>HttpContext</c> exists, i.e. during the
        ///         initial render. This service is resolved from the CIRCUIT scope and the globals are
        ///         emitted from <c>OnAfterRenderAsync</c>, where <c>HttpContext</c> is null — so when
        ///         the session is unavailable the value is RECOMPUTED from
        ///         <see cref="FirstSteps.CheckIfPending" /> (the very call that wrote the session entry
        ///         in <c>User.asmx</c>) rather than silently degrading to an empty string, which would
        ///         lose the first-steps prompt for every new administrator.
        ///     </para>
        /// </summary>
        private async Task<string> GetFirstStepsPendingAsync()
        {
            string fromSession = ReadFirstStepsPending(this.httpContextAccessor);

            if (!string.IsNullOrEmpty(fromSession))
            {
                return fromSession;
            }

            try
            {
                long globalLoginId = await this.currentLogin.GetGlobalLoginIdAsync().ConfigureAwait(false);

                if (globalLoginId <= 0)
                {
                    return string.Empty;
                }

                return FirstSteps.CheckIfPending(globalLoginId).ToString();
            }
            catch (Exception ex)
            {
                //Never let the globals script fail over this one value, but never hide it either.
                Log.Warning("Could not determine the {Key} value. {Exception}.",
                    FirstStepsPendingSessionKey, ex);
                return string.Empty;
            }
        }

        private static string ReadFirstStepsPending(IHttpContextAccessor httpContextAccessor)
        {
            try
            {
                HttpContext context = httpContextAccessor == null ? null : httpContextAccessor.HttpContext;

                if (context == null)
                {
                    //No HttpContext: this scope was created for an interactive circuit rather than for
                    //the initial render. Say so instead of returning a silent empty value.
                    Log.Debug(
                        "The {Key} session value is not readable in this scope (no HttpContext); the javascript global falls back to an empty value.",
                        FirstStepsPendingSessionKey);
                    return string.Empty;
                }

                return context.Session.GetString(FirstStepsPendingSessionKey) ?? string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                //Session not configured/available for this request (e.g. a static asset request).
                Log.Debug("Could not read the {Key} session value. {Exception}.", FirstStepsPendingSessionKey, ex);
                return string.Empty;
            }
        }
    }
}
