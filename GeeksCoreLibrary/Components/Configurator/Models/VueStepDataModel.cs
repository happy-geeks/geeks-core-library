using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDataModel
{
    /// <summary>
    /// Gets or sets the position of the step as a dash separated string.
    /// </summary>
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets the name of the step.
    /// </summary>
    public string StepName { get; set; }

    /// <summary>
    /// Gets or sets the step's options. While it can contain any number of properties, the following are required: "id", "value", "name".
    /// </summary>
    public IEnumerable<Dictionary<string, object>> Options { get; set; }

    /// <summary>
    /// Gets or sets the step's dependencies. The dependencies determine the step's visibility.
    /// </summary>
    public IEnumerable<VueStepDependencyModel> Dependencies { get; set; }
}