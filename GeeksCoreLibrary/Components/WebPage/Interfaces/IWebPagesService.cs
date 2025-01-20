using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Models;

namespace GeeksCoreLibrary.Components.WebPage.Interfaces;

/// <summary>
/// A service for doing things with webpages from the webpage module in Wiser.
/// </summary>
public interface IWebPagesService
{
    /// <summary>
    /// Gets a webpage via a fixed URL. In Wiser, users can set a fixed URL for each webpage item.
    /// This function will find the webpage with a specific URL, if it exusts.
    /// </summary>
    /// <param name="fixedUrl">The URL to get the webpage for.</param>
    /// <returns>A (named) Tuple with information about the webpage.</returns>
    Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrlAsync(string fixedUrl);

    /// <summary>
    /// Gets a single webpage based on settings from the webpage component.
    /// </summary>
    /// <param name="settings">The <see cref="WebPageCmsSettingsModel"/> to get the webpage for.</param>
    /// <param name="extraData">Optional: Any extra data for replacements.</param>
    /// <returns>A <see cref="DataTable"/> with the results.</returns>
    Task<DataTable> GetWebPageResultAsync(WebPageCmsSettingsModel settings, Dictionary<string, string> extraData = null);
}