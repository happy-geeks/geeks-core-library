using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class ExternalConfigurationModel
{
    /// <summary>
    /// Gets or sets the ID of the external configuration.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }
}