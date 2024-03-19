using System;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Models;

namespace GeeksCoreLibrary.Modules.Redirect.Services
{
    public class RedirectService : IRedirectService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;

        public RedirectService(IDatabaseConnection databaseConnection, IObjectsService objectsService)
        {
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
        }

        /// <inheritdoc />
        public async Task<RedirectModel> GetRedirectAsync(Uri uri)
        {
            var result = new RedirectModel();
            databaseConnection.AddParameter("url1", uri.ToString()); // With host and query-strings
            databaseConnection.AddParameter("url2", uri.PathAndQuery); // Without host and query-strings
            if (!String.IsNullOrEmpty(uri.Query))
            {
                databaseConnection.AddParameter("url3", uri.ToString().Split('?')[0]); // With host without query-strings
                databaseConnection.AddParameter("url4", uri.PathAndQuery.Split('?')[0]); // Without host without query-strings
            }

            var query = $@"SELECT
                            oldUrl.`value` AS oldUrl,
                            newUrl.`value` AS newUrl,
                            permanent.`value` AS permanent
                        FROM {WiserTableNames.WiserItem} AS redirect
                        JOIN {WiserTableNames.WiserItemDetail} AS oldUrl ON oldUrl.item_id = redirect.id AND oldUrl.`key` = 'oldurl' AND oldUrl.`value` IN (?url1, ?url2{(String.IsNullOrEmpty(uri.Query) ? String.Empty : ", ?url3, ?url4")})                            
                        JOIN {WiserTableNames.WiserItemDetail} AS newUrl ON newUrl.item_id = redirect.id AND newUrl.`key` = 'newurl'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS permanent ON permanent.item_id=redirect.id AND permanent.`key` = 'permanent'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS ordering ON ordering.item_id=redirect.id AND ordering.`key` = 'ordering'
                        WHERE redirect.entity_type = 'redirect'
                        ORDER BY CAST(ordering.value AS SIGNED) DESC
                        LIMIT 1";

            var dataTable = await databaseConnection.GetAsync(query);

            if (dataTable.Rows.Count != 1)
            {
                return result;
            }

            result.OldUrl= dataTable.Rows[0].Field<string>("oldUrl");
            result.NewUrl = dataTable.Rows[0].Field<string>("newUrl");
            result.Permanent = dataTable.Rows[0].Field<string>("permanent") == "1";

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> RedirectModuleIsEnabledAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("autoredirectmodule");
            return result.Equals("1", StringComparison.Ordinal) || result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<string> GetMainDomainForRedirectAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("autoredirectmaindomain");

            // Return if not enabled.
            if (!result.Equals("1", StringComparison.Ordinal) && !result.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return String.Empty;
            }

            return await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
        }

        /// <inheritdoc />
        public async Task<bool> ShouldRedirectToUrlWithTrailingSlashAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("redirectRequiresTrailingSlash");
            return result.Equals("1", StringComparison.Ordinal) || result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<bool> ShouldRedirectToLowerCaseUrlAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("redirectRequiresLowerCase");
            return result.Equals("1", StringComparison.Ordinal) || result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<bool> ShouldRedirectToHttpsAsync()
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync("requiressl");
            return result.Equals("1", StringComparison.Ordinal) || result.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}