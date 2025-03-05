using System.Threading.Tasks;
using ZXing;

namespace GeeksCoreLibrary.Modules.Barcodes.Interfaces;

/// <summary>
/// The service to generate barcodes, including QR codes.
/// </summary>
public interface IBarcodesService
{
    /// <summary>
    /// Generates a new barcode for the given string. This also supports generating QR codes.
    /// </summary>
    /// <param name="input">The string that needs to be turned into a barcode.</param>
    /// <param name="format">
    /// The barcode format. Possible formats are:
    /// <list type="bullet">
    /// <item><description>AZTEC</description></item>
    /// <item><description>CODABAR</description></item>
    /// <item><description>CODE_39</description></item>
    /// <item><description>CODE_93</description></item>
    /// <item><description>CODE_128</description></item>
    /// <item><description>DATA_MATRIX</description></item>
    /// <item><description>EAN_8</description></item>
    /// <item><description>EAN_13</description></item>
    /// <item><description>ITF</description></item>
    /// <item><description>PDF_417</description></item>
    /// <item><description>QR_CODE</description></item>
    /// <item><description>UPC_A</description></item>
    /// <item><description>UPC_E</description></item>
    /// <item><description>MSI</description></item>
    /// <item><description>PLESSEY</description></item>
    /// </list>
    /// </param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>The bytes of the image file.</returns>
    Task<byte[]> GenerateBarcodeAsync(string input, BarcodeFormat format, int width, int height);
}