using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueConfiguratorDataModel
{
    /// <summary>
    /// Gets or sets the configurator ID.
    /// </summary>
    [JsonProperty("configuratorId")]
    public ulong ConfiguratorId { get; set; }

    /// <summary>
    /// Gets or sets the steps data.
    /// </summary>
    [JsonProperty("stepsData")]
    public List<VueStepDataModel> StepsData { get; set; }

    /// <summary>
    /// Gets or sets the price calculation query.
    /// </summary>
    [JsonIgnore]
    public string PriceCalculationQuery { get; set; }

    /// <summary>
    /// Gets or sets the delivery time calculation query.
    /// </summary>
    [JsonIgnore]
    public string DeliveryTimeCalculationQuery { get; set; }
}