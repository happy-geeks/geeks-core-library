using System;
using System.IO;
using BarcodeLib;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

public class CachedBarcodesService : IBarcodesService
{
    private readonly IBarcodesService barcodesService;
    private readonly IWebHostEnvironment webHostEnvironment;

    public CachedBarcodesService(IBarcodesService barcodesService, IWebHostEnvironment webHostEnvironment)
    {
        this.barcodesService = barcodesService;
        this.webHostEnvironment = webHostEnvironment;
    }

    /// <inheritdoc />
    public byte[] GenerateBarcode(string input, TYPE type, int width, int height)
    {
        var basePath = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
        var filename = $"barcode_{input}.png";
        var filePath = Path.Combine(basePath, filename);

        var file = new FileInfo(filePath);
        if (file.Exists && file.LastWriteTime >= DateTime.Now.AddHours(-1))
        {
            return File.ReadAllBytes(filePath);
        }

        // Generate new barcode if it doesn't exist yet or if it's older than one hour.
        var fileBytes = barcodesService.GenerateBarcode(input, type, width, height);
        FileSystemHelpers.SaveFileToContentFilesFolder(webHostEnvironment, filename, fileBytes);
        return fileBytes;
    }
}