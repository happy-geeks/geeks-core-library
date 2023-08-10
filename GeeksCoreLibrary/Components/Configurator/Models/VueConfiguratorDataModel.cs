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
    /// Gets or sets whether the summary step should be added to the progress bar.
    /// </summary>
    [JsonProperty("configuratorName")]
    public bool ShowSummaryProgressBarStep { get; set; }

    /// <summary>
    /// Gets or sets the summary step name.
    /// </summary>
    [JsonProperty("summaryStepName")]
    public string SummaryStepName { get; set; }

    /// <summary>
    /// Gets or sets if the configurator needs to start the configuration at an extern API when starting the configurator.
    /// </summary>
    [JsonProperty("startExternalConfigurationOnStart")]
    public bool StartExternalConfigurationOnStart { get; set; }
    
    /// <summary>
    /// Gets or sets the external configuration information.
    /// </summary>
    [JsonProperty("externalConfiguration")]
    public ExternalConfigurationModel ExternalConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the steps data.
    /// </summary>
    [JsonProperty("stepsData")]
    public List<VueStepDataModel> StepsData { get; set; }

    #region Server-side only properties

    /// <summary>
    /// Gets or sets the main HTML template.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string MainTemplate { get; set; }

    /// <summary>
    /// Gets or sets the progress HTML template.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string ProgressBarTemplate { get; set; }

    /// <summary>
    /// Gets or sets the progress step HTML template.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string ProgressBarStepTemplate { get; set; }

    /// <summary>
    /// Gets or sets the progress HTML template.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string ProgressTemplate { get; set; }

    /// <summary>
    /// Gets or sets the summary HTML template.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string SummaryTemplate { get; set; }

    /// <summary>
    /// Gets or sets the price calculation query.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string PriceCalculationQuery { get; set; }

    /// <summary>
    /// Gets or sets the delivery time calculation query.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it's not needed in the client-side.
    /// </remarks>
    [JsonIgnore]
    public string DeliveryTimeCalculationQuery { get; set; }

    #endregion
}