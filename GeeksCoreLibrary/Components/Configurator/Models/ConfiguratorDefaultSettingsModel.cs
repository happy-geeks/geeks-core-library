using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Configurator.Models;

internal class ConfiguratorDefaultSettingsModel
{
    #region Tab DataSource properties

    [DefaultValue("{configurator}")]
    internal string ConfiguratorName { get; }

    #endregion

    #region Tab Layout properties

    [DefaultValue(@"<div class='jjl_configurator_mainstep' id='jjl_configurator_step-{mainStepCount}' data-jconfigurator-name='{currentMainStepName}' data-content-id='{contentId}' data-jconfigurator-step-options='{mainstep_options}'>
            {mainStepContent}
        </div>")]
    internal string MainStepHtml { get; } = "";

    [DefaultValue(@"<div style='{style}' data-jconfigurator-is-required='{isrequired}' class='jjl_configurator_step' data-jconfigurator-name='{stepname}' id='jjl_configurator_step-{mainStepNumber}-{stepNumber}' data-jconfigurator-step-options='{mainstep_options}' {dependsOn}>
            {stepContent}
        </div>")]
    internal string StepHtml { get; } = "";

    [DefaultValue(@"<div data-jconfigurator-is-required='{isrequired}' class='jjl_configurator_substep' id='jjl_configurator_substep-{mainStepNumber}-{stepNumber}-{subStepNumber}' data-jconfigurator-step-options='{mainstep_options}' {dependsOn}>
            {subStepContent}
        </div>")]
    internal string SubStepHtml { get; } = "";

    [DefaultValue(@"<div class='jjl_summary'>
            <div class='jjl_summary_template'>{progress_template}</div>
            <div class='jjl_summary_mainstep_template'>{progress_step_template}</div>
            <div class='jjl_summary_step_template'>{progress_substep_template}</div>
        </div>")]
    internal string SummaryHtml { get; } = "";

    [DefaultValue(@"<div class='jjl_finalsummary'>
            <div class='jjl_finalsummary_template'>{summary_template}</div>
            <div class='jjl_finalsummary_mainstep_template'>{summary_mainstep_template}</div>
            <div class='jjl_finalsummary_step_template'>{summary_step_template}</div>
        </div>")]
    internal string FinalSummaryHtml { get; } = "";

    [DefaultValue(@"<div class='jjl_summary_pre'>
            <div class='jjl_summary_pre_template'>{progress_pre_template}</div>
            <div class='jjl_summary_pre_mainstep_template'>{progress_pre_step_template}</div>
            <div class='jjl_summary_pre_step_template'>{progress_pre_substep_template}</div>
        </div>")]
    internal string MobilePreProgressHtml { get; } = "";

    [DefaultValue(@"<div class='jjl_summary_post'>
            <div class='jjl_summary_post_template'>{progress_post_template}</div>
            <div class='jjl_summary_post_mainstep_template'>{progress_post_step_template}</div>
            <div class='jjl_summary_post_step_template'>{progress_post_substep_template}</div>
        </div>")]
    internal string MobilePostProgressHtml { get; } = "";

    #endregion
}