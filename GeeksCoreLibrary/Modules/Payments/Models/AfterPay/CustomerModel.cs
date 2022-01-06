using GeeksCoreLibrary.Modules.Payments.Enums.AfterPay;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class CustomerModel
    {
        public string CustomerNumber { get; set; }
        public string IdentificationNumber { get; set; }
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobilePhone { get; set; }
        public string BirthDate { get; set; }
        public string CustomerCategory { get; set; }
        public AddressModel Address { get; set; }
        public CustomerRiskModel RiskData { get; set; }
        public string ConversationLanguage { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DistributionTypes DistributionType { get; set; }
        public string VatId { get; set; }
    }
}