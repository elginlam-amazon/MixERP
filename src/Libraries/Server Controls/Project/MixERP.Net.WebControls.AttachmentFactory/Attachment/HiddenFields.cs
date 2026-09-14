using MixERP.Net.Framework.Web;
using MixERP.Net.Framework.Web.WebControls;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment
    {
        private void CreateHiddenFields(Control container)
        {
            using (HiddenField uploadedFilesHidden = new HiddenField())
            {
                uploadedFilesHidden.ID = "UploadedFilesHidden";

                container.Controls.Add(uploadedFilesHidden);
            }
        }
    }
}