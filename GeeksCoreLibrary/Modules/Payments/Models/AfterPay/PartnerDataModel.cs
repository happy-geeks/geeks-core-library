namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class PartnerDataModel
    {
        public string PspName { get; set; }
        public string PspType { get; set; }
        public string TrackingProvider { get; set; }
        public string TrackingSessionId { get; set; }
        public string TrackingScore { get; set; }
        public string ChallengedScore { get; set; }
    }
}