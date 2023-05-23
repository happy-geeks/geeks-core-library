using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueConfiguratorDataModel
{
    /// <summary>
    /// Gets or sets the steps data.
    /// </summary>
    [JsonProperty("stepsData")]
    public IList<VueStepDataModel> StepsData { get; set; }
}