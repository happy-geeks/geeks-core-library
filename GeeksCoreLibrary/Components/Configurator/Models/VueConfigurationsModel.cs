using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueConfigurationsModel
{
    /// <summary>
    /// Gets or sets the name of the configurator.
    /// </summary>
    [JsonProperty("configuratorName")]
    public string ConfiguratorName { get; set; }

    /// <summary>
    /// Gets or sets the URL of the image.
    /// </summary>
    [JsonProperty("imageUrl")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the amount of times the user wants to order this configuration.
    /// </summary>
    [JsonProperty("quantity")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the configured steps. The key is the name of the step.
    /// </summary>
    [JsonProperty("items")]
    public Dictionary<string, VueStepDataModel> Items { get; set; }

    /// <summary>
    /// Gets or sets the query string values.
    /// </summary>
    [JsonProperty("qsItems")]
    public Dictionary<string, string> QueryStringItems { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the external configuration information if the configurator is connected to an external API.
    /// </summary>
    [JsonProperty("externalConfiguration")]
    public ExternalConfigurationModel ExternalConfiguration { get; set; }
}