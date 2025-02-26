using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ZXing;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

/// <inheritdoc cref="IBarcodesService"/>
public class CachedBarcodesService(
    IOptions<GclSettings> gclSettings,
    IBarcodesService barcodesService,
    IFileCacheService fileCacheService,
    IWebHostEnvironment webHostEnvironment = null)
    : IBarcodesService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<byte[]> GenerateBarcodeAsync(string input, BarcodeFormat format, int width, int height)
    {
        // Retrieve the path of the cache directory.
        var cacheBasePath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        var filename = $"barcode_{format:G}_{width}x{height}_{input}.png";
        var filePath = Path.Combine(cacheBasePath, filename);

        var (fileBytes, _) = await fileCacheService.GetOrAddAsync(filePath, async () =>
                (await barcodesService.GenerateBarcodeAsync(input, format, width, height), true),
            gclSettings.DefaultItemFileCacheDuration);

        return fileBytes;
    }
}