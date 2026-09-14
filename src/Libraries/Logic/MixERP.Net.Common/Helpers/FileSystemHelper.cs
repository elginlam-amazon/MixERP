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
using System.IO;
using System.IO.Compression;
using System.Web.Hosting;

namespace MixERP.Net.Common.Helpers
{
    public static class FileSystemHelper
    {
        public static bool IsDirectoryWritable(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            try
            {
                string physicalPath = HostingEnvironment.MapPath(directory);
                if (!Directory.Exists(physicalPath))
                {
                    return false;
                }
                // Try to write a temp file to check writability
                string testFile = Path.Combine(physicalPath, Path.GetRandomFileName());
                using (File.Create(testFile))
                {
                }
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Unzip(string archive, string destination)
        {
            ZipFile.ExtractToDirectory(archive, destination);
        }
    }
}
