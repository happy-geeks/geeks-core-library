using System;
using BarcodeLib;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Barcodes.Controllers;

[Area("Barcodes")]
public class BarcodesController : Controller
{
    private readonly ILogger<BarcodesController> logger;
    private readonly GclSettings gclSettings;
    private readonly IBarcodesService barcodesService;

    public BarcodesController(ILogger<BarcodesController> logger, GclSettings gclSettings, IBarcodesService barcodesService)
    {
        this.logger = logger;
        this.gclSettings = gclSettings;
        this.barcodesService = barcodesService;
    }

    [HttpGet, Route("barcode")]
    public IActionResult Barcode(string input, TYPE type, int width, int height, string downloadFileName = null)
    {
        var bytes = barcodesService.GenerateBarcode(input, type, width, height);
        return String.IsNullOrWhiteSpace(downloadFileName)
            ? File(bytes, "image/png")
            : File(bytes, "image/png", downloadFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? downloadFileName : $"{downloadFileName}.png");
    }
}