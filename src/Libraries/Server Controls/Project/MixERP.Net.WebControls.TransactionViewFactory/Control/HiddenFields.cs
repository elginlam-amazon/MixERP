using MixERP.Net.Framework.Web;
using MixERP.Net.Framework.Web.WebControls;

namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView
    {
        private HiddenField selectedValuesHidden;

        private void AddHiddenFields(Control container)
        {
            this.selectedValuesHidden = new HiddenField();
            this.selectedValuesHidden.ID = "SelectedValuesHidden";
            container.Controls.Add(this.selectedValuesHidden);
        }
    }
}