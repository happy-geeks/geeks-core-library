using System;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZXing;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

/// <inheritdoc cref="IBarcodesService"/>
public class CachedBarcodesService : IBarcodesService
{
    private readonly GclSettings gclSettings;
    private readonly IBarcodesService barcodesService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly ILogger<CachedBarcodesService> logger;

    /// <summary>
    /// Creates a new instance of <see cref="CachedBarcodesService"/>.
    /// </summary>
    public CachedBarcodesService(IOptions<GclSettings> gclSettings, IBarcodesService barcodesService, ILogger<CachedBarcodesService> logger, IWebHostEnvironment webHostEnvironment = null)
    {
        this.gclSettings = gclSettings.Value;
        this.barcodesService = barcodesService;
        this.webHostEnvironment = webHostEnvironment;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateBarcodeAsync(string input, BarcodeFormat format, int width, int height)
    {
        byte[] fileBytes;

        // Retrieve the path of the cache directory.
        var cacheBasePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(cacheBasePath))
        {
            // Log a warning if the directory doesn't exist, and generate a new barcode.
            logger.LogWarning($"Files cache is enabled but the directory '{cacheBasePath}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
            fileBytes = await barcodesService.GenerateBarcodeAsync(input, format, width, height);
            return fileBytes;
        }

        var filename = $"barcode_{format:G}_{width}x{height}_{input}.png";
        var filePath = Path.Combine(cacheBasePath, filename);

        var file = new FileInfo(filePath);
        if (file.Exists && DateTime.UtcNow.Subtract(file.LastWriteTimeUtc) <= gclSettings.DefaultItemFileCacheDuration)
        {
            return await File.ReadAllBytesAsync(filePath);
        }

        // Generate new barcode if it doesn't exist yet or if it's older than one hour.
        fileBytes = await barcodesService.GenerateBarcodeAsync(input, format, width, height);
        await FileSystemHelpers.SaveToFileCacheDirectoryAsync(webHostEnvironment, filename, fileBytes);
        return fileBytes;
    }

    /// <inheritdoc />
    public byte[] GenerateBarcode(string input, BarcodeFormat format, int width, int height)
    {
        byte[] fileBytes;

        // Retrieve the path of the cache directory.
        var cacheBasePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(cacheBasePath))
        {
            // Log a warning if the directory doesn't exist, and generate a new barcode.
            logger.LogWarning($"Files cache is enabled but the directory '{cacheBasePath}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
            fileBytes = barcodesService.GenerateBarcode(input, format, width, height);
            return fileBytes;
        }

        var filename = $"barcode_{format:G}_{width}x{height}_{input}.png";
        var filePath = Path.Combine(cacheBasePath, filename);

        var file = new FileInfo(filePath);
        if (file.Exists && DateTime.UtcNow.Subtract(file.LastWriteTimeUtc) <= gclSettings.DefaultItemFileCacheDuration)
        {
            return File.ReadAllBytes(filePath);
        }

        // Generate new barcode if it doesn't exist yet or if it's older than one hour.
        fileBytes = barcodesService.GenerateBarcode(input, format, width, height);
        FileSystemHelpers.SaveToFileCacheDirectory(webHostEnvironment, filename, fileBytes);
        return fileBytes;
    }
}