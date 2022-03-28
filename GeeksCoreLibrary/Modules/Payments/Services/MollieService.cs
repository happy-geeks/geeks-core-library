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
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed")
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

            // Build and execute payment request.
            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiKey, "Bearer")
            };
            var restRequest = new RestRequest("/payments", Method.POST);
            restRequest.AddParameter("amount[currency]", await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_currency", "EUR"), ParameterType.GetOrPost);
            restRequest.AddParameter("amount[value]", totalPrice.ToString("F2", CultureInfo.InvariantCulture), ParameterType.GetOrPost);
            restRequest.AddParameter("description", await objectsService.FindSystemObjectByDomainNameAsync("PSP_description"), ParameterType.GetOrPost);
            restRequest.AddParameter("redirectUrl", BuildRedirectUrl(shoppingBaskets.First().Main.Id), ParameterType.GetOrPost);
            restRequest.AddParameter("webhookUrl", await BuildWebhookUrlAsync(), ParameterType.GetOrPost);

            if (!String.IsNullOrWhiteSpace(locale))
            {
                restRequest.AddParameter("locale", locale, ParameterType.GetOrPost);
            }

            if (paymentMethod != PaymentMethods.Unknown)
            {
                restRequest.AddParameter("method", GetPaymentMethodName(paymentMethod), ParameterType.GetOrPost);
            }

            if (paymentMethod == PaymentMethods.Ideal)
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

            if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
            {
                restRequest.AddParameter("testmode", "true", ParameterType.GetOrPost);
            }

            var restResponse = await restClient.ExecuteAsync(restRequest);

            if (restResponse.StatusCode != HttpStatusCode.Created)
            {
                // Payment request failed.
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL"),
                    ErrorMessage = GetErrorMessageInResponse(JObject.Parse(restResponse.Content))
                };
            }

            var responseJson = JObject.Parse(restResponse.Content);

            return new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = responseJson["_links"]?["checkout"]?.ToString()
            };
        }

        private static string GetPaymentMethodName(PaymentMethods paymentMethod)
        {
            var result = paymentMethod switch
            {
                PaymentMethods.WireTransfer => "banktransfer",
                PaymentMethods.Mastercard => "creditcard",
                PaymentMethods.Visa => "creditcard",
                PaymentMethods.SofortBanking => "sofort",
                _ => paymentMethod.ToString("G").ToLowerInvariant()
            };
            return result;
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

        private string BuildRedirectUrl(ulong orderId)
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return String.Empty;
            }

            var redirectUrl = new UriBuilder
            {
                Host = httpContextAccessor.HttpContext.Request.Host.Host,
                Scheme = httpContextAccessor.HttpContext.Request.Scheme,
                Path = "payment_return.gcl"
            };
            var queryString = new NameValueCollection
            {
                ["gcl_psp"] = "mollie",
                ["order_id"] = orderId.ToString()
            };
            redirectUrl.Query = queryString.ToString() ?? String.Empty;

            return redirectUrl.ToString();
        }

        private async Task<string> BuildWebhookUrlAsync()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return String.Empty;
            }

            var webhookUrlObjectValue = await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl");

            UriBuilder webhookUrl;
            if (String.IsNullOrWhiteSpace(webhookUrlObjectValue))
            {
                webhookUrl = new UriBuilder
                {
                    Host = httpContextAccessor.HttpContext.Request.Host.Host,
                    Scheme = httpContextAccessor.HttpContext.Request.Scheme,
                    Path = "payment_in.gcl"
                };
            }
            else
            {
                webhookUrl = new UriBuilder(webhookUrlObjectValue);
            }

            var queryString = HttpUtility.ParseQueryString(webhookUrl.Query);
            queryString["gcl_psp"] = "mollie";

            webhookUrl.Query = queryString.ToString() ?? String.Empty;

            return webhookUrl.ToString();
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

            if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
            {
                restRequest.AddParameter("testmode", "true", ParameterType.GetOrPost);
            }

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
                await LogPaymentActionAsync(String.Empty, (int)restResponse.StatusCode);

                return new StatusUpdateResult
                {
                    Successful = false,
                    Status = "error"
                };
            }

            // The invoice number is sent as the metadata, which can be retrieved here.
            var invoiceNumber = responseJson["metadata"]?.ToString();

            await LogPaymentActionAsync(invoiceNumber, (int)restResponse.StatusCode);

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
            var mollieOrderId = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "order_id");

            var restClient = new RestClient(ApiBaseUrl)
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(apiKey, "Bearer")
            };
            var restRequest = new RestRequest($"/payments/{mollieOrderId}", Method.GET);

            if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
            {
                restRequest.AddParameter("testmode", "true", ParameterType.GetOrPost);
            }

            var restResponse = await restClient.ExecuteAsync(restRequest);

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

            var redirectUrl = status switch
            {
                "paid" => await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL"),
                "pending" => await objectsService.FindSystemObjectByDomainNameAsync("PSP_pendingURL"),
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

        public async Task<bool> LogPaymentActionAsync(string invoiceNumber, int status)
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

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.Mollie, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString());
        }
    }
}
