using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Configurator.Models;

internal class ConfiguratorVueSettingsModel
{
    #region Tab DataSource properties

    /// <summary>
    /// The name of the current configurator, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    [DefaultValue("{configurator}")]
    public string ConfiguratorName { get; }

    #endregion

    #region Tab Layout properties
    
    /// <summary>
    /// The HTML for a main step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    [DefaultValue(@"<div class=""configurator-step config-level-0"">
        {mainStepContent}
    </div>")]
    public string MainStepHtml { get; }

    /// <summary>
    /// The HTML for a step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    [DefaultValue(@"<div class=""configurator-step config-level-1"">
        {stepContent}
    </div>")]
    public string StepHtml { get; }

    /// <summary>
    /// The HTML for a sub step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    [DefaultValue(@"<div class=""configurator-step config-level-2"">
        {subStepContent}
    </div>")]
    public string SubStepHtml { get; }

    /// <summary>
    /// The HTML for the summary of the configurator, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    public string SummaryHtml { get; }

    /// <summary>
    /// The HTML for the final summary of the configurator, shown on the overview page at the end, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Vue" />.
    /// </summary>
    [DefaultValue(@"<div id=""summary"">
        <div class=""summary-step-1"">
            <h2>Step 1</h2>
            Question 1: {{ getChoiceData(""question1"").name }}
            Question 2: {{ getChoiceData(""question2"").name }}
        </div>
        <div class=""summary-step-2"">
            <h2>Step 2</h2>
            Question 3: {{ getChoiceData(""question3"").name }}
            Question 4: {{ getChoiceData(""question4"").name }}
        </div>
    </div>")]
    public string FinalSummaryHtml { get; }
    
    #endregion
}