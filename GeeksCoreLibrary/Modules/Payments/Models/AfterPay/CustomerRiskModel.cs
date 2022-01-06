namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class CustomerRiskModel
    {
        public bool ExistingCustomer { get; set; }
        public bool VerifiedCustomerIdentification { get; set; }
        public bool MarketingOptIn { get; set; }
        public string CustomerSince { get; set; }
        public string CustomerClassification { get; set; }
        public string AcquisitionChannel { get; set; }
        public bool HasCustomerCard { get; set; }
        public string CustomerCardSince { get; set; }
        public string CustomerCardClassification { get; set; }
        public string ProfileTrackingId { get; set; }
        public string IpAddress { get; set; }
        public long NumberOfTransactions { get; set; }
        public string CustomerIndividualScore { get; set; }
        public string UserAgent { get; set; }
    }
}