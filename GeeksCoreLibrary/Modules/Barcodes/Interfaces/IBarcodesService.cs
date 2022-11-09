using BarcodeLib;

namespace GeeksCoreLibrary.Modules.Barcodes.Interfaces;

public interface IBarcodesService
{
    /// <summary>
    /// Generates a new barcode for the given string.
    /// </summary>
    /// <param name="input">The string that needs to be turned into a barcode.</param>
    /// <param name="type">The type of barcode.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>The bytes of the image file.</returns>
    byte[] GenerateBarcode(string input, TYPE type, int width, int height);
}