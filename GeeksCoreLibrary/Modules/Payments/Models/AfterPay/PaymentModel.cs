using GeeksCoreLibrary.Modules.Payments.Enums.AfterPay;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class PaymentModel
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentTypes Type { get; set; }
        public string ContractId { get; set; }
        public DirectDebitModel DirectDebit { get; set; }
        public CampaignModel Campaign { get; set; }
        public string Invoice { get; set; }
        public AccountModel Account { get; set; }
        public ConsolidatedInvoiceModel ConsolidatedInvoice { get; set; }
        public InstallmentModel Installment { get; set; }
    }
}