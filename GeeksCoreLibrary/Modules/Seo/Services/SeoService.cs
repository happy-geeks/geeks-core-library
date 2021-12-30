using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Models;

namespace GeeksCoreLibrary.Modules.Seo.Services
{
    public class SeoService : ISeoService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;

        public SeoService(IDatabaseConnection databaseConnection, IObjectsService objectsService)
        {
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
        }

        /// <inheritdoc />
        public async Task<PageMetaDataModel> GetSeoDataForPageAsync(Uri pageUri)
        {
            var result = new PageMetaDataModel();
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("url", pageUri.AbsolutePath);

            var query = $@"SELECT
	                        seo.id,
	                        detail.`key`,
	                        CONCAT_WS('', detail.`value`, detail.long_value) AS `value`
                        FROM {WiserTableNames.WiserItem} AS seo
                        JOIN {WiserTableNames.WiserItemDetail} AS url ON url.item_id = seo.id AND url.`key` = 'url' AND url.`value` = ?url
                        JOIN {WiserTableNames.WiserItemDetail} AS detail ON detail.item_id = seo.id
                        WHERE seo.entity_type = 'seo'";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            var robots = new List<string>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var key = dataRow.Field<string>("key");
                var value = dataRow.Field<string>("value");
                if (String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                switch (key.ToLowerInvariant())
                {
                    case "page_title":
                        result.PageTitle = value;
                        break;
                    case "canonical":
                        result.Canonical = value;
                        break;
                    case "no_index":
                        if (value == "1")
                        {
                            robots.Add("noindex");
                        }

                        break;
                    case "no_follow":
                        if (value == "1")
                        {
                            robots.Add("nofollow");
                        }

                        break;
                    case "meta_title":
                        result.MetaTags.TryAdd("title", value);
                        break;
                    case "meta_description":
                        result.MetaTags.TryAdd("description", value);
                        break;
                    case "seo_text":
                        result.SeoText = value;
                        break;
                    case "h1":
                        result.H1Text = value;
                        break;
                    case "h2":
                        result.H2Text = value;
                        break;
                    case "h3":
                        result.H3Text = value;
                        break;
                }
            }

            if (robots.Any())
            {
                result.MetaTags.TryAdd("robots", String.Join(",", robots));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> SeoModuleIsEnabledAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("seomodule");
            return result.Equals("1", StringComparison.Ordinal) || result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<XDocument> GenerateSiteMap()
        {
            var sitemapQuery = await objectsService.FindSystemObjectByDomainNameAsync("googlesitemapquery");
            if (String.IsNullOrWhiteSpace(sitemapQuery))
            {
                return null;
            }

            var dataTable = await databaseConnection.GetAsync(sitemapQuery);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            var sitemap = new XDocument(new XDeclaration("1.0", "UTF-8", ""));
            sitemap.Add(new XElement(ns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                new XAttribute("xmlns", ns)));

            var hasModifiedDateColumn = dataTable.Columns.Contains("lastmodified");
            var hasFrequenceColumn = dataTable.Columns.Contains("frequence");
            var hasPriorityColumn = dataTable.Columns.Contains("priority");

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var url = dataRow.Field<string>("url");
                if (String.IsNullOrWhiteSpace(url))
                {
                    // No point in adding empty urls.
                    continue;
                }

                var urlElement = new XElement(ns + "url");
                urlElement.Add(new XElement(ns + "loc", url));

                if (hasModifiedDateColumn && !dataRow.IsNull("lastmodified"))
                {
                    urlElement.Add(new XElement(ns + "lastmod", dataRow["lastmodified"].ToString()));
                }
                if (hasFrequenceColumn && !dataRow.IsNull("frequence"))
                {
                    urlElement.Add(new XElement(ns + "changefreq", dataRow["frequence"].ToString()));
                }
                if (hasPriorityColumn && !dataRow.IsNull("priority"))
                {
                    urlElement.Add(new XElement(ns + "priority", dataRow["priority"].ToString()));
                }

                sitemap.Root.Add(urlElement);
            }

            return sitemap;
        }

        /// <inheritdoc />
        public async Task<XDocument> GenerateImageSiteMap()
        {
            var sitemapQuery = await objectsService.FindSystemObjectByDomainNameAsync("googleimagesitemapquery");
            if (String.IsNullOrWhiteSpace(sitemapQuery))
            {
                return null;
            }

            var dataTable = await databaseConnection.GetAsync(sitemapQuery);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            XNamespace nsSitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace nsImage = "http://www.google.com/schemas/sitemap-image/1.1";
            var sitemap = new XDocument(new XDeclaration("1.0", "UTF-8", ""));
            sitemap.Add(new XElement(nsSitemap + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", nsImage),
                new XAttribute("xmlns", nsSitemap)));

            var hasCaptionColumn = dataTable.Columns.Contains("caption");
            var hasGeoLocationColumn = dataTable.Columns.Contains("geo_location");
            var hasTitleColumn = dataTable.Columns.Contains("title");
            var hasLicenseColumn = dataTable.Columns.Contains("license");

            var groupedRows = dataTable.Rows.Cast<DataRow>().GroupBy(dr => dr.Field<string>("url"));
            foreach (var group in groupedRows)
            {
                if (String.IsNullOrWhiteSpace(group.Key))
                {
                    // No point in adding empty urls.
                    continue;
                }

                var urlElement = new XElement(nsSitemap + "url");
                urlElement.Add(new XElement(nsSitemap + "loc", group.Key));

                foreach (var dataRow in group)
                {
                    var imageLocation = dataRow.Field<string>("image_location");
                    if (String.IsNullOrWhiteSpace(imageLocation))
                    {
                        // No point in adding images without an URL to the sitemap.
                        continue;
                    }

                    var imageElement = new XElement(nsImage + "image");
                    imageElement.Add(new XElement(nsImage + "loc", imageLocation));

                    if (hasCaptionColumn && !dataRow.IsNull("caption"))
                    {
                        imageElement.Add(new XElement(nsImage + "caption", dataRow.Field<string>("caption")));
                    }
                    if (hasGeoLocationColumn && !dataRow.IsNull("geo_location"))
                    {
                        imageElement.Add(new XElement(nsImage + "geo_location", dataRow.Field<string>("geo_location")));
                    }
                    if (hasTitleColumn && !dataRow.IsNull("title"))
                    {
                        imageElement.Add(new XElement(nsImage + "title", dataRow.Field<string>("title")));
                    }
                    if (hasLicenseColumn && !dataRow.IsNull("license"))
                    {
                        imageElement.Add(new XElement(nsImage + "license", dataRow.Field<string>("license")));
                    }

                    urlElement.Add(imageElement);
                }

                sitemap.Root.Add(urlElement);
            }

            return sitemap;
        }
    }
}