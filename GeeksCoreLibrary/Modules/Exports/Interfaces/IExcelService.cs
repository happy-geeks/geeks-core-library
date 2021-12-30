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
    }
}
