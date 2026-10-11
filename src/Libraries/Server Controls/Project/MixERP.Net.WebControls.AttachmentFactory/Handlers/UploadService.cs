using MixERP.Net.ApplicationState.Cache;
using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using MixERP.Net.Web.Script.Services;
using MixERP.Net.Web.Services;

namespace MixERP.Net.WebControls.AttachmentFactory.Handlers
{
    /// <summary>
    ///     Script service mapped to FileUploadHanlder.asmx. The host's script-service endpoint dispatches its web methods
    ///     (the former IHttpHandlerFactory/WebServiceHandlerFactory plumbing is not needed on ASP.NET Core).
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
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
            dynamic result = JsonConvert.DeserializeObject<dynamic>(json);

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