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

using MixERP.Net.Common.Helpers;
using MixERP.Net.Entities;

namespace MixERP.Net.WebControls.StockTransactionFactory
{
    public partial class StockTransactionForm : WebControlBase
    {
        private void CreateTopFormPanel(dynamic container)
        {
            using (dynamic segment = HtmlControlHelper.GetSegment())
            {
                using (dynamic table = new HtmlTable())
                {
                    table.Attributes.Add("class", "ui form");

                    this.AddTopFormLabels(table);
                    this.AddTopFormControls(table);
                    segment.Controls.Add(table);
                }

                container.Controls.Add(segment);
            }


            using (dynamic form = HtmlControlHelper.GetForm())
            {
                using (dynamic fields = HtmlControlHelper.GetFields("two fields"))
                {
                    if (this.ShowShippingInformation)
                    {
                        AddShippingAddressCompositeField(fields);
                    }

                    if (this.Book == TranBook.Sales && this.ShowSalesType)
                    {
                        this.AddSalesTypeField(fields);
                    }

                    form.Controls.Add(fields);
                }

                container.Controls.Add(form);
            }
        }
    }
}