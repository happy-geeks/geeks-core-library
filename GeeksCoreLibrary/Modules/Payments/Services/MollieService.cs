using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Payments.Models.Mollie;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class MollieService : IPaymentServiceProviderService, IScopedService
    {
        private const string ApiBaseUrl = "https://api.mollie.com/v2";

        /// <inheritdoc />
        public bool LogPaymentActions { get; set; }

        private readonly ILogger<MollieService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;

        public MollieService(ILogger<MollieService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethod, string invoiceNumber)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethod.PaymentServiceProvider.FailUrl
                };
            }

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            // Retrieve the API key. A development-specific one can be set for testing on development environments.
            var apiKey = await GetApiKeyAsync();
            var locale = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_locale");

            var description = $"Order #{invoiceNumber}";

            // Build and execute payment request.
            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiKey, "Bearer")
            };
            var restRequest = new RestRequest("/payments", Method.POST);
            restRequest.AddParameter("amount[currency]", await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_currency", "EUR"), ParameterType.GetOrPost);
            restRequest.AddParameter("amount[value]", totalPrice.ToString("F2", CultureInfo.InvariantCulture), ParameterType.GetOrPost);
            restRequest.AddParameter("description", description, ParameterType.GetOrPost);
            restRequest.AddParameter("redirectUrl", BuildRedirectUrl(invoiceNumber), ParameterType.GetOrPost);
            restRequest.AddParameter("webhookUrl", await BuildWebhookUrlAsync(paymentMethod.PaymentServiceProvider.WebhookUrl, invoiceNumber), ParameterType.GetOrPost);

            if (!String.IsNullOrWhiteSpace(locale))
            {
                restRequest.AddParameter("locale", locale, ParameterType.GetOrPost);
            }

            if (!String.IsNullOrWhiteSpace(paymentMethod?.ExternalName))
            {
                restRequest.AddParameter("method", paymentMethod.ExternalName, ParameterType.GetOrPost);
            }

            if (String.Equals(paymentMethod?.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
            {
                var issuerValue = shoppingBaskets.First().Main.GetDetailValue("issuer");
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
                    ActionData = paymentMethod.PaymentServiceProvider.FailUrl,
                    ErrorMessage = GetErrorMessageInResponse(JObject.Parse(restResponse.Content))
                };
            }

            var responseJson = JObject.Parse(restResponse.Content);

            var paymentId = responseJson["id"]?.ToString();
            var status = responseJson["status"]?.ToString();
            foreach (var (main, lines) in shoppingBaskets)
            {
                var history = new StringBuilder(main.GetDetailValue("psptransactionstatushistory"));
                if (history.Length > 0)
                {
                    history.Append(", ");
                }
                history.Append(status);

                main.SetDetail("psptransactionid", paymentId);
                main.SetDetail("psptransactionstatus", status);
                main.SetDetail("psptransactionstatushistory", history.ToString());

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

        private string BuildRedirectUrl(string invoiceNumber)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return String.Empty;
            }

            var request = httpContextAccessor.HttpContext.Request;

            var redirectUrl = new UriBuilder
            {
                Host = request.Host.Host,
                Scheme = request.Scheme,
                Path = "payment_return.gcl"
            };

            if (request.Host.Port.HasValue && !request.Host.Port.Value.InList(80, 443))
            {
                redirectUrl.Port = request.Host.Port.Value;
            }

            var queryString = new NameValueCollection
            {
                ["gcl_psp"] = "mollie",
                ["invoice_number"] = invoiceNumber
            };
            redirectUrl.Query = queryString.ToQueryString() ?? String.Empty;

            return redirectUrl.ToString();
        }

        private async Task<string> BuildWebhookUrlAsync(string webhookUrl, string invoiceNumber)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return String.Empty;
            }
            
            // TODO: Refactor this method so that we can use it for all PSPs.
            var webhookUrlBuilder =  new UriBuilder(webhookUrl);
            var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
            queryString["gcl_psp"] = "mollie";
            queryString["invoice_number"] = invoiceNumber;

            webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

            return webhookUrlBuilder.ToString();
        }

        /// <inheritdoc />
        public async Task<StatusUpdateResult> ProcessStatusUpdateAsync()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "Error retrieving status: No HttpContext available."
                };
            }

            // Mollie sends one POST parameter called "id".
            var mollieOrderId = httpContextAccessor.HttpContext.Request.Form["id"];

            // Retrieve the API key. A development-specific one can be set for testing on development environments.
            var apiKey = await GetApiKeyAsync();

            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiKey, "Bearer")
            };
            var restRequest = new RestRequest($"/payments/{mollieOrderId}", Method.GET);

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
                await LogPaymentActionAsync(String.Empty, (int)restResponse.StatusCode, responseBody: restResponse.Content);

                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "error"
                };
            }

            // The invoice number is sent as the metadata, which can be retrieved here.
            var invoiceNumber = responseJson["metadata"]?.ToString();

            await LogPaymentActionAsync(invoiceNumber, (int)restResponse.StatusCode, responseBody: restResponse.Content);

            return new StatusUpdateResult
            {
                Successful = status.Equals("paid", StringComparison.OrdinalIgnoreCase),
                Status = status
            };
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync()
        {
            var apiKey = await GetApiKeyAsync();
            var invoiceNumber = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "invoice_number");

            var baskets = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(invoiceNumber);
            if (baskets == null || baskets.Count == 0)
            {
                await LogPaymentActionAsync(invoiceNumber, 0, error: $"Unknown invoice number: {invoiceNumber}");

                // Unknown invoice number.
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL")
                };
            }

            // The Mollie payment ID is saved in all baskets, so just use the one from the first basket.
            var molliePaymentId = baskets.First().ShoppingBasket.GetDetailValue("psptransactionid");

            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiKey, "Bearer")
            };
            var restRequest = new RestRequest($"/payments/{molliePaymentId}", Method.GET);

            var restResponse = await restClient.ExecuteAsync(restRequest);

            await LogPaymentActionAsync(invoiceNumber, (int)restResponse.StatusCode, responseBody: restResponse.Content);

            if (restResponse.StatusCode != HttpStatusCode.OK)
            {
                // Payment request failed.
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL")
                };
            }

            // Payment status request succeeded.
            var responseJson = JObject.Parse(restResponse.Content);
            var status = responseJson["status"]?.ToString() ?? String.Empty;

            var successUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL");
            var pendingUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_pendingURL");
            if (String.IsNullOrWhiteSpace(pendingUrl))
            {
                pendingUrl = successUrl;
            }

            var redirectUrl = status switch
            {
                "paid" => successUrl,
                "pending" => pendingUrl,
                _ => await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL")
            };

            return new PaymentReturnResult
            {
                Action = PaymentResultActions.Redirect,
                ActionData = redirectUrl
            };
        }

        /// <summary>
        /// Returns the live API key for acceptance and live environments, and the dev API key for development and test environments.
        /// If no dev API key is set, the live API key will always be returned.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetApiKeyAsync()
        {
            string result = null;

            if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
            {
                result = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_dev");
            }

            if (String.IsNullOrWhiteSpace(result))
            {
                result = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_live");
            }

            return result;
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

        public async Task<bool> LogPaymentActionAsync(string invoiceNumber, int status, string requestBody = "", string responseBody = "", string error = "")
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

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.Mollie, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString(), requestBody, responseBody, error);
        }
    }
}
