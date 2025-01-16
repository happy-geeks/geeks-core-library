using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Repeater.Interfaces;
using GeeksCoreLibrary.Components.Repeater.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.Repeater.Services;

public class RepeatersService(
    IDatabaseConnection databaseConnection,
    ILanguagesService languagesService,
    IOptions<GclSettings> gclSettings,
    IHttpContextAccessor httpContextAccessor = null)
    : IRepeatersService, IScopedService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<List<ProductBannerModel>> GetProductBannersAsync()
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            throw new Exception("HttpContext is not available.");
        }

        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        var fullUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).PathAndQuery;
        databaseConnection.AddParameter("gcl_baseUrl", fullUrl);
        databaseConnection.AddParameter("gcl_languageCode", languagesService.CurrentLanguageCode ?? "");
        databaseConnection.AddParameter("gcl_publishedEnvironment", (int) gclSettings.Environment);
        var dataTable = await databaseConnection.GetAsync($"""
                                                           
                                                                               SELECT
                                                                                   productbanner.id,
                                                                                   productbanner.title,
                                                                                   base_url.`value` AS base_url,
                                                                                   IFNULL(url_contains.`value`, '') AS url_contains,
                                                                                   position.`value` AS position,
                                                                                   method.`value` AS method,
                                                                                   CONCAT_WS('', content.`value`, content.long_value) AS content,
                                                                                   banner_size.`value` AS banner_size
                                                                               FROM {WiserTableNames.WiserItem} AS productbanner
                                                                               JOIN {WiserTableNames.WiserItemDetail} AS base_url ON base_url.item_id = productbanner.id AND base_url.`key` = 'base_url'
                                                                               LEFT JOIN {WiserTableNames.WiserItemDetail} AS url_contains ON url_contains.item_id = productbanner.id AND url_contains.`key` = 'url_contains'
                                                                               JOIN {WiserTableNames.WiserItemDetail} AS position ON position.item_id = productbanner.id AND position.`key` = 'position'
                                                                               JOIN {WiserTableNames.WiserItemDetail} AS method ON method.item_id = productbanner.id AND method.`key` = 'method'
                                                                               JOIN {WiserTableNames.WiserItemDetail} AS content ON content.item_id = productbanner.id AND content.`key` = 'content'
                                                                               JOIN {WiserTableNames.WiserItemDetail} AS banner_size ON banner_size.item_id = productbanner.id AND banner_size.`key` = 'banner_size'
                                                                               LEFT JOIN {WiserTableNames.WiserItemDetail} AS start_date ON start_date.item_id = productbanner.id AND start_date.`key` = 'start_date'
                                                                               LEFT JOIN {WiserTableNames.WiserItemDetail} AS end_date ON end_date.item_id = productbanner.id AND end_date.`key` = 'end_date'
                                                                               LEFT JOIN {WiserTableNames.WiserItemDetail} AS language_code ON language_code.item_id = productbanner.id AND language_code.`key` = 'language_code'
                                                                               WHERE
                                                                                   productbanner.entity_type = 'productbanner'
                                                                                   AND productbanner.published_environment >= ?gcl_publishedEnvironment
                                                                                   AND base_url.`value` IN (?gcl_baseurl, '*')
                                                                                   AND (language_code.`value` IS NULL OR language_code.`value` IN (?gcl_languageCode, '', '*'))
                                                                                   AND (start_date.`value` IS NULL OR start_date.`value` = '' OR CONVERT(start_date.`value`, DATETIME) < NOW())
                                                                                   AND (end_date.`value` IS NULL OR end_date.`value` = '' OR CONVERT(end_date.`value`, DATETIME) > NOW())
                                                           """);

        var results = new List<ProductBannerModel>();
        if (dataTable.Rows.Count == 0)
        {
            return results;
        }

        foreach (DataRow dataRow in dataTable.Rows)
        {
            var urlContains = dataRow.Field<string>("url_contains");
            if (!String.IsNullOrWhiteSpace(urlContains) && !urlContains.Split(";").Where(x => !String.IsNullOrWhiteSpace(x)).Any(x => fullUrl.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var productBannerId = dataRow.Field<ulong>("id");
            if (results.Any(x => x.ItemId == productBannerId))
            {
                continue;
            }

            results.Add(new ProductBannerModel
            {
                ItemId = productBannerId,
                Name = dataRow.Field<string>("title"),
                BaseUrl = dataRow.Field<string>("base_url"),
                UrlContains = dataRow.Field<string>("url_contains"),
                Position = Convert.ToInt32(dataRow["position"]),
                Method = (ProductBannerModel.PlacingMethods) Convert.ToInt32(dataRow["method"]),
                Content = dataRow.Field<string>("content"),
                BannerSize = Convert.ToInt32(dataRow["banner_size"])
            });
        }

        return results;
    }
}