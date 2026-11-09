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

using MixERP.Net.Common;
using System.Reflection;

namespace MixERP.Net.WebControls.ReportEngine
{
        public partial class Report : WebControlBase
    {
        protected void CreateChildControls()
        {
            this.reportContainer = new Panel();
            this.AddHiddenControls(this.reportContainer);
            this.AddCommandPanel(this.reportContainer);
            this.AddReportBody(this.reportContainer);
            this.Controls.Add(this.reportContainer);

            PageUtility.AddMeta(this.Page, "generator", Assembly.GetAssembly(typeof (Report)).GetName().Name);

            if (this.AutoInitialize)
            {
                this.InitializeReport();
            }
        }


        protected void RecreateChildControls()
        {
            this.EnsureChildControls();
        }

        protected void Render(dynamic w)
        {
            this.reportContainer.RenderControl(w);
        }
    }
}