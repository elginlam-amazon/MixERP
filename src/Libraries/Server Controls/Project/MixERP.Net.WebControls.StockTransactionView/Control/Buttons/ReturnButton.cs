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

using MixERP.Net.Framework;
using MixERP.Net.i18n.Resources;
using System;
using System.Collections.ObjectModel;

namespace MixERP.Net.WebControls.StockTransactionViewFactory
{
    public partial class StockTransactionView : WebControlBase
    {
        private void CreateReturnButton(dynamic container)
        {
            // Stubbed: WebForms UI building removed for net10.0
        }

        private void ReturnButton_Click(dynamic sender, EventArgs e)
        {
            Collection<long> values = this.GetSelectedValues();

            if (string.IsNullOrWhiteSpace(this.ReturnButtonUrl))
            {
                throw new MixERPException(Warnings.ReturnButtonUrlNull);
            }

            this.Page.Response.Redirect(this.ReturnButtonUrl + "?TranId=" + values[0]);
        }
    }
}