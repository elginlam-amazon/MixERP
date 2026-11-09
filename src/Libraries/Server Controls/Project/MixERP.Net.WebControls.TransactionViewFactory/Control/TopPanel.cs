using MixERP.Net.Common.Helpers;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Common;

namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView : WebControlBase
    {
        private dynamic bookInputText;
        private dynamic dateFromDateTextBox;
        private dynamic dateToDateTextBox;
        private dynamic officeInputText;
        private dynamic postedByInputText;
        private dynamic reasonInputText;
        private dynamic referenceNumberInputText;
        private Button showButton;
        private dynamic statementReferenceInputText;
        private dynamic statusInputText;
        private dynamic tranCodeInputText;
        private dynamic tranIdInputText;
        private dynamic verifiedByInputText;

        private void AddBookInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.bookInputText = new HtmlInputText();
                this.bookInputText.Attributes.Add("placeholder", Titles.Book);

                field.Controls.Add(this.bookInputText);
                container.Controls.Add(field);
            }
        }

        private void AddDateFromDateTextBox(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                using (dynamic iconInput = new HtmlGenericControl("div"))
                {
                    iconInput.Attributes.Add("class", "ui icon input");

                    this.dateFromDateTextBox = new DateTextBox();
                    this.dateFromDateTextBox.ID = "DateFromDateTextBox";
                    this.dateFromDateTextBox.CssClass = "date";
                    this.dateFromDateTextBox.Mode = this.DateFromFromFrequencyType;
                    this.dateFromDateTextBox.Required = true;
                    this.dateFromDateTextBox.Catalog = this.Catalog;
                    this.dateFromDateTextBox.OfficeId = this.OfficeId;

                    iconInput.Controls.Add(this.dateFromDateTextBox);

                    using (dynamic icon = new HtmlGenericControl("i"))
                    {
                        icon.Attributes.Add("class", "icon calendar pointer");
                        icon.Attributes.Add("onclick", "$('#DateFromDateTextBox').datepicker('show');");
                        iconInput.Controls.Add(icon);
                    }

                    field.Controls.Add(iconInput);

                    container.Controls.Add(field);
                }
            }
        }

        private void AddDatetoDateTextBox(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                using (dynamic iconInput = new HtmlGenericControl("div"))
                {
                    iconInput.Attributes.Add("class", "ui icon input");

                    this.dateToDateTextBox = new DateTextBox();
                    this.dateToDateTextBox.ID = "DateToDateTextBox";
                    this.dateToDateTextBox.CssClass = "date";
                    this.dateToDateTextBox.Mode = this.DateToFrequencyType;
                    this.dateToDateTextBox.Required = true;
                    this.dateToDateTextBox.Catalog = this.Catalog;
                    this.dateToDateTextBox.OfficeId = this.OfficeId;

                    iconInput.Controls.Add(this.dateToDateTextBox);

                    using (dynamic icon = new HtmlGenericControl("i"))
                    {
                        icon.Attributes.Add("class", "icon calendar pointer");
                        icon.Attributes.Add("onclick", "$('#DateToDateTextBox').datepicker('show');");
                        iconInput.Controls.Add(icon);
                    }
                    field.Controls.Add(iconInput);
                    container.Controls.Add(field);
                }
            }
        }

        private void AddOfficeInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.officeInputText = new HtmlInputText();
                this.officeInputText.Attributes.Add("placeholder", Titles.Office);

                field.Controls.Add(this.officeInputText);
                container.Controls.Add(field);
            }
        }

        private void AddPostedByInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.postedByInputText = new HtmlInputText();
                this.postedByInputText.Attributes.Add("placeholder", Titles.PostedBy);

                field.Controls.Add(this.postedByInputText);
                container.Controls.Add(field);
            }
        }

        private void AddReasonInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.reasonInputText = new HtmlInputText();
                this.reasonInputText.Attributes.Add("placeholder", Titles.Reason);

                field.Controls.Add(this.reasonInputText);
                container.Controls.Add(field);
            }
        }

        private void AddReferenceNumberInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.referenceNumberInputText = new HtmlInputText();
                this.referenceNumberInputText.Attributes.Add("placeholder", Titles.ReferenceNumber);

                field.Controls.Add(this.referenceNumberInputText);
                container.Controls.Add(field);
            }
        }

        private void AddShowbutton(dynamic container)
        {
            this.showButton = new Button();
            this.showButton.Text = Titles.Show;
            this.showButton.CssClass = "blue ui button";
            this.showButton.Click += this.ShowButton_Click;

            container.Controls.Add(this.showButton);
        }

        private void AddStatementReferenceInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.statementReferenceInputText = new HtmlInputText();
                this.statementReferenceInputText.Attributes.Add("placeholder", Titles.StatementReference);

                field.Controls.Add(this.statementReferenceInputText);
                container.Controls.Add(field);
            }
        }

        private void AddStatusInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.statusInputText = new HtmlInputText();
                this.statusInputText.Attributes.Add("placeholder", Titles.Status);

                field.Controls.Add(this.statusInputText);
                container.Controls.Add(field);
            }
        }

        private void AddTopPanel(dynamic container)
        {
            using (dynamic form = new HtmlGenericControl("div"))
            {
                form.Attributes.Add("class", "ui form segment");

                using (dynamic fields = new HtmlGenericControl("div"))
                {
                    fields.Attributes.Add("class", "eight fields");

                    this.AddDateFromDateTextBox(fields);
                    this.AddDatetoDateTextBox(fields);
                    this.AddTranIdInputText(fields);
                    this.AddTranCodeInputText(fields);
                    this.AddBookInputText(fields);
                    this.AddReferenceNumberInputText(fields);
                    this.AddStatementReferenceInputText(fields);
                    this.AddPostedByInputText(fields);

                    form.Controls.Add(fields);
                }

                using (dynamic fields = new HtmlGenericControl("div"))
                {
                    fields.Attributes.Add("class", "eight fields");

                    this.AddOfficeInputText(fields);
                    this.AddStatusInputText(fields);
                    this.AddVerifiedByInputText(fields);
                    this.AddReasonInputText(fields);
                    this.AddShowbutton(fields);

                    form.Controls.Add(fields);
                }

                container.Controls.Add(form);
            }
        }

        private void AddTranCodeInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.tranCodeInputText = new HtmlInputText();
                this.tranCodeInputText.Attributes.Add("placeholder", Titles.TranCode);

                field.Controls.Add(this.tranCodeInputText);
                container.Controls.Add(field);
            }
        }

        private void AddTranIdInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.tranIdInputText = new HtmlInputText();
                this.tranIdInputText.Attributes.Add("placeholder", Titles.TranId);
                this.tranIdInputText.Attributes.Add("class", "integer");

                field.Controls.Add(this.tranIdInputText);

                container.Controls.Add(field);
            }
        }

        private void AddVerifiedByInputText(dynamic container)
        {
            using (dynamic field = HtmlControlHelper.GetField())
            {
                this.verifiedByInputText = new HtmlInputText();
                this.verifiedByInputText.Attributes.Add("placeholder", Titles.VerifiedBy);

                field.Controls.Add(this.verifiedByInputText);
                container.Controls.Add(field);
            }
        }
    }
}