using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.WebPage.Services
{
    public class WebPagesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, ILanguagesService languagesService, IStringReplacementsService stringReplacementsService)
        : IWebPagesService, IScopedService
    {
        private readonly GclSettings gclSettings = gclSettings.Value;

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrlAsync(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }
            
            var queryBuilder = new StringBuilder(@$"SELECT 
                                                        webPage.id,
	                                                    IFNULL(titleSeo.value, webPage.title) AS name,
	                                                    CONCAT_WS('/', IFNULL(parentTitleSeo1.value, parent1.title), IFNULL(parentTitleSeo2.value, parent2.title), IFNULL(parentTitleSeo3.value, parent3.title), IFNULL(parentTitleSeo4.value, parent4.title), IFNULL(parentTitleSeo5.value, parent5.title)) AS path,
	                                                    CONCAT_WS('/', parent1.id, parent2.id, parent3.id, parent4.id, parent5.id) AS parents
                                                    FROM {WiserTableNames.WiserItem} AS webPage
                                                    JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = webPage.id AND fixedUrl.`key` = 'fixed_url' AND fixedUrl.value = ?fixedUrl
                                                    LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = webPage.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
");

            for (var i = 1; i <= 5; i++)
            {
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItemLink} AS link{i} ON link{i}.item_id = {(i == 1 ? "webPage.id" : $"parent{i - 1}.id")} And link{i}.type = 1");
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItem} AS parent{i} ON parent{i}.id = link{i}.destination_item_id And parent{i}.published_environment >= ?publishedEnvironment");
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItemDetail} AS parentTitleSeo{i} ON parentTitleSeo{i}.item_id = parent{i}.id And parentTitleSeo{i}.`key` = '{CoreConstants.SeoTitlePropertyName}'");
            }

            queryBuilder.AppendLine(@"WHERE webPage.published_environment >= ?publishedEnvironment
                                    LIMIT 1");

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("fixedUrl", fixedUrl);
            databaseConnection.AddParameter("publishedEnvironment", (int)gclSettings.Environment);
            var dataTable = await databaseConnection.GetAsync(queryBuilder.ToString());
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var firstRow = dataTable.Rows[0];
            return (
                Id: firstRow.Field<ulong>("id"),
                Title: firstRow.Field<string>("name"),
                FixedUrl: fixedUrl,
                Path: firstRow.Field<string>("path")?.Split('/').ToList(),
                Parents: firstRow.Field<string>("parents")?.Split('/').Select(UInt64.Parse).ToList()
            );
        }

        /// <inheritdoc />
        public async Task<DataTable> GetWebPageResultAsync(WebPageCmsSettingsModel settings, Dictionary<string, string> extraData = null)
        {
            var query = new StringBuilder($@"SELECT
    webPage.id,
    webPage.title AS `name`,
    CONCAT_WS('', webPageHtml.`value`, webPageHtml.long_value) AS `html`,
    webPageHtml.language_code,
    webPageTitle.`value` AS `title`,
    CONCAT_WS('', webPageDescription.`value`, webPageDescription.long_value) AS `metadescription`
FROM `{WiserTableNames.WiserItem}` AS webPage
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageSeoName ON webPageSeoName.item_id = webPage.id AND webPageSeoName.`key` = '{CoreConstants.SeoTitlePropertyName}'
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageHtml ON webPageHtml.item_id = webPage.id AND webPageHtml.`key` = 'html' AND webPageHtml.language_code = ?languageCode
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageTitle ON webPageTitle.item_id = webPage.id AND webPageTitle.`key` = 'title' AND webPageTitle.language_code = ?languageCode
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageDescription ON webPageDescription.item_id = webPage.id AND webPageDescription.`key` = 'description' AND webPageDescription.language_code = ?languageCode
");

            var pathMustContain = settings.PathMustContainName;
            if (!String.IsNullOrWhiteSpace(pathMustContain) && settings.SearchNumberOfLevels > 0)
            {
                if (settings.HandleRequest)
                {
                    pathMustContain = stringReplacementsService.DoHttpRequestReplacements(pathMustContain);
                }

                for (var i = 1; i <= settings.SearchNumberOfLevels; i++)
                {
                    var itemLinkAlias = $"searchUpLink{i}";
                    var itemAlias = $"searchUpItem{i}";
                    var titleAlias = $"item{i}Title";
                    var seoTitleAlias = $"item{i}SeoName";
                    var previousLink = i == 1 ? "webPage.id" : $"searchUpLink{i - 1}.destination_item_id";

                    query.AppendLine($@"LEFT JOIN `{WiserTableNames.WiserItemLink}` AS `{itemLinkAlias}` ON `{itemLinkAlias}`.item_id = {previousLink}
LEFT JOIN `{WiserTableNames.WiserItem}` AS `{itemAlias}` ON `{itemAlias}`.id = `{itemLinkAlias}`.destination_item_id
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS `{titleAlias}` ON `{titleAlias}`.item_id = `{itemAlias}`.id AND `{titleAlias}`.`key` = 'title'
LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS `{seoTitleAlias}` ON `{seoTitleAlias}`.item_id = `{itemAlias}`.id AND `{seoTitleAlias}`.`key` = '{CoreConstants.SeoTitlePropertyName}'");
                }
            }

            // WHERE part.
            query.Append("WHERE webPage.entity_type = 'webpagina' AND webPage.published_environment >= ?environment");

            if (settings.PageId > 0)
            {
                query.Append(" AND webPage.id = ?pageItemId");
            }
            else if (!String.IsNullOrWhiteSpace(settings.PageName))
            {
                query.Append(" AND IFNULL(webPageSeoName.`value`, webPageTitle.`value`) = ?pageName");

                if (!String.IsNullOrWhiteSpace(pathMustContain) && settings.SearchNumberOfLevels > 0)
                {
                    query.Append(" AND CONCAT_WS('/', ''");
                    for (var i = settings.SearchNumberOfLevels; i > 0; i--)
                    {
                        query.Append($", IFNULL(`item{i}SeoName`.`value`, `item{i}SeoName`.`value`)");
                    }
                    query.Append(", '') LIKE CONCAT('%', ?path, '%')");
                }
            }
            query.AppendLine();

            // LIMIT part.
            query.Append("LIMIT 1"); 

            // Make sure the language code has a value.
            if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                // This function fills the property "CurrentLanguageCode".
                await languagesService.GetLanguageCodeAsync();
            }

            databaseConnection.AddParameter("pageName", await stringReplacementsService.DoAllReplacementsAsync(stringReplacementsService.DoReplacements(settings.PageName, extraData)));
            databaseConnection.AddParameter("pageItemId", settings.PageId);
            databaseConnection.AddParameter("path", await stringReplacementsService.DoAllReplacementsAsync(stringReplacementsService.DoReplacements(settings.PathMustContainName, extraData)));
            databaseConnection.AddParameter("languageCode", await stringReplacementsService.DoAllReplacementsAsync(languagesService.CurrentLanguageCode ?? ""));
            databaseConnection.AddParameter("environment", (int)gclSettings.Environment);
            return await databaseConnection.GetAsync(query.ToString(), true);
        }
    }
}
