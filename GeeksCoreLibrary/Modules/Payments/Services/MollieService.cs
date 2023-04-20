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
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
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
using Microsoft.Extensions.Options;
using Mollie.Api.Client;
using Mollie.Api.Models;
using Newtonsoft.Json.Linq;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class MollieService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
    {
        private readonly ILogger<MollieService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        
        public MollieService(ILogger<MollieService> logger,
            IObjectsService objectsService,
            IShoppingBasketsService shoppingBasketsService,
            IDatabaseConnection databaseConnection,
            IDatabaseHelpersService databaseHelpersService, 
            IOptions<GclSettings> gclSettings, 
            IHttpContextAccessor httpContextAccessor = null) 
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
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

            var totalPrice = await CalculatePriceAsync(shoppingBaskets);

            // Retrieve the API key. A development-specific one can be set for testing on development environments.
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            mollieSettings.Locale = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_locale");

            var mollieClient = new OrderClient(mollieSettings.ApiKey);
            var orderRequest = await CreateOrderRequestAsync(totalPrice, mollieSettings, invoiceNumber, paymentMethodSettings, shoppingBaskets, userDetails);

            OrderResponse orderResponse;
            try
            {
                orderResponse = await mollieClient.CreateOrderAsync(orderRequest);
            }
            catch (MollieApiException ex)
            {
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
                var history = new StringBuilder(main.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentHistoryProperty));
                if (history.Length > 0)
                {
                    history.Append(", ");
                }
                history.Append(status);

                main.SetDetail(Components.OrderProcess.Models.Constants.PaymentProviderTransactionId, paymentId);
                main.SetDetail(Components.OrderProcess.Models.Constants.PaymentProviderTransactionStatus, status);
                main.SetDetail(Components.OrderProcess.Models.Constants.PaymentHistoryProperty, history.ToString());

                await shoppingBasketsService.SaveAsync(main, lines, await shoppingBasketsService.GetSettingsAsync());
            }
        }

        private static string GetIssuerName(string issuerValue)
        {
            var issuerConstants = typeof(IdealIssuers).GetFields(BindingFlags.Public | BindingFlags.Static);
            var issuerConstant = issuerConstants.FirstOrDefault(mi => mi.Name.Equals(issuerValue, StringComparison.OrdinalIgnoreCase) || mi.Name.Equals($"ideal_{issuerValue}", StringComparison.OrdinalIgnoreCase));

            if (issuerConstant != null)
            {
                return (string)issuerConstant.GetValue(null);
            }

            return null;
        }

        private string BuildUrl(string webhookUrl, string invoiceNumber)
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return String.Empty;
            }
            
            // TODO: Refactor this method so that we can use it for all PSPs.
            var webhookUrlBuilder =  new UriBuilder(webhookUrl);
            var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
            queryString["invoice_number"] = invoiceNumber;

            webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

            return webhookUrlBuilder.ToString();
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
            
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
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
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
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
            var mollieOrderId = baskets.First().Order.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentProviderTransactionId);

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

        private async Task<OrderRequest> CreateOrderRequestAsync(decimal totalPrice,
            MollieSettingsModel mollieSettings,
            string invoiceNumber,
            PaymentMethodSettingsModel paymentMethodSettings,
            ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
            WiserItemModel userDetails)
        {
            var orderRequest = new OrderRequest
            {
                Amount = new Amount
                {
                    Value = totalPrice.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                OrderNumber = invoiceNumber,
                RedirectUrl = BuildUrl(mollieSettings.ReturnUrl, invoiceNumber),
                WebhookUrl = BuildUrl(mollieSettings.WebhookUrl, invoiceNumber),
                Locale =  mollieSettings.Locale,
                Method = paymentMethodSettings.ExternalName,
                Lines = await ConvertShoppingBasketsToOrderLinesAsync(shoppingBaskets, mollieSettings),
                BillingAddress = CreateBillingAddress(userDetails),
                ShippingAddress = CreateAddress(userDetails, "shipping_"),
                Testmode = gclSettings.Environment.InList(Environments.Test, Environments.Development)
            };

            if (String.Equals(paymentMethodSettings?.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
            {
                var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentMethodIssuerProperty);
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
        
        private OrderAddressDetails CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
        {
            //If a prefix is given but any of the required values doesn't contain a value return null.
            if (!String.IsNullOrWhiteSpace(detailKeyPrefix) && 
                (String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}street")))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}city"))
                 || String.IsNullOrWhiteSpace(userDetails.GetDetailValue($"{detailKeyPrefix}country")))
            {
                return null;
            }

            return new OrderAddressDetails()
            {
                StreetAndNumber =
                    $"{userDetails.GetDetailValue($"{detailKeyPrefix}street")} {userDetails.GetDetailValue($"{detailKeyPrefix}housenumber")}{userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix")}",
                PostalCode = userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"),
                City = userDetails.GetDetailValue($"{detailKeyPrefix}_city"),
                Country = userDetails.GetDetailValue($"{detailKeyPrefix}_country"),
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
            return address;
        }

        private async Task<IEnumerable<OrderLineRequest>> ConvertShoppingBasketsToOrderLinesAsync(
            ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
            MollieSettingsModel mollieSettings)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var orderRequests = new List<OrderLineRequest>();
            foreach (var basket in shoppingBaskets)
            {
                foreach (var basketLine in basket.Lines)
                {
                    OrderLineRequest orderLineRequest =
                        await ConvertOrderLineFromBasketLineAsync(
                            basket.Main, 
                            basketLine,
                            basketSettings,
                            mollieSettings);
                    orderRequests.Add(orderLineRequest);
                }
            }

            return orderRequests;
        }

        private async Task<OrderLineRequest> ConvertOrderLineFromBasketLineAsync(
            WiserItemModel basket,
            WiserItemModel basketLine,
            ShoppingBasketCmsSettingsModel basketSettings,
            MollieSettingsModel mollieSettings)
        {
            var linePrice = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.PspPriceInVat
            );
            var linePriceVatOnly = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.VatOnly
            );
            var lineProductQuantity = Convert.ToInt32(basketLine.GetDetailValue(basketSettings.QuantityPropertyName));

            return new OrderLineRequest()
            {
                Name = basketLine.GetDetailValue("title"),
                UnitPrice = new Amount()
                {
                    Value = (linePrice / lineProductQuantity).ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                TotalAmount = new Amount()
                {
                    Value = linePrice.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                DiscountAmount = new Amount()
                {
                    Value = basketLine.GetDetailValue(basketSettings.DiscountPropertyName),
                    Currency = mollieSettings.Currency
                },
                VatAmount = new Amount()
                {
                    Value = linePriceVatOnly.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                Quantity = lineProductQuantity,
                VatRate = basketLine.GetDetailValue(basketSettings.VatRatePropertyName),
            };
        }
    }
}
