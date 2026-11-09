using MixERP.Net.ApplicationState.Cache;
using System;
using System.Collections.ObjectModel;

namespace MixERP.Net.WebControls.AttachmentFactory.Handlers
{
    public class UploadService
    {
        public bool UndoUpload(string uploadedFilesJson)
        {
            Collection<Entities.Core.Attachment> uploads = GetAttachmentCollection(uploadedFilesJson);
            string attachmentsDirectory = Helpers.ConfigurationHelper.GetAttachmentsDirectory(AppUsers.GetCurrentUserDB());

            foreach (Entities.Core.Attachment upload in uploads)
            {
                string path = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    attachmentsDirectory.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar),
                    upload.FilePath ?? string.Empty);

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

            if (string.IsNullOrWhiteSpace(json))
                return details;

            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                if (parsed.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in parsed.EnumerateArray())
                    {
                        Entities.Core.Attachment detail = new Entities.Core.Attachment();
                        if (item.TryGetProperty("Comment", out var c)) detail.Comment = c.GetString();
                        if (item.TryGetProperty("FilePath", out var fp)) detail.FilePath = fp.GetString();
                        if (item.TryGetProperty("OriginalFileName", out var ofn)) detail.OriginalFileName = ofn.GetString();
                        details.Add(detail);
                    }
                }
            }
            catch { }

            return details;
        }
    }
}
