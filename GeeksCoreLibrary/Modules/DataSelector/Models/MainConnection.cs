using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class MainConnection
    {
        public string EntityName { get; set; }

        [JsonProperty("scope")]
        public Scope[] Scopes { get; set; }

        [JsonProperty(ItemConverterType = typeof(FieldConverter))]
        public Field[] Fields { get; set; }
    }
}
