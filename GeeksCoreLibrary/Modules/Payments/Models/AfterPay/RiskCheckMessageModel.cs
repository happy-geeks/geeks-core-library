namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class RiskCheckMessageModel
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string CustomerFacingMessage { get; set; }
        public string ActionCode { get; set; }
        public string FieldReference { get; set; }
    }
}