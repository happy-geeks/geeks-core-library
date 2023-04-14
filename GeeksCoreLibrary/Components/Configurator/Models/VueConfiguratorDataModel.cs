using System.Collections.Generic;

namespace GeeksCoreLibrary.Components.Configurator.Models;

public class VueConfiguratorDataModel
{
    /// <summary>
    /// Gets or sets the steps data.
    /// </summary>
    public IEnumerable<VueStepDataModel> StepsData { get; set; }
}