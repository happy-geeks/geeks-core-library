using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace GeeksCoreLibrary.Core.Helpers;

public static class CompressionHelpers
{
    /// <summary>
    /// Extracts a Zip file to the desired folder.
    /// </summary>
    /// <param name="zipFilePath">The absolute path to the Zip file that should be extracted.</param>
    /// <param name="outFolder">The output folder where the Zip file will be extracted to.</param>
    /// <param name="password">The password that the Zip file was encrypted with.</param>
    /// <returns></returns>
    public static bool ExtractZipFile(string zipFilePath, string outFolder, String password = null)
    {
        ZipFile zipFile = null;

        try
        {
            var fileStream = File.OpenRead(zipFilePath);
            zipFile = new ZipFile(fileStream);

            // AES encrypted entries are handled automatically
            if (String.IsNullOrEmpty(password))
            {
                zipFile.Password = password;
            }

            foreach (ZipEntry zipEntry in zipFile)
            {
                // Ignore directories
                if (!zipEntry.IsFile)
                {
                    continue;
                }

                var entryFileName = zipEntry.Name;

                // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                // 4K is optimum
                var buffer = new byte[4095];
                using var zipStream = zipFile.GetInputStream(zipEntry);
                // Manipulate the output filename here as desired.
                var fullZipToPath = Path.Combine(outFolder, entryFileName);
                var directoryName = Path.GetDirectoryName(fullZipToPath);
                if (!String.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                using var streamWriter = File.Create(fullZipToPath);
                StreamUtils.Copy(zipStream, streamWriter, buffer);
            }

            return true;
        }
        finally
        {
            if (zipFile != null)
            {
                zipFile.IsStreamOwner = true;
                zipFile.Close();
            }
        }
    }
}