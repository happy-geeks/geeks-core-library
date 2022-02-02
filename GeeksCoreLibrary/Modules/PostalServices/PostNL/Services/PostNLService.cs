using GeeksCoreLibrary.Modules.PostalServices.PostNL;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Services
{
    public class PostNLService : IPostNLService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IObjectsService objectService;

        public PostNLService(ILogger<PostNLService> logger,
            IDatabaseConnection databaseConnection,
            IOptions<GclSettings> gclSettings,
            IObjectsService objectService)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
            this.objectService = objectService;
        }

        public static List<string> EuropeanCountries = new() { "AT", "IT", "BE", "LV", "BG", "LT", "HR", "LU", "CY", "CZ", "DK", "EE", "PL", "FI", "PT", "FR", "RO", "DE", "SK", "SI", "GR", "ES", "HU", "SE", "IE" };

        public enum ShippingLocations
        {
            Netherlands,
            Europe,
            Global
        }

        /// <summary>
        /// Cleans the PostNL log table
        /// </summary>
        private async Task CleanLogs()
        {
            await databaseConnection.ExecuteAsync("DELETE FROM cust_postnl_log WHERE datetime < DATE_SUB(NOW(), INTERVAL 2 WEEK)");
        }

        /// <summary>
        /// Gets the settings for the specified shipping location
        /// </summary>
        /// <param name="shippingLocation"></param>
        /// <returns></returns>
        public async Task<SettingsModel> GetSettings(ShippingLocations shippingLocation)
        {
            var result = new SettingsModel();
            switch (shippingLocation)
            {
                case ShippingLocations.Netherlands:
                    result.CustomerCode = await objectService.FindSystemObjectByDomainNameAsync("PostNlNetherlandsCustomerCode");
                    result.CustomerNumber = await objectService.FindSystemObjectByDomainNameAsync("PostNlNetherlandsCustomerNumber");
                    result.BarcodeType = "3S";
                    result.BarcodeSerie = "000000000-999999999";
                    result.ProductCode = "3085";
                    break;

                case ShippingLocations.Europe:
                    result.CustomerCode = await objectService.FindSystemObjectByDomainNameAsync("PostNlEuropeCustomerCode");
                    result.CustomerNumber = await objectService.FindSystemObjectByDomainNameAsync("PostNlNetherlandsCustomerNumber");
                    result.BarcodeType = "3S";
                    result.BarcodeSerie = "0000000-9999999";
                    result.ProductCode = "4952";
                    break;

                case ShippingLocations.Global:
                    result.CustomerCode = await objectService.FindSystemObjectByDomainNameAsync("PostNlGlobalCustomerCode");
                    result.CustomerNumber = await objectService.FindSystemObjectByDomainNameAsync("PostNlEuropeCustomerNumber");
                    result.BarcodeType = "CL";
                    result.BarcodeSerie = "0000-9999";
                    result.ProductCode = "4945";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(shippingLocation), shippingLocation, null);
            }

            return result;
        }

        /// <summary>
        /// Creates a new barcode using the PostNL Api
        /// </summary>
        /// <param name="orderId">The orderId</param>
        /// <param name="shippingLocation">The location the order will be send to</param>
        /// <returns>A model containing the barcode created using the api</returns>
        public async Task<BarcodeResponseModel> CreateNewBarcode(string orderId, ShippingLocations shippingLocation = ShippingLocations.Netherlands)
        {
            var exceptionMessage = "";
            var responseString = "";
            var requestString = "";

            try
            {
                var restClient = new RestClient(gclSettings.PostNlApiBaseUrl);
                var restRequest = new RestRequest("/shipment/v1_1/barcode", Method.GET);
                var settings = await GetSettings(shippingLocation);

                restRequest.AddQueryParameter("CustomerCode", settings.CustomerCode);
                restRequest.AddQueryParameter("CustomerNumber", settings.CustomerNumber);
                restRequest.AddQueryParameter("Type", settings.BarcodeType);
                restRequest.AddQueryParameter("Serie", settings.BarcodeSerie);
                restRequest.AddHeader("apiKey", gclSettings.PostNlShippingApiKey);

                var response = await restClient.ExecuteAsync<BarcodeResponseModel>(restRequest);
                responseString = response.Content;
                if (response.ErrorException != null)
                {
                    exceptionMessage = response.ErrorException.ToString();
                }

                return response.Data;
            }
            catch (Exception exception)
            {
                exceptionMessage = exception.ToString();
                throw;
            }
            finally
            {
                await CleanLogs();

                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("order_id", orderId);
                databaseConnection.AddParameter("request", requestString);
                databaseConnection.AddParameter("response", responseString);
                databaseConnection.AddParameter("exception", exceptionMessage);

                await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync("cust_postnl_log", 0UL);
            }
        }

        /// <summary>
        /// Generates a new label using the PostNL api
        /// </summary>
        /// <param name="orderId">The orderId of the order the label must be created for</param>
        /// <param name="request">Request model containing all the data used for creating the label</param>
        /// <returns>Model containing the information of the generated label</returns>
        public async Task<ShipmentResponseModel> CreateTrackTraceLabel(string orderId, ShipmentRequestModel request)
        {
            var exceptionMessage = "";
            var responseString = "";
            var requestString = "";

            try
            {
                var restClient = new RestClient(gclSettings.PostNlApiBaseUrl);
                var restRequest = new RestRequest("/v1/shipment?confirm=true", Method.POST);
                requestString = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                restRequest.AddParameter("application/json", requestString, ParameterType.RequestBody);
                restRequest.AddJsonBody(request);
                restRequest.AddHeader("apiKey", gclSettings.PostNlShippingApiKey);

                var response = await restClient.ExecuteAsync<ShipmentResponseModel>(restRequest);

                responseString = response.Content;
                if (response.ErrorException != null)
                {
                    exceptionMessage = response.ErrorException.ToString();
                }

                return response.Data;
            }
            catch (Exception exception)
            {
                exceptionMessage = exception.ToString();
                throw;
            }
            finally
            {
                await CleanLogs();

                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("order_id", orderId);
                databaseConnection.AddParameter("request", requestString);
                databaseConnection.AddParameter("response", responseString);
                databaseConnection.AddParameter("exception", exceptionMessage);

                await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync("cust_postnl_log", 0UL);
            }
        }
    }
}