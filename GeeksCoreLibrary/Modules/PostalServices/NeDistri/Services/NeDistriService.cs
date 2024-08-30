using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Interfaces;
using GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;

using OrderProcessConstants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Services;

public class NeDistriService : INeDistriService, IScopedService
{
    private readonly GclSettings gclSettings;
    private readonly IWiserItemsService wiserItemsService;
    private readonly ILogger<NeDistriService> logger;

    private readonly JsonSerializerSettings jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Formatting = Formatting.Indented
    };

    public NeDistriService( IOptions<GclSettings> gclSettings, IWiserItemsService wiserItemsService, ILogger<NeDistriService> logger)
    {
        this.gclSettings = gclSettings.Value;
        this.wiserItemsService = wiserItemsService;
        this.logger = logger;
    }

    public async Task<string> GenerateShippingLabelAsync(string encryptedOrderIds, string labelType, int colliAmount,
        int? userCode, OrderType orderType)
    {
        // Decrypt all order ids
        var orderIds = DecryptOrderIds(encryptedOrderIds);

        // Authenticate with the NE distri service API
        // This gets us a reusable JWT token
        var authenticationResult = await AuthenticateAsync();
        var options = new RestClientOptions(gclSettings.NeDistriApiBaseUrl)
        {
            Authenticator = new JwtAuthenticator(authenticationResult.Token)
        };
        using var restClient = new RestClient(options);

        StringBuilder results = new StringBuilder();
        foreach (var orderId in orderIds)
        {
            var orderItem = await wiserItemsService.GetItemDetailsAsync(orderId, entityType: OrderProcessConstants.OrderEntityType, skipPermissionsCheck: true);
            
            var createOrderResponse = await CreateOrderAsync(orderItem, userCode, colliAmount, labelType, orderType, restClient);

            if (createOrderResponse.Response is null)
            {
                results.AppendLine(createOrderResponse.Message);
                continue;
            }
            
            orderItem.SetDetail("NeDistri_orderId", createOrderResponse.Response.Id);

            var barcodeResponses = await GetBarcodeAsync(createOrderResponse.Response.Id, orderId, restClient);
            if (barcodeResponses.Responses is null)
            {
                results.AppendLine(createOrderResponse.Message);
                continue;
            }
            
            foreach (var barcodeResponse in barcodeResponses.Responses)
            {
                orderItem.SetDetail("NeDistri_barcode", barcodeResponse.Barcode, append: true);
                orderItem.SetDetail("NeDistri_ruleId", barcodeResponse.RuleId, append: true);
                orderItem.SetDetail("NeDistri_coliNumber", barcodeResponse.ColiNumber, append: true);

                await wiserItemsService.SaveAsync(orderItem);
                await wiserItemsService.AddItemFileAsync(new WiserItemFileModel
                {
                    ItemId = orderId,
                    Content = Convert.FromBase64String(barcodeResponse.Attachment),
                    Extension = ".pdf",
                    FileName = $"NeDistri-{createOrderResponse.Response.Id}-{labelType}.pdf",
                    ContentType = "application/pdf",
                    PropertyName = "NeDistri_label",
                    Title = barcodeResponse.Barcode
                }, skipPermissionsCheck: true);
            }

            results.AppendLine($"Label aanmaken was succesvol voor order {orderId}");
        }

        return results.ToString();
    }

    private async Task<(IEnumerable<BarcodeResponse> Responses, string Message)> GetBarcodeAsync(int distriOrderId, ulong wiserOrderId, IRestClient restClient)
    {
        var restRequest = new RestRequest($"/api/v1/order-stickers?id={distriOrderId}");

        var barcodeResponse = await restClient.ExecuteAsync(restRequest);

        if (!barcodeResponse.IsSuccessful)
        {
            logger.LogError($"Request to get order failed for order {wiserOrderId}. This is order {distriOrderId} in NE DistriService. Response: {barcodeResponse.Content}");
            return (null, $"Het ophalen van de label ging mis voor order {wiserOrderId}. Hiervoor is wel order {distriOrderId} aangemaakt in NE DistriService");
        }

        return (JsonConvert.DeserializeObject<IEnumerable<BarcodeResponse>>(barcodeResponse.Content, jsonSettings), null);
    }

    private List<ulong> DecryptOrderIds(string encryptedOrderIds)
    {
        var orderIds = new List<ulong>();
        foreach (var encryptedId in encryptedOrderIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string decryptedOrderId;
            try
            {
                decryptedOrderId = encryptedId.DecryptWithAesWithSalt(withDateTime: true, minutesValidOverride: 30);
            }
            catch (Exception e)
            {
                logger.LogError(e,"Something went wrong when decrypting order id in NeDistriService");
                throw;
            }

            var parsed = UInt64.TryParse(decryptedOrderId, out var parsedOrderId);
            if (!parsed)
            {
                throw new ArgumentException(encryptedOrderIds);
            }
            
            orderIds.Add(parsedOrderId);
        }

        return orderIds;
    }

    private async Task<(CreateOrderResponse Response, string Message)> CreateOrderAsync(WiserItemModel orderItem, int? userCode, int colliAmount,
        string labelType, OrderType orderType, IRestClient restClient)
    {
        var requestModel = new CreateOrderModel
        {
            Address = await CreateAddressModelAsync(orderItem),
            UserCode = userCode,
            Reference = new List<string>
            {
                orderItem.Id.ToString()
            },
            OrderType = orderType,
            Rules = new List<RuleModel>
            {
                new()
                {
                    Amount = colliAmount,
                    Unit = labelType
                }
            }
        };
        var createOrderRequestBody = JsonConvert.SerializeObject(requestModel, jsonSettings);

        var restRequest = new RestRequest("/api/v1/order", Method.Post);
        restRequest.AddStringBody(createOrderRequestBody, DataFormat.Json);

        var createOrderResponse = await restClient.ExecuteAsync(restRequest);

        if (!createOrderResponse.IsSuccessful)
        {
            logger.LogError($"Request to recreate order in NeDistriService was rejected. The given error was: ${createOrderResponse.Content}");
            return (null, "Het maken van de order");
        }
        
        return (JsonConvert.DeserializeObject<CreateOrderResponse>(createOrderResponse.Content!, jsonSettings), null);
    }

    private async Task<AuthenticationResponse> AuthenticateAsync()
    {
        var authenticationModel = new AuthenticationModel
        {
            Login = gclSettings.NeDistriShippingLogin,
            // The Nonce is a random string that makes sure the hash of every authentication request is unique
            Nonce = SecurityHelpers.GenerateRandomPassword()
        };

        var authenticationBody = JsonConvert.SerializeObject(authenticationModel, jsonSettings);
        var key = gclSettings.NeDistriSecretKey;
        
        // To authenticate we need create a signature
        // To do this we create a SHA515 hash of the body 
        string signature;
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(key);
            var authenticationBodyBytes = Encoding.UTF8.GetBytes(authenticationBody);
            var hashBytes = rsa.SignData(authenticationBodyBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            signature = Convert.ToBase64String(hashBytes);
        }
        
        var options = new RestClientOptions(gclSettings.NeDistriApiBaseUrl);
        using var restClient = new RestClient(options);
        var request = new RestRequest("/api/v1/auth", Method.Post);
        
        // Add the authentication signature to our request
        request.AddHeaders(new List<KeyValuePair<string, string>>()
        {
            new("Signature", signature)
        });
        // We add the body in string form because the body has to be identical
        // Because this includes formatting like whitespace and newlines
        request.AddStringBody(authenticationBody, ContentType.Json);
        var authResponse = await restClient.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<AuthenticationResponse>(authResponse.Content);
    }

    private async Task<AddressModel> CreateAddressModelAsync(WiserItemModel orderDetails)
    {
        var prefix = !String.IsNullOrEmpty(orderDetails.GetDetailValue("shipping_zipcode")) ? "shipping_" : String.Empty;

        var city = orderDetails.GetDetailValue($"{prefix}city");
        var countrycode = await GetCountryCodeAsync(orderDetails, $"{prefix}country");
        var firstName = orderDetails.GetDetailValue("firstname");
        var lastname = orderDetails.GetDetailValue("lastname");
        var lastNamePrefix = orderDetails.GetDetailValue("prefix");
        var houseNumber = orderDetails.GetDetailValue($"{prefix}housenumber");
        var houseNumberAddition = orderDetails.GetDetailValue($"{prefix}housenumber_suffix");
        var street = orderDetails.GetDetailValue($"{prefix}street");
        var zipcode = orderDetails.GetDetailValue($"{prefix}zipcode");
        var email = orderDetails.GetDetailValue("email");

        if (!String.IsNullOrEmpty(lastNamePrefix))
        {
            lastname = $"{lastNamePrefix} {lastname}";
        }
        return new AddressModel()
        {
            Address = $"{street} {houseNumber}{houseNumberAddition}",
            Country = countrycode,
            Email = email,
            Name = $"{firstName} {lastname}",
            Place = city,
            Zipcode = zipcode
        };
    }
    
    /// <summary>
    /// Function for getting the country short name code based on the specified Wiser item
    /// </summary>
    /// <param name="orderDetails">The details of the order</param>
    /// <param name="entityName">The entity name of the property containing the country code</param>
    /// <returns>The country code</returns>
    private async Task<string> GetCountryCodeAsync(WiserItemModel orderDetails, string entityName)
    {
        var orderCountryCode = orderDetails.GetDetailValue(entityName);
        string countryCode;

        if (UInt64.TryParse(orderCountryCode, out var countryId))
        {
            var countryItem = await wiserItemsService.GetItemDetailsAsync(countryId, skipPermissionsCheck: true);
            countryCode = countryItem.GetDetailValue("name_short")?.ToUpper();
        }
        else
        {
            countryCode = orderCountryCode;
        }

        return countryCode;
    }
}