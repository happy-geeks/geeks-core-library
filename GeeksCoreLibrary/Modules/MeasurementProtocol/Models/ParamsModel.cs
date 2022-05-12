using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models
{
    public class ParamsModel
    {
        public string Currency { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        public decimal Value { get; set; }

        public string Affiliation { get; set; }

        public string Coupon { get; set; }

        public decimal Shipping { get; set; }

        public decimal Tax { get; set; }

        [JsonProperty("payment_type")]
        public string PaymentType { get; set; }

        public List<ItemModel> Items { get; set; }
    }
}
