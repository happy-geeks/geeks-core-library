using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Interfaces
{
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
        /// Get the number of rows in the file.
        /// </summary>
        /// <param name="filePath">The path to the Excel file to read from.</param>
        /// <returns></returns>
        int GetRowCount(string filePath);

        /// <summary>
        /// Get the lines from the Excel file.
        /// </summary>
        /// <param name="filePath">The path to the Excel file to read from.</param>
        /// <param name="numberOfColumns">The number of columns that need to be retrieved for each row.</param>
        /// <param name="skipFirstLine">Optional: If set to true the first line will not be included in the results.</param>
        /// <param name="firstColumnAreIds">If the first column are for IDs of Wiser Item the default value will be set on "0" to indicate a new item instead of an empty string.</param>
        /// <returns></returns>
        List<List<string>> GetLines(string filePath, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false);
    }
}
