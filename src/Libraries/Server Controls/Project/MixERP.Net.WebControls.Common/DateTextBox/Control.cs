using MixERP.Net.ApplicationState;
using MixERP.Net.Common.jQueryHelper;
using MixERP.Net.Entities;
using MixERP.Net.i18n.Resources;
using MixERP.Net.WebControls.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MixERP.Net.WebControls.Common
{
    public sealed partial class DateTextBox
    {
        public dynamic compareValidator;
        public dynamic maxDateCompareValidator;
        public dynamic requiredValidator;
        public dynamic textBox;

        private readonly List<object> _controls = new List<object>();
        public List<object> Controls => _controls;

        public void EnsureChildControls() { }

        protected void CreateChildControls() { }
        protected void RecreateChildControls() { }
        protected void Render(object w) { }

        private void AddMaxDateValidator() { }
        private void AddMinDateValidator() { }

        private void InitializeDate()
        {
            if (this.officeId.Equals(0))
            {
                return;
            }

            FrequencyDates model = DatePersister.GetFrequencyDates(this.Catalog, this.officeId);
            DateTime date = model.Today;

            switch (this.mode)
            {
                case FrequencyType.MonthStartDate:
                    date = model.MonthStartDate;
                    break;
                case FrequencyType.MonthEndDate:
                    date = model.MonthEndDate;
                    break;
                case FrequencyType.QuarterStartDate:
                    date = model.QuarterStartDate;
                    break;
                case FrequencyType.QuarterEndDate:
                    date = model.QuarterEndDate;
                    break;
                case FrequencyType.HalfStartDate:
                    date = model.FiscalHalfStartDate;
                    break;
                case FrequencyType.HalfEndDate:
                    date = model.FiscalHalfEndDate;
                    break;
                case FrequencyType.FiscalYearStartDate:
                    date = model.FiscalYearStartDate;
                    break;
                case FrequencyType.FiscalYearEndDate:
                    date = model.FiscalYearEndDate;
                    break;
            }
        }
    }
}
