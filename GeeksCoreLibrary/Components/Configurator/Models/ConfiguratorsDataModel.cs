using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    public class ConfiguratorsDataModel
    {

        public ulong ConfiguratorId { get; set; }
        public ulong MainStepId { get; set; }
        public ulong StepId{ get; set; }
        public ulong SubStepId { get; set; }
        public ulong DuplicateConfiguratorId { get; set; }
        public ulong DuplicateMainStepId { get; set; }
        public ulong DuplicateStepId { get; set; }
        public ulong DuplicateSubStepId { get; set; }

        public string MainStepName { get; set; }
        public string StepName { get; set; }
        public string SubStepName { get; set; }

        public string MainStepTemplate { get; set; }

        [Column("template")]
        public string Template { get; set; }
        [Column("step_template")]
        public string StepTemplate { get; set; }
        [Column("progress_template")]
        public string ProgressTemplate { get; set; }
        [Column("progress_step_template")]
        public string ProgressStepTemplate { get; set; }
        [Column("progress_substep_template")]
        public string ProgressSubStepTemplate { get; set; }
        [Column("pre_progress_template")]
        public string PreProgressTemplate { get; set; }
        [Column("pre_progress_step_template")]
        public string PreProgressStepTemplate { get; set; }
        [Column("pre_progress_substep_template")]
        public string PreProgressSubStepTemplate { get; set; }
        [Column("post_progress_template")]
        public string PostProgressTemplate { get; set; }
        [Column("post_progress_step_template")]
        public string PostProgressStepTemplate { get; set; }
        [Column("post_progress_substep_template")]
        public string PostProgressSubStepTemplate { get; set; }

        [Column("custom_query")]
        public string CustomQuery { get; set; }
        [Column("fixed_valuelist")]
        public string FixedValueList { get; set; }
        [Column("mainstep_values_template")]
        public string MainStepValuesTemplate { get; set; }

        [Column("values_template")]
        public string ValuesTemplate { get; set; }
        [Column("substep_values_template")]
        public string SubStepValuesTemplate { get; set; }
        
        [Column("variable_name")]
        public string VariableName { get; set; }
        [Column("default_step_template")]
        public string DefaultStepTemplate { get; set; }


        [Column("min_value")]
        public string MinValue { get; set; }
        [Column("max_value")]
        public string MaxValue { get; set; }
        [Column("datasource_connectedid")]
        public string DatasourceConnectedId { get; set; }
        [Column("isrequired")]
        public string IsRequired { get; set; }
        [Column("deliverytime_query")]
        public string DeliveryTimeQuery { get; set; }
        [Column("price_calculation_query")]
        public string PriceCalculationQuery { get; set; }
        [Column("check_connectedid")]
        public string CheckConnectedId { get; set; }
        [Column("duplicatelayoutfrom")]
        public int DuplicateLayoutFrom { get; set; }
        [Column("summary_template")]
        public string SummaryTemplate { get; set; }
        [Column("summary_mainstep_template")]
        public string SummaryMainStepTemplate { get; set; }
        [Column("summary_step_template")]
        public string SummaryStepTemplate { get; set; }
        [Column("urlregex")]
        public string UrlRegex { get; set; }
        [Column("own_data_values")]
        public string OwnDataValues { get; set; }

        [Column("step_options")]
        public string StepOptions { get; set; }
        [Column("custom_param_name")]
        public string CustomParamName { get; set; }
        [Column("custom_param_query")]
        public string CustomParamQuery { get; set; }
        [Column("custom_param_dependencies")]
        public string CustomParamDependencies { get; set; }
        [Column("pre_render_steps_query")]
        public string PreRenderStepsQuery { get; set; }

        [Column("configurator_free_content1")]
        public string ConfiguratorFreeContent1 { get; set; }
        [Column("configurator_free_content2")]
        public string ConfiguratorFreeContent2 { get; set; }
        [Column("configurator_free_content3")]
        public string ConfiguratorFreeContent3 { get; set; }
        [Column("configurator_free_content4")]
        public string ConfiguratorFreeContent4 { get; set; }
        [Column("configurator_free_content5")]
        public string ConfiguratorFreeContent5 { get; set; }

        [Column("mainstep_free_content1")]
        public string MainStepFreeContent1 { get; set; }
        [Column("mainstep_free_content2")]
        public string MainStepFreeContent2 { get; set; }
        [Column("mainstep_free_content3")]
        public string MainStepFreeContent3 { get; set; }
        [Column("mainstep_free_content4")]
        public string MainStepFreeContent4 { get; set; }
        [Column("mainstep_free_content5")]
        public string MainStepFreeContent5 { get; set; }

        [Column("step_free_content1")]
        public string StepFreeContent1 { get; set; }
        [Column("step_free_content2")]
        public string StepFreeContent2 { get; set; }
        [Column("step_free_content3")]
        public string StepFreeContent3 { get; set; }
        [Column("step_free_content4")]
        public string StepFreeContent4 { get; set; }
        [Column("step_free_content5")]
        public string StepFreeContent5 { get; set; }

        [Column("substep_free_content1")]
        public string SubSteFreeContent1 { get; set; }
        [Column("substep_free_content2")]
        public string SubSteFreeContent2 { get; set; }
        [Column("substep_free_content3")]
        public string SubSteFreeContent3 { get; set; }
        [Column("substep_free_content4")]
        public string SubSteFreeContent4 { get; set; }
        [Column("substep_free_content5")]
        public string SubStepFreeContent5 { get; set; }

        [Column("mainsteps_datasource_connectedtype")]
        public string  MainStepDatasourceConnectedType { get; set; }
        [Column("datasource_connectedtype")]
        public string DatasourceConnectedType { get; set; }
        [Column("substep_datasource_connectedtype")]
        public string SubStepDatasourceConnectedType { get; set; }

        [Column("mainsteps_datasource")]
        public string MainStepDatasource { get; set; }
        [Column("datasource")]
        public string Datasource { get; set; }
        [Column("substep_datasource")]
        public string SubStepDatasource { get; set; }

        [Column("mainsteps_datasource_connectedid")]
        public int MainStepDatasourceDataselectorId { get; set; }
        [Column("datasource_dataselectorid")]
        public int DatasourceDataselectorId { get; set; }
        [Column("substep_datasource_connectedid")]
        public int SubStepDatasourceDataselectorId { get; set; }
    }
}
