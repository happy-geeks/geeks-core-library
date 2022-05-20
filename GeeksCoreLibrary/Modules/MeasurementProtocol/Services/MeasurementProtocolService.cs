using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Models;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Services
{
    public class MeasurementProtocolService : IMeasurementProtocolService, IScopedService
    {
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IDatabaseConnection databaseConnection;

        public MeasurementProtocolService(IStringReplacementsService stringReplacementsService, IDatabaseConnection databaseConnection)
        {
            this.stringReplacementsService = stringReplacementsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice)
        {
            var replaceData = new Dictionary<string, object>()
            {
                {"total_price", totalBasketPrice},
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolBeginCheckoutJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasketLines);
        }

        public async Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice, string paymentMethodId)
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
                {"total_price", totalBasketPrice},
                {"payment_method", dataTable.Rows[0].Field<string>("title") }
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolAddPaymentInfoJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasketLines);
        }

        public async Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice, decimal tax, string transactionId)
        {
            var replaceData = new Dictionary<string, object>()
            {
                {"total_price", totalBasketPrice},
                {"tax_price", tax },
                {"transaction_id", transactionId }
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolPurchaseJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasketLines);
        }

        private async Task SendMeasurement(string eventJson, string itemJson, Dictionary<string, object> replaceData, List<WiserItemModel> shoppingBasketLines)
        {
            if (String.IsNullOrWhiteSpace(eventJson) || String.IsNullOrWhiteSpace(itemJson))
            {
                return;
            }

            var items = GetItemsFromBasketLines(itemJson, shoppingBasketLines);
            var result = stringReplacementsService.DoReplacements(eventJson, replaceData, "[{", "}]");
            var eventModel = JsonConvert.DeserializeObject<EventModel>(result);
            eventModel.Params.Items = items;
        }

        private List<ItemModel> GetItemsFromBasketLines(string measurementProtocolItemJson, List<WiserItemModel> shoppingBasketLines)
        {
            var items = new List<ItemModel>();

            for (var i = 0; i <shoppingBasketLines.Count; i++)
            {
                var replaceData = new Dictionary<string, object>();

                foreach (var detail in shoppingBasketLines[i].Details)
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
    }
}
