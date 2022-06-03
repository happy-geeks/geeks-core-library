using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mime;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Services
{
    public class MeasurementProtocolService : IMeasurementProtocolService, IScopedService
    {
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly ILogger<MeasurementProtocolService> logger;

        public MeasurementProtocolService(IShoppingBasketsService shoppingBasketsService, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection, ILogger<MeasurementProtocolService> logger)
        {
            this.shoppingBasketsService = shoppingBasketsService;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.databaseConnection = databaseConnection;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings)
        {
            var replaceData = new Dictionary<string, object>();
            await SendMeasurement(orderProcessSettings, orderProcessSettings.MeasurementProtocolBeginCheckoutJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        /// <inheritdoc />
        public async Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string paymentMethodId)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            databaseConnection.AddParameter("paymentMethodId", paymentMethodId);
            var dataTable = await databaseConnection.GetAsync($"SELECT title FROM {WiserTableNames.WiserItem} WHERE id = ?paymentMethodId LIMIT 1");

            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            var replaceData = new Dictionary<string, object>()
            {
                {"payment_method", dataTable.Rows[0].Field<string>("title") }
            };

            await SendMeasurement(orderProcessSettings, orderProcessSettings.MeasurementProtocolAddPaymentInfoJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        /// <inheritdoc />
        public async Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string transactionId)
        {
            var tax = await shoppingBasketsService.GetPriceAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings, ShoppingBasket.PriceTypes.VatOnly);

            var replaceData = new Dictionary<string, object>()
            {
                {"tax_price", tax },
                {"transaction_id", transactionId }
            };

            await SendMeasurement(orderProcessSettings, orderProcessSettings.MeasurementProtocolPurchaseJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        /// <summary>
        /// Build the request and send it to Google Analytics.
        /// </summary>
        /// <param name="orderProcessSettings">The settings of the orde process.</param>
        /// <param name="eventJson">The Json to use for the event.</param>
        /// <param name="itemJson">The Json to use for each item.</param>
        /// <param name="replaceData">The event specific data that can be used for replacements.</param>
        /// <param name="shoppingBasket">The shopping basket to use for the event.</param>
        /// <param name="shoppingBasketLines">The shopping basket lines to use for the event.</param>
        /// <param name="shoppingBasketSettings">The settings of the shopping basket.</param>
        /// <returns></returns>
        private async Task SendMeasurement(OrderProcessSettingsModel orderProcessSettings, string eventJson, string itemJson, Dictionary<string, object> replaceData, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings)
        {
            if (String.IsNullOrWhiteSpace(orderProcessSettings.MeasurementProtocolMeasurementId) || String.IsNullOrWhiteSpace(orderProcessSettings.MeasurementProtocolApiSecret) || String.IsNullOrWhiteSpace(eventJson) || String.IsNullOrWhiteSpace(itemJson))
            {
                return;
            }

            var clientId = shoppingBasket.GetDetailValue(Components.Account.Models.Constants.DefaultGoogleCidFieldName);
            if (String.IsNullOrWhiteSpace(clientId))
            {
                return;
            }

            var totalBasketPrice = await shoppingBasketsService.GetPriceAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
            var coupons = await GetCoupons(shoppingBasketLines);

            replaceData.Add("total_price", totalBasketPrice);
            replaceData.Add("coupon", coupons);

            var products = shoppingBasketsService.GetLines(shoppingBasketLines, Constants.OrderLineProductType);
            var items = GetItemsFromBasketLines(itemJson, products);
            var result = stringReplacementsService.DoReplacements(eventJson, replaceData, "[{", "}]");
            var eventModel = JsonConvert.DeserializeObject<EventModel>(result);
            eventModel.Params.Items = items;

            var requestData = new MeasurementProtocolRequestModel()
            {
                ClientId = clientId,
                Events = new List<EventModel>()
                {
                    eventModel
                }
            };

            // Use NewtonSoft to serialize instead of RestSharp's serialization.
            var body = JsonConvert.SerializeObject(requestData);

            try
            {
                var client = new RestClient("https://www.google-analytics.com");
                var request = new RestRequest("/mp/collect", Method.Post);
                request.AddParameter("measurement_id", orderProcessSettings.MeasurementProtocolMeasurementId.DecryptWithAesWithSalt(), ParameterType.QueryString);
                request.AddParameter("api_secret", orderProcessSettings.MeasurementProtocolApiSecret.DecryptWithAesWithSalt(), ParameterType.QueryString);
                request.AddParameter(MediaTypeNames.Application.Json, body, ParameterType.RequestBody);

                var response = await client.ExecuteAsync(request);
                logger.LogInformation($"Measurement Protocol (GA4): Received response with status code '{response.StatusCode}' while sending a measurement protocol request to Google with data:\n{body}");
            }
            catch (Exception e)
            {
                logger.LogError($"Measurement Protocol (GA4): An error has occurred while sending a measurement protocol request to Google with data:\n{body}\n\nGiving error: {e}");
            }
        }

        /// <summary>
        /// Get all items based on the item Json.
        /// </summary>
        /// <param name="measurementProtocolItemJson">The Json for each item.</param>
        /// <param name="products">The products of the shopping basket to add to the items list.</param>
        /// <returns>Returns a list of items for the request.</returns>
        private List<ItemModel> GetItemsFromBasketLines(string measurementProtocolItemJson, List<WiserItemModel> products)
        {
            var items = new List<ItemModel>();

            for (var i = 0; i < products.Count; i++)
            {
                var replaceData = new Dictionary<string, object>();

                // Allow each item detail of the product line to be used for replacements.
                foreach (var detail in products[i].Details)
                {
                    replaceData.Add(detail.Key, detail.Value);
                }

                // Replace the values in Json before deserializing it.
                var itemJson = stringReplacementsService.DoReplacements(measurementProtocolItemJson, replaceData, "[{", "}]");
                var item = JsonConvert.DeserializeObject<ItemModel>(itemJson);
                item.Index = i;
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Get the names of the used coupons during this order, comma separated.
        /// </summary>
        /// <param name="shoppingBasketLines">The shopping basket lines.</param>
        /// <returns>Returns the comma separated names of the sued coupons.</returns>
        private async Task<string> GetCoupons(List<WiserItemModel> shoppingBasketLines)
        {
            var result = new List<string>();

            foreach (var basketLine in shoppingBasketsService.GetLines(shoppingBasketLines, Constants.OrderLineCouponType))
            {
                var couponItemId = basketLine.GetDetailValue<ulong>(Components.ShoppingBasket.Models.Constants.ConnectedItemIdProperty);
                if (couponItemId == 0)
                {
                    continue;
                }

                var couponItem = await wiserItemsService.GetItemDetailsAsync(couponItemId, skipPermissionsCheck: true);
                if (couponItem is not { Id: > 0 })
                {
                    continue;
                }

                var code = couponItem.GetDetailValue<string>("code");
                if (String.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                result.Add(code);
            }

            return String.Join(',', result);
        }
    }
}
