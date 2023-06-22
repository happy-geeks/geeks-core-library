using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Payments.Models.Mollie;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoneNumbers;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class MollieService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
    {
        private readonly ILogger<MollieService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;

        public MollieService(
            ILogger<MollieService> logger,
            IObjectsService objectsService,
            IShoppingBasketsService shoppingBasketsService,
            IDatabaseConnection databaseConnection,
            IDatabaseHelpersService databaseHelpersService,
            IHttpContextAccessor httpContextAccessor = null)
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(
            ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
            WiserItemModel userDetails,
            PaymentMethodSettingsModel paymentMethodSettings,
            string invoiceNumber)
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            // Retrieve the API key. A development-specific one can be set for testing on development environments.
            var mollieSettings = (MollieSettingsModel) paymentMethodSettings.PaymentServiceProvider;
            mollieSettings.Locale = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_locale");

            // Locale is required for the mollie Orders api so if we couldn't find the locale
            // we set it using the request headers send by the browser
            // if that is not set we use EN-us as the default
            if (String.IsNullOrWhiteSpace(mollieSettings.Locale))
            {
                mollieSettings.Locale = httpContextAccessor?.HttpContext.Request.Headers.AcceptLanguage.First();
                mollieSettings.Locale = mollieSettings.Locale?.Split(',').First() ?? "EN-us";
            }

            var mollieClient = new OrderClient(mollieSettings.ApiKey);
            var orderRequest = await CreateOrderRequestAsync(
                invoiceNumber,
                shoppingBaskets,
                userDetails,
                paymentMethodSettings);

            OrderResponse orderResponse;
            try
            {
                orderResponse = await mollieClient.CreateOrderAsync(orderRequest);
            }
            catch (MollieApiException ex)
            {
                logger.LogError(ex, "An error occurred while trying to create an order via the Mollie Order API.");

                // Payment request failed.
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = mollieSettings.FailUrl,
                    ErrorMessage = GetErrorMessageInResponse(JObject.Parse(ex.Message))
                };
            }

            var paymentId = orderResponse.Id;
            var status = orderResponse.Status;
            await ProcessOrderResponseAsync(paymentId, status, shoppingBaskets);

            return new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = orderResponse.Links.Checkout.Href
            };
        }

        private async Task ProcessOrderResponseAsync(string paymentId, string status, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            foreach (var (main, lines) in shoppingBaskets)
            {
                var history = new StringBuilder(main.GetDetailValue(Constants.PaymentHistoryProperty));
                if (history.Length > 0)
                {
                    history.Append(", ");
                }

                history.Append(status);

                main.SetDetail(Constants.PaymentProviderTransactionId, paymentId);
                main.SetDetail(Constants.PaymentProviderTransactionStatus, status);
                main.SetDetail(Constants.PaymentHistoryProperty, history.ToString());

                await shoppingBasketsService.SaveAsync(main, lines, await shoppingBasketsService.GetSettingsAsync());
            }
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "Error retrieving status: No HttpContext available."
                };
            }

            var mollieSettings = (MollieSettingsModel) paymentMethodSettings.PaymentServiceProvider;
            var mollieClient = new OrderClient(mollieSettings.ApiKey);

            // Mollie sends one POST parameter called "id".
            var mollieOrderId = httpContextAccessor.HttpContext.Request.Form["id"];

            OrderResponse mollieOrder;
            try
            {
                mollieOrder = await mollieClient.GetOrderAsync(mollieOrderId);
            }
            catch (MollieApiException ex)
            {
                await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, String.Empty, ex.Details.Status, responseBody: ex.Message);
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "error"
                };
            }

            // The invoice number is sent as the metadata, which can be retrieved here.
            var invoiceNumber = mollieOrder.Metadata;

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, invoiceNumber, 200, responseBody: JsonConvert.SerializeObject(mollieOrder));

            return new StatusUpdateResult
            {
                Successful = mollieOrder.Status.Equals("paid", StringComparison.OrdinalIgnoreCase),
                Status = mollieOrder.Status
            };
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            var mollieSettings = (MollieSettingsModel) paymentMethodSettings.PaymentServiceProvider;
            var invoiceNumber = HttpContextHelpers.GetRequestValue(httpContextAccessor?.HttpContext, "invoice_number");

            var baskets = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(invoiceNumber);
            if (baskets == null || baskets.Count == 0)
            {
                await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, invoiceNumber, 0, error: $"Unknown invoice number: {invoiceNumber}");

                // Unknown invoice number.
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            // The Mollie payment ID is saved in all baskets, so just use the one from the first basket.
            var mollieOrderId = baskets.First().Order.GetDetailValue(Constants.PaymentProviderTransactionId);

            var mollieOrderClient = new OrderClient(mollieSettings.ApiKey);

            OrderResponse mollieOrder;
            try
            {
                mollieOrder = await mollieOrderClient.GetOrderAsync(mollieOrderId);
            }
            catch (MollieApiException ex)
            {
                await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, String.Empty, ex.Details.Status, responseBody: ex.Message);
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, invoiceNumber, 200, responseBody: JsonConvert.SerializeObject(mollieOrder));

            var successUrl = paymentMethodSettings.PaymentServiceProvider.SuccessUrl;
            var pendingUrl = paymentMethodSettings.PaymentServiceProvider.PendingUrl;
            if (String.IsNullOrWhiteSpace(pendingUrl))
            {
                pendingUrl = successUrl;
            }

            var redirectUrl = mollieOrder.Status switch
            {
                "paid" => successUrl,
                "pending" => pendingUrl,
                _ => paymentMethodSettings.PaymentServiceProvider.FailUrl
            };

            return new PaymentReturnResult
            {
                Action = PaymentResultActions.Redirect,
                ActionData = redirectUrl
            };
        }

        /// <summary>
        /// Attempts to retrieve the error message from Mollie's generic error format.
        /// </summary>
        /// <param name="response">A <see cref="JObject"/> that represents the error.</param>
        /// <returns>A combination of the title and detail properties, or the string "Unknown error" if title and detail are not present in the JObject.</returns>
        private static string GetErrorMessageInResponse(JObject response)
        {
            if (response != null && response.ContainsKey("title") && response.ContainsKey("detail"))
            {
                return $"{response["title"]}: {response["detail"]}";
            }

            return "Unknown error";
        }

        public async Task<OrderRequest> CreateOrderRequestAsync(
            string invoiceNumber,
            ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
            WiserItemModel userDetails,
            PaymentMethodSettingsModel paymentMethodSettings)
        {
            var mollieSettings = (MollieSettingsModel) paymentMethodSettings.PaymentServiceProvider;
            var totalPrice = await CalculatePriceAsync(shoppingBaskets);
            var orderRequest = new OrderRequest
            {
                Amount = CreateAmountModel(totalPrice, mollieSettings.Currency),
                OrderNumber = invoiceNumber,
                RedirectUrl = BuildUrl(mollieSettings.ReturnUrl, invoiceNumber),
                WebhookUrl = BuildUrl(mollieSettings.WebhookUrl, invoiceNumber),
                Locale = mollieSettings.Locale,
                Method = paymentMethodSettings.ExternalName,
                Lines = await ConvertShoppingBasketsToOrderLinesAsync(shoppingBaskets, mollieSettings),
                BillingAddress = CreateBillingAddress(userDetails),
                ShippingAddress = CreateAddress(userDetails, "shipping_")
            };

            if (String.Equals(paymentMethodSettings.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
            {
                var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Constants.PaymentMethodIssuerProperty);
                var issuerName = GetIssuerName(issuerValue);
                orderRequest.Payment = new IDealSpecificParameters
                {
                    Issuer = issuerName,
                };
            }

            // Metadata is always sent back.
            orderRequest.Metadata = invoiceNumber;
            return orderRequest;
        }

        private static string GetIssuerName(string issuerValue)
        {
            var issuerConstants = typeof(IdealIssuers).GetFields(BindingFlags.Public | BindingFlags.Static);
            var issuerConstant = issuerConstants.FirstOrDefault(mi =>
                                                                    mi.Name.Equals(issuerValue, StringComparison.OrdinalIgnoreCase) ||
                                                                    mi.Name.Equals($"ideal_{issuerValue}", StringComparison.OrdinalIgnoreCase));

            if (issuerConstant != null)
            {
                return (string) issuerConstant.GetValue(null);
            }

            return null;
        }

        private OrderAddressDetails CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
        {
            var street = userDetails.GetDetailValue($"{detailKeyPrefix}street");
            var zipcode = userDetails.GetDetailValue($"{detailKeyPrefix}zipcode");
            var city = userDetails.GetDetailValue($"{detailKeyPrefix}city");
            var country = userDetails.GetDetailValue($"{detailKeyPrefix}country");

            //If a prefix is given but any of the required values doesn't contain a value return null.
            if ((!String.IsNullOrWhiteSpace(detailKeyPrefix) &&
                 String.IsNullOrWhiteSpace(street))
                || String.IsNullOrWhiteSpace(zipcode)
                || String.IsNullOrWhiteSpace(city)
                || String.IsNullOrWhiteSpace(country))
            {
                return null;
            }

            var houseNumber = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber");
            var houseNumberSuffix = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix");
            return new OrderAddressDetails
            {
                StreetAndNumber =
                    $"{street} {houseNumber}{houseNumberSuffix}",
                PostalCode = zipcode,
                City = city,
                Country = country,
            };
        }

        private OrderAddressDetails CreateBillingAddress(WiserItemModel userDetails)
        {
            var address = CreateAddress(userDetails);
            address.OrganizationName = userDetails.GetDetailValue("companyname");
            address.GivenName = userDetails.GetDetailValue("firstname");
            address.FamilyName = userDetails.GetDetailValue("lastname");
            address.Email = userDetails.GetDetailValue("email");
            address.Phone = userDetails.GetDetailValue("phone");

            if (String.IsNullOrWhiteSpace(address.Phone))
            {
                return address;
            }

            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var phoneObject = phoneNumberUtil.Parse(address.Phone, address.Country);
            address.Phone = phoneNumberUtil.Format(phoneObject, PhoneNumberFormat.E164);

            return address;
        }

        private async Task<IEnumerable<OrderLineRequest>> ConvertShoppingBasketsToOrderLinesAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, MollieSettingsModel mollieSettings)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var orderRequests = new List<OrderLineRequest>();
            foreach (var basket in shoppingBaskets)
            {
                foreach (var basketLine in basket.Lines)
                {
                    var orderLineRequest = await ConvertBasketLineToOrderLineAsync(basket.Main, basketLine, basketSettings, mollieSettings);
                    orderRequests.Add(orderLineRequest);
                }
            }

            return orderRequests;
        }

        private async Task<OrderLineRequest> ConvertBasketLineToOrderLineAsync(WiserItemModel basket,
                                                                               WiserItemModel basketLine,
                                                                               ShoppingBasketCmsSettingsModel basketSettings,
                                                                               MollieSettingsModel mollieSettings)
        {
            var name = basketLine.GetDetailValue("title");

            // Non-products like shipping cost might not have a name
            // For those we use the type as name
            // This can be further improved by using localisation
            if (String.IsNullOrEmpty(name))
            {
                name = basketLine.GetDetailValue("type");
            }

            // get prices using the shoppingbasketService
            var linePrice = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings
            );
            var linePriceVatOnly = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.VatOnly
            );
            var discountAmount = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.DiscountInVat
            );

            var quantityDetail = basketLine.GetDetailValue(basketSettings.QuantityPropertyName);
            var parseSucceeded = Int32.TryParse(quantityDetail, out var lineProductQuantity);

            // Some types of order lines do not have quantities
            // we set 1 for those
            if (!parseSucceeded)
                lineProductQuantity = 1;

            var vatRate = Convert.ToInt32(Math.Round(100 / linePrice * linePriceVatOnly));
            var vatFactor = await shoppingBasketsService.GetVatFactorByRateAsync(basket, basketSettings, vatRate) * 100;

            return new OrderLineRequest
            {
                Name = name,
                UnitPrice = CreateAmountModel(linePrice / lineProductQuantity, mollieSettings.Currency),
                TotalAmount = CreateAmountModel(linePrice, mollieSettings.Currency),
                DiscountAmount = CreateAmountModel(discountAmount, mollieSettings.Currency),
                VatAmount = CreateAmountModel(linePriceVatOnly, mollieSettings.Currency),
                Quantity = lineProductQuantity,
                VatRate = vatFactor.ToString("F2", CultureInfo.InvariantCulture),
            };
        }

        private Amount CreateAmountModel(decimal price, string currency)
        {
            return new Amount
            {
                Value = price.ToString("F2", CultureInfo.InvariantCulture),
                Currency = currency
            };
        }

        private string BuildUrl(string webhookUrl, string invoiceNumber)
        {
            // TODO: Refactor this method so that we can use it for all PSPs.
            var webhookUrlBuilder = new UriBuilder(webhookUrl);
            var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
            queryString["invoice_number"] = invoiceNumber;

            webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

            return webhookUrlBuilder.ToString();
        }

        private async Task<decimal> CalculatePriceAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            return totalPrice;
        }
    }
}