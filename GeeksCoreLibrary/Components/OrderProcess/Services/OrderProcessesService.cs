using System;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Options;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    public class OrderProcessesService : IOrderProcessesService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        public OrderProcessesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl)?> GetOrderProcessViaFixedUrl(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }
            
            var query = @$"SELECT 
                                orderProcess.id,
	                            IFNULL(titleSeo.value, orderProcess.title) AS name
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}' AND (fixedUrl.value = ?fixedUrl)
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = orderProcess.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
                            WHERE orderProcess.entity_type = '{Constants.OrderProcessEntityType}'
                            AND orderProcess.published_environment >= ?publishedEnvironment
                            LIMIT 1";
            
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("fixedUrl", fixedUrl);
            databaseConnection.AddParameter("publishedEnvironment", (int)gclSettings.Environment);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var firstRow = dataTable.Rows[0];
            return (
                Id: firstRow.Field<ulong>("id"),
                Title: firstRow.Field<string>("name"),
                FixedUrl: fixedUrl
            );
        }
    }
}
