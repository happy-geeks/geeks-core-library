using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Interfaces;

public interface ICsvService
{
    /// <summary>
    /// Create an csv formatted string from a Json array.
    /// </summary>
    /// <param name="data">The Json array containing the data to be converted to csv format.</param>
    /// <param name="delimiter"></param>
    /// <returns>csv formatted string</returns>
    string JsonArrayToCsv(JArray data, string delimiter = ";");

    /// <summary>
    /// Write CSV data directly to a stream from a DbDataReader, enabling true streaming exports.
    /// </summary>
    /// <param name="dataReader">The DbDataReader containing the data to export.</param>
    /// <param name="outputStream">The stream to write CSV data to.</param>
    /// <param name="delimiter">The delimiter to use between fields.</param>
    Task DbDataReaderToCsvStreamAsync(DbDataReader dataReader, Stream outputStream, string delimiter = ";");
}