namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models
{
    public class PostNlSettings
    {
        public string PostNlNetherlandsCustomerCode { get; set; }
        public string PostNlNetherlandsCustomerNumber { get; set; }
        public string PostNlEuropeCustomerCode { get; set; }
        public string PostNlEuropeCustomerNumber { get; set; }
        public string PostNlGlobalCustomerCode { get; set; }
        public string PostNlGlobalCustomerNumber { get; set; }
        public string PostNlApiBaseUrl { get; set; }
        public string PostNlShippingApiKey { get; set; }
        public string PostNlTariffNumber { get; set; }
    }
}
