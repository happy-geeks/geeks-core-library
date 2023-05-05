using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers.Mollie;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mollie.Api.Client;
using Mollie.Api.Models.Order;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly IOrderRequestBuilder requestBuilder;

        public MollieService(
            ILogger<MollieService> logger,
            IObjectsService objectsService,
            IShoppingBasketsService shoppingBasketsService,
            IDatabaseConnection databaseConnection,
            IDatabaseHelpersService databaseHelpersService,
            IOrderRequestBuilder requestBuilder, 
            IHttpContextAccessor httpContextAccessor = null) 
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.requestBuilder = requestBuilder;
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
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
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
            var orderRequest = await requestBuilder.CreateOrderRequestAsync(
                invoiceNumber, 
                shoppingBaskets, 
                userDetails, 
                mollieSettings, 
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
    }
}
