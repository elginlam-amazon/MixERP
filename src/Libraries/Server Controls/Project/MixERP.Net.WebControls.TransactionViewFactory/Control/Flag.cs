using MixERP.Net.WebControls.Flag;

namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView : WebControlBase
    {
        private void AddFlagControl(dynamic container)
        {
            using (FlagControl flag = new FlagControl())
            {
                flag.ID = "FlagPopUnder";
                flag.AssociatedControlId = "FlagButton";
                flag.OnClientClick = "return getSelectedItems();";
                flag.CssClass = "ui form segment initially hidden";
                flag.Updated += this.Flag_Updated;
                flag.Catalog = this.Catalog;

                container.Controls.Add(flag);
            }
        }
    }
}