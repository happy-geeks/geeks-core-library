using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Enums.AfterPay;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Payments.Models.AfterPay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class AfterPayService : IPaymentServiceProviderService, IScopedService
    {
        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        private readonly ILogger<BuckarooService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly ILanguagesService languagesService;

        public AfterPayService(Logger<BuckarooService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection, ILanguagesService languagesService)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
            this.languagesService = languagesService;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            var request = "";
            var response = "";
            var error = "";

            try
            {
                var basketSettings = await shoppingBasketsService.GetSettingsAsync();

                var totalPriceWithVat = 0M;
                var totalPriceWithoutVat = 0M;
                foreach (var (main, lines) in shoppingBaskets)
                {
                    totalPriceWithVat += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
                    totalPriceWithVat += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceExVat);
                }

                var apiKey = await objectsService.FindSystemObjectByDomainNameAsync("AFTERPAY_ApiKey");
                var merchantImageUrl = await objectsService.FindSystemObjectByDomainNameAsync("AFTERPAY_MerchantImageUrl");
                var useTestEnvironment = gclSettings.Environment.InList(Environments.Test, Environments.Development);

                PaymentTypes afterPayPaymentType;
                switch (paymentMethod)
                {
                    case PaymentMethods.OnInvoice:
                        afterPayPaymentType = PaymentTypes.Invoice;
                        break;
                    case PaymentMethods.FlexPayment:
                        afterPayPaymentType = PaymentTypes.Account;
                        break;
                    case PaymentMethods.FixedInstallments:
                        afterPayPaymentType = PaymentTypes.Installment;
                        break;
                    case PaymentMethods.ConsolidatedInvoice:
                        afterPayPaymentType = PaymentTypes.Consolidatedinvoice;
                        break;
                    case PaymentMethods.CampaignInvoice:
                        afterPayPaymentType = PaymentTypes.DirectDebitInvoice;
                        break;
                    default:
                        return new PaymentRequestResult
                        {
                            Action = PaymentRequestActions.Redirect,
                            ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                            Successful = false,
                            ErrorMessage = $"Unknown or unsupported payment method '{paymentMethod:G}'"
                        };
                }

                var orderItems = new List<ItemModel>();
                foreach (var (main, lines) in shoppingBaskets)
                {
                    foreach (var basketLine in lines)
                    {
                        // Get the title of the product. If it is a coupon no title is provided and description will be used instead.
                        var name = basketLine.GetDetailValue("title");
                        if (String.IsNullOrWhiteSpace(name))
                        {
                            name = basketLine.GetDetailValue("description");
                        }

                        var lineType = basketLine.GetDetailValue("type");
                        var afterPayItemType = lineType.ToLowerInvariant() switch
                        {
                            "product" => OrderItemTypes.PhysicalArticle,
                            "digital_product" => OrderItemTypes.DigitalArticle,
                            "discount" => OrderItemTypes.Discount,
                            "coupon" => OrderItemTypes.Discount,
                            "gift_card" => OrderItemTypes.GiftCard,
                            "info" => OrderItemTypes.Info,
                            "shipping_costs" => OrderItemTypes.ShippingFee,
                            "paymentmethod_costs" => OrderItemTypes.Surcharge,
                            "surcharge" => OrderItemTypes.Surcharge,
                            _ => throw new ArgumentOutOfRangeException(nameof(lineType), lineType)
                        };

                        orderItems.Add(new ItemModel
                        {
                            ProductId = basketLine.GetDetailValue("connecteditemid"),
                            Description = name,
                            Type = afterPayItemType,
                            NetUnitPrice = await shoppingBasketsService.GetLinePriceAsync(main, basketLine, basketSettings, ShoppingBasket.PriceTypes.ExVatInDiscount, true),
                            GrossUnitPrice = await shoppingBasketsService.GetLinePriceAsync(main, basketLine, basketSettings, ShoppingBasket.PriceTypes.InVatInDiscount, true),
                            Quantity = basketLine.GetDetailValue<decimal>(basketSettings.QuantityPropertyName),
                            VatPercent = (await shoppingBasketsService.GetVatRuleByRateAsync(main, basketSettings, basketLine.GetDetailValue<int>(basketSettings.VatRatePropertyName))).Percentage,
                            VatAmount = await shoppingBasketsService.GetLinePriceAsync(main, basketLine, basketSettings, ShoppingBasket.PriceTypes.VatOnly, true),
                            ImageUrl = basketLine.GetDetailValue("imageUrl"),
                            ProductUrl = basketLine.GetDetailValue("productUrl"),
                            AdditionalInformation = basketLine.GetDetailValue("additionalInformation")
                        });
                    }
                }

                var requestData = new AuthorizePaymentRequestModel
                {
                    Payment = new PaymentModel
                    {
                        Type = afterPayPaymentType
                    },
                    Customer = CreateCustomerModel(userDetails, "invoice_") ?? CreateCustomerModel(userDetails),
                    DeliveryCustomer = CreateCustomerModel(userDetails, "delivery_"),
                    Order = new OrderModel
                    {
                        Number = invoiceNumber,
                        Currency = "EUR",
                        TotalNetAmount = totalPriceWithoutVat,
                        TotalGrossAmount = totalPriceWithVat,
                        MerchantImageUrl = String.IsNullOrWhiteSpace(merchantImageUrl) ? null : merchantImageUrl,
                        Items = orderItems
                    },
                    YourReference = shoppingBaskets.First().Main.GetDetailValue("reference"),
                    OurReference = invoiceNumber
                };

                request = JsonConvert.SerializeObject(requestData);

                var restClient = new RestClient(useTestEnvironment ? "https://sandbox.afterpay.io" : "https://api.afterpay.io");
                var restRequest = new RestRequest("/api/v3/checkout/authorize");
                restRequest.AddHeader("X-Auth-Key", apiKey);
                restRequest.AddJsonBody(requestData, "application/json");
                var restResponse = restClient.Execute<AuthorizePaymentResponseModel>(restRequest);
                response = restResponse.Content;

                var success = restResponse.StatusCode == HttpStatusCode.OK;
                var errorMessage = success ? null : restResponse.ErrorMessage;
                string redirectUrl;

                if (!success)
                {
                    redirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
                }
                else
                {
                    switch (restResponse.Data?.Outcome?.ToLowerInvariant())
                    {
                        case "accepted":
                            redirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL");
                            break;
                        case "pending":
                            redirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_pendingURL", await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL"));
                            break;
                        case "rejected":
                            errorMessage = "Rejected by AfterPay";
                            redirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_rejectURL");
                            break;
                        case "notevaluated":
                            errorMessage = "Not evaluated by AfterPay";
                            redirectUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Outcome", restResponse.Data?.Outcome);
                    }
                }

                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = redirectUrl,
                    ErrorMessage = errorMessage,
                    Successful = success
                };
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                throw;
            }
            finally
            {
                await LogPaymentActionAsync(invoiceNumber, request, response, error);
            }
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Convert the user details to a <see cref="CustomerModel"/>.
        /// </summary>
        /// <param name="userDetails">The <see cref="WiserItemModel"/> containing the user details.</param>
        /// <param name="detailKeyPrefix">Additional string as a prefix for "street", "zipcode", "city", "country", "housenumber" and "housenumber_suffix". For example for shipping.</param>
        /// <returns>Returns a <see cref="CustomerModel"/> with the required information.</returns>
        private CustomerModel CreateCustomerModel(WiserItemModel userDetails, string detailKeyPrefix = "")
        {
            var firstName = userDetails.GetDetailValue($"{detailKeyPrefix}firstname");
            var lastName = userDetails.GetDetailValue($"{detailKeyPrefix}lastname");
            if (String.IsNullOrWhiteSpace(firstName) && String.IsNullOrWhiteSpace(lastName))
            {
                return null;
            }

            var result = new CustomerModel
            {
                CustomerNumber = userDetails.Id.ToString(),
                IdentificationNumber = userDetails.GetDetailValue("ssn"),
                Salutation = userDetails.GetDetailValue($"{detailKeyPrefix}salutation"),
                FirstName = firstName,
                LastName = lastName,
                CompanyName = userDetails.GetDetailValue($"{detailKeyPrefix}companyname"),
                Email = userDetails.GetDetailValue($"{detailKeyPrefix}email"),
                Phone = userDetails.GetDetailValue($"{detailKeyPrefix}phone"),
                MobilePhone = userDetails.GetDetailValue($"{detailKeyPrefix}mobilePhone"),
                BirthDate = userDetails.GetDetailValue($"{detailKeyPrefix}birthdate"),
                CustomerCategory = userDetails.GetDetailValue($"{detailKeyPrefix}customerCategory") ?? "Person",
                RiskData = new CustomerRiskModel
                {
                    IpAddress = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                },
                ConversationLanguage = languagesService.CurrentLanguageCode,
                DistributionType = DistributionTypes.Email
            };

            var street = userDetails.GetDetailValue($"{detailKeyPrefix}street");
            var zipCode = userDetails.GetDetailValue($"{detailKeyPrefix}zipcode");
            var city = userDetails.GetDetailValue($"{detailKeyPrefix}city");
            var country = userDetails.GetDetailValue($"{detailKeyPrefix}country");
            if (!String.IsNullOrWhiteSpace(street) || !String.IsNullOrWhiteSpace(zipCode) || !String.IsNullOrWhiteSpace(city) || !String.IsNullOrWhiteSpace(country))
            {
                result.Address = new AddressModel
                {
                    Street = street,
                    StreetNumber = userDetails.GetDetailValue($"{detailKeyPrefix}houseNumber"),
                    StreetNumberAdditional = userDetails.GetDetailValue($"{detailKeyPrefix}houseNumber_suffix"),
                    PostalCode = zipCode,
                    PostalPlace = city,
                    CountryCode = country
                };
            }
            
            return result;
        }

        public async Task<bool> LogPaymentActionAsync(string invoiceNumber, string requestBody = "", string responseBody = "", string error = "")
        {
            if (!LogPaymentActions || httpContextAccessor?.HttpContext == null)
            {
                return false;
            }

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.AfterPay, invoiceNumber, requestBody: requestBody, responseBody: responseBody, error: error);
        }
    }
}
