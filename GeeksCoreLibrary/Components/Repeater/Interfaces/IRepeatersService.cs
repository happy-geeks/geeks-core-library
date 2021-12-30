using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Repeater.Models;

namespace GeeksCoreLibrary.Components.Repeater.Interfaces
{
    public interface IRepeatersService
    {
        /// <summary>
        /// Gets all product banners. These are static banners that should be places in certain positions in the results of a repeater.
        /// </summary>
        /// <returns></returns>
        Task<List<ProductBannerModel>> GetProductBannersAsync();
    }
}
