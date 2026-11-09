using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.StockAdjustmentFactory.Helpers;

namespace MixERP.Net.WebControls.StockAdjustmentFactory
{
    public partial class FormView : WebControlBase
    {
        private void AddErrorLabelBottom()
        {
            using (dynamic errorLabel = new HtmlGenericControl())
            {
                errorLabel.TagName = "div";
                errorLabel.ID = "ErrorLabel";
                errorLabel.Attributes.Add("class", "big error vpad16");
                this.container.Controls.Add(errorLabel);
            }
        }

        private void AddSaveButton(dynamic div)
        {
            using (dynamic button = new HtmlInputButton())
            {
                button.ID = "SaveButton";
                button.Value = Titles.Save;
                button.Attributes.Add("class", "ui small blue button");
                div.Controls.Add(button);
            }
        }

        private void AddStatementReferenceTextArea(dynamic fields)
        {
            using (dynamic field = FormHelper.GetField())
            {
                using (dynamic label = new HtmlGenericControl())
                {
                    label.TagName = "label";
                    label.Attributes.Add("for", "StatementReferenceTextArea");
                    label.InnerText = Titles.StatementReference;
                    field.Controls.Add(label);
                }

                using (HtmlTextArea statementReferenceTextArea = new HtmlTextArea())
                {
                    statementReferenceTextArea.ID = "StatementReferenceTextArea";
                    statementReferenceTextArea.Rows = 4;

                    field.Controls.Add(statementReferenceTextArea);
                }

                fields.Controls.Add(field);
            }
        }

        private void AddSourceStoreSelect(dynamic fields)
        {
            if (!this.DisplaySourceStore)
            {
                return;
            }

            using (dynamic field = FormHelper.GetField())
            {
                using (dynamic label = new HtmlGenericControl())
                {
                    label.TagName = "label";
                    label.Attributes.Add("for", "SourceStoreSelect");
                    label.InnerText = Titles.DeliverFrom;
                    field.Controls.Add(label);
                }

                using (HtmlSelect sourceStoreSelect = new HtmlSelect())
                {
                    sourceStoreSelect.ID = "SourceStoreSelect";

                    field.Controls.Add(sourceStoreSelect);
                }

                fields.Controls.Add(field);
            }
        }

        private void AddShippingCompanySelect(dynamic fields)
        {
            if (!this.DisplayShipper)
            {
                return;
            }

            using (dynamic field = FormHelper.GetField())
            {
                using (dynamic label = new HtmlGenericControl())
                {
                    label.TagName = "label";
                    label.Attributes.Add("for", "ShippingCompanySelect");
                    label.InnerText = Titles.ShippingCompany;
                    field.Controls.Add(label);
                }

                using (HtmlSelect shippingCompanySelect = new HtmlSelect())
                {
                    shippingCompanySelect.ID = "ShippingCompanySelect";

                    field.Controls.Add(shippingCompanySelect);
                }

                fields.Controls.Add(field);
            }
        }

        private void CreateBottomPanel()
        {
            using (dynamic fields = FormHelper.GetFields())
            {
                fields.TagName = "div";
                fields.Attributes.Add("class", "ui form");
                fields.Attributes.Add("style", "width:290px;");

                this.AddSourceStoreSelect(fields);
                this.AddShippingCompanySelect(fields);
                this.AddStatementReferenceTextArea(fields);
                this.AddSaveButton(fields);

                this.container.Controls.Add(fields);
            }
        }
    }
}