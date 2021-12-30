using BuckarooSdk.DataTypes;
using BuckarooSdk.DataTypes.RequestBases;
using BuckarooSdk.Services.CreditCards.BanContact.Request;
using BuckarooSdk.Services.CreditCards.Request;
using BuckarooSdk.Services.Ideal.TransactionRequest;
using BuckarooSdk.Services.PayPal;
using BuckarooSdk.Transaction;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    public class BuckarooService : IPaymentServiceProviderService, IScopedService
    {
        public bool LogPaymentActions { get; set; }

        private readonly ILogger<BuckarooService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;

        public BuckarooService(ILogger<BuckarooService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber)
        {
            // https://github.com/buckaroo-it/BuckarooSdk_DotNet/blob/master/BuckarooSdk.Tests/Services

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            // Retrieve the website key and API key.
            var websiteKey = await objectsService.FindSystemObjectByDomainNameAsync("BUCK_merchantid");
            var apiKey = await objectsService.FindSystemObjectByDomainNameAsync("BUCK_secret");

            // Check if the test environment should be used.
            var useTestEnvironment = gclSettings.Environment.InList(Environments.Test, Environments.Development);

            var buckarooClient = new BuckarooSdk.SdkClient();

            var transaction = buckarooClient.CreateRequest()
                .Authenticate(websiteKey, apiKey, !useTestEnvironment, CultureInfo.CurrentCulture)
                .TransactionRequest()
                .SetBasicFields(new TransactionBase
                {
                    Currency = "EUR",
                    AmountDebit = totalPrice,
                    Invoice = invoiceNumber,
                    PushUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl"),
                    Description = await objectsService.FindSystemObjectByDomainNameAsync("PSP_description"),
                    ReturnUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL"),
                    ReturnUrlCancel = await objectsService.FindSystemObjectByDomainNameAsync("PSP_cancelURL"),
                    ReturnUrlError = await objectsService.FindSystemObjectByDomainNameAsync("PSP_errorURL"),
                    ReturnUrlReject = await objectsService.FindSystemObjectByDomainNameAsync("PSP_rejectURL"),
                    ContinueOnIncomplete = CheckIfContinueOnIncompleteIsAllowed(shoppingBaskets, paymentMethod) ? ContinueOnIncomplete.RedirectToHTML : ContinueOnIncomplete.No
                });

            ConfiguredServiceTransaction serviceTransaction;
            switch (paymentMethod)
            {
                case PaymentMethods.Ideal:
                    serviceTransaction = InitializeIdealPayment(transaction, shoppingBaskets);
                    break;
                case PaymentMethods.Mastercard:
                    serviceTransaction = InitializeMasterCardPayment(transaction);
                    break;
                case PaymentMethods.Visa:
                    serviceTransaction = InitializeVisaPayment(transaction);
                    break;
                case PaymentMethods.PayPal:
                    serviceTransaction = InitializePayPalPayment(transaction);
                    break;
                case PaymentMethods.Bancontact:
                    serviceTransaction = InitializeBancontactPayment(transaction);
                    break;
                default:
                    return new PaymentRequestResult
                    {
                        Action = PaymentRequestActions.Redirect,
                        ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                        Successful = false
                    };
            }

            var response = await serviceTransaction.ExecuteAsync();

            return new PaymentRequestResult
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = response.RequiredAction.RedirectURL
            };
        }

        private ConfiguredServiceTransaction InitializeIdealPayment(ConfiguredTransaction transaction, IEnumerable<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            // Bank name.
            var issuerValue = shoppingBaskets.First().Main.GetDetailValue("issuer");
            var issuerName = GetIssuerName(issuerValue);

            return transaction.Ideal()
                .Pay(new IdealPayRequest
                {
                    Issuer = String.IsNullOrWhiteSpace(issuerName) ? null : issuerName
                });
        }

        private ConfiguredServiceTransaction InitializePayPalPayment(ConfiguredTransaction transaction)
        {
            return transaction.PayPal()
                .Pay(new PayPalPayRequest());
        }

        private ConfiguredServiceTransaction InitializeMasterCardPayment(ConfiguredTransaction transaction)
        {
            return transaction.MasterCard().Pay(new CreditCardPayRequest());
        }

        private ConfiguredServiceTransaction InitializeVisaPayment(ConfiguredTransaction transaction)
        {
            return transaction.Visa().Pay(new CreditCardPayRequest());
        }

        private ConfiguredServiceTransaction InitializeBancontactPayment(ConfiguredTransaction transaction)
        {
            return transaction.Bancontact().Pay(new BancontactPayRequest());
        }

        private bool CheckIfContinueOnIncompleteIsAllowed(IEnumerable<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, PaymentMethods paymentMethod)
        {
            if (paymentMethod != PaymentMethods.Ideal)
            {
                return false;
            }

            var issuerValue = shoppingBaskets.First().Main.GetDetailValue("issuer");
            var issuerName = GetIssuerName(issuerValue);
            return String.IsNullOrWhiteSpace(issuerName);
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

            var invoiceNumber = httpContextAccessor.HttpContext.Request.Query["brq_invoicenumber"].ToString();
            if (String.IsNullOrWhiteSpace(invoiceNumber))
            {
                return new StatusUpdateResult
                {
                    Status = "No invoice number in request found; unable to process status update.",
                    Successful = false
                };
            }

            var buckarooClient = new BuckarooSdk.SdkClient();

            // Retrieve the website key and API key.
            var websiteKey = await objectsService.FindSystemObjectByDomainNameAsync("BUCK_merchantid");
            var apiKey = await objectsService.FindSystemObjectByDomainNameAsync("BUCK_secret");

            // Read the entire body, which should be a JSON body from Buckaroo.
            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();
            var bodyAsBytes = Encoding.UTF8.GetBytes(bodyJson);

            // Create nonce.
            var timeSpan = DateTime.UtcNow - DateTime.UnixEpoch;
            var requestTimeStamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();
            var pushUrl = await objectsService.FindSystemObjectByDomainNameAsync("PSP_notifyurl");

            var pushSignature = buckarooClient.GetSignatureCalculationService().CalculateSignature(bodyAsBytes, HttpMethods.Post, requestTimeStamp, Guid.NewGuid().ToString("N"), pushUrl, websiteKey, apiKey);
            var authHeader = $"hmac {pushSignature}";

            BuckarooSdk.DataTypes.Push.Push push;

            try
            {
                push = buckarooClient.GetPushHandler(apiKey).DeserializePush(bodyAsBytes, pushUrl, authHeader);
            }
            catch (System.Security.Authentication.AuthenticationException)
            {
                await LogPaymentAction(invoiceNumber, 0, bodyJson);

                return new StatusUpdateResult
                {
                    Status = "Signature was incorrect.",
                    Successful = false
                };
            }

            var successful = push.Status.Code.Code == BuckarooSdk.Constants.Status.Success;
            var statusMessage = push.Status.Code.Description;

            await LogPaymentAction(invoiceNumber, push.Status.Code.Code, bodyJson);

            return new StatusUpdateResult
            {
                Status = statusMessage,
                Successful = successful
            };
        }

        public async Task<bool> LogPaymentAction(string invoiceNumber, int status, string bodyJson)
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

            return await LoggingHelpers.AddLogEntryAsync(databaseConnection, PaymentServiceProviders.Buckaroo, invoiceNumber, status, headers.ToString(), queryString.ToString(), formValues.ToString(), bodyJson);
        }

        #region Helper functions

        private string GetIssuerName(string issuerValue)
        {
            var buckarooIssuerConstants = typeof(BuckarooSdk.Services.Ideal.Constants.Issuers).GetFields(BindingFlags.Public | BindingFlags.Static);
            var issuerConstant = buckarooIssuerConstants.FirstOrDefault(mi => mi.Name.Equals(issuerValue, StringComparison.OrdinalIgnoreCase));

            if (issuerConstant != null)
            {
                return (string)issuerConstant.GetValue(null);
            }

            // Check for legacy types (which were numbers).
            return LegacyMappingsHelper.GetBuckarooIssuer(issuerValue);
        }

        #endregion
    }
}
