
namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView : WebControlBase
    {
        private void AddHeading(dynamic container)
        {
            using (dynamic h1 = new HtmlGenericControl("h1"))
            {
                h1.InnerText = this.Text;

                container.Controls.Add(h1);
            }
        }
    }
}