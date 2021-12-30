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

namespace GeeksCoreLibrary.Components.Repeater.Services
{
    public class RepeatersService : IRepeatersService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;

        public RepeatersService(IDatabaseConnection databaseConnection, ILanguagesService languagesService, IHttpContextAccessor httpContextAccessor, IOptions<GclSettings> gclSettings)
        {
            this.databaseConnection = databaseConnection;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<List<ProductBannerModel>> GetProductBannersAsync()
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                throw new Exception("HttpContext is not available.");
            }

            var fullUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).PathAndQuery;
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("baseUrl", fullUrl);
            databaseConnection.AddParameter("languageCode", languagesService.CurrentLanguageCode ?? "");
            databaseConnection.AddParameter("publishedEnvironment", (int)gclSettings.Environment);
            var dataTable = await databaseConnection.GetAsync($@"
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
                        AND productbanner.published_environment >= ?publishedEnvironment
                        AND base_url.`value` IN (?baseurl, '*')
                        AND (language_code.`value` IS NULL OR language_code.`value` IN (?languageCode, '', '*'))
                        AND (start_date.`value` IS NULL OR start_date.`value` = '' OR CONVERT(start_date.`value`, DATETIME) < NOW())
                        AND (end_date.`value` IS NULL OR end_date.`value` = '' OR CONVERT(end_date.`value`, DATETIME) > NOW())");

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
                    Method = (ProductBannerModel.PlacingMethods)Convert.ToInt32(dataRow["method"]),
                    Content = dataRow.Field<string>("content"),
                    BannerSize = Convert.ToInt32(dataRow["banner_size"])
                });
            }

            return results;
        }
    }
}