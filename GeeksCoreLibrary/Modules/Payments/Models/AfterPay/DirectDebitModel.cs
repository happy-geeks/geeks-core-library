namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class DirectDebitModel
    {
        public string BankCode { get; set; }
        public string BankAccount { get; set; }
        public string Token { get; set; }
    }
}