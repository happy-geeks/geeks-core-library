using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
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
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators.OAuth2;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class MollieService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
    {
        private const string ApiBaseUrl = "https://api.mollie.com/v2";

        private readonly ILogger<MollieService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;

        public MollieService(ILogger<MollieService> logger, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService) 
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            // Retrieve the API key. A development-specific one can be set for testing on development environments.
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            mollieSettings.Locale = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_locale");

            var description = $"Order #{invoiceNumber}";

            // Build and execute payment request.
            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(mollieSettings.ApiKey, "Bearer")
            };
            var restRequest = new RestRequest("/payments", Method.Post);
            restRequest.AddParameter("amount[currency]", mollieSettings.Currency, ParameterType.GetOrPost);
            restRequest.AddParameter("amount[value]", totalPrice.ToString("F2", CultureInfo.InvariantCulture), ParameterType.GetOrPost);
            restRequest.AddParameter("description", description, ParameterType.GetOrPost);
            restRequest.AddParameter("redirectUrl", BuildUrl(mollieSettings.ReturnUrl, invoiceNumber), ParameterType.GetOrPost);
            restRequest.AddParameter("webhookUrl", BuildUrl(mollieSettings.WebhookUrl, invoiceNumber), ParameterType.GetOrPost);

            if (!String.IsNullOrWhiteSpace(mollieSettings.Locale))
            {
                restRequest.AddParameter("locale", mollieSettings.Locale, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(paymentMethodSettings?.ExternalName))
            {
                restRequest.AddParameter("method", paymentMethodSettings.ExternalName, ParameterType.GetOrPost);
            }

            if (String.Equals(paymentMethodSettings?.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
            {
                var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentMethodIssuerProperty);
                var issuerName = GetIssuerName(issuerValue);

                if (!String.IsNullOrWhiteSpace(issuerName))
                {
                    restRequest.AddParameter("issuer", issuerName, ParameterType.GetOrPost);
                }
            }

            // Metadata is always sent back.
            restRequest.AddParameter("metadata", invoiceNumber, ParameterType.GetOrPost);

            var restResponse = await restClient.ExecuteAsync(restRequest);

            if (restResponse.StatusCode != HttpStatusCode.Created)
            {
                // Payment request failed.
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = mollieSettings.FailUrl,
                    ErrorMessage = GetErrorMessageInResponse(JObject.Parse(restResponse.Content))
                };
            }

            var responseJson = JObject.Parse(restResponse.Content);

            var paymentId = responseJson["id"]?.ToString();
            var status = responseJson["status"]?.ToString();
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

            return new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = responseJson["_links"]?["checkout"]?["href"]?.ToString()
            };
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
            if (httpContextAccessor.HttpContext == null)
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
            if (httpContextAccessor.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "Error retrieving status: No HttpContext available."
                };
            }

            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;

            // Mollie sends one POST parameter called "id".
            var mollieOrderId = httpContextAccessor.HttpContext.Request.Form["id"];

            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(mollieSettings.ApiKey, "Bearer")
            };
            var restRequest = new RestRequest($"/payments/{mollieOrderId}", Method.Get);

            // Execute the request. The result will be a JSON object.
            // For more info: https://docs.mollie.com/reference/v2/payments-api/get-payment
            var restResponse = await restClient.ExecuteAsync(restRequest);
            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "error"
                };
            }

            var responseJson = JObject.Parse(restResponse.Content);
            var status = responseJson["status"]?.ToString();

            if (String.IsNullOrWhiteSpace(status))
            {
                await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, String.Empty, (int)restResponse.StatusCode, responseBody: restResponse.Content);

                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "error"
                };
            }

            // The invoice number is sent as the metadata, which can be retrieved here.
            var invoiceNumber = responseJson["metadata"]?.ToString();

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, invoiceNumber, (int)restResponse.StatusCode, responseBody: restResponse.Content);

            return new StatusUpdateResult
            {
                Successful = status.Equals("paid", StringComparison.OrdinalIgnoreCase),
                Status = status
            };
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            var mollieSettings = (MollieSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            var invoiceNumber = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "invoice_number");

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
            var molliePaymentId = baskets.First().ShoppingBasket.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentProviderTransactionId);

            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(mollieSettings.ApiKey, "Bearer")
            };
            var restRequest = new RestRequest($"/payments/{molliePaymentId}", Method.Get);

            var restResponse = await restClient.ExecuteAsync(restRequest);

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Mollie, invoiceNumber, (int)restResponse.StatusCode, responseBody: restResponse.Content);

            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                // Payment request failed.
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            // Payment status request succeeded.
            var responseJson = JObject.Parse(restResponse.Content);
            var status = responseJson["status"]?.ToString() ?? String.Empty;

            var successUrl = paymentMethodSettings.PaymentServiceProvider.SuccessUrl;
            var pendingUrl = paymentMethodSettings.PaymentServiceProvider.PendingUrl;
            if (String.IsNullOrWhiteSpace(pendingUrl))
            {
                pendingUrl = successUrl;
            }

            var redirectUrl = status switch
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
