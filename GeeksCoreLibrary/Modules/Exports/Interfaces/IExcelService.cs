using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Interfaces;

public interface IExcelService
{
    /// <summary>
    /// Create an Excel file from a Json array.
    /// </summary>
    /// <param name="data">The Json array containing the information to be shown in the Excel file.</param>
    /// <param name="sheetName">The name of the worksheet in the Excel file.</param>
    /// <returns></returns>
    byte[] JsonArrayToExcel(JArray data, string sheetName = "Data");

    /// <summary>
    /// Get the values of each cell from the first row as column names.
    /// </summary>
    /// <param name="filePath">The path to the Excel file to read from.</param>
    /// <returns></returns>
    List<string> GetColumnNames(string filePath);

    /// <summary>
    /// Get the values of each cell from the first row as column names.
    /// </summary>
    /// <param name="stream">The Excel file stream to read from.</param>
    /// <returns></returns>
    List<string> GetColumnNames(Stream stream);

    /// <summary>
    /// Get the number of rows in the file.
    /// </summary>
    /// <param name="filePath">The path to the Excel file to read from.</param>
    /// <returns></returns>
    int GetRowCount(string filePath);

    /// <summary>
    /// Get the number of rows in the file.
    /// </summary>
    /// <param name="stream">The Excel file stream to read from.</param>
    /// <returns></returns>
    int GetRowCount(Stream stream);

    /// <summary>
    /// Get the lines from the Excel file.
    /// </summary>
    /// <param name="filePath">The path to the Excel file to read from.</param>
    /// <param name="numberOfColumns">The number of columns that need to be retrieved for each row.</param>
    /// <param name="skipFirstLine">Optional: If set to true the first line will not be included in the results.</param>
    /// <param name="firstColumnAreIds">If the first column are for IDs of Wiser Item the default value will be set on "0" to indicate a new item instead of an empty string.</param>
    /// <returns></returns>
    List<List<string>> GetLines(string filePath, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false);

    /// <summary>
    /// Get the lines from the Excel file.
    /// </summary>
    /// <param name="stream">The Excel file stream to read from.</param>
    /// <param name="numberOfColumns">The number of columns that need to be retrieved for each row.</param>
    /// <param name="skipFirstLine">Optional: If set to true the first line will not be included in the results.</param>
    /// <param name="firstColumnAreIds">If the first column are for IDs of Wiser Item the default value will be set on "0" to indicate a new item instead of an empty string.</param>
    /// <returns></returns>
    List<List<string>> GetLines(Stream stream, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false);

    /// <summary>
    /// Write an Excel file from a DbDataReader to a stream, processing rows one at a time to minimize memory usage.
    /// Uses a forward-only OpenXML writer so large result sets are not buffered in an in-memory DOM.
    /// When <paramref name="outputStream"/> is not readable and seekable (e.g. an HTTP response body),
    /// the workbook is built in a temporary file and then copied to the output stream.
    /// Stops with an error if the Excel sheet row limit (1,048,576) would be exceeded.
    /// </summary>
    /// <param name="dataReader">The DbDataReader containing the data to export.</param>
    /// <param name="outputStream">The stream to write the Excel (.xlsx) package to. The stream is left open.</param>
    /// <param name="sheetName">The name of the worksheet in the Excel file.</param>
    /// <param name="cancellationToken">Token used to stop reading/writing when the caller disconnects or times out.</param>
    Task DbDataReaderToExcelAsync(DbDataReader dataReader, Stream outputStream, string sheetName = "Data", CancellationToken cancellationToken = default);
}