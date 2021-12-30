using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.DataSelector.Models
{
    public class ScopeRow
    {
        [JsonProperty(ItemConverterType = typeof(FieldConverter))]
        public Field Key { get; set; }

        public string Operator { get; set; }

        public object Value { get; set; }
    }
}
