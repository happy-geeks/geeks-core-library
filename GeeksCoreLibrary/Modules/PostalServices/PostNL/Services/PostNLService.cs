using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
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
        private readonly IWiserItemsService wiserItemsService;

        private const string PostNlLogTableName = "postnl_log";

        public PostNLService(ILogger<PostNLService> logger,
            IDatabaseConnection databaseConnection,
            IOptions<GclSettings> gclSettings,
            IObjectsService objectService,
            IWiserItemsService wiserItemsService)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
            this.objectService = objectService;
            this.wiserItemsService = wiserItemsService;
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
        private async Task CleanLogsAsync()
        {
            await databaseConnection.ExecuteAsync($"DELETE FROM {PostNlLogTableName} WHERE datetime < DATE_SUB(NOW(), INTERVAL 2 WEEK)");
        }

        /// <summary>
        /// Gets the settings for the specified shipping location
        /// </summary>
        /// <param name="shippingLocation"></param>
        /// <returns></returns>
        public async Task<SettingsModel> GetSettingsAsync(ShippingLocations shippingLocation)
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
                var countryItem = await wiserItemsService.GetItemDetailsAsync(countryId);
                countryCode = countryItem.GetDetailValue("name_short")?.ToUpper();
            }
            else
            {
                countryCode = orderCountryCode;
            }

            return countryCode;
        }

        /// <summary>
        /// Generate the shipping label based on the specified orderId
        /// </summary>
        /// <param name="encryptedOrderIds">Comma separated string of orderIds to create a shipping label</param>
        /// <returns>Action result containing the text for the reason of the result or error</returns>
        public async Task<string> GenerateShippingLabelAsync(string encryptedOrderIds)
        {
            var result = new List<string>();

            foreach (var encryptedId in encryptedOrderIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var postNlDetailsItemId = UInt64.Parse(await this.objectService.FindSystemObjectByDomainNameAsync("postnl_details_item_id"));
                var orderId = encryptedId.DecryptWithAes(withDateTime: true, minutesValidOverride: 30);
                var postNLDetails = await wiserItemsService.GetItemDetailsAsync(postNlDetailsItemId);
                var orderDetails = await wiserItemsService.GetItemDetailsAsync(UInt64.Parse(orderId));

                if (orderDetails == null || orderDetails.Id == 0)
                {
                    result.Add($"Order met ID '{orderId}' niet gevonden!");
                    continue;
                }

                var barcode = orderDetails.GetDetailValue("postnl_barcode");
                var shippingAddress = new AddressModel
                {
                    City = orderDetails.GetDetailValue("shipping_city"),
                    Countrycode = await GetCountryCodeAsync(orderDetails, "shipping_country"),
                    FirstName = orderDetails.GetDetailValue("firstname"),
                    Name = orderDetails.GetDetailValue("lastname"),
                    HouseNumber = orderDetails.GetDetailValue("shipping_housenumber"),
                    HouseNumberAddition = orderDetails.GetDetailValue("shipping_housenumber_suffix"),
                    Street = orderDetails.GetDetailValue("shipping_street"),
                    Zipcode = orderDetails.GetDetailValue("shipping_zipcode"),
                    AddressType = "01"
                };
                if (String.IsNullOrWhiteSpace(shippingAddress.Zipcode))
                {
                    shippingAddress.Zipcode = orderDetails.GetDetailValue("zipcode");
                    shippingAddress.City = orderDetails.GetDetailValue("city");
                    shippingAddress.Street = orderDetails.GetDetailValue("street");
                    shippingAddress.HouseNumber = orderDetails.GetDetailValue("housenumber");
                    shippingAddress.HouseNumberAddition = orderDetails.GetDetailValue("housenumber_suffix");
                    shippingAddress.Countrycode = await GetCountryCodeAsync(orderDetails, "country");
                }

                ShippingLocations shippingLocation;
                if (String.Equals("NL", shippingAddress.Countrycode ?? "NL", StringComparison.OrdinalIgnoreCase))
                {
                    shippingLocation = ShippingLocations.Netherlands;
                }
                else if (EuropeanCountries.Any(c => c.Equals(shippingAddress.Countrycode, StringComparison.OrdinalIgnoreCase)))
                {
                    shippingLocation = ShippingLocations.Europe;
                }
                else
                {
                    shippingLocation = ShippingLocations.Global;
                }

                barcode = (await CreateNewBarcodeAsync(orderId, shippingLocation))?.Barcode;

                var settings = await GetSettingsAsync(shippingLocation);
                var postNlRequest = new ShipmentRequestModel
                {
                    Customer = new CustomerModel
                    {
                        CustomerCode = settings.CustomerCode,
                        CustomerNumber = settings.CustomerNumber,
                        Address = new AddressModel
                        {
                            AddressType = "02",
                            CompanyName = postNLDetails.GetDetailValue("company_name"),
                            City = postNLDetails.GetDetailValue("city"),
                            Countrycode = postNLDetails.GetDetailValue("country"),
                            HouseNumberAddition = postNLDetails.GetDetailValue("number_ex"),
                            HouseNumber = postNLDetails.GetDetailValue("number"),
                            Street = postNLDetails.GetDetailValue("street"),
                            Zipcode = postNLDetails.GetDetailValue("zipcode")
                        },
                        Email = orderDetails.GetDetailValue("email")
                    },
                    Message = new MessageModel
                    {
                        MessageId = orderId,
                        MessageTimeStamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        Printertype = "GraphicFile|PDF"
                    },
                    Shipments = new List<ShipmentModel>
                        {
                            new ShipmentModel
                            {
                                ProductCodeDelivery = settings.ProductCode,
                                Addresses = new List<AddressModel> { shippingAddress },
                                Barcode = barcode,
                                Contacts = new List<ContactModel>
                                {
                                    new ContactModel
                                    {
                                        Email = orderDetails.GetDetailValue("email"),
                                        SmsNumber = orderDetails.GetDetailValue("phone")
                                    }
                                },
                                Remark = orderId
                            }
                        }
                };
                if (shippingLocation == ShippingLocations.Global)
                {
                    postNlRequest.Shipments.First().Customs = new CustomsModel
                    {
                        Content = new List<CustomsContentModel>(),
                        Currency = "EUR",
                        HandleAsNonDeliverable = "false",
                        Invoice = "true",
                        InvoiceNumber = orderId,
                        ShipmentType = "Commercial Goods"
                    };
                    var orderLines = await wiserItemsService.GetLinkedItemDetailsAsync(UInt64.Parse(orderId), 5002, "orderline");
                    foreach (WiserItemModel orderLine in orderLines)
                    {
                        postNlRequest.Shipments.First()
                            .Customs.Content.Add(new CustomsContentModel
                            {
                                Description = orderLine.GetDetailValue("title"),
                                CountryOfOrigin = "NL",
                                HsTariffNumber = "621112",
                                Quantity = orderLine.GetDetailValue("quantity"),
                                Value = orderLine.GetDetailValue("price").Replace(",", "."),
                                Weight = "500"
                            });
                    }
                }

                var postNlResponse = await CreateTrackTraceLabelAsync(orderId, postNlRequest);
                if (postNlResponse?.ResponseShipments == null || !postNlResponse.ResponseShipments.Any())
                {
                    result.Add($"Order {orderId}: Er is iets fout gegaan met de koppeling met de PostNL API.");
                    continue;
                }

                barcode = postNlResponse.ResponseShipments.First().Barcode;

                orderDetails.SetDetail("postnl_barcode", barcode);
                orderDetails.SetDetail("country_code", await GetCountryCodeAsync(orderDetails, "country"));

                await wiserItemsService.UpdateAsync(orderDetails.Id, orderDetails);
                foreach (var labelResponseShipment in postNlResponse.ResponseShipments)
                {
                    if (labelResponseShipment.Errors.Any())
                    {
                        result.Add($"Order {orderId}: De PostNL API heeft een of meer fouten gegeven: {String.Join(", ", labelResponseShipment.Errors.Select(x => x.Description))}");
                        continue;
                    }

                    foreach (var label in labelResponseShipment.Labels)
                    {
                        await wiserItemsService.AddItemFileAsync(new WiserItemFileModel
                        {
                            ItemId = ulong.Parse(orderId),
                            Content = Convert.FromBase64String(label.Content),
                            Extension = ".pdf",
                            FileName = $"{label.Labeltype}.pdf",
                            ContentType = "application/pdf",
                            PropertyName = "postnl_label",
                            Title = barcode
                        });
                    }
                }

                result.Add($"Order {orderId}: Er is succesvol een verzendlabel gegenereerd en verstuurd naar de klant, deze kan gevonden worden op de tab 'PostNL' van deze order. De track&trace code is: {barcode}");
            }

            return result.ToString();
        }

        /// <summary>
        /// Creates a new barcode using the PostNL Api
        /// </summary>
        /// <param name="orderId">The orderId</param>
        /// <param name="shippingLocation">The location the order will be send to</param>
        /// <returns>A model containing the barcode created using the api</returns>
        public async Task<BarcodeResponseModel> CreateNewBarcodeAsync(string orderId, ShippingLocations shippingLocation = ShippingLocations.Netherlands)
        {
            var exceptionMessage = "";
            var responseString = "";
            var requestString = "";

            try
            {
                var restClient = new RestClient(gclSettings.PostNlApiBaseUrl);
                var restRequest = new RestRequest("/shipment/v1_1/barcode", Method.GET);
                var settings = await GetSettingsAsync(shippingLocation);

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
                await CleanLogsAsync();

                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("order_id", orderId);
                databaseConnection.AddParameter("request", requestString);
                databaseConnection.AddParameter("response", responseString);
                databaseConnection.AddParameter("exception", exceptionMessage);

                await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(PostNlLogTableName, 0UL);
            }
        }

        /// <summary>
        /// Generates a new label using the PostNL api
        /// </summary>
        /// <param name="orderId">The orderId of the order the label must be created for</param>
        /// <param name="request">Request model containing all the data used for creating the label</param>
        /// <returns>Model containing the information of the generated label</returns>
        public async Task<ShipmentResponseModel> CreateTrackTraceLabelAsync(string orderId, ShipmentRequestModel request)
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
                await CleanLogsAsync();

                databaseConnection.ClearParameters();
                databaseConnection.AddParameter("order_id", orderId);
                databaseConnection.AddParameter("request", requestString);
                databaseConnection.AddParameter("response", responseString);
                databaseConnection.AddParameter("exception", exceptionMessage);

                await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(PostNlLogTableName, 0UL);
            }
        }
    }
}