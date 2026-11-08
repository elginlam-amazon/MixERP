using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;

namespace MixERP.Net.WebControls.AttachmentFactory.Helpers
{
    public static class ConfigurationHelper
    {
        //The former private const "AttachmentFactoryConfigFileLocation" is removed: it was
        //already dead in the original (a grep over the solution finds no reader) and it named
        //an appSetting pointing at a legacy external *.config file. All four methods below read
        //their values from the database through DbConfig.GetAttachmentParameter instead.

        public static string GetAllowedExtensions(string catalog)
        {
            return DbConfig.GetAttachmentParameter(catalog, "AllowedExtensions");
        }

        public static string GetUploadHandlerUrl(string catalog)
        {
            string url = DbConfig.GetAttachmentParameter(catalog, "UploadHandlerUrl");
            return PageUtility.ResolveUrl(url);
        }

        public static string GetAttachmentsDirectory(string catalog)
        {
            string url = DbConfig.GetAttachmentParameter(catalog, "AttachmentsDirectory");
            return PageUtility.ResolveUrl(url);
        }

        public static string GetUndoUploadServiceUrl(string catalog)
        {
            string url = DbConfig.GetAttachmentParameter(catalog, "UndoUploadServiceUrl");
            return PageUtility.ResolveUrl(url);
        }
    }
}