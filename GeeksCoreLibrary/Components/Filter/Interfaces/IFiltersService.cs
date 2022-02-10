using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Filter.Models;

namespace GeeksCoreLibrary.Components.Filter.Interfaces
{
    public interface IFiltersService
    {
        /// <summary>
        /// Method to return the filter query part for repeater module
        /// </summary>
        /// <param name="forFilterItemsQuery">If query part is for filter items query, then LEFT JOINS instead of INNER JOINS will be returned.</param>
        /// <param name="givenFilterGroups">Give filter groups ik known. Otherwise this function will get the filter groups.</param>
        /// <returns>A <see cref="string"/> containing the joins.</returns>
        Task<QueryPartModel> GetFilterQueryPartAsync(bool forFilterItemsQuery = false, Dictionary<string, FilterGroup> givenFilterGroups = null);

        /// <summary>
        /// Function returns the filters keys from the GCLFilters parameter, which is used for search engine friendly URLs in combination with dynamic filters
        /// </summary>
        /// <param name="filterParameter"></param>
        /// <returns></returns>
        Dictionary<string, string> GetFiltersByParameter(string filterParameter);

        /// <summary>
        /// Get the filter groups for the selected category or general
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="extraFilterProperties"></param>
        /// <returns></returns>
        Task<Dictionary<string, FilterGroup>> GetFilterGroupsAsync(ulong categoryId = 0, string extraFilterProperties = "");
    }
}
