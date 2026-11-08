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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MixERP.Net.Entities.Office;
using MixERP.Net.ReportManager;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     The two one-shot <c>Application_Start</c> steps of the deleted
    ///     <c>Global.asax.cs</c>: <c>GlobalLogin.CreateTable()</c> and
    ///     <c>Repository.DownloadAndInstallReports()</c>.
    ///     <para>
    ///         Implemented as an <see cref="IHostedService" /> rather than a
    ///         <see cref="BackgroundService" /> because the work is start-up work, not a loop:
    ///         <c>StartAsync</c> is awaited by the host BEFORE the server starts accepting
    ///         requests, which is exactly the ordering <c>Application_Start</c> guaranteed, and
    ///         it runs exactly once per process.
    ///     </para>
    ///     <para>
    ///         <c>Repository.DownloadAndInstallReports()</c> is an <c>async Task</c> that
    ///         <c>Application_Start</c> called WITHOUT awaiting (its result was discarded and any
    ///         fault was unobserved). It is awaited here, so a failure is now visible in the log
    ///         instead of vanishing — but it is caught, so a report-download failure cannot stop
    ///         the application from starting, which matches the legacy outcome.
    ///     </para>
    /// </summary>
    public sealed class MixERPStartupTasks : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Application_Start step 4: create public.global_logins when absent.
            try
            {
                GlobalLogin.CreateTable();
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred while creating the global login table. {Exception}.", ex);
            }

            // Application_Start step 5: download and install the standard reports.
            try
            {
                await Repository.DownloadAndInstallReports();
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred while installing reports. {Exception}.", ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
