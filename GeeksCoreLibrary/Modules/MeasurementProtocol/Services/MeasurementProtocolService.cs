using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Models;
using Newtonsoft.Json;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Services
{
    public class MeasurementProtocolService : IMeasurementProtocolService, IScopedService
    {
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IDatabaseConnection databaseConnection;

        public MeasurementProtocolService(IShoppingBasketsService shoppingBasketsService, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection)
        {
            this.shoppingBasketsService = shoppingBasketsService;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings)
        {
            var coupon = await GetCoupons(shoppingBasketLines);

            var replaceData = new Dictionary<string, object>();

            await SendMeasurement(orderProcessSettings.MeasurementProtocolBeginCheckoutJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        public async Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string paymentMethodId)
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("paymentMethodId", paymentMethodId);
            var dataTable = await databaseConnection.GetAsync("SELECT title FROM wiser_item WHERE id = ?paymentMethodId LIMIT 1");

            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            var replaceData = new Dictionary<string, object>()
            {
                {"payment_method", dataTable.Rows[0].Field<string>("title") }
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolAddPaymentInfoJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        public async Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string transactionId)
        {
            var tax = await shoppingBasketsService.GetPriceAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings, ShoppingBasket.PriceTypes.VatOnly);

            var replaceData = new Dictionary<string, object>()
            {
                {"tax_price", tax },
                {"transaction_id", transactionId }
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolPurchaseJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
        }

        private async Task SendMeasurement(string eventJson, string itemJson, Dictionary<string, object> replaceData, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings)
        {
            if (String.IsNullOrWhiteSpace(eventJson) || String.IsNullOrWhiteSpace(itemJson))
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
        }

        private List<ItemModel> GetItemsFromBasketLines(string measurementProtocolItemJson, List<WiserItemModel> products)
        {
            var items = new List<ItemModel>();

            for (var i = 0; i < products.Count; i++)
            {
                var replaceData = new Dictionary<string, object>();

                foreach (var detail in products[i].Details)
                {
                    replaceData.Add(detail.Key, detail.Value);
                }

                var itemJson = stringReplacementsService.DoReplacements(measurementProtocolItemJson, replaceData, "[{", "}]");
                var item = JsonConvert.DeserializeObject<ItemModel>(itemJson);
                item.Index = i;
                items.Add(item);
            }

            return items;
        }

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
