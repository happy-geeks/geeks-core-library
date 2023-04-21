using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using BuckarooSdk.DataTypes.ParameterGroups.CreditManagement3;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models.Mollie;
using ISO3166;
using ISO3166NL;
using Mollie.Api.Models;
using Mollie.Api.Models.Order;
using Mollie.Api.Models.Order.Request.PaymentSpecificParameters;
using PhoneNumbers;

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
            ShippingAddress = CreateAddress(userDetails, "shipping_")
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
            City = userDetails.GetDetailValue($"{detailKeyPrefix}city"),
            Country = ToIso3166(userDetails.GetDetailValue($"{detailKeyPrefix}country")),
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
            var parseSucceeded =  Int32.TryParse(quantityDetail, out var lineProductQuantity);
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
            var webhookUrlBuilder =  new UriBuilder(webhookUrl);
            var queryString = HttpUtility.ParseQueryString(webhookUrlBuilder.Query);
            queryString["invoice_number"] = invoiceNumber;

            webhookUrlBuilder.Query = queryString.ToString() ?? String.Empty;

            return webhookUrlBuilder.ToString();
        }

        private static string ToIso3166(string country)
        {
            var isoCode = (from countryItem in Country.List
                where countryItem.TwoLetterCode == country || countryItem.Name == country ||
                      countryItem.ThreeLetterCode == country
                select countryItem.TwoLetterCode).FirstOrDefault();

            if (isoCode is null)
            {
                isoCode = (from countryItem in Land.List
                    where countryItem.TweeLetterCode == country || countryItem.Naam == country ||
                          countryItem.DrieLetterCode == country
                    select countryItem.TweeLetterCode).FirstOrDefault();
            }

            return isoCode;
        }
}