using MixERP.Net.Entities;
using System;

namespace MixERP.Net.WebControls.Common
{
    public partial class DateTextBox
    {
        private bool enableValidation;
        private FrequencyType mode;
        private string cssClass;
        private bool disabled;
        private string text;
        private int officeId;
        private string catalog;

        public string CssClass
        {
            get { return this.cssClass; }
            set { this.cssClass = value; }
        }

        public bool Disabled
        {
            get { return this.disabled; }
            set { this.disabled = value; }
        }

        public bool EnableValidation
        {
            get { return this.enableValidation; }
            set { this.enableValidation = value; }
        }

        public string ID { get; set; }

        public DateTime? MaxDate { get; set; }

        public DateTime? MinDate { get; set; }

        public FrequencyType Mode
        {
            get { return this.mode; }
            set
            {
                this.mode = value;
                this.InitializeDate();
            }
        }

        public int OfficeId
        {
            get { return this.officeId; }
            set
            {
                this.officeId = value;
                this.InitializeDate();
            }
        }

        public string Catalog
        {
            get { return this.catalog; }
            set { this.catalog = value; }
        }

        public bool Required { get; set; }

        public string Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        public string ValidatorCssClass { get; set; }

        public object Width { get; set; }
    }
}
