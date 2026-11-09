
namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment : WebControlBase
    {
        private bool disposed;

        private dynamic placeHolder;
        private dynamic warningLabel;

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


            if (this.placeHolder != null)
            {
                this.placeHolder.Dispose();
                this.placeHolder = null;
            }

            if (this.warningLabel != null)
            {
                this.warningLabel.Dispose();
                this.warningLabel = null;
            }

            this.disposed = true;
        }
    }
}