using System;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.DataSelectorParser.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.DataSelectorParser.Services
{
    public class DataSelectorParsersService(ILogger<DataSelectorParsersService> logger, IDataSelectorsService dataSelectorsService, IStringReplacementsService stringReplacementsService)
        : IDataSelectorParsersService, IScopedService
    {
        /// <inheritdoc />
        public async Task<JToken> GetDataSelectorResponseAsync(string dataSelectorId = null, string dataSelectorJson = null)
        {
            // No ID or raw JSON set, try to use demo response, or simply return an empty string.
            if (String.IsNullOrWhiteSpace(dataSelectorId) && String.IsNullOrWhiteSpace(dataSelectorJson))
            {
                return null;
            }

            // Handle request.
            string requestJson = null;
            if (!String.IsNullOrWhiteSpace(dataSelectorId))
            {
                if (!Int32.TryParse(await stringReplacementsService.DoAllReplacementsAsync(dataSelectorId), out var selectorId))
                {
                    return null;
                }

                requestJson = await stringReplacementsService.DoAllReplacementsAsync(await dataSelectorsService.GetDataSelectorJsonAsync(selectorId), evaluateLogicSnippets: false, removeUnknownVariables: false);
            }
            else if (!String.IsNullOrWhiteSpace(dataSelectorJson))
            {
                requestJson = await stringReplacementsService.DoAllReplacementsAsync(dataSelectorJson, evaluateLogicSnippets: false, removeUnknownVariables: false);
            }

            if (String.IsNullOrWhiteSpace(requestJson))
            {
                return null;
            }

            var dataSelector = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSelector>(requestJson);
            var request = new DataSelectorRequestModel
            {
                Settings = dataSelector
            };

            if (request.Settings != null)
            {
                request.Settings.Insecure = false;
            }

            var (dataSelectorResult, statusCode, error) = await dataSelectorsService.GetJsonResponseAsync(request);

            if (statusCode != HttpStatusCode.OK)
            {
                logger.LogError($"Data Selector Parser encountered an error when trying to retrieve data selector: {error}");
                return null;
            }

            if (dataSelectorResult is not { HasValues: true })
            {
                logger.LogDebug("Data selector returned an empty result.");
                return null;
            }

            return dataSelectorResult;
        }
    }
}
