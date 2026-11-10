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
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using MixERP.Net.Common;
using MixERP.Net.Common.Helpers;
using MixERP.Net.Framework;
using SkiaSharp;

namespace MixERP.Net.Core.Modules.BackOffice.Handlers
{
    /// <summary>
    ///     Serves resized images. Replaces the ImageHandler.ashx IHttpHandler at the same url
    ///     (?Path=~/x.png&amp;W=..&amp;H=..). Resizing moved from System.Drawing to SkiaSharp.
    /// </summary>
    [Route("Modules/BackOffice/Handlers/ImageHandler.ashx")]
    public class ImageHandler : Controller
    {
        [HttpGet]
        public IActionResult ProcessImage([FromQuery(Name = "Path")] string path, [FromQuery(Name = "W")] string w,
            [FromQuery(Name = "H")] string h)
        {
            int width = 0;
            int height = 0;

            path = Conversion.TryCastString(path);

            if (!string.IsNullOrWhiteSpace(w))
            {
                width = Convert.ToInt32(w, CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(h))
            {
                height = Convert.ToInt32(h, CultureInfo.InvariantCulture);
            }

            string physicalPath = WebContext.MapPath(path);

            // Only files inside the site may be served (the Web Forms MapPath refused paths
            // outside the application; Path.Combine does not).
            if (string.IsNullOrWhiteSpace(physicalPath) || !IsInsideSite(physicalPath) || !System.IO.File.Exists(physicalPath))
            {
                return new EmptyResult();
            }

            FileInfo file = new FileInfo(physicalPath);

            if (!IsValidBitmap(physicalPath))
            {
                physicalPath = WebContext.MapPath("~/Static/images/document.png");
            }

            byte[] buffer = ImageHelper.GetResizedImage(System.IO.File.ReadAllBytes(physicalPath), width, height);

            if (buffer == null)
            {
                return new EmptyResult();
            }

            this.Response.Headers.CacheControl = "public, max-age=2592000";
            this.Response.Headers.Expires = DateTime.UtcNow.AddMonths(1).ToString("R", CultureInfo.InvariantCulture);
            this.Response.Headers.LastModified = DateTime.Now.ToShortDateString();

            return this.File(buffer, ImageHelper.GetContentType(file.Extension));
        }

        private static bool IsInsideSite(string physicalPath)
        {
            string root = Path.GetFullPath(WebContext.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(physicalPath).StartsWith(root, StringComparison.Ordinal);
        }

        private static bool IsValidBitmap(string path)
        {
            try
            {
                using (SKCodec codec = SKCodec.Create(path))
                {
                    return codec != null;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
