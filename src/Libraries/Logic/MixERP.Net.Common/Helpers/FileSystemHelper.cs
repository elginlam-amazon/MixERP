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
        /// <summary>
        ///     Determines whether the application can write into the supplied directory.
        /// </summary>
        /// <remarks>
        ///     Code Access Security (<c>FileIOPermission</c> / <c>PermissionSet</c>) was removed in
        ///     modern .NET, so permissions can no longer be asserted declaratively. The directory is
        ///     probed instead by creating and deleting a scratch file, which is what the CAS demand
        ///     was standing in for.
        /// </remarks>
        public static bool IsDirectoryWritable(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            string path = HostingEnvironment.MapPath(directory);

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            string probe = Path.Combine(path, Path.GetRandomFileName());

            try
            {
                using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1,
                    FileOptions.DeleteOnClose))
                {
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        public static void Unzip(string archive, string destnation)
        {
            ZipFile.ExtractToDirectory(archive, destnation);
        }
    }
}