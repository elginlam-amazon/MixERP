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
using System.Web;
using System.Web.UI;
using MixERP.Net.Entities.Office;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.ReportManager;

namespace MixERP.Net.FrontEnd
{
    /// <summary>
    ///     Application events, raised by the System.Web adapters (registered with AddHttpApplication&lt;Global&gt;()).
    ///     Routes are registered in Program.cs.
    /// </summary>
    public class Global : HttpApplication
    {
        public Global()
        {
            this.PreRequestHandlerExecute += ApplyPageDefaults;
        }

        /// <summary>
        ///     Applies the former Web.config &lt;pages clientIDMode="Static" enableViewState="false"
        ///     enableEventValidation="false" /&gt; defaults; page directives still override them.
        /// </summary>
        private static void ApplyPageDefaults(object sender, EventArgs e)
        {
            Page page = HttpContext.Current == null ? null : HttpContext.Current.Handler as Page;

            if (page == null)
            {
                return;
            }

            page.ClientIDMode = ClientIDMode.Static;
            page.EnableViewState = false;
            page.EnableEventValidation = false;
        }

        private void Application_Error(object sender, EventArgs e)
        {
            ApplicationError.Handle(this.Server.GetLastError());
        }

        private void Application_Start(object sender, EventArgs e)
        {
            LogManager.IntializeLogger();
            UpdateManager.CheckForUpdates(this.Application);

            GlobalLogin.CreateTable();
            Repository.DownloadAndInstallReports();
        }
    }
}