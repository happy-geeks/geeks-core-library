namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class InstallmentModel
    {
        public long ProfileNo { get; set; }
        public long NumberOfInstallments { get; set; }
        public long CustomerInterestRate { get; set; }
    }
}