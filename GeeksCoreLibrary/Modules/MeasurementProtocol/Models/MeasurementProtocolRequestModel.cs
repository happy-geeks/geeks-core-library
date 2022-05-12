using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models
{
    public class MeasurementProtocolRequestModel
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        public List<EventModel> Events { get; set; }
    }
}
