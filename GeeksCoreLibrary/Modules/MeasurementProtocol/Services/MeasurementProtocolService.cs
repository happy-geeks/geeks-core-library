using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Models;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Services
{
    public class MeasurementProtocolService : IMeasurementProtocolService, IScopedService
    {
        private readonly IStringReplacementsService stringReplacementsService;

        public MeasurementProtocolService(IStringReplacementsService stringReplacementsService)
        {
            this.stringReplacementsService = stringReplacementsService;
        }

        /// <inheritdoc />
        public async Task BeginCheckoutEventAsync(decimal totalBasketPrice, OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines)
        {
            var replaceData = new Dictionary<string, object>()
            {
                {"total_price", totalBasketPrice},
            };

            await SendMeasurement(orderProcessSettings.MeasurementProtocolBeginCheckoutJson, orderProcessSettings.MeasurementProtocolItemJson, replaceData, shoppingBasketLines);
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
