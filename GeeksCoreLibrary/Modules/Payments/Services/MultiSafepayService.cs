using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using MultiSafepay;
using MultiSafepay.Model;
using ArgumentException = System.ArgumentException;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class MultiSafepayService : IPaymentServiceProviderService, IScopedService
    {
        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        private readonly ILogger<MultiSafepayService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;

        private MultiSafepayClient client;

        public MultiSafepayService(ILogger<MultiSafepayService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
        }

        /// <summary>
        /// Create the client based on the environment.
        /// </summary>
        private void SetupEnvironment(string apiKey)
        {
            client = new MultiSafepayClient(apiKey, gclSettings.Environment.InList(Environments.Live, Environments.Acceptance) ? "https://api.multisafepay.com/v1/json/" : "https://testapi.multisafepay.com/v1/json/");
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var multiSafepaySettings = (MultiSafepaySettingsModel)paymentMethodSettings.PaymentServiceProvider;

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            var totalPriceInCents = (int) Math.Round(totalPrice * 100);

            var order = new Order
            {
                Type = OrderType.Redirect,
                OrderId = invoiceNumber,
                GatewayId = paymentMethodSettings.ExternalName,
                AmountInCents = totalPriceInCents,
                CurrencyCode = multiSafepaySettings.Currency,
                PaymentOptions = new PaymentOptions(multiSafepaySettings.WebhookUrl,
                    multiSafepaySettings.SuccessUrl,
                    multiSafepaySettings.FailUrl)
            };

            SetupEnvironment(multiSafepaySettings.ApiKey);

            try
            {
                var response = client.CustomOrder(order);

                return new PaymentRequestResult
                {
                    Successful = true,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = response.PaymentUrl
                };
            }
            catch (MultiSafepayException exception)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl,
                    Successful = false,
                    ErrorMessage = exception.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Status = "Request not available; unable to process status update.",
                    Successful = false
                };
            }

            // Retrieve the order with the given transaction id/order id to check the status.
            var orderId = httpContextAccessor.HttpContext.Request.Query["transactionid"].ToString();
            var multiSafepaySettings = (MultiSafepaySettingsModel)paymentMethodSettings.PaymentServiceProvider;
            OrderResponse response;

            SetupEnvironment(multiSafepaySettings.ApiKey);

            try
            {
                response = client.GetOrder(orderId);
            }
            catch (MultiSafepayException)
            {
                await LogPaymentAction(orderId, 0);

                return new StatusUpdateResult
                {
                    Status = "Unable to retrieve order information.",
                    Successful = false
                };
            }

            var success = response.Status.ToLower() == "completed";

            await LogPaymentAction(orderId, success ? 1 : 0);

            return new StatusUpdateResult
            {
                Status = response.Status,
                Successful = success
            };
        }

        private async Task<bool> LogPaymentAction(string invoiceNumber, int status)
        {
            if (!LogPaymentActions || httpContextAccessor?.HttpContext == null)
            {
                return false;
            }

            var headers = new StringBuilder();
            var queryString = new StringBuilder();
            var formValues = new StringBuilder();

            foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Headers)
            {
                headers.AppendLine($"{key}: {value}");
            }

            foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Query)
            {
                queryString.AppendLine($"{key}: {value}");
            }

            if (httpContextAccessor.HttpContext.Request.HasFormContentType)
            {
                foreach (var (key, value) in httpContextAccessor.HttpContext.Request.Form)
                {
                    formValues.AppendLine($"{key}: {value}");
                }
            }

            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.MultiSafepay, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString(), bodyJson);
        }
    }
}
