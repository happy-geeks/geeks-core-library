using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.DataSelector.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.DataSelector.Interfaces
{
    public interface IDataSelectorsService
    {
        /// <summary>
        /// Retrieves the data selector JSON from the database by ID.
        /// </summary>
        /// <param name="dataSelectorId"></param>
        Task<string> GetDataSelectorJsonAsync(int dataSelectorId);

        /// <summary>
        /// Retrieves a query from the queries table in Wiser.
        /// </summary>
        /// <param name="queryId"></param>
        /// <returns></returns>
        Task<string> GetWiserQueryAsync(int queryId);

        /// <summary>
        /// Creates the entire query for the data selector.
        /// </summary>
        /// <param name="itemsRequest"></param>
        Task<string> GetQueryAsync(ItemsRequest itemsRequest);

        /// <summary>
        /// Will replace request variables in the scopes of the main connection and the other connections.
        /// </summary>
        /// <param name="selector">The <see cref="Models.DataSelector"/> object to update.</param>
        void ReplaceVariableValuesInDataSelector(Models.DataSelector selector);

        /// <summary>
        /// Will replace request variables in the scopes of the given connections.
        /// </summary>
        /// <param name="connections">The collection of <see cref="Connection"/> objects to update.</param>
        void ReplaceVariableValuesInConnections(IEnumerable<Connection> connections);

        /// <summary>
        /// Will replace request variables in the given scopes.
        /// </summary>
        /// <param name="scopes">The collection of <see cref="Scope"/> objects to update.</param>
        void ReplaceVariableValuesInScopes(IEnumerable<Scope> scopes);

        /// <summary>
        /// Executes the data selector query and returns the results as JSON.
        /// </summary>
        /// <param name="data">The <see cref="DataSelectorRequestModel"/> with the settings of the data selector.</param>
        /// <param name="skipSecurity">Skip the security if the data selector is not set to insecure loading, when loading from a secure location.</param>
        Task<(JArray Result, HttpStatusCode StatusCode, string Error)> GetJsonResponseAsync(DataSelectorRequestModel data, bool skipSecurity = false);

        /// <summary>
        /// Executes the data selector query and creates an Excel document from the result.
        /// </summary>
        /// <param name="data">The <see cref="DataSelectorRequestModel"/> with the settings of the data selector.</param>
        Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToExcelAsync(DataSelectorRequestModel data);

        /// <summary>
        /// Executes the data selector query and replaces the results in a HTML template and returns that HTML.
        /// </summary>
        /// <param name="data">The <see cref="DataSelectorRequestModel"/> with the settings of the data selector.</param>
        Task<(string Result, HttpStatusCode StatusCode, string Error)> ToHtmlAsync(DataSelectorRequestModel data);
        
        /// <summary>
        /// Executes the data selector query and replaces the results in a HTML template and converts that HTML to a PDF.
        /// </summary>
        /// <param name="data">The <see cref="DataSelectorRequestModel"/> with the settings of the data selector.</param>
        Task<(FileContentResult Result, HttpStatusCode StatusCode, string Error)> ToPdfAsync(DataSelectorRequestModel data);

        /// <summary>
        /// Generates a new instance of <see cref="ItemsRequest"/> that you can use for certain data selector requests.
        /// </summary>
        /// <param name="data">The <see cref="DataSelectorRequestModel"/> with the settings of the data selector.</param>
        /// <param name="skipSecurity">Skip the security if the data selector is not set to insecure loading, when loading from a secure location.</param>
        Task<(ItemsRequest Result, HttpStatusCode StatusCode, string Error)> InitializeItemsRequestAsync(DataSelectorRequestModel data, bool skipSecurity = false);

        /// <summary>
        /// Replaces all data selectors in a HTML template with the rendered versions.
        /// </summary>
        /// <param name="template">The HTML template that might contain one or more data selectors.</param>
        /// <returns>The same template but with all data selectors fully rendered.</returns>
        Task<string> ReplaceAllDataSelectorsAsync(string template);
    }
}
