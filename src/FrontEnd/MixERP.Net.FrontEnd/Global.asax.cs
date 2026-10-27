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
using System.Web.Routing;
using System.Web.UI;
using MixERP.Net.Entities.Office;
using MixERP.Net.FrontEnd.Application;
using MixERP.Net.ReportManager;

namespace MixERP.Net.FrontEnd
{
    public class Global : HttpApplication
    {
        protected void Application_Error(object sender, EventArgs e)
        {
            ApplicationError.Handle(this.Server.GetLastError());
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            LogManager.IntializeLogger();
            UpdateManager.CheckForUpdates(this.Application);
            Routes.RegisterRoutes(RouteTable.Routes);

            GlobalLogin.CreateTable();
            Repository.DownloadAndInstallReports();
        }

        /// <summary>
        ///     Applies the former Web.config &lt;pages enableViewState="false" enableEventValidation="false" clientIDMode="Static" /&gt;
        ///     defaults before the page is processed, so values in the @Page directive still take precedence.
        /// </summary>
        protected void Application_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            Page page = this.Context.Handler as Page;

            if (page == null)
            {
                return;
            }

            page.ClientIDMode = ClientIDMode.Static;
            page.EnableViewState = false;
            page.EnableEventValidation = false;
        }
    }
}
