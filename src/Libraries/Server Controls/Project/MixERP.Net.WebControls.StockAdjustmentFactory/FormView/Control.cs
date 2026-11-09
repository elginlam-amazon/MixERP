using MixERP.Net.WebControls.Common;
using System;

namespace MixERP.Net.WebControls.StockAdjustmentFactory
{
            public partial class FormView : WebControlBase
    {
        private dynamic container;
        private dynamic dateTextBox;
        public string ItemIdQuerySericeUrl { get; set; }
        public string ItemPopupUrl { get; set; }
        public string ItemServiceUrl { get; set; }
        public string StoreServiceUrl { get; set; }
        public string ShippingCompanyServiceUrl { get; set; }
        public string Text { get; set; }
        public string UnitServiceUrl { get; set; }
        public bool ValidateSides { get; set; }
        public bool HideSides { get; set; }

        protected void CreateChildControls()
        {
            this.container = new HtmlGenericControl();
            this.CreateHeader();
            this.AddRuler();
            this.CreateTopPanel();
            this.CreateGridPanel();
            this.AddErrorLabelBottom();
            this.CreateBottomPanel();
            this.AddInlineScript();
            this.AddJavascript();

            this.Controls.Add(this.container);
        }

        protected void RecreateChildControls()
        {
            this.EnsureChildControls();
        }

        protected void Render(dynamic w)
        {
            this.container.RenderControl(w);
        }

        #region IDisposable

        private bool disposed;

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
                base.Dispose();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (this.container != null)
            {
                this.container.Dispose();
                this.container = null;
            }

            if (this.dateTextBox != null)
            {
                this.dateTextBox.Dispose();
                this.dateTextBox = null;
            }

            this.disposed = true;
        }

        #endregion
    }
}