using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
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
    private static readonly string BaseUrl = "https://rest.pay.nl/";
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
        var basketSettings = await shoppingBasketsService.GetSettingsAsync();
        
        var totalPrice = 0M;
        foreach (var (main, lines) in conceptOrders)
        {
            totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
        }

        var payNlSettings = (PayNLSettingsModel)paymentMethodSettings.PaymentServiceProvider;

        // Build and execute payment request.
        var restClient = new RestClient(new RestClientOptions(BaseUrl)
        {
            Authenticator = new HttpBasicAuthenticator(payNlSettings.ApiCode, payNlSettings.ApiKey)
        });
        
        var restRequest = new RestRequest("/v2/transactions", Method.Post);
        
        // PayNL expects the price in euro cents
        restRequest.AddParameter("amount", (int)Math.Round(totalPrice * 100), ParameterType.GetOrPost);
        
        restRequest.AddParameter("description", payNlSettings.Title, ParameterType.GetOrPost);
        restRequest.AddParameter("returnUrl", payNlSettings.ReturnUrl, ParameterType.GetOrPost);

        var restResponse = await restClient.ExecuteAsync(restRequest);

        if (restResponse.StatusCode != HttpStatusCode.Created)
        {
            return new PaymentRequestResult
            {
                Successful = false,
                Action = PaymentRequestActions.Redirect,
                ActionData = payNlSettings.FailUrl
            };
        }
        
        var responseJson = JObject.Parse(restResponse.Content);
        return new PaymentRequestResult
        {
            Successful = true,
            Action = PaymentRequestActions.Redirect,
            ActionData = responseJson["_links"]?["checkout"]?["href"]?.ToString()
        };
    }

    public Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings,
        PaymentMethodSettingsModel paymentMethodSettings)
    {
        throw new System.NotImplementedException();
    }
}