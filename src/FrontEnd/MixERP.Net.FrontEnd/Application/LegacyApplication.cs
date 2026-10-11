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
along with MixERP.  If not, see <http://www.gnu.org/licenses />.
***********************************************************************************/

using System;
using System.Collections.Concurrent;
using MixERP.Net.Entities.Office;
using MixERP.Net.ReportManager;
using Serilog;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Application lifecycle formerly implemented in Global.asax (Application_Start / Application_Error).
    /// </summary>
    public static class LegacyApplication
    {
        /// <summary>
        ///     Application-wide state (formerly HttpApplicationState, e.g. "UpdateAvailable").
        /// </summary>
        public static ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static void Start()
        {
            LogManager.IntializeLogger();
            UpdateManager.CheckForUpdates(State);

            try
            {
                GlobalLogin.CreateTable();
                Repository.DownloadAndInstallReports();
            }
            catch (Exception ex)
            {
                //Startup must not fail when the database or the report repository is not reachable yet
                //(e.g. before the installation has been completed).
                Log.Error("Application start-up tasks failed. {Exception}.", ex);
            }
        }

        public static void Error(Exception exception)
        {
            ApplicationError.Handle(exception);
        }
    }
}
