using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueStepDependencyModel
{
    /// <summary>
    /// Gets or sets the dependency's step name.
    /// </summary>
    public string StepName { get; set; }
    
    /// <summary>
    /// Gets or sets the values the dependency should have in order for the step that depends on this dependency to be visible.
    /// </summary>
    public IEnumerable<string> Values { get; set; }
}