using System.IO;

namespace SwiftSeek
{
    static class FileUtils
    {
        public static bool IsBinary(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[512];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0) return true;
                }
            }
            catch
            {
                // Assume binary if we can't read the file
                return true;
            }

            return false;
        }
    }
}