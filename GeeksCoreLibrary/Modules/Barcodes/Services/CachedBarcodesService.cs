using System;
using System.IO;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ZXing;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

/// <inheritdoc cref="IBarcodesService"/>
public class CachedBarcodesService : IBarcodesService
{
    private readonly GclSettings gclSettings;
    private readonly IBarcodesService barcodesService;
    private readonly IWebHostEnvironment webHostEnvironment;

    /// <summary>
    /// Creates a new instance of <see cref="CachedBarcodesService"/>.
    /// </summary>
    public CachedBarcodesService(IOptions<GclSettings> gclSettings, IBarcodesService barcodesService, IWebHostEnvironment webHostEnvironment)
    {
        this.gclSettings = gclSettings.Value;
        this.barcodesService = barcodesService;
        this.webHostEnvironment = webHostEnvironment;
    }

    /// <inheritdoc />
    public byte[] GenerateBarcode(string input, BarcodeFormat format, int width, int height)
    {
        var basePath = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
        var filename = $"barcode_{format:G}_{width}x{height}_{input}.png";
        var filePath = Path.Combine(basePath, filename);

        var file = new FileInfo(filePath);
        if (file.Exists && DateTime.UtcNow.Subtract(file.LastWriteTimeUtc) <= gclSettings.DefaultItemFileCacheDuration)
        {
            return File.ReadAllBytes(filePath);
        }

        // Generate new barcode if it doesn't exist yet or if it's older than one hour.
        var fileBytes = barcodesService.GenerateBarcode(input, format, width, height);
        FileSystemHelpers.SaveFileToContentFilesFolder(webHostEnvironment, filename, fileBytes);
        return fileBytes;
    }
}