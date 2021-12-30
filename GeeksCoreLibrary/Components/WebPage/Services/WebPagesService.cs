using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.WebPage.Services
{
    public class WebPagesService : IWebPagesService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        public WebPagesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrl(string fixedUrl)
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
                                                    LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = webPage.id AND titleSeo.`key` = 'title_seo'
");

            for (var i = 1; i <= 5; i++)
            {
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItemLink} AS link{i} ON link{i}.item_id = {(i == 1 ? "webPage.id" : $"parent{i - 1}.id")} And link{i}.type = 1");
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItem} AS parent{i} ON parent{i}.id = link{i}.destination_item_id And parent{i}.published_environment >= ?publishedEnvironment");
                queryBuilder.AppendLine($"LEFT JOIN {WiserTableNames.WiserItemDetail} AS parentTitleSeo{i} ON parentTitleSeo{i}.item_id = parent{i}.id And parentTitleSeo{i}.`key` = 'title_seo'");
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
    }
}
