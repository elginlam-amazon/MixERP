using MixERP.Net.Framework.Web;
using MixERP.Net.Framework.Web.HtmlControls;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment
    {
        private void CreateWarningLabel(Control container)
        {
            using (HtmlGenericControl p = new HtmlGenericControl("p"))
            {
                this.warningLabel = new HtmlGenericControl("span");
                this.warningLabel.ID = "WarningLabel";
                this.warningLabel.Attributes.Add("class", "big error");

                p.Controls.Add(this.warningLabel);

                container.Controls.Add(p);
            }
        }
    }
}