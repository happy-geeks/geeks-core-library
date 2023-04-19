using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeeksCoreLibrary.Modules.Payments.Services;

/// <inheritdoc cref="IPaymentServiceProviderService" />
public class PayNlService : PaymentServiceProviderBaseService, IPaymentServiceProviderService, IScopedService
{
    private const string BaseUrl = "https://rest.pay.nl/";
    private readonly IDatabaseHelpersService databaseHelpersService;
    private readonly IDatabaseConnection databaseConnection;
    private readonly ILogger<PaymentServiceProviderBaseService> logger;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GclSettings gclSettings;
    private readonly IShoppingBasketsService shoppingBasketsService;

    protected PayNlService(
        IDatabaseHelpersService databaseHelpersService, 
        IDatabaseConnection databaseConnection, 
        ILogger<PaymentServiceProviderBaseService> logger, 
        IOptions<GclSettings> gclSettings, IShoppingBasketsService shoppingBasketsService, IHttpContextAccessor httpContextAccessor = null) : base(databaseHelpersService, databaseConnection, logger, httpContextAccessor)
    {
        this.databaseHelpersService = databaseHelpersService;
        this.databaseConnection = databaseConnection;
        this.logger = logger;
        this.shoppingBasketsService = shoppingBasketsService;
        this.gclSettings = gclSettings.Value;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, WiserItemModel userDetails,
        PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber)
    {
        var payNlSettings = (PayNLSettingsModel)paymentMethodSettings.PaymentServiceProvider;

        var validationResult = ValidatePayNLSettings(payNlSettings);
        if (!validationResult.Valid)
        {
            logger.LogError(validationResult.Message);
            return new PaymentRequestResult
            {
                Successful = false,
                Action = PaymentRequestActions.Redirect,
                ActionData = payNlSettings.FailUrl
            };
        }

        var totalPrice = await CalculatePriceAsync(conceptOrders);

        // Build and execute payment request.
        var restClient = CreateRestClient(payNlSettings);
        var restRequest = CreateTransactionStartRequest(totalPrice, payNlSettings, invoiceNumber);
        var restResponse = await restClient.ExecuteAsync(restRequest);
        var responseJson = JObject.Parse(restResponse.Content);
        var responseSuccessful = restResponse.StatusCode == HttpStatusCode.Created;
        
        return new PaymentRequestResult
        {
            Successful = responseSuccessful,
            Action = PaymentRequestActions.Redirect,
            ActionData = (responseSuccessful) ? responseJson["paymentUrl"]?.ToString() : payNlSettings.FailUrl
        };
    }

    public Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings,
        PaymentMethodSettingsModel paymentMethodSettings)
    {
        throw new System.NotImplementedException();
    }

    private static RestClient CreateRestClient(PayNLSettingsModel payNlSettings) =>
        new RestClient(new RestClientOptions(BaseUrl)
        {
            Authenticator = new HttpBasicAuthenticator(payNlSettings.Username, payNlSettings.Password)
        });

    private async Task<decimal> CalculatePriceAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders)
    {
        var basketSettings = await shoppingBasketsService.GetSettingsAsync();

        var totalPrice = 0M;
        foreach (var (main, lines) in conceptOrders)
        {
            totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
        }

        return totalPrice;
    }

    private (bool Valid, string Message) ValidatePayNLSettings(PayNLSettingsModel payNlSettings)
    {
        if (String.IsNullOrEmpty(payNlSettings.Username) || String.IsNullOrEmpty(payNlSettings.Password))
        {
            return (false, "PayNL misconfigured: No username or password set.");
        }

        if (payNlSettings.Username.StartsWith("AT-") && String.IsNullOrEmpty(payNlSettings.ServiceId))
        {
            return (false, "PayNL misconfigured: Username is an AT-code but no ServiceId is set.");
        }

        return (true, null);
    }

    private RestRequest CreateTransactionStartRequest(decimal totalPrice, PayNLSettingsModel payNlSettings, string invoiceNumber)
    {
        var restRequest = new RestRequest("/v2/transactions", Method.Post);

        restRequest.AddJsonBody(new
        {
            serviceId = payNlSettings.ServiceId,
            amount = new
            {
                value = (int)Math.Round(totalPrice * 100),
                currency = payNlSettings.Currency
            },
            description = $"Order #{invoiceNumber}",
            returnUrl = payNlSettings.ReturnUrl,
            exchangeUrl = payNlSettings.WebhookUrl,
            integration = new
            {
                testMode = gclSettings.Environment.InList(Environments.Test, Environments.Development)
            }
        });

        return restRequest;
    }
}