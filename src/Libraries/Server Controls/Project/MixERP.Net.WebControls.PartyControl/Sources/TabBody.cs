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

using MixERP.Net.i18n.Resources;

namespace MixERP.Net.WebControls.PartyControl
{
    public partial class PartyControl : WebControlBase
    {
        private void AddHiddenField(dynamic p, string id)
        {
            using (dynamic hidden = new HiddenField())
            {
                hidden.ID = id;
                p.Controls.Add(hidden);
            }
        }

        private void AddTabBody(dynamic p)
        {
            using (dynamic homeTab = ControlHelper.GetGenericControl(@"div", "ui active tab bottom stacked attached segment"))
            {
                homeTab.Attributes.Add("data-tab", "home");
                homeTab.ID = "home";
                p.Controls.Add(homeTab);
            }

            p.Controls.Add(this.GetPartySummaryTab());
            p.Controls.Add(this.GetTransactionSummaryTab());
            p.Controls.Add(this.GetAddressInfoTab());
        }

        private dynamic GetAddressInfoTab()
        {
            using (dynamic addressInfoDiv = ControlHelper.GetGenericControl(@"div", @"ui tab bottom stacked attached teal segment"))
            {
                addressInfoDiv.Attributes.Add("data-tab", "contact-info");
                addressInfoDiv.ID = "addresses-and-contact-info";

                using (dynamic h3 = ControlHelper.GetGenericControl("h3", "ui header"))
                {
                    h3.InnerHtml = "  <i class='globe icon'></i>" + Titles.AddressAndContactInfo;

                    addressInfoDiv.Controls.Add(h3);
                }

                using (dynamic table = new HtmlTable())
                {
                    table.Attributes.Add("class", "ui table segment");
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.DefaultAddress, @"AddressDiv", "div"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.ShippingAddresses, @"ShippingAddressesDiv", "div"));
                    addressInfoDiv.Controls.Add(table);
                }

                return addressInfoDiv;
            }
        }

        private dynamic GetPartySummaryTab()
        {
            using (dynamic partSummaryDiv = ControlHelper.GetGenericControl(@"div", @"ui tab bottom stacked attached green segment"))
            {
                partSummaryDiv.Attributes.Add("data-tab", "party-summary");

                partSummaryDiv.ID = "party-summary";

                using (dynamic table = new HtmlTable())
                {
                    table.Attributes.Add("class", "ui table segment");
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.PartyType, @"PartyTypeSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.EmailAddress, @"EmailAddressSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.PANNumber, @"PANNumberSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.SSTNumber, @"SSTNumberSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.CSTNumber, @"CSTNumberSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.CreditAllowed, @"CreditAllowedSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.MaximumCreditPeriod, @"MaxCreditPeriodSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.MaximumCreditAmount, @"MaxCreditAmountSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.GLHead, @"GLHeadSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.DefaultCurrency, @"DefaultCurrencySpan", @"span"));
                    partSummaryDiv.Controls.Add(table);
                }

                return partSummaryDiv;
            }
        }

        private dynamic GetTransactionSummaryTab()
        {
            using (dynamic transactionSummaryDiv = ControlHelper.GetGenericControl(@"div", @"ui tab bottom stacked attached red segment"))
            {
                transactionSummaryDiv.Attributes.Add("data-tab", "transaction-summary");

                transactionSummaryDiv.ID = "transaction-summary";

                using (dynamic table = new HtmlTable())
                {
                    table.Attributes.Add("class", "ui table segment");
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.TotalDueAmount, @"TotalDueAmountSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.TotalDueAmountCurrentOffice, @"OfficeDueAmountSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.LastPaymentDate, @"LastPaymentDateSpan", @"span"));
                    table.Rows.Add(ControlHelper.GetNewRow(Titles.TransactionValue, @"TransactionValueSpan", @"span"));

                    transactionSummaryDiv.Controls.Add(table);
                }

                return transactionSummaryDiv;
            }
        }
    }
}