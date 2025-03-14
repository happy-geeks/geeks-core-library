using System;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Models.Pro6PP;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace GeeksCoreLibrary.Core.Services;

public class GeoLocationService(IObjectsService objectsService, ILogger<GeoLocationService> logger)
    : IGeoLocationService, IScopedService
{
    /// <inheritdoc />
    public async Task<AddressInfoModel> GetAddressInfoAsync(string zipCode, string houseNumber, string houseNumberAddition = "", string country = "")
    {
        var authKey = await objectsService.FindSystemObjectByDomainNameAsync("pro6pp_key");
        if (String.IsNullOrWhiteSpace(authKey))
        {
            return new AddressInfoModel {Success = false, Error = "No auth key set."};
        }

        // Create client and request.
        var restClient = new RestClient("https://api.pro6pp.nl", configureSerialization: serializerConfig => serializerConfig.UseNewtonsoftJson());

        var restRequest = new RestRequest("/v1/autocomplete");
        restRequest.AddQueryParameter("auth_key", authKey);

        // It's possible to send a zip-code in "6PP" or "4PP" format. A different query string is used for both variants.
        switch (country)
        {
            case "nl":
                switch (zipCode.Length)
                {
                    case >= 6:
                        restRequest.AddQueryParameter($"{country}_sixpp", zipCode.Replace(" ", "")[..6]);
                        break;
                    case >= 4:
                        restRequest.AddQueryParameter($"{country}_fourpp", zipCode[..4]);
                        break;
                    default:
                        return new AddressInfoModel {Success = false, Error = $"Incorrect ZIP code format: {zipCode}"};
                }

                break;
            case "be":
                if (zipCode.Length >= 4)
                {
                    restRequest.AddQueryParameter($"{country}_fourpp", zipCode[..4]);
                }
                else
                {
                    return new AddressInfoModel {Success = false, Error = $"Incorrect ZIP code format: {zipCode}"};
                }

                break;
            default:
                return new AddressInfoModel {Success = false, Error = $"Unknown or unsupported country code: {country}. Valid country codes are 'nl' and 'be'."};
        }

        if (!String.IsNullOrWhiteSpace(houseNumber))
        {
            // When a house number is entered, the extension should always be sent as well, otherwise the API will treat the request as if a house number wasn't provided.
            restRequest.AddQueryParameter("streetnumber", houseNumber);
            restRequest.AddQueryParameter("extension", houseNumberAddition);
        }

        // Although JSON format is the default, it is explicitly set anyway, in case the default ever changes.
        restRequest.AddQueryParameter("format", "json");

        try
        {
            var restResult = await restClient.ExecuteAsync<Pro6PPAutoCompleteResultModel>(restRequest);
            if (!restResult.IsSuccessful || restResult.Data == null || !restResult.Data.Status.Equals("ok", StringComparison.OrdinalIgnoreCase) || !restResult.Data.Results.Any())
            {
                return new AddressInfoModel {Success = false, Error = restResult.Data?.Error?.Message ?? "No matches found, or an unknown error occurred in 6PP API."};
            }

            return new AddressInfoModel
            {
                Success = true,
                StreetName = restResult.Data.Results.First().StreetName,
                PlaceName = restResult.Data.Results.First().City,
                Province = restResult.Data.Results.First().Province,
                Municipality = restResult.Data.Results.First().Municipality,
                Longitude = restResult.Data.Results.First().Longitude,
                Latitude = restResult.Data.Results.First().Latitude
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "An error occurred while retrieving address information from 6PP API.");
            return new AddressInfoModel {Success = false, Error = exception.Message};
        }
    }
}