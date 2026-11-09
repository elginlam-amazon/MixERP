
namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment : WebControlBase
    {
        private void CreateUploadPanel(WebControlBase container)
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
