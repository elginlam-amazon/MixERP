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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Helpers;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Port of <c>Base/MixERPWebpage.InitializeCulture()</c> → <c>SetCulture()</c>: the
    ///     signed-in user's <c>logins.View.Culture</c> wins over everything else. Expressed as a
    ///     request-culture provider so it runs for EVERY request, which is what overriding
    ///     <c>Page.InitializeCulture</c> on the common page base achieved.
    ///     <para>
    ///         Returning null falls through to the next provider in
    ///         <c>RequestLocalizationOptions.RequestCultureProviders</c>; the next one is
    ///         <c>AcceptLanguageHeaderRequestCultureProvider</c>, which is the behaviour of the
    ///         deleted <c>&lt;globalization culture="auto" uiCulture="auto" /&gt;</c>.
    ///     </para>
    /// </summary>
    public sealed class MixERPUserCultureProvider : RequestCultureProvider
    {
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null || httpContext.User == null ||
                httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                return Task.FromResult<ProviderCultureResult>(null);
            }

            string cultureName = GetUserCulture();

            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return Task.FromResult<ProviderCultureResult>(null);
            }

            // SetCulture() set CurrentCulture AND CurrentUICulture to the same value.
            return Task.FromResult(new ProviderCultureResult(cultureName, cultureName));
        }

        private static string GetUserCulture()
        {
            try
            {
                var logins = AppUsers.GetCurrent();

                if (logins == null || logins.View == null)
                {
                    return null;
                }

                return logins.View.Culture;
            }
            catch (Exception ex)
            {
                Log.Warning("Could not resolve the current user's culture. {Exception}.", ex);
                return null;
            }
        }
    }

    /// <summary>
    ///     Builds the supported-culture list from the <c>Cultures</c> MixERP parameter — the same
    ///     list the SignIn language selector offers
    ///     (<c>ConfigurationHelper.GetParameter("Cultures").Split(',')</c> in
    ///     <c>SignIn.aspx.cs</c>). A request asking for a culture outside the list would
    ///     otherwise be silently coerced to the default, so the two surfaces must agree.
    /// </summary>
    public static class SupportedCultures
    {
        /// <summary>
        ///     The configured culture names, or an empty list when the parameter is unavailable
        ///     (at which point request localization keeps its default single culture).
        /// </summary>
        public static IList<CultureInfo> Get()
        {
            List<CultureInfo> cultures = new List<CultureInfo>();

            string value;

            try
            {
                value = ConfigurationHelper.GetParameter("Cultures");
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read the Cultures parameter. {Exception}.", ex);
                return cultures;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return cultures;
            }

            foreach (string name in value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                try
                {
                    cultures.Add(new CultureInfo(name));
                }
                catch (CultureNotFoundException)
                {
                    Log.Warning("Ignoring unknown culture {Culture} from the Cultures parameter.", name);
                }
            }

            return cultures;
        }
    }
}
