using MixERP.Net.Web.UI;
using MixERP.Net.Web.UI.HtmlControls;

namespace MixERP.Net.WebControls.TransactionViewFactory
{
    public partial class TransactionView
    {
        private void AddHeading(Control container)
        {
            using (HtmlGenericControl h1 = new HtmlGenericControl("h1"))
            {
                h1.InnerText = this.Text;

                container.Controls.Add(h1);
            }
        }
    }
}