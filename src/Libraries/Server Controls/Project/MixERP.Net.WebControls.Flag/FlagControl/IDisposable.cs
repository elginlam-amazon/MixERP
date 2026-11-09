namespace MixERP.Net.WebControls.Flag
{
    public partial class FlagControl : WebControlBase
    {
        private bool disposed;

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
                base.Dispose();
            }
        }

        private void Dispose(bool disposing)
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

            if (this.flagDropDownlist != null)
            {
                this.flagDropDownlist.Dispose();
                this.flagDropDownlist = null;
            }

            if (this.updateButton != null)
            {
                this.updateButton.Dispose();
                this.updateButton = null;
            }

            this.disposed = true;
        }
    }
}