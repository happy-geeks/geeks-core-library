using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OmniKassa.Exceptions;
using OmniKassa.Model;
using OmniKassa.Model.Enums;
using OmniKassa.Model.Order;
using OmniKassa.Model.Response;
using OmniKassa.Model.Response.Notification;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    /// <inheritdoc cref="IPaymentServiceProviderService" />
    public class RaboOmniKassaService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, ITransientService
    {
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        private OmniKassa.Environment environment = OmniKassa.Environment.SANDBOX;
        private string refreshToken = "";
        private string signingKey = "";

        public RaboOmniKassaService(IShoppingBasketsService shoppingBasketsService, IObjectsService objectsService, IHttpContextAccessor httpContextAccessor, IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, IDatabaseHelpersService databaseHelpersService, ILogger<RaboOmniKassaService> logger) 
            : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
        {
            this.shoppingBasketsService = shoppingBasketsService;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <summary>
        /// Set the refresh token, signing key and environment based on the environment.
        /// </summary>
        /// <returns></returns>
        private void SetupEnvironment(RaboOmniKassaSettingsModel settings)
        {
            refreshToken = settings.RefreshToken;
            signingKey = settings.SigningKey;
            environment = gclSettings.Environment.InList(Environments.Acceptance, Environments.Live) ? OmniKassa.Environment.PRODUCTION : OmniKassa.Environment.SANDBOX;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var raboOmniKassaSettings = (RaboOmniKassaSettingsModel)paymentMethodSettings.PaymentServiceProvider;

            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
            }

            var orderBuilder = new MerchantOrder.Builder()
                .WithMerchantOrderId(invoiceNumber)
                .WithAmount(Money.FromDecimal(Currency.EUR, totalPrice))
                .WithMerchantReturnURL(paymentMethodSettings.PaymentServiceProvider.SuccessUrl)
                .WithOrderItems(CreateOrderItems(shoppingBaskets));

            try
            {
                var billingAddress = CreateAddress(userDetails);
                var shippingDetails = CreateAddress(userDetails, "shipping_") ?? billingAddress; //If no shipping address has been provided use billing address.
                var paymentBrand = ConvertPaymentMethodToPaymentBrand(paymentMethodSettings);

                orderBuilder.WithBillingDetail(billingAddress)
                    .WithShippingDetail(shippingDetails)
                    .WithPaymentBrand(paymentBrand);
            }
            //Converting the country code throws an argument exception if the code is not supported.
            //Converting payment method throws an argument exception if the method is not supported.
            catch (ArgumentException)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl,
                    Successful = false,
                    ErrorMessage = $"Unknown or unsupported payment method '{paymentMethodSettings:G}'"
                };
            }

            orderBuilder.WithPaymentBrandForce(PaymentBrandForce.FORCE_ALWAYS); //Don't allow customers to change payment method on the Rabo OmniKassa website.

            var merchantOrder = orderBuilder.Build();

            SetupEnvironment(raboOmniKassaSettings);

            var endpoint = OmniKassa.Endpoint.Create(environment, signingKey, refreshToken);

            MerchantOrderResponse response;

            try
            {
                response = await endpoint.Announce(merchantOrder);
            }
            catch (InvalidAccessTokenException)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl,
                    Successful = false,
                    ErrorMessage = "Failed to authenticate with Rabo omni kassa API"
                };
            }

            return new PaymentRequestResult()
            {
                Successful = true,
                Action = PaymentRequestActions.Redirect,
                ActionData = response.RedirectUrl
            };
        }

        /// <summary>
        /// Convert the order lines in the shopping baskets to a <see cref="OrderItem"/>.
        /// </summary>
        /// <param name="shoppingBaskets">The shopping baskets to convert.</param>
        /// <returns>A collection of <see cref="OrderItem"/>s.</returns>
        private List<OrderItem> CreateOrderItems(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            var orderItems = new List<OrderItem>();

            foreach (var (main, lines) in shoppingBaskets)
            {
                foreach (var line in lines)
                {
                    // Get the title of the product. If it is a coupon no title is provided and description will be used instead.
                    var name = line.GetDetailValue("title");
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        name = line.GetDetailValue("description");
                    }

                    var orderItem = new OrderItem.Builder()
                        .WithId(line.GetDetailValue(Components.ShoppingBasket.Models.Constants.ConnectedItemIdProperty))
                        .WithName(name)
                        .WithDescription(name)
                        .WithQuantity(line.GetDetailValue<int>("quantity"))
                        .WithAmount(Money.FromDecimal(Currency.EUR, line.GetDetailValue<decimal>("price")))
                        .Build();
                    
                    orderItems.Add(orderItem);
                }
            }

            return orderItems;
        }

        /// <summary>
        /// Convert the user details to an <see cref="Address"/>.
        /// Throws <see cref="ArgumentException"/> when the provided country code does not correspond with a country supported by Rabo OmniKassa.
        /// </summary>
        /// <param name="userDetails">The <see cref="WiserItemModel"/> containing the user details.</param>
        /// <param name="detailKeyPrefix">Additional string as a prefix for "street", "zipcode", "city", "country", "housenumber" and "housenumber_suffix". For example for shipping.</param>
        /// <returns>Returns an <see cref="Address"/> with the required information.</returns>
        private Address CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
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

            var addressBuilder = new Address.Builder()
                .WithFirstName(userDetails.GetDetailValue("firstname"))
                .WithLastName(userDetails.GetDetailValue("lastname"))
                .WithStreet(userDetails.GetDetailValue($"{detailKeyPrefix}street"))
                .WithPostalCode(userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"))
                .WithCity(userDetails.GetDetailValue($"{detailKeyPrefix}city"))
                .WithCountryCode(EnumHelpers.ToEnum<CountryCode>(userDetails.GetDetailValue($"{detailKeyPrefix}country")));

            //Add house number if provided to Wiser.
            var houseNumber = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber");
            if (String.IsNullOrWhiteSpace(houseNumber))
            {
                return addressBuilder.Build();
            }

            addressBuilder.WithHouseNumber(houseNumber);

            //Add house number addition if an addition has been provided to Wiser.
            var houseNumberAddition = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix");
            if (!String.IsNullOrWhiteSpace(houseNumberAddition))
            {
                addressBuilder.WithHouseNumberAddition(houseNumberAddition);
            }

            return addressBuilder.Build();
        }

        /// <summary>
        /// Convert <see cref="PaymentMethods"/> to <see cref="PaymentBrand"/>.
        /// Throws <see cref="ArgumentException"/> when the provided <see cref="PaymentMethods"/> is not supported by Rabo OmniKassa.
        /// </summary>
        /// <param name="paymentMethod">The <see cref="PaymentMethods"/> to convert.</param>
        /// <returns>Returns the <see cref="PaymentBrand"/> of the corresponding brand.</returns>
        private PaymentBrand ConvertPaymentMethodToPaymentBrand(PaymentMethodSettingsModel paymentMethod)
        {
            switch (paymentMethod.ExternalName.ToUpperInvariant())
            {
                case "IDEAL":
                   return PaymentBrand.IDEAL;
                case "AFTERPAY":
                    return PaymentBrand.AFTERPAY;
                case "PAYPAL":
                    return PaymentBrand.PAYPAL;
                case "MASTERCARD":
                    return PaymentBrand.MASTERCARD;
                case "VISA":
                    return PaymentBrand.VISA;
                case "BANCONTACT":
                    return PaymentBrand.BANCONTACT;
                case "MAESTRO":
                    return PaymentBrand.MAESTRO;
                case "V_PAY":
                case "VPAY":
                   return PaymentBrand.V_PAY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentMethod.ExternalName), paymentMethod.ExternalName);
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

            var raboOmniKassaSettings = (RaboOmniKassaSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            SetupEnvironment(raboOmniKassaSettings);

            PaymentCompletedResponse response;
            try
            {
                response = CreatePaymentCompletedResponse();
            }
            catch (IllegalSignatureException)
            {
                return new StatusUpdateResult
                {
                    Status = "Illegal signature received; unable to process status update.",
                    Successful = false
                };
            }

            await LogIncomingPaymentActionAsync(PaymentServiceProviders.RaboOmniKassa, response.OrderId, Convert.ToInt32(response.Status));

            switch (response.Status)
            {
                case PaymentStatus.COMPLETED:
                    return new StatusUpdateResult
                    {
                        Successful = true
                    };
                case PaymentStatus.CANCELLED:
                    return new StatusUpdateResult
                    {
                        Status = "User cancelled the order at the PSP.",
                        Successful = false
                    };
                case PaymentStatus.EXPIRED:
                    return new StatusUpdateResult
                    {
                        Status = "The order expired at the PSP.",
                        Successful = false
                    };
                default:
                    return new StatusUpdateResult
                    {
                        Status = "Unknown status; unable to process status update.",
                        Successful = false
                    };
            }
        }

        /// <summary>
        /// Create a <see cref="PaymentCompletedResponse"/> based on the request query.
        /// Throws <see cref="IllegalSignatureException"/> when the provided information by the query is not signed by the correct key.
        /// </summary>
        /// <returns>Returns a valid <see cref="PaymentCompletedResponse"/> object.</returns>
        private PaymentCompletedResponse CreatePaymentCompletedResponse()
        {
            var orderId = httpContextAccessor.HttpContext.Request.Query["order_id"].ToString(); //Invoice number as provided by us.
            var status = httpContextAccessor.HttpContext.Request.Query["status"].ToString();
            var signature = httpContextAccessor.HttpContext.Request.Query["signature"].ToString();

            return PaymentCompletedResponse.Create(orderId, status, signature, signingKey);
        }

        /// <summary>
        /// Get the corresponding redirect URL after the return from the PSP website.
        /// Rabo OmniKassa only provides one return url containing information about the status of the order.
        /// </summary>
        /// <returns>Returns the url to redirect the user to.</returns>
        public string GetRedirectUrlOnReturnFromPSP(PaymentMethodSettingsModel paymentMethodSettings)
        {
            var raboOmniKassaSettings = (RaboOmniKassaSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            if (httpContextAccessor?.HttpContext == null)
            {
                return raboOmniKassaSettings.FailUrl;
            }

            SetupEnvironment(raboOmniKassaSettings);
            
            PaymentCompletedResponse response;
            try
            {
                response = CreatePaymentCompletedResponse();
            }
            catch (IllegalSignatureException)
            {
                return raboOmniKassaSettings.FailUrl;
            }

            switch (response.Status)
            {
                case PaymentStatus.COMPLETED:
                    return raboOmniKassaSettings.SuccessUrl;
                case PaymentStatus.IN_PROGRESS:
                    var pendingUrl = raboOmniKassaSettings.PendingUrl;

                    //Redirect to success url if no specific pending url has been provided.
                    if (String.IsNullOrWhiteSpace(pendingUrl))
                    {
                        pendingUrl = raboOmniKassaSettings.SuccessUrl;
                    }

                    return pendingUrl;
                case PaymentStatus.CANCELLED:
                    return raboOmniKassaSettings.FailUrl;
                case PaymentStatus.EXPIRED:
                    return raboOmniKassaSettings.FailUrl;
                default:
                    return raboOmniKassaSettings.FailUrl;
            }
        }

        /// <summary>
        /// Handle the notifications that are provided by Rabo OmniKassa by means of a webhook.
        /// </summary>
        /// <returns></returns>
        public async Task HandleNotification(PaymentMethodSettingsModel paymentMethodSettings)
        {
            if (httpContextAccessor?.HttpContext == null)
            {
                return;
            }

            var raboOmniKassaSettings = (RaboOmniKassaSettingsModel)paymentMethodSettings.PaymentServiceProvider;
            SetupEnvironment(raboOmniKassaSettings);

            //Get the notification from the body.
            using var reader = new StreamReader(httpContextAccessor.HttpContext.Request.Body);
            var bodyJson = await reader.ReadToEndAsync();
            var notification = JsonConvert.DeserializeObject<ApiNotification>(bodyJson);

            if (notification == null)
            {
                return;
            }
            
            try
            {
                notification.ValidateSignature(signingKey);
            }
            catch (IllegalSignatureException)
            {
                return;
            }

            var notifyUrlBase = raboOmniKassaSettings.WebhookUrl;
            var endpoint = OmniKassa.Endpoint.Create(environment, signingKey, refreshToken);

            //Retrieve all MerchantOrderStatusResponses that are available.
            MerchantOrderStatusResponse response;
            do
            {
                response = await endpoint.RetrieveAnnouncement(notification);
                try
                {
                    response.ValidateSignature(signingKey);
                }
                catch (IllegalSignatureException)
                {
                    return;
                }
                
                //Handle each MerchantOrderResult separately to comply with the operation of the PaymentService.
                foreach (var result in response.OrderResults)
                {
                    //Ignore updates with the status of "IN_PROGRESS" in case those are given, only handle definitive states.
                    if (result.OrderStatus == PaymentStatus.IN_PROGRESS)
                    {
                        await LogIncomingPaymentActionAsync(PaymentServiceProviders.RaboOmniKassa, result.MerchantOrderId, Convert.ToInt32(result.OrderStatus));
                        continue;
                    }

                    //Prepare the signature data. The information and order needs to be the same as in PaymentCompletedResponse.
                    var signatureData = new List<string>
                    {
                        result.MerchantOrderId,
                        result.OrderStatus.ToString()
                    };
                    var signature = Signable.CalculateSignature(signatureData, Convert.FromBase64String(signingKey));

                    var notifyUrl = $"{notifyUrlBase}&order_id={result.MerchantOrderId}&status={result.OrderStatus}&signature={signature}";

                    var request = (HttpWebRequest) WebRequest.Create(notifyUrl);
                    _ = request.GetResponseAsync(); //Ignore the return value.
                }
            } while (response.MoreOrderResultsAvailable);
        }
    }
}
