using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class OrderModel
    {
        public string Number { get; set; }
        public decimal TotalNetAmount { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public string Currency { get; set; }
        public OrderRiskModel Risk { get; set; }
        public string MerchantImageUrl { get; set; }
        public List<ItemModel> Items { get; set; }
    }
}