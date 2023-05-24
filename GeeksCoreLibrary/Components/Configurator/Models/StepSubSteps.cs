using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class StepSubSteps
{
    /// <summary>
    /// Gets or sets the position of the step.
    /// </summary>
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets the positions of the sub steps. The key is the sub step's name and the value is the sub step's position.
    /// </summary>
    public Dictionary<string, string> SubStepPositions { get; set; }
}