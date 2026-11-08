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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Updater.Api;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Port of <c>Application/UpdateManager.CheckForUpdates(HttpApplicationState)</c>, which
    ///     <c>Global.asax.cs</c>'s <c>Application_Start</c> called as an <c>async void</c>
    ///     fire-and-forget.
    ///     <para>
    ///         Behaviour preserved exactly: read the <c>AutoSuggestUpdate</c> updater parameter,
    ///         do nothing when it is not true, otherwise await <c>GetLatestRelease()</c> and, when
    ///         a release comes back non-null, raise the update-available flag. Any exception is
    ///         logged with the identical message template and swallowed.
    ///     </para>
    ///     <para>
    ///         Two things changed, both deliberate: <c>async void</c> became a managed
    ///         <see cref="BackgroundService" /> (an unobserved exception in an
    ///         <c>async void</c> crashed the worker process), and
    ///         <c>application["UpdateAvailable"] = true</c> became the singleton
    ///         <see cref="IUpdateAvailabilityState" />. The HTTP call runs on an
    ///         <c>IHttpClientFactory</c> client rather than the library's own process-wide one.
    ///     </para>
    /// </summary>
    public sealed class UpdateCheckService : BackgroundService
    {
        /// <summary>
        ///     Name of the named HTTP client registered in Program.cs.
        /// </summary>
        public const string HttpClientName = "MixERPUpdater";

        private readonly IHttpClientFactory httpClientFactory;
        private readonly IUpdateAvailabilityState updateAvailabilityState;

        public UpdateCheckService(IHttpClientFactory httpClientFactory,
            IUpdateAvailabilityState updateAvailabilityState)
        {
            this.httpClientFactory = httpClientFactory;
            this.updateAvailabilityState = updateAvailabilityState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool autoSuggestUpdate =
                Conversion.TryCastBoolean(ConfigurationHelper.GetUpdaterParameter("AutoSuggestUpdate"));

            if (!autoSuggestUpdate)
            {
                return;
            }

            try
            {
                MixERP.Net.Updater.UpdateManager updater = new MixERP.Net.Updater.UpdateManager();

                HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);

                Release release = await updater.GetLatestRelease(client, stoppingToken);

                if (release != null)
                {
                    this.updateAvailabilityState.SetUpdateAvailable();
                }
            }
            catch (OperationCanceledException)
            {
                // Host shutdown during the check: not an error, and must not be logged as one.
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred. {Exception}.", ex);
            }
        }
    }
}
