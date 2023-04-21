using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDependencyModel
{
    /// <summary>
    /// Gets or sets the dependency's step name.
    /// </summary>
    [JsonProperty("stepName")]
    public string StepName { get; set; }
    
    /// <summary>
    /// Gets or sets the values the dependency should have in order for the step that depends on this dependency to be visible.
    /// </summary>
    [JsonProperty("values")]
    public IEnumerable<string> Values { get; set; }
}