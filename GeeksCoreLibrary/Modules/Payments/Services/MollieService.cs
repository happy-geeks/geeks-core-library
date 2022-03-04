using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
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
            string apiKey;
            if (gclSettings.Environment == Environments.Development)
            {
                apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_dev");
            }
            else
            {
                apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_live");
            }

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
            restRequest.AddParameter("redirectUrl", BuildRedirectUrl(), ParameterType.GetOrPost);
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

            var restResponse = await restClient.ExecuteAsync(restRequest);

            if (restResponse.StatusCode != HttpStatusCode.Created)
            {
                // Payment request failed.
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL"),
                    ErrorMessage = "" // TODO: See if Mollie returns an error of some kind.
                };
            }

            var responseJson = JObject.Parse(restResponse.Content);

            return new PaymentRequestResult()
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = responseJson["_links"]?["checkout"]?.ToString()
            };
        }

        private string GetPaymentMethodName(PaymentMethods paymentMethod)
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

        private string GetIssuerName(string issuerValue)
        {
            // TODO
            return String.Empty;
        }

        private string BuildRedirectUrl()
        {
            var redirectUrl = new UriBuilder
            {
                Host = httpContextAccessor.HttpContext.Request.Host.Host,
                Scheme = httpContextAccessor.HttpContext.Request.Scheme,
                Path = "payment_return.gcl"
            };
            var queryString = new NameValueCollection
            {
                ["gcl_psp"] = "mollie"
            };
            redirectUrl.Query = queryString.ToString() ?? String.Empty;

            return redirectUrl.ToString();
        }

        private async Task<string> BuildWebhookUrlAsync()
        {
            var webhookUrl = new UriBuilder(await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl"));
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
            string apiKey;
            if (gclSettings.Environment == Environments.Development)
            {
                apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_dev");
            }
            else
            {
                apiKey = await objectsService.FindSystemObjectByDomainNameAsync("MOLLIE_apikey_live");
            }

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
                Successful = status == "paid",
                Status = status
            };
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync()
        {
            throw new NotImplementedException();
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
