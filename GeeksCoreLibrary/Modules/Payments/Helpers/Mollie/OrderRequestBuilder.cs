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
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models.Mollie;
using Mollie.Api.Models;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using PhoneNumbers;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Modules.Payments.Helpers.Mollie;

public class OrderRequestBuilder : IOrderRequestBuilder, ITransientService
{
    private readonly IShoppingBasketsService shoppingBasketsService;
    private MollieSettingsModel mollieSettings;

    public OrderRequestBuilder(IShoppingBasketsService shoppingBasketsService)
    {
        this.shoppingBasketsService = shoppingBasketsService;
    }

    public async Task<OrderRequest> CreateOrderRequestAsync(
        string invoiceNumber,
        ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets,
        WiserItemModel userDetails,
        MollieSettingsModel mollieSettingsModel,
        PaymentMethodSettingsModel paymentMethodSettingsModel)
    {
        mollieSettings = mollieSettingsModel;
        var totalPrice = await CalculatePriceAsync(shoppingBaskets);
        var orderRequest = new OrderRequest
        {
            Amount = CreateAmountModel(totalPrice),
            OrderNumber = invoiceNumber,
            RedirectUrl = BuildUrl(mollieSettings.ReturnUrl, invoiceNumber),
            WebhookUrl = BuildUrl(mollieSettings.WebhookUrl, invoiceNumber),
            Locale = mollieSettings.Locale,
            Method = paymentMethodSettingsModel.ExternalName,
            Lines = await ConvertShoppingBasketsToOrderLinesAsync(shoppingBaskets),
            BillingAddress = CreateBillingAddress(userDetails),
            ShippingAddress = CreateAddress(userDetails, "shipping_")
        };

        if (String.Equals(paymentMethodSettingsModel?.ExternalName, "ideal", StringComparison.OrdinalIgnoreCase))
        {
            var issuerValue = shoppingBaskets.First().Main.GetDetailValue(Constants.PaymentMethodIssuerProperty);
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
        var issuerConstant = issuerConstants.FirstOrDefault(mi =>
            mi.Name.Equals(issuerValue, StringComparison.OrdinalIgnoreCase) ||
            mi.Name.Equals($"ideal_{issuerValue}", StringComparison.OrdinalIgnoreCase));

        if (issuerConstant != null)
        {
            return (string)issuerConstant.GetValue(null);
        }

        return null;
    }

    private OrderAddressDetails CreateAddress(WiserItemModel userDetails, string detailKeyPrefix = "")
    {
        var street = userDetails.GetDetailValue($"{detailKeyPrefix}street");
        var zipcode = userDetails.GetDetailValue($"{detailKeyPrefix}zipcode");
        var city = userDetails.GetDetailValue($"{detailKeyPrefix}city");
        var country = userDetails.GetDetailValue($"{detailKeyPrefix}country");
        
        //If a prefix is given but any of the required values doesn't contain a value return null.
        if ((!String.IsNullOrWhiteSpace(detailKeyPrefix) &&
             String.IsNullOrWhiteSpace(street))
            || String.IsNullOrWhiteSpace(zipcode)
            || String.IsNullOrWhiteSpace(city)
            || String.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        var houseNumber = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber");
        var houseNumberSuffix = userDetails.GetDetailValue($"{detailKeyPrefix}housenumber_suffix");
        return new OrderAddressDetails()
        {
            StreetAndNumber =
                $"{street} {houseNumber}{houseNumberSuffix}",
            PostalCode = zipcode,
            City = city,
            Country = country,
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

        if (String.IsNullOrWhiteSpace(address.Phone))
        {
            return address;
        }

        var phoneNumberUtil = PhoneNumberUtil.GetInstance();
        var phoneObject = phoneNumberUtil.Parse(address.Phone, address.Country);
        address.Phone = phoneNumberUtil.Format(phoneObject, PhoneNumberFormat.E164);

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
                    await ConvertBasketLineToOrderLineAsync(
                        basket.Main,
                        basketLine,
                        basketSettings);
                orderRequests.Add(orderLineRequest);
            }
        }

        return orderRequests;
    }

    private async Task<OrderLineRequest> ConvertBasketLineToOrderLineAsync(
        WiserItemModel basket,
        WiserItemModel basketLine,
        ShoppingBasketCmsSettingsModel basketSettings)
    {
        var name = basketLine.GetDetailValue("title");

        // Non-products like shipping cost might not have a name
        // For those we use the type as name
        // This can be further improved by using localisation
        if (String.IsNullOrEmpty(name))
        {
            name = basketLine.GetDetailValue("type");
        }

        // get prices using the shoppingbasketService
        var linePrice = await shoppingBasketsService.GetLinePriceAsync(
            basket,
            basketLine,
            basketSettings
        );
        var linePriceVatOnly = await shoppingBasketsService.GetLinePriceAsync(
            basket,
            basketLine,
            basketSettings,
            ShoppingBasket.PriceTypes.VatOnly
        );
        var discountAmount = await shoppingBasketsService.GetLinePriceAsync(
            basket,
            basketLine,
            basketSettings,
            ShoppingBasket.PriceTypes.DiscountInVat
        );

        var quantityDetail = basketLine.GetDetailValue(basketSettings.QuantityPropertyName);
        var parseSucceeded = Int32.TryParse(quantityDetail, out var lineProductQuantity);
        // Some types of order lines do not have quantities
        // we set 1 for those
        if (!parseSucceeded)
            lineProductQuantity = 1;

        var vatRate = Convert.ToInt32(Math.Round(100 / linePrice * linePriceVatOnly));
        var vatFactor = await shoppingBasketsService.GetVatFactorByRateAsync(basket, basketSettings, vatRate) * 100;

        return new OrderLineRequest()
        {
            Name = name,
            UnitPrice = CreateAmountModel(linePrice / lineProductQuantity),
            TotalAmount = CreateAmountModel(linePrice),
            DiscountAmount = CreateAmountModel(discountAmount),
            VatAmount = CreateAmountModel(linePriceVatOnly),
            Quantity = lineProductQuantity,
            VatRate = vatFactor.ToString("F2", CultureInfo.InvariantCulture),
        };
    }

    private Amount CreateAmountModel(decimal price)
    {
        return new Amount
        {
            Value = price.ToString("F2", CultureInfo.InvariantCulture),
            Currency = mollieSettings.Currency
        };
    }

    private string BuildUrl(string webhookUrl, string invoiceNumber)
    {
        // TODO: Refactor this method so that we can use it for all PSPs.
        var webhookUrlBuilder = new UriBuilder(webhookUrl);
        var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
        queryString["invoice_number"] = invoiceNumber;

        webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

        return webhookUrlBuilder.ToString();
    }
    
    private async Task<decimal> CalculatePriceAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets)
    {
        var basketSettings = await shoppingBasketsService.GetSettingsAsync();

        var totalPrice = 0M;
        foreach (var (main, lines) in shoppingBaskets)
        {
            totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, ShoppingBasket.PriceTypes.PspPriceInVat);
        }

        return totalPrice;
    }
}