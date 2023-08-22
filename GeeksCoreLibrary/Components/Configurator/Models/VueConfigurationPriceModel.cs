using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueConfigurationPriceModel
{
    /// <summary>
    /// Gets or sets the customer price of the configuration.
    /// </summary>
    [JsonProperty("customerPrice")]
    public decimal CustomerPrice { get; set; }

    /// <summary>
    /// Gets or sets the starting price of the configuration.
    /// </summary>
    [JsonProperty("fromPrice")]
    public decimal FromPrice { get; set; }

    /// <summary>
    /// Gets or sets the calculated purchase price of the configuration.
    /// </summary>
    [JsonProperty("purchasePrice")]
    public decimal PurchasePrice { get; set; }
}