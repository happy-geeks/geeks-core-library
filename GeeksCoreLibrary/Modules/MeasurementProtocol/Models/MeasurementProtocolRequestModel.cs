using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models
{
    public class MeasurementProtocolRequestModel
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("events")]
        public List<EventModel> Events { get; set; }
    }
}
