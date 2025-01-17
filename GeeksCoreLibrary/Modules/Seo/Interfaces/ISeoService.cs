using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeeksCoreLibrary.Modules.Seo.Models;

namespace GeeksCoreLibrary.Modules.Seo.Interfaces;

public interface ISeoService
{
    /// <summary>
    /// Gets all SEO data for specific page.
    /// </summary>
    /// <param name="pageUri">The URI of the page to get the SEO data of.</param>
    /// <returns>A <see cref="PageMetaDataModel"/> with the SEO data.</returns>
    Task<PageMetaDataModel> GetSeoDataForPageAsync(Uri pageUri);

    /// <summary>
    /// Gets whether or not the SEO module is enabled in the settings.
    /// </summary>
    /// <returns>A <see langword="bool"/> indicating whether or not the SEO module is enabled.</returns>
    Task<bool> SeoModuleIsEnabledAsync();

    /// <summary>
    /// Generates a sitemap based on the googlesitemapquery from the Wiser settings.
    /// </summary>
    /// <returns>Returns <see langword="null"/> is there is no query defined, or if the query returns no results. Otherwise returns an <see cref="XDocument"/> with the sitemap.</returns>
    Task<XDocument> GenerateSiteMap();

    /// <summary>
    /// Generates an images sitemap based on the googleimagesitemapquery from the Wiser settings.
    /// </summary>
    /// <returns>Returns <see langword="null"/> is there is no query defined, or if the query returns no results. Otherwise returns an <see cref="XDocument"/> with the sitemap.</returns>
    Task<XDocument> GenerateImageSiteMap();
}