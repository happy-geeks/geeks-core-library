using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models
{
    public class ContactModel
    {
        public string Type { get; set; } = "01";

        public string Email { get; set; }

        [JsonProperty("SMSNr")]
        public string SmsNumber { get; set; }
    }
}