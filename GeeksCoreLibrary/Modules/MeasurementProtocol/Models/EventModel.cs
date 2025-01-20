using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models;

/// <summary>
/// A model for events that are send to the Google Measurement Protocol.
/// </summary>
public class EventModel
{
    /// <summary>
    /// Gets or sets the name of the event to be send to Google Measurement Protocol.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameters of the event.
    /// </summary>
    [JsonProperty("params")]
    public ParamsModel Params { get; set; }
}