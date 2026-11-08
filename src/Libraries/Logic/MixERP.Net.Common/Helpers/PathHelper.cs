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
    public static class PathHelper
    {
        public static string Combine(string path1, string path2)
        {
            string normalized = NormalizePhysicalPath(path2);
            return Path.Combine(path1, normalized);
        }

        public static string NormalizePhysicalPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            //The original implementation hard-coded the Windows separator ("\\"). On a Linux
            //container "\\" is a legal filename character, so that transform collapsed
            //"Resources/Reports" into a single segment literally named "Resources\Reports",
            //silently resolving every resource/report/attachment lookup to a missing file.
            //Normalizing to Path.DirectorySeparatorChar is byte-identical on Windows (where
            //the separator IS '\\') and correct everywhere else.
            string normalized = path.Replace('/', Path.DirectorySeparatorChar)
                                    .Replace('\\', Path.DirectorySeparatorChar);

            if (normalized.StartsWith(Path.DirectorySeparatorChar))
            {
                normalized = normalized.Substring(1);
            }

            return normalized;
        }
    }
}