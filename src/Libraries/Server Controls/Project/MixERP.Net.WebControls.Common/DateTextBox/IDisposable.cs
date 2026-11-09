using System;

namespace MixERP.Net.WebControls.Common
{
    public partial class DateTextBox : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
            }
        }
    }
}
