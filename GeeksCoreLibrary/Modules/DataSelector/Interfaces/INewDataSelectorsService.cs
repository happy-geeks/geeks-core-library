using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.DataSelector.Models;

namespace GeeksCoreLibrary.Modules.DataSelector.Interfaces;

public interface INewDataSelectorsService
{
    /// <summary>
    /// Creates the entire query for the data selector.
    /// </summary>
    /// <param name="itemsRequest"></param>
    Task<string> GetQueryAsync(ItemsRequest itemsRequest);
}