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

using System.IO;

namespace MixERP.Net.Common.Helpers
{
    public static class DirectoryHelper
    {
        public static void Empty(this DirectoryInfo directory)
        {
            if (!Directory.Exists(directory.FullName))
            {
                return;
            }

            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }

        public static void CopyDirectory(string source, string destination)
        {
            // Equivalent of Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(source, destination, overwrite: true).
            DirectoryInfo sourceDirectory = new DirectoryInfo(source);

            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException(source);
            }

            Directory.CreateDirectory(destination);

            foreach (FileInfo file in sourceDirectory.GetFiles())
            {
                file.CopyTo(Path.Combine(destination, file.Name), true);
            }

            foreach (DirectoryInfo subDirectory in sourceDirectory.GetDirectories())
            {
                CopyDirectory(subDirectory.FullName, Path.Combine(destination, subDirectory.Name));
            }
        }
    }
}