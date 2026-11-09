
namespace MixERP.Net.WebControls.StockAdjustmentFactory
{
    public partial class FormView : WebControlBase
    {
        private void CreateHeader()
        {
            using (dynamic header = new HtmlGenericControl())
            {
                header.TagName = "h2";
                header.InnerText = this.Text;
                this.container.Controls.Add(header);
            }
        }

        private void AddRuler()
        {
            using (dynamic divider = new HtmlGenericControl("div"))
            {
                divider.Attributes.Add("class", "ui divider");
                this.container.Controls.Add(divider);
            }
        }
    }
}
