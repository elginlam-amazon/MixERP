using MixERP.Net.Framework.Web;
using MixERP.Net.Framework.Web.HtmlControls;
using MixERP.Net.Framework.Web.WebControls;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment
    {
        private void CreateUploadPanel(Control container)
        {
            using (HtmlGenericControl fileUploads = new HtmlGenericControl("div"))
            {
                fileUploads.ID = "FileUploads";

                using (Table table = new Table())
                {
                    table.CssClass = "ui table";
                    table.Style.Add("width", "100%");

                    this.CreateHeaderRow(table);
                    this.CreateRow(table);

                    fileUploads.Controls.Add(table);
                }

                container.Controls.Add(fileUploads);
            }
        }
    }
}