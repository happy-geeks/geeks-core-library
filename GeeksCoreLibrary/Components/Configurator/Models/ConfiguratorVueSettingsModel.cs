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
    /// The HTML for for a main step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='configurator-step config-level-0'>
        {mainStepContent}
    </div>")]
    public string MainStepHtml { get; }

    /// <summary>
    /// The HTML for for a step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='configurator-step config-level-1'>
        {stepContent}
    </div>")]
    public string StepHtml { get; }

    /// <summary>
    /// The HTML for for a sub step, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='configurator-step config-level-2'>
        {subStepContent}
    </div>")]
    public string SubStepHtml { get; }

    /// <summary>
    /// The HTML for the summary of the configurator, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='jjl_summary'>
        <div class='jjl_summary_template'>{progress_template}</div>
        <div class='jjl_summary_mainstep_template'>{progress_step_template}</div>
        <div class='jjl_summary_step_template'>{progress_substep_template}</div>
    </div>")]
    public string SummaryHtml { get; }

    /// <summary>
    /// The HTML for the final summary of the configurator, shown on the overview page at the end, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='jjl_finalsummary'>
        <div class='jjl_finalsummary_template'>{summary_template}</div>
        <div class='jjl_finalsummary_mainstep_template'>{summary_mainstep_template}</div>
        <div class='jjl_finalsummary_step_template'>{summary_step_template}</div>
    </div>")]
    public string FinalSummaryHtml { get; }

    /// <summary>
    /// The first part of HTML for showing the progress on mobile devices, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='jjl_summary_pre'>
        <div class='jjl_summary_pre_template'>{progress_pre_template}</div>
        <div class='jjl_summary_pre_mainstep_template'>{progress_pre_step_template}</div>
        <div class='jjl_summary_pre_step_template'>{progress_pre_substep_template}</div>
    </div>")]
    public string MobilePreProgressHtml { get; }

    /// <summary>
    /// The last part of HTML for showing the progress on mobile devices, when the <see cref="ConfiguratorCmsSettingsModel.ComponentMode"/> is <see cref="Configurator.ComponentModes.Default" />.
    /// </summary>
    [DefaultValue(@"<div class='jjl_summary_post'>
        <div class='jjl_summary_post_template'>{progress_post_template}</div>
        <div class='jjl_summary_post_mainstep_template'>{progress_post_step_template}</div>
        <div class='jjl_summary_post_step_template'>{progress_post_substep_template}</div>
    </div>")]
    public string MobilePostProgressHtml { get; }
    
    #endregion
}