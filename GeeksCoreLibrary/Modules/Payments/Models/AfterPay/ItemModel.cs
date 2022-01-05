using GeeksCoreLibrary.Modules.Payments.Enums.AfterPay;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class ItemModel
    {
        public string ProductId { get; set; }
        public string GroupId { get; set; }
        public string Description { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderItemTypes Type { get; set; }
        public decimal NetUnitPrice { get; set; }
        public decimal GrossUnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatAmount { get; set; }
        public string ImageUrl { get; set; }
        public string ProductUrl { get; set; }
        public string MarketPlaceSellerId { get; set; }
        public string AdditionalInformation { get; set; }
    }
}