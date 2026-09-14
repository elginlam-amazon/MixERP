using System;
using System.IO;
using System.IO.Compression;

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

            string relativePath = directory.TrimStart('~').TrimStart('/', '\\');
            string path = Path.IsPathRooted(directory)
                ? directory
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));

            try
            {
                Directory.CreateDirectory(path);
                string probe = Path.Combine(path, $".write-test-{Guid.NewGuid():N}");
                using (File.Create(probe, 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
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
