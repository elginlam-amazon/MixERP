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

namespace MixERP.Net.WebControls.TransactionChecklist
{
    public partial class TransactionChecklistForm : WebControlBase
    {
        private dynamic okButton;
        private dynamic reasonTextBox;

        private void AddCancelButton(dynamic p)
        {
            using (dynamic button = new HtmlInputButton())
            {
                button.ID = "CancelButton";
                button.CausesValidation = false;
                button.Value = Titles.Cancel;
                button.Attributes.Add("class", "ui red small submit button");

                p.Controls.Add(button);
            }
        }

        private void AddOkButton(dynamic p)
        {
            this.okButton = new Button();
            this.okButton.ID = "OKButton";
            this.okButton.CausesValidation = true;
            this.okButton.Text = Titles.OK;
            this.okButton.Attributes.Add("class", "ui red small submit button");
            // this.okButton.Click += this.OkButton_Click; // stub: dynamic event not supported

            p.Controls.Add(this.okButton);
        }

        private void AddReasonTextBox(dynamic p)
        {
            this.reasonTextBox = new TextBox();
            this.reasonTextBox.ID = "ReasonTextBox";
            this.reasonTextBox.ClientIDMode = ClientIDMode.Static;
            this.reasonTextBox.TextMode = TextBoxMode.MultiLine;
            this.reasonTextBox.Rows = 5;

            p.Controls.Add(this.reasonTextBox);

            using (dynamic reasonTextBoxRequired = new RequiredFieldValidator())
            {
                reasonTextBoxRequired.ID = "ReasonTextBoxRequired";
                reasonTextBoxRequired.ControlToValidate = this.reasonTextBox.ID;
                reasonTextBoxRequired.EnableClientScript = true;
                reasonTextBoxRequired.ErrorMessage = Labels.ThisFieldIsRequired;
                reasonTextBoxRequired.CssClass = "error-message";
                reasonTextBoxRequired.Display = ValidatorDisplay.Dynamic;
                p.Controls.Add(reasonTextBoxRequired);
            }
        }

        private void AddSpacer(dynamic div)
        {
            using (dynamic spacer = new HtmlGenericControl("span"))
            {
                spacer.Attributes.Add("class", "spacer");
                spacer.InnerHtml = "&nbsp;";
                div.Controls.Add(spacer);
            }
        }

        private void AddWidthdrawDiv(dynamic p)
        {
            if (this.DisplayWithdrawButton)
            {
                using (dynamic withdrawDiv = new HtmlGenericControl())
                {
                    withdrawDiv.TagName = "div";
                    withdrawDiv.ID = "WithdrawDiv";
                    withdrawDiv.Attributes.Add("style", "max-width:" + this.GetWidth() + "px;");

                    this.AddWithdrawHeader(withdrawDiv);
                    this.AddWithdrawPanelBody(withdrawDiv);
                    this.AddWithdrawFooter(withdrawDiv);
                    p.Controls.Add(withdrawDiv);
                }
            }
        }

        private void AddWithdrawFooter(dynamic div)
        {
            using (dynamic footer = new HtmlGenericControl("div"))
            {
                footer.Attributes.Add("class", "ui bottom attached red info message");
                footer.InnerHtml = @"<i class='icon help'></i>" + Questions.AreYouSure;
                div.Controls.Add(footer);
            }
        }

        private void AddWithdrawHeader(dynamic div)
        {
            using (dynamic headerContainer = new HtmlGenericControl("div"))
            {
                headerContainer.Attributes.Add("class", "ui attached red message");
                headerContainer.Attributes.Add("style", "display:block;");

                using (dynamic header = new HtmlGenericControl("div"))
                {
                    header.Attributes.Add("class", "header");
                    header.InnerText = Questions.WithdrawalReason;
                    headerContainer.Controls.Add(header);
                }

                div.Controls.Add(headerContainer);
            }
        }

        private void AddWithdrawPanelBody(dynamic div)
        {
            using (dynamic form = new HtmlGenericControl("div"))
            {
                form.Attributes.Add("class", "ui form attached fluid segment");

                using (dynamic paragraph = new HtmlGenericControl("p"))
                {
                    paragraph.InnerText = Labels.TransactionWithdrawalInformation;
                    form.Controls.Add(paragraph);
                }

                using (dynamic field = new HtmlGenericControl("div"))
                {
                    field.Attributes.Add("class", "field");
                    this.AddReasonTextBox(field);
                    form.Controls.Add(field);
                }

                this.AddOkButton(form);
                this.AddSpacer(form);
                this.AddCancelButton(form);

                div.Controls.Add(form);
            }
        }
    }
}