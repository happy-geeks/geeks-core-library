using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDataModel
{
    /// <summary>
    /// Gets or sets the position of the step as a dash separated string.
    /// </summary>
    [JsonProperty("position")]
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets the name of the step.
    /// </summary>
    [JsonProperty("stepName")]
    public string StepName { get; set; }

    /// <summary>
    /// Gets or sets the step's options. While it can contain any number of properties, the following are required: "id", "value", "name".
    /// </summary>
    [JsonProperty("options")]
    public IEnumerable<Dictionary<string, object>> Options { get; set; }

    /// <summary>
    /// Gets or sets the step's dependencies. The dependencies determine the step's visibility.
    /// </summary>
    [JsonProperty("dependencies")]
    public IEnumerable<VueStepDependencyModel> Dependencies { get; set; }

    /// <summary>
    /// Gets or sets the query that retrieves the step data and step options data.
    /// </summary>
    /// <remarks>
    /// This property is not serialized to JSON because it is not needed in the client-side and because it would be a security risk.
    /// </remarks>
    [JsonIgnore]
    public string DataQuery { get; set; }
}