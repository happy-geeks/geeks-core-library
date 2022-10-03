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
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class BuckarooService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
    {
        private readonly ILogger<BuckarooService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IDatabaseConnection databaseConnection;

        public BuckarooService(ILogger<BuckarooService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IShoppingBasketsService shoppingBasketsService, IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService) 
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.shoppingBasketsService = shoppingBasketsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
        {
            // https://github.com/buckaroo-it/BuckarooSdk_DotNet/blob/master/BuckarooSdk.Tests/Services

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var buckarooSettings = (BuckarooSettingsModel)paymentMethodSettings.PaymentServiceProvider;

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            // Check if the test environment should be used.
            var useTestEnvironment = gclSettings.Environment.InList(Environments.Test, Environments.Development);

            var buckarooClient = new BuckarooSdk.SdkClient();

            var transaction = buckarooClient.CreateRequest()
                .Authenticate(buckarooSettings.WebsiteKey, buckarooSettings.SecretKey, !useTestEnvironment, CultureInfo.CurrentCulture)
                .TransactionRequest()
                .SetBasicFields(new TransactionBase
                {
                    Currency = buckarooSettings.Currency,
                    AmountDebit = totalPrice,
                    Invoice = invoiceNumber,
                    PushUrl = buckarooSettings.WebhookUrl,
                    ReturnUrl = buckarooSettings.SuccessUrl,
                    ReturnUrlCancel = buckarooSettings.FailUrl,
                    ReturnUrlError = buckarooSettings.FailUrl,
                    ReturnUrlReject = buckarooSettings.FailUrl,
                    ContinueOnIncomplete = CheckIfContinueOnIncompleteIsAllowed(shoppingBaskets, paymentMethodSettings.ExternalName) ? ContinueOnIncomplete.RedirectToHTML : ContinueOnIncomplete.No
                });

            ConfiguredServiceTransaction serviceTransaction;
            switch (paymentMethodSettings.ExternalName.ToUpperInvariant())
            {
                case "IDEAL":
                    serviceTransaction = InitializeIdealPayment(transaction, shoppingBaskets);
                    break;
                case "MASTERCARD":
                    serviceTransaction = InitializeMasterCardPayment(transaction);
                    break;
                case "VISA":
                    serviceTransaction = InitializeVisaPayment(transaction);
                    break;
                case "PAYPAL":
                    serviceTransaction = InitializePayPalPayment(transaction);
                    break;
                case "BANCONTACT":
                    serviceTransaction = InitializeBancontactPayment(transaction);
                    break;
                default:
                    return new PaymentRequestResult
                    {
                        Action = PaymentRequestActions.Redirect,
                        ActionData = buckarooSettings.FailUrl,
                        Successful = false,
                        ErrorMessage = $"Unknown or unsupported payment method '{paymentMethodSettings}'"
                    };
            }

            var response = await serviceTransaction.ExecuteAsync();

            var successStatusCodes = new List<int> {190, 790};
            if (response?.Status?.Code?.Code == null || !successStatusCodes.Contains(response.Status.Code.Code) || String.IsNullOrWhiteSpace(response.RequiredAction?.RedirectURL))
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = buckarooSettings.FailUrl,
                    ErrorMessage = response?.Status?.Code?.Description
                };
            }

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
            var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentMethodIssuerProperty);
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

        private bool CheckIfContinueOnIncompleteIsAllowed(IEnumerable<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, string paymentMethod)
        {
            if (!String.Equals(paymentMethod, "ideal", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var issuerValue = shoppingBaskets.First().Main.GetDetailValue("issuer");
            var issuerName = GetIssuerName(issuerValue);
            return String.IsNullOrWhiteSpace(issuerName);
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

            var invoiceNumber = httpContextAccessor.HttpContext.Request.Query["brq_invoicenumber"].ToString();
            if (String.IsNullOrWhiteSpace(invoiceNumber))
            {
                return new StatusUpdateResult
                {
                    Status = "No invoice number in request found; unable to process status update.",
                    Successful = false
                };
            }

            var buckarooSettings = (BuckarooSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            var buckarooClient = new BuckarooSdk.SdkClient();

            // Read the entire body, which should be a JSON body from Buckaroo.
            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();
            var bodyAsBytes = Encoding.UTF8.GetBytes(bodyJson);

            // Create nonce.
            var timeSpan = DateTime.UtcNow - DateTime.UnixEpoch;
            var requestTimeStamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();

            var pushSignature = buckarooClient.GetSignatureCalculationService().CalculateSignature(bodyAsBytes, HttpMethods.Post, requestTimeStamp, Guid.NewGuid().ToString("N"), buckarooSettings.WebhookUrl, buckarooSettings.WebsiteKey, buckarooSettings.SecretKey);
            var authHeader = $"hmac {pushSignature}";

            BuckarooSdk.DataTypes.Push.Push push;

            try
            {
                push = buckarooClient.GetPushHandler(buckarooSettings.SecretKey).DeserializePush(bodyAsBytes, buckarooSettings.WebhookUrl, authHeader);
            }
            catch (System.Security.Authentication.AuthenticationException)
            {
                await LogIncomingPaymentActionAsync(PaymentServiceProviders.Buckaroo, invoiceNumber, 0, bodyJson);

                return new StatusUpdateResult
                {
                    Status = "Signature was incorrect.",
                    Successful = false
                };
            }

            var successful = push.Status.Code.Code == BuckarooSdk.Constants.Status.Success;
            var statusMessage = push.Status.Code.Description;

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.Buckaroo, invoiceNumber, push.Status.Code.Code, bodyJson);

            return new StatusUpdateResult
            {
                Status = statusMessage,
                Successful = successful
            };
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
