using System.Drawing;
using BarcodeLib;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

public class BarcodesService : IBarcodesService, IScopedService
{
    /// <inheritdoc />
    public byte[] GenerateBarcode(string input, TYPE type, int width, int height)
    {
        var barcode = new Barcode();
        barcode.Encode(type, input, width, height);
        var fileBytes = barcode.GetImageData(SaveTypes.PNG);
        return fileBytes;
    }
}