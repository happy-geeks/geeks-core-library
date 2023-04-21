using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models.Mollie;
using Mollie.Api.Models;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;

namespace GeeksCoreLibrary.Modules.Payments.Helpers.Mollie;

public class OrderRequestBuilder
{
    private readonly IShoppingBasketsService shoppingBasketsService;
    private readonly MollieSettingsModel mollieSettings;
    private readonly GclSettings gclSettings;
    private readonly PaymentMethodSettingsModel paymentMethodSettings;

    public OrderRequestBuilder(IShoppingBasketsService shoppingBasketsService, MollieSettingsModel mollieSettings, GclSettings gclSettings, PaymentMethodSettingsModel paymentMethodSettings)
    {
        this.shoppingBasketsService = shoppingBasketsService;
        this.mollieSettings = mollieSettings;
        this.gclSettings = gclSettings;
        this.paymentMethodSettings = paymentMethodSettings;
    }
    
    public async Task<OrderRequest> CreateOrderRequestAsync(decimal totalPrice,
        string invoiceNumber,
        ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
        WiserItemModel userDetails)
    {
        var orderRequest = new OrderRequest
        {
            Amount = new Amount
            {
                Value = totalPrice.ToString("F2", CultureInfo.InvariantCulture),
                Currency = mollieSettings.Currency
            },
            OrderNumber = invoiceNumber,
            RedirectUrl = BuildUrl(mollieSettings.ReturnUrl, invoiceNumber),
            WebhookUrl = BuildUrl(mollieSettings.WebhookUrl, invoiceNumber),
            Locale =  mollieSettings.Locale,
            Method = paymentMethodSettings.ExternalName,
            Lines = await ConvertShoppingBasketsToOrderLinesAsync(shoppingBaskets),
            BillingAddress = CreateBillingAddress(userDetails),
            ShippingAddress = CreateAddress(userDetails, "shipping_"),
            Testmode = gclSettings.Environment.InList(Environments.Test, Environments.Development)
        };

        if (String.Equals(paymentMethodSettings?.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
        {
            var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Components.OrderProcess.Models.Constants.PaymentMethodIssuerProperty);
            var issuerName = GetIssuerName(issuerValue);
            orderRequest.Payment = new IDealSpecificParameters
            {
                Issuer = issuerName,
            };
        }
            
        // Metadata is always sent back.
        orderRequest.Metadata = invoiceNumber;
        return orderRequest;
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

    private OrderAddressDetails CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
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

        return new OrderAddressDetails()
        {
            StreetAndNumber =
                $"{userDetails.GetDetailValue($"{detailKeyPrefix}street")} {userDetails.GetDetailValue($"{detailKeyPrefix}housenumber")}{userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix")}",
            PostalCode = userDetails.GetDetailValue($"{detailKeyPrefix}zipcode"),
            City = userDetails.GetDetailValue($"{detailKeyPrefix}_city"),
            Country = userDetails.GetDetailValue($"{detailKeyPrefix}_country"),
        };
    }

    private OrderAddressDetails CreateBillingAddress(WiserItemModel userDetails)
    {
        var address = CreateAddress(userDetails);
        address.OrganizationName = userDetails.GetDetailValue("companyname");
        address.GivenName = userDetails.GetDetailValue("firstname");
        address.FamilyName = userDetails.GetDetailValue("lastname");
        address.Email = userDetails.GetDetailValue("email");
        address.Phone = userDetails.GetDetailValue("phone");
        return address;
    }
    
    private async Task<IEnumerable<OrderLineRequest>> ConvertShoppingBasketsToOrderLinesAsync(
            ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
        {
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var orderRequests = new List<OrderLineRequest>();
            foreach (var basket in shoppingBaskets)
            {
                foreach (var basketLine in basket.Lines)
                {
                    OrderLineRequest orderLineRequest =
                        await ConvertOrderLineFromBasketLineAsync(
                            basket.Main, 
                            basketLine,
                            basketSettings);
                    orderRequests.Add(orderLineRequest);
                }
            }

            return orderRequests;
        }

        private async Task<OrderLineRequest> ConvertOrderLineFromBasketLineAsync(
            WiserItemModel basket,
            WiserItemModel basketLine,
            ShoppingBasketCmsSettingsModel basketSettings)
        {
            var linePrice = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.PspPriceInVat
            );
            var linePriceVatOnly = await shoppingBasketsService.GetLinePriceAsync(
                basket,
                basketLine,
                basketSettings,
                ShoppingBasket.PriceTypes.VatOnly
            );
            var lineProductQuantity = Convert.ToInt32(basketLine.GetDetailValue(basketSettings.QuantityPropertyName));

            return new OrderLineRequest()
            {
                Name = basketLine.GetDetailValue("title"),
                UnitPrice = new Amount()
                {
                    Value = (linePrice / lineProductQuantity).ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                TotalAmount = new Amount()
                {
                    Value = linePrice.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                DiscountAmount = new Amount()
                {
                    Value = basketLine.GetDetailValue(basketSettings.DiscountPropertyName),
                    Currency = mollieSettings.Currency
                },
                VatAmount = new Amount()
                {
                    Value = linePriceVatOnly.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = mollieSettings.Currency
                },
                Quantity = lineProductQuantity,
                VatRate = basketLine.GetDetailValue(basketSettings.VatRatePropertyName),
            };
        }
        
        private string BuildUrl(string webhookUrl, string invoiceNumber)
        {
            // TODO: Refactor this method so that we can use it for all PSPs.
            var webhookUrlBuilder =  new UriBuilder(webhookUrl);
            var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
            queryString["invoice_number"] = invoiceNumber;

            webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

            return webhookUrlBuilder.ToString();
        }
}