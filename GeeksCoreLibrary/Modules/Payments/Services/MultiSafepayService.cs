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
        /// <returns></returns>
        private async Task SetupEnvironment()
        {
            if (gclSettings.Environment.InList(Environments.Live, Environments.Acceptance))
            {
                var apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MSP_apiKey");
                client = new MultiSafepayClient(apiKey, "https://api.multisafepay.com/v1/json/");
            }
            else
            {
                var apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MSP_apiKey_test");
                client = new MultiSafepayClient(apiKey, "https://testapi.multisafepay.com/v1/json/");
            }
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            var totalPriceInCents = (int) Math.Round(totalPrice * 100);
            string paymentMethodId;

            try
            {
                paymentMethodId = ConvertPaymentMethodToId(paymentMethod);
            }
            //Converting payment method throws an argument exception if the method is not supported.
            catch (ArgumentException)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    Successful = false,
                    ErrorMessage = $"Unknown or unsupported payment method '{paymentMethod:G}'"
                };
            }

            var order = new Order
            {
                Type = OrderType.Redirect,
                OrderId = invoiceNumber,
                GatewayId = paymentMethodId,
                AmountInCents = totalPriceInCents,
                CurrencyCode = "EUR",
                Description = await objectsService.FindSystemObjectByDomainNameAsync("PSP_description"),
                PaymentOptions = new PaymentOptions(await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl"),
                                                    await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL"),
                                                    await objectsService.FindSystemObjectByDomainNameAsync("PSP_cancelURL"))
            };

            await SetupEnvironment();

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
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    Successful = false,
                    ErrorMessage = exception.Message
                };
            }
        }

        /// <summary>
        /// Convert <see cref="PaymentMethods"/> to a string id that is supported.
        /// Throws <see cref="ArgumentException"/> when the provided <see cref="PaymentMethods"/> is not supported by MultiSafepay.
        /// </summary>
        /// <param name="paymentMethod">The <see cref="PaymentMethods"/> tp convert.</param>
        /// <returns>Returns the id of the corresponding method.</returns>
        private string ConvertPaymentMethodToId(PaymentMethods paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethods.Maestro:
                    return "MAESTRO";
                case PaymentMethods.WireTransfer:
                    return "BANKTRANS";
                case PaymentMethods.SofortBanking:
                    return "DIRECTBANK";
                case PaymentMethods.Giropay:
                    return "GIROPAY";
                case PaymentMethods.Bancontact:
                    return "MISTERCASH";
                case PaymentMethods.EPS:
                    return "EPS";
                case PaymentMethods.Ideal:
                    return "IDEAL";
                case PaymentMethods.Trustly:
                    return "TRUSTLY";
                case PaymentMethods.Mastercard:
                    return "MASTERCARD";
                case PaymentMethods.ApplePay:
                    return "APPLEPAY";
                case PaymentMethods.Visa:
                    return "VISA";
                default:
                    throw new ArgumentException("The provided payment method can't be converted to the corresponding id.");
            }
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Status = "Request not available; unable to process status update.",
                    Successful = false
                };
            }

            //Retrieve the order with the given transaction id/order id to check the status.
            var orderId = httpContextAccessor.HttpContext.Request.Query["transactionid"].ToString();
            OrderResponse response;

            await SetupEnvironment();

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

        public async Task<bool> LogPaymentAction(string invoiceNumber, int status)
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
