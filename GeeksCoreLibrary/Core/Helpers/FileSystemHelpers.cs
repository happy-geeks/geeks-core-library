﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Hosting;

namespace GeeksCoreLibrary.Core.Helpers;

public static class FileSystemHelpers
{
    public static string GetFileCacheDirectory(IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var result = Path.Combine(webHostEnvironment.ContentRootPath, Constants.AppDataDirectoryName, Constants.FilesCacheDirectoryName);
        return !Directory.Exists(result) ? null : result;
    }

    public static string GetOutputCacheDirectory(IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var result = Path.Combine(webHostEnvironment.ContentRootPath, Constants.AppDataDirectoryName, Constants.OutputCacheDirectoryName);
        return !Directory.Exists(result) ? null : result;
    }

    /// <summary>
    /// Get the full path to the public files directory.
    /// As the name indicates, this is for storing files that are public and can be accessed by anyone.
    /// So make sure you don't store any sensitive information in this directory.
    /// </summary>
    /// <param name="webHostEnvironment">The <see cref="IWebHostEnvironment"/> that provides information about the web hosting environment an application is running in.</param>
    /// <returns>If the directory exists, it returns the absolute path to the directory. Otherwise, it returns <c>null</c>.</returns>
    public static string GetPublicFilesDirectory(IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var result = Path.Combine(webHostEnvironment.WebRootPath, Constants.PublicFilesDirectoryName);
        return !Directory.Exists(result) ? null : result;
    }

    public static string SaveToFileCacheDirectory(IWebHostEnvironment webHostEnvironment, string filename, byte[] fileBytes)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var directoryLocation = GetFileCacheDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(directoryLocation))
        {
            return null;
        }

        var fileLocation = Path.Combine(directoryLocation, Path.GetFileName(filename));
        File.WriteAllBytes(fileLocation, fileBytes);
        return fileLocation;
    }

    /// <summary>
    /// Save a file to the public files directory.
    /// As the name indicates, this is for storing files that are public and can be accessed by anyone.
    /// So make sure you don't store any sensitive information in this directory.
    /// </summary>
    /// <param name="webHostEnvironment">The <see cref="IWebHostEnvironment"/> that provides information about the web hosting environment an application is running in.</param>
    /// <param name="filename">The name of the file (including extension). Any directory or location information will be stripped, only the name of the file will be used.</param>
    /// <param name="fileBytes">The byte array with the contents of the file.</param>
    /// <returns>If the file was saved successfully, then the absolute path to the file will be returned. Otherwise, <c>null</c> will be returned.</returns>
    public static string SaveToPublicFilesDirectory(IWebHostEnvironment webHostEnvironment, string filename, byte[] fileBytes)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var directoryLocation = GetPublicFilesDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(directoryLocation))
        {
            return null;
        }

        var fileLocation = Path.Combine(directoryLocation, Path.GetFileName(filename));
        File.WriteAllBytes(fileLocation, fileBytes);
        return fileLocation;
    }

    /// <summary>
    /// Save a file to the public files directory.
    /// As the name indicates, this is for storing files that are public and can be accessed by anyone.
    /// So make sure you don't store any sensitive information in this directory.
    /// </summary>
    /// <param name="webHostEnvironment">The <see cref="IWebHostEnvironment"/> that provides information about the web hosting environment an application is running in.</param>
    /// <param name="filename">The name of the file (including extension). Any directory or location information will be stripped, only the name of the file will be used.</param>
    /// <param name="fileBytes">The byte array with the contents of the file.</param>
    /// <returns>If the file was saved successfully, then the absolute path to the file will be returned. Otherwise, <c>null</c> will be returned.</returns>
    public static async Task<string> SaveToPublicFilesDirectoryAsync(IWebHostEnvironment webHostEnvironment, string filename, byte[] fileBytes)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        var directoryLocation = GetPublicFilesDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(directoryLocation))
        {
            return null;
        }

        var fileLocation = Path.Combine(directoryLocation, Path.GetFileName(filename));
        await File.WriteAllBytesAsync(fileLocation, fileBytes);
        return fileLocation;
    }

    public static string GetMediaTypeByMagicNumber(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            return "";
        }

        // These are the byte arrays that determine the magic numbers.
        // Source: https://en.wikipedia.org/wiki/List_of_file_signatures.
        byte[] bmpMagicNumber = [0x42, 0x4D];
        byte[] jpegMagicNumberStart = [0xFF, 0xD8];
        byte[] jpegMagicNumberEnd = [0xFF, 0xD9];
        byte[] jpegXlMagicNumberA = [0xFF, 0xA];
        byte[] jpegXlMagicNumberB = [0x0, 0x0, 0x0, 0xC, 0x4A, 0x58, 0x4C, 0x20, 0xD, 0xA, 0x87, 0xA];
        byte[] pngMagicNumber = [0x89, 0x50, 0x4E, 0x47, 0xD, 0xA, 0x1A, 0xA];
        byte[] gifMagicNumberA = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61];
        byte[] gifMagicNumberB = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61];
        byte[] tiffMagicNumberA = [0x49, 0x49, 0x2A, 0x0];
        byte[] tiffMagicNumberB = [0x4D, 0x4D, 0x0, 0x2A];
        byte[] avifMagicNumber = [0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66];
        byte[] flifMagicNumber = [0x46, 0x4, 0x49, 0x46];
        byte[] icoMagicNumber = [0x0, 0x0, 0x1, 0x0];

        // WebP's header is a bit more complex. The first 4 bytes are the ASCII characters "RIFF", followed by 4 bytes that represent the file size
        // which are followed by 4 characters that are the ASCII characters "WEBP". Bytes 5 through 8 are ignored as they could be anything.
        byte[] webpMagicNumberStart = [0x52, 0x49, 0x46, 0x46];
        byte[] webpMagicNumberEnd = [0x57, 0x45, 0x42, 0x50];

        string mimeType;
        if (fileBytes.Take(4).SequenceEqual(webpMagicNumberStart) && fileBytes.Skip(8).Take(4).SequenceEqual(webpMagicNumberEnd))
        {
            // WEBP header is "RIFF....WEBP" with the 4 dots representing the bytes that make up the file size.
            mimeType = "image/webp";
        }
        else if (fileBytes.Take(2).SequenceEqual(bmpMagicNumber))
        {
            // BMP
            mimeType = "image/bmp";
        }
        else if (fileBytes.Take(2).SequenceEqual(jpegMagicNumberStart) && fileBytes.SkipWhile((_, index) => index < fileBytes.Length - 2).Take(2).SequenceEqual(jpegMagicNumberEnd))
        {
            // JPEG
            mimeType = "image/jpeg";
        }
        else if (fileBytes.Take(2).SequenceEqual(jpegXlMagicNumberA) || fileBytes.Take(12).SequenceEqual(jpegXlMagicNumberB))
        {
            // JPEG XL
            mimeType = "image/jxl";
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
        else if (fileBytes.Skip(4).Take(8).SequenceEqual(avifMagicNumber))
        {
            // AVIF
            mimeType = "image/avif";
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
            "avifs" => "image/avif",
            "ico" => "image/x-icon",
            _ => $"image/{extension}"
        };
    }
}