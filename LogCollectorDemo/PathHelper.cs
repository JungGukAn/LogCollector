using System.IO;

namespace Util
{
    public class PathHelper
    {
        public static bool TryFindFilePathFromAppLocation(string basePath, out string path, int retryCount = 10)
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();

            var fileName = Path.GetFileName(basePath);
            var directoryName = Path.GetDirectoryName(assembly.Location);

            var fullPath = Path.Combine(directoryName, fileName);

            return TryFindFilePath(fullPath, out path, retryCount);
        }

        public static bool TryFindFilePath(string basePath, out string path, int retryCount = 10)
        {
            path = default;

            if (File.Exists(basePath))
            {
                path = basePath;
                return true;
            }

            var directoryName = Path.GetDirectoryName(basePath);
            var fileName = Path.GetFileName(basePath);

            if (string.IsNullOrWhiteSpace(directoryName))
            {
                directoryName = Directory.GetCurrentDirectory();
            }

            var directory = new DirectoryInfo(directoryName).Parent;

            for (int i = 0; i < retryCount; i++)
            {
                path = Path.Combine(directory.FullName, fileName);

                if (File.Exists(path))
                {
                    return true;
                }

                if (directory.Parent == null)
                    break;

                directory = directory.Parent;
            }

            return false;
        }
    }
}
