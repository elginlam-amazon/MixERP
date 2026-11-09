using MixERP.Net.Common;
using System;

namespace MixERP.Net.WebControls.Flag
{
    public partial class FlagControl : WebControlBase
    {
        public delegate void FlagUpdated(dynamic sender, FlagUpdatedEventArgs e);

        public event FlagUpdated Updated;

        private void ButtonOnClick(dynamic sender, EventArgs eventArgs)
        {
            if (this.Updated != null)
            {
                this.Updated(this, new FlagUpdatedEventArgs(this.GetFlagTypeId()));
            }
        }

        private int GetFlagTypeId()
        {
            int flagTypeId = Conversion.TryCastInteger(this.flagDropDownlist.SelectedValue);
            return flagTypeId;
        }
    }
}