using System;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZXing;

namespace GeeksCoreLibrary.Modules.Barcodes.Controllers;

[Area("Barcodes")]
[Route("barcodes")]
public class BarcodesController(ILogger<BarcodesController> logger, IBarcodesService barcodesService)
    : Controller
{
    /// <summary>
    /// Generates a new barcode as a PNG image. This also supports QR codes.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="format">The barcode format.</param>
    /// <param name="width">The width of the barcode image.</param>
    /// <param name="height">The height of the barcode image.</param>
    /// <param name="downloadFileName">The file name the image should be downloaded with. If this name doesn't end with ".png", it will be automatically added.</param>
    /// <returns>The image as a file.</returns>
    [Route("generate")]
    [HttpGet]
    public IActionResult Barcode(string input, BarcodeFormat format, int width, int height, string downloadFileName = null)
    {
        if (width <= 0 || height <= 0)
        {
            logger.LogError("Tried to create {format:G} barcode for string '{input}', but supplied dimensions were incorrect. Both width and height should be a value higher than 0, but supplied dimensions were {width}x{height}.", format, input, width, height);
            return BadRequest("Width and height must both be a value higher than 0.");
        }

        if (format.InList(BarcodeFormat.EAN_8, BarcodeFormat.UPC_E) && input.Length is < 7 or > 8)
        {
            logger.LogError("Tried to create {format:G} barcode for string '{input}', but supplied input was incorrect. Should be 7 (without checksum) or 8 (with checksum) characters long, but got {inputLength} characters.", format, input, input.Length);
            return BadRequest("Input should be 7 (without checksum) or 8 (with checksum) characters long.");
        }

        if (format.InList(BarcodeFormat.EAN_13, BarcodeFormat.UPC_A) && input.Length is < 12 or > 13)
        {
            logger.LogError("Tried to create {format:G} barcode for string '{input}', but supplied input was incorrect. Should be 12 (without checksum) or 13 (with checksum) characters long, but got {inputLength} characters.", format, input, input.Length);
            return BadRequest("Input should be 12 (without checksum) or 13 (with checksum) characters long.");
        }

        if (format == BarcodeFormat.ITF && input.Length % 2 == 1)
        {
            logger.LogError("Tried to create {format:G} barcode for string '{input}', but supplied input was incorrect. The length of the input should be even (was {inputLength}).", format, input, input.Length);
            return BadRequest("The length of the input should be even.");
        }

        var bytes = barcodesService.GenerateBarcode(input, format, width, height);
        return String.IsNullOrWhiteSpace(downloadFileName)
            ? File(bytes, "image/png")
            : File(bytes, "image/png", downloadFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? downloadFileName : $"{downloadFileName}.png");
    }
}