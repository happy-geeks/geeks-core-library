using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class AdditionalDataModel
    {
        public string PluginProvider { get; set; }
        public string PluginVersion { get; set; }
        public string ShopUrl { get; set; }
        public string ShopPlatform { get; set; }
        public string ShopPlatformVersion { get; set; }
        public List<MarketplaceModel> Marketplace { get; set; }
        public SubscriptionModel Subscription { get; set; }
        public PartnerDataModel PartnerData { get; set; }
        public string AdditionalPaymentInfo { get; set; }
    }
}