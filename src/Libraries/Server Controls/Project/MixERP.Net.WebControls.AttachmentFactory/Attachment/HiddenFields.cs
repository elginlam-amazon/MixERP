
namespace MixERP.Net.WebControls.AttachmentFactory
{
    public sealed partial class Attachment : WebControlBase
    {
        private void CreateHiddenFields(WebControlBase container)
        {
            using (HiddenField uploadedFilesHidden = new HiddenField())
            {
                uploadedFilesHidden.ID = "UploadedFilesHidden";

                container.Controls.Add(uploadedFilesHidden);
            }
        }
    }
}
