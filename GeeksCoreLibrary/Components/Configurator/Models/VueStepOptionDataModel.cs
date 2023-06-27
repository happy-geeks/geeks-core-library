using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepOptionDataModel
{
    /// <summary>
    /// Gets or sets the option's unique ID.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the option's value.
    /// </summary>
    [JsonProperty("value")]
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the option's display name.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets whether the option is the default option.
    /// </summary>
    [JsonProperty("isDefaultOption")]
    public bool IsDefaultOption { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; set; }
}