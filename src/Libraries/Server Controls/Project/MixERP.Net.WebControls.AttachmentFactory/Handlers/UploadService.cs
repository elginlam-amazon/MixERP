using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Framework.WebServices;
using System.Collections.ObjectModel;
using System.Web.Script.Serialization;

namespace MixERP.Net.WebControls.AttachmentFactory.Handlers
{
    /// <summary>
    ///     Attachment upload service, exposed by the host application at "~/FileUploadHanlder.asmx/{method}".
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [ScriptService]
    public class UploadService : WebService
    {
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Xml)]
        public bool UndoUpload(string uploadedFilesJson)
        {
            Collection<Entities.Core.Attachment> uploads = GetAttachmentCollection(uploadedFilesJson);

            string attachmentsDirectory =
                Helpers.ConfigurationHelper.GetAttachmentsDirectory(AppUsers.GetCurrentUserDB());

            foreach (Entities.Core.Attachment upload in uploads)
            {
                string path = this.Server.MapPath(attachmentsDirectory + upload.FilePath);

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            return true;
        }

        public static Collection<Entities.Core.Attachment> GetAttachmentCollection(string json)
        {
            Collection<Entities.Core.Attachment> details = new Collection<Entities.Core.Attachment>();
                        var jss = new JavaScriptSerializer();

            dynamic result = jss.Deserialize<dynamic>(json);

            if (result != null)
            {
                foreach (dynamic item in result)
                {
                    Entities.Core.Attachment detail = new Entities.Core.Attachment();
                    detail.Comment = item["Comment"];
                    detail.FilePath = item["FilePath"];
                    detail.OriginalFileName = item["OriginalFileName"];

                    details.Add(detail);
                }
            }

            return details;
        }
    }
}