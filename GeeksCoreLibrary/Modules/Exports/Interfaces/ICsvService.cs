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
}