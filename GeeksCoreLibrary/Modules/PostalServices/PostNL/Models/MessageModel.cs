using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class MessageModel
{
    [JsonProperty("MessageID")]
    public string MessageId { get; set; }

    public string MessageTimeStamp { get; set; }
    public string Printertype { get; set; }
}