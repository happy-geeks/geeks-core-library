using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models;

/// <summary>
/// A model for a Measurement Protocol request.
/// </summary>
public class MeasurementProtocolRequestModel
{
    /// <summary>
    /// Gets or sets the Google tracking ID of the client/user/customer.
    /// </summary>
    [JsonProperty("client_id")]
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the events to be send in the request.
    /// </summary>
    [JsonProperty("events")]
    public List<EventModel> Events { get; set; }
}