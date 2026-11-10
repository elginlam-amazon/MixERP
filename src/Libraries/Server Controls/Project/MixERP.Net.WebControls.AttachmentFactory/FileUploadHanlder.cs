/********************************************************************************
Copyright (C) Binod Nepal, Mix Open Foundation (http://mixof.org).

This file is part of MixERP.

MixERP is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MixERP is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MixERP.  If not, see <http://www.gnu.org/licenses/>.
***********************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MixERP.Net.ApplicationState.Cache;
using MixERP.Net.Framework;

namespace MixERP.Net.WebControls.AttachmentFactory
{
    /// <summary>
    ///     Receives attachment uploads. Replaces the FileUploadHanlder.ashx IHttpHandler (same
    ///     url, same plain-text "file1,file2" response).
    /// </summary>
    [Route("FileUploadHanlder.ashx")]
    public class FileUploadHanlder : Controller
    {
        [AcceptVerbs("GET", "POST")]
        public IActionResult ProcessRequest()
        {
            string attachmentsDirectory = Helpers.ConfigurationHelper.GetAttachmentsDirectory(AppUsers.GetCurrentUserDB());
            Collection<string> uploadedFiles = new Collection<string>();

            if (this.Request.HasFormContentType && this.Request.Form.Files.Count > 0)
            {
                List<string> allowedExtensions = GetAllowedExtensions();

                foreach (IFormFile file in this.Request.Form.Files)
                {
                    string extension = Path.GetExtension(file.FileName);

                    if (allowedExtensions.Contains(extension.Replace(".", "")))
                    {
                        string fileName = RandomNumber().ToString(CultureInfo.InvariantCulture) + "_" + Path.GetFileName(file.FileName);
                        string savePath = WebContext.MapPath(Path.Combine(attachmentsDirectory, fileName));

                        using (FileStream stream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                        {
                            file.CopyTo(stream);
                        }

                        uploadedFiles.Add(fileName);
                    }
                }
            }

            return this.Content(string.Join(",", uploadedFiles), "text/plain");
        }

        private static List<string> GetAllowedExtensions()
        {
            return Helpers.ConfigurationHelper.GetAllowedExtensions(AppUsers.GetCurrentUserDB()).Split(',').ToList();
        }

        private static int RandomNumber()
        {
            return RandomNumberGenerator.GetInt32(9999999);
        }
    }
}
