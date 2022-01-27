using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.DataSelectorParser.Interfaces
{
    public interface IDataSelectorParsersService
    {
        /// <summary>
        /// Retrieves the response of a data selector based on a data selector ID or request JSON.
        /// </summary>
        /// <returns>A <see cref="JToken"/> that represents the response of the data selector.</returns>
        Task<JToken> GetDataSelectorResponseAsync(string dataSelectorId = null, string dataSelectorJson = null);
    }
}
