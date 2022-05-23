using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models
{
    public class EventModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("params")]
        public ParamsModel Params { get; set; }
    }
}
