using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace GeeksCoreLibrary.Core.Helpers;

public static class FileSystemHelpers
{
    public static string GetContentFilesFolderPath(IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var result = Path.Combine(webHostEnvironment.WebRootPath, Modules.ItemFiles.Models.Constants.DefaultFilesDirectory);
        return !Directory.Exists(result) ? null : result;
    }

    public static string GetContentCacheFolderPath(IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var result = Path.Combine(webHostEnvironment.WebRootPath, "contentcache");
        return !Directory.Exists(result) ? null : result;
    }

    public static string SaveFileToContentFilesFolder(IWebHostEnvironment webHostEnvironment, string filename, byte[] fileBytes)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var path = Path.Combine(GetContentFilesFolderPath(webHostEnvironment), Path.GetFileName(filename));
        File.WriteAllBytes(Path.Combine(GetContentFilesFolderPath(webHostEnvironment), Path.GetFileName(filename)), fileBytes);
        return path;
    }

    public static string GetMediaTypeByMagicNumber(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            return "";
        }

        // These are the byte arrays that determine the magic numbers.
        // Source: https://en.wikipedia.org/wiki/List_of_file_signatures.
        byte[] bmpMagicNumber = { 0x42, 0x4D };
        byte[] jpegMagcicNumberStart = { 0xFF, 0xD8 };
        byte[] jpegMagicNumberEnd = { 0xFF, 0xD9 };
        byte[] pngMagicNumber = { 0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA };
        byte[] gifMagicNumberA = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
        byte[] gifMagicNumberB = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
        byte[] tiffMagicNumberA = { 0x49, 0x49, 0x2A, 0x0 };
        byte[] tiffMagicNumberB = { 0x4D, 0x4D, 0x0, 0x2A };
        byte[] flifMagicNumber = { 0x46, 0x4, 0x49, 0x46 };
        byte[] icoMagicNumber = { 0x0, 0x0, 0x1, 0x0 };

        // WebP's header is a bit more complex. The first 4 bytes are the ASCII characters "RIFF", followed by 4 bytes that represent the file size
        // which are followed by 4 characters that are the ASCII characters "WEBP". Bytes 5 through 8 are ignored as they could be anything.
        byte[] webpMagicNumberA = { 0x52, 0x49, 0x46, 0x46 };
        byte[] webpMagicNumberB = { 0x57, 0x45, 0x42, 0x50 };

        string mimeType;
        if (fileBytes.Take(4).SequenceEqual(webpMagicNumberA) && fileBytes.Skip(8).Take(4).SequenceEqual(webpMagicNumberB))
        {
            // WEBP header is "RIFF....WEBP" with the 4 dots representing the bytes that make up the file size.
            mimeType = "image/webp";
        }
        else if (fileBytes.Take(2).SequenceEqual(bmpMagicNumber))
        {
            // BMP
            mimeType = "image/bmp";
        }
        else if (fileBytes.Take(2).SequenceEqual(jpegMagcicNumberStart) && fileBytes.SkipWhile((_, index) => index < fileBytes.Length - 2).Take(2).SequenceEqual(jpegMagicNumberEnd))
        {
            // JPEG
            mimeType = "image/jpeg";
        }
        else if (fileBytes.Take(8).SequenceEqual(pngMagicNumber))
        {
            // PNG
            mimeType = "image/png";
        }
        else if (fileBytes.Take(6).SequenceEqual(gifMagicNumberA) || fileBytes.Take(6).SequenceEqual(gifMagicNumberB))
        {
            // GIF
            mimeType = "image/gif";
        }
        else if (fileBytes.Take(4).SequenceEqual(tiffMagicNumberA) || fileBytes.Take(4).SequenceEqual(tiffMagicNumberB))
        {
            // TIFF
            mimeType = "image/tiff";
        }
        else if (fileBytes.Take(4).SequenceEqual(flifMagicNumber))
        {
            // FLIF
            mimeType = "image/flif";
        }
        else if (fileBytes.Take(4).SequenceEqual(icoMagicNumber))
        {
            // ICO
            // NOTE: The "official" MIME type for ICO files is actually "image/vnd.microsoft.icon". However, not even Microsoft uses this, and all
            // modern browsers use "image/x-icon" instead. While most of them also recognize the "official" one, "image/x-icon" has better overall support.
            mimeType = "image/x-icon";
        }
        else if (Encoding.UTF8.GetString(fileBytes, 0, 256).Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            mimeType = "image/svg+xml";
        }
        else
        {
            // UNKNOWN
            // NOTE: There's not guarantee that this works.
            mimeType = "image/*";
        }

        return mimeType;
    }

    /// <summary>
    /// <para>
    /// Turns an image extension into a IANA media type (a.k.a. MIME type). Most extensions will just turn into "image/{extension}" but there are a few exceptions.
    /// See below for the exceptions.
    /// </para>
    /// <list>
    ///     <item>
    ///         <term>jpg, jpe, jif, jfif, jfi</term>
    ///         <description>image/jpeg</description>
    ///     </item>
    ///     <item>
    ///         <term>svg</term>
    ///         <description>image/svg+xml</description>
    ///     </item>
    ///     <item>
    ///         <term>tif</term>
    ///         <description>image/tiff</description>
    ///     </item>
    ///     <item>
    ///         <term>ico</term>
    ///         <description>image/x-icon</description>
    ///     </item>
    /// </list>
    /// </summary>
    /// <param name="extension">The extension of an image. The dot in front will be trimmed if present.</param>
    /// <returns>The correct media type.</returns>
    public static string GetMediaTypeByExtension(string extension)
    {
        if (String.IsNullOrWhiteSpace(extension))
        {
            return String.Empty;
        }

        extension = extension.ToLowerInvariant().TrimStart('.');
        return extension switch
        {
            "jpg" => "image/jpeg",
            "jpe" => "image/jpeg",
            "jif" => "image/jpeg",
            "jfif" => "image/jpeg",
            "jfi" => "image/jpeg",
            "svg" => "image/svg+xml",
            "tif" => "image/tiff",
            "ico" => "image/x-icon",
            _ => $"image/{extension}"
        };
    }
}