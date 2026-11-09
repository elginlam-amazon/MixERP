using MixERP.Net.ApplicationState.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    public class FileUploadHanlder
    {
        public bool IsReusable => true;

        private List<string> GetAllowedExtensions()
        {
            return Helpers.ConfigurationHelper.GetAllowedExtensions(AppUsers.GetCurrentUserDB()).Split(',').ToList();
        }

        private int RandomNumber()
        {
            Random rnd = new Random();
            return rnd.Next(9999999);
        }
    }
}
