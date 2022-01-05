namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class MarketplaceModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RegisteredSince { get; set; }
        public string Rating { get; set; }
        public long Transactions { get; set; }
    }
}