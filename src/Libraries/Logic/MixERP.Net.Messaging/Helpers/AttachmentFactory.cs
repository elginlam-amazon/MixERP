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

using MimeKit;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace MixERP.Net.Messaging.Email
{
    public static class AttachmentFactory
    {
        /// <summary>
        ///     Builds MIME attachment parts for the supplied files.
        ///     Replaces System.Net.Mail.Attachment (obsolete together with SmtpClient) with
        ///     MimeKit's MimePart, keeping every content-disposition field that was set before.
        /// </summary>
        public static IEnumerable<MimeEntity> GetAttachments(params string[] files)
        {
            Collection<MimeEntity> attachments = new Collection<MimeEntity>();

            if (files != null)
            {
                foreach (string file in files)
                {
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        //MediaTypeNames.Application.Octet == "application/octet-stream".
                        MimePart attachment = new MimePart("application", "octet-stream")
                        {
                            Content = new MimeContent(File.OpenRead(file)),
                            ContentTransferEncoding = ContentEncoding.Base64
                        };

                        ContentDisposition disposition =
                            new ContentDisposition(ContentDisposition.Attachment)
                            {
                                CreationDate = File.GetCreationTime(file),
                                ModificationDate = File.GetLastWriteTime(file),
                                ReadDate = File.GetLastAccessTime(file),
                                FileName = Path.GetFileName(file),
                                Size = new FileInfo(file).Length
                            };

                        attachment.ContentDisposition = disposition;

                        //The original code wrapped the attachment in a "using" block and then
                        //returned it, disposing the file stream before the message was ever
                        //sent. The attachments are owned by the caller (the MimeMessage, which
                        //disposes them with the message), so they are not disposed here.
                        attachments.Add(attachment);
                    }
                }
            }

            return attachments;
        }
    }
}
