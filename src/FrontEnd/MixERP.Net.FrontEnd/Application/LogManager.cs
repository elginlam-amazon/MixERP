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
using System.IO;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Common.Helpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace MixERP.Net.FrontEnd.Application
{
    /// <summary>
    ///     Serilog bootstrap, ported from the WebForms <c>LogManager</c> that
    ///     <c>Global.asax.cs</c>'s <c>Application_Start</c> invoked.
    ///     <para>
    ///         Only two things changed, both forced by platform removals:
    ///         <c>System.Web.Hosting.HostingEnvironment.MapPath("~/Resource/Temp")</c> became the
    ///         supplied <c>IWebHostEnvironment.ContentRootPath</c> combine, and Serilog 1.x's
    ///         <c>WriteTo.RollingFile</c> became Serilog 4's <c>WriteTo.File</c> with a daily
    ///         rolling interval. The directory resolution order, the
    ///         <c>&lt;date&gt;/log.txt</c> layout, the <c>MinimumLogLevel</c> database parameter
    ///         and the "Application started." entry are unchanged.
    ///     </para>
    /// </summary>
    internal static class LogManager
    {
        /// <summary>
        ///     Content root supplied by Program.cs; replaces <c>HostingEnvironment.MapPath</c>,
        ///     which does not exist on modern .NET.
        /// </summary>
        private static string contentRootPath = AppContext.BaseDirectory;

        private static string GetLogDirectory()
        {
            string path = GetApplicationLogDirectory();

            if (string.IsNullOrWhiteSpace(path))
            {
                // Was HostingEnvironment.MapPath("~/Resource/Temp").
                return Path.Combine(contentRootPath, "Resource", "Temp");
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        ///     Reads the <c>ApplicationLogDirectory</c> MixERP parameter. The original read it
        ///     directly; at startup there is no signed-in user and therefore no catalog, so a
        ///     failure here must NOT prevent the logger from being created — it falls back to
        ///     <c>Resource/Temp</c>, exactly as an empty parameter value did.
        /// </summary>
        private static string GetApplicationLogDirectory()
        {
            try
            {
                return DbConfig.GetMixERPParameter(AppUsers.GetCurrentUserDB(), "ApplicationLogDirectory");
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string GetMinimumLogLevel()
        {
            try
            {
                return DbConfig.GetMixERPParameter(AppUsers.GetCurrentUserDB(), "MinimumLogLevel");
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string GetLogFileName()
        {
            string applicationLogDirectory = GetLogDirectory();
            string filePath = Path.Combine(applicationLogDirectory,
                DateTime.Now.Date.ToShortDateString().Replace(@"/", "-"), "log.txt");
            return filePath;
        }

        private static LoggerConfiguration GetConfiguration()
        {
            string minimumLogLevel = GetMinimumLogLevel();

            LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch();

            LogEventLevel logLevel;
            Enum.TryParse(minimumLogLevel, out logLevel);

            levelSwitch.MinimumLevel = logLevel;

            string logFileName = GetLogFileName();

            string directory = Path.GetDirectoryName(logFileName);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return
                new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.File(logFileName, rollingInterval: RollingInterval.Day);
        }

        /// <summary>
        ///     Creates the global Serilog logger. <paramref name="contentRoot" /> replaces the
        ///     removed <c>HostingEnvironment</c> ambient content root.
        /// </summary>
        internal static void IntializeLogger(string contentRoot)
        {
            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                contentRootPath = contentRoot;
            }

            Log.Logger = GetConfiguration().CreateLogger();

            Log.Information("Application started.");
        }
    }
}
