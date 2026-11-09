
namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView : WebControlBase
    {
        private dynamic selectedValuesHidden;

        private void AddHiddenFields(dynamic container)
        {
            this.selectedValuesHidden = new HiddenField();
            this.selectedValuesHidden.ID = "SelectedValuesHidden";
            container.Controls.Add(this.selectedValuesHidden);
        }
    }
}