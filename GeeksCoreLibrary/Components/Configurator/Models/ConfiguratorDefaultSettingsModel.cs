using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using System.ComponentModel;

namespace GeeksCoreLibrary.Components.Configurator.Models
{
    internal class ConfiguratorDefaultSettingsModel 
    {
        #region Tab DataSource properties
        /// <summary>
        /// The name of the current configurator, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue("{configurator}")]
        public string ConfiguratorName{ get; } = "";

        /// <summary>
        /// The query that retreived the basic data for the configurator, such as the templates for all steps, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT 
            #SUMMARY
            IFNULL(configvalues_duplicate.summary_template,configvalues.summary_template) AS summary_template
            ,IFNULL(configvalues_duplicate.summary_mainstep_template,configvalues.summary_mainstep_template) AS summary_mainstep_template
            ,IFNULL(configvalues_duplicate.summary_step_template,configvalues.summary_step_template) AS summary_step_template

            #PROGRESS
            ,IFNULL(configvalues_duplicate.pre_progress_template,configvalues.pre_progress_template) AS progress_pre_template
            ,IFNULL(configvalues_duplicate.pre_progress_step_template,configvalues.pre_progress_step_template) AS progress_pre_step_template
            ,IFNULL(configvalues_duplicate.pre_progress_substep_template,configvalues.pre_progress_substep_template) AS progress_pre_substep_template

            ,IFNULL(configvalues_duplicate.post_progress_template,configvalues.post_progress_template) AS progress_post_template
            ,IFNULL(configvalues_duplicate.post_progress_step_template,configvalues.post_progress_step_template) AS progress_post_step_template
            ,IFNULL(configvalues_duplicate.post_progress_substep_template,configvalues.post_progress_substep_template) AS progress_post_substep_template

            ,IFNULL(configvalues_duplicate.progress_template,configvalues.progress_template) AS progress_template
            ,IFNULL(configvalues_duplicate.progress_step_template,configvalues.progress_step_template) AS progress_step_template
            ,IFNULL(configvalues_duplicate.progress_substep_template,configvalues.progress_substep_template) AS progress_substep_template

            #CONFIGURATOR
	        ,configurator.name
	        ,IFNULL(configvalues_duplicate.template,configvalues.template) AS template
            ,configvalues.price_calculation_query
            ,configvalues.deliverytime_query
            ,configvalues.custom_param_name
            ,configvalues.custom_param_dependencies
            ,IFNULL(configvalues.custom_param_query, '') AS custom_param_query
            ,configvalues.pre_render_steps_query

            #MAIN STEPS
	        #,IF(mainstepvalues.step_template IS NULL OR TRIM(mainstepvalues.step_template)='',IFNULL(configvalues_duplicate.step_template,configvalues.step_template),mainstepvalues.step_template) AS mainstep_template
            ,IF(IFNULL(mainstepvalues_duplicate.step_template,mainstepvalues.step_template) IS NULL OR TRIM(IFNULL(mainstepvalues_duplicate.step_template,mainstepvalues.step_template))='',IFNULL(configvalues_duplicate.step_template,configvalues.step_template),IFNULL(mainstepvalues_duplicate.step_template,mainstepvalues.step_template)) AS mainstep_template
	        ,mainsteps.name AS mainstepname
            ,mainstepvalues.free_content1 AS mainstep_free_content1
            ,mainstepvalues.free_content2 AS mainstep_free_content2
            ,mainstepvalues.free_content3 AS mainstep_free_content3
            ,mainstepvalues.free_content4 AS mainstep_free_content4
            ,mainstepvalues.free_content5 AS mainstep_free_content5
            ,mainstepvalues.step_options AS mainstep_options

            #STEPS
	        ,IF(IFNULL(stepvalues_duplicate.step_template,stepvalues.step_template) IS NULL OR TRIM(IFNULL(stepvalues_duplicate.step_template,stepvalues.step_template))='',IFNULL(configvalues_duplicate.default_step_template,configvalues.default_step_template),IFNULL(stepvalues_duplicate.step_template,stepvalues.step_template)) AS step_template
	        ,steps.name AS stepname
	        ,IFNULL(stepvalues_duplicate.values_template,stepvalues.values_template) AS values_template
	        ,stepvalues.DataSource
	        ,stepvalues.custom_query
            ,stepvalues.own_data_values
	        ,stepvalues.fixed_valuelist
            ,stepvalues.DataSource_connectedtype
            ,stepvalues.DataSource_dataselectorid
            ,IF(IFNULL(stepvalues.variable_name,'')='',steps.seoname,stepvalues.variable_name) AS variable_name
            ,stepvalues.free_content1 AS step_free_content1
            ,stepvalues.free_content2 AS step_free_content2
            ,stepvalues.free_content3 AS step_free_content3
            ,stepvalues.free_content4 AS step_free_content4
            ,stepvalues.free_content5 AS step_free_content5
	        ,stepvalues.step_options AS step_options
            ,stepvalues.DataSource_connectedid
            ,stepvalues.isrequired
            ,stepvalues.check_connectedid

            #SUBSTEPS
	        ,substeps.name AS substepname
	        ,IFNULL(substepvalues_duplicate.step_template,substepvalues.step_template) AS substep_template
	        ,IFNULL(substepvalues_duplicate.values_template,substepvalues.values_template) AS substep_values_template
	        ,substepvalues.DataSource AS substep_DataSource
	        ,substepvalues.custom_query AS substep_custom_query
            ,substepvalues.own_data_values AS substep_own_data_values
	        ,substepvalues.fixed_valuelist AS substep_fixed_valuelist
            ,substepvalues.DataSource_connectedtype AS substep_DataSource_connectedtype
            ,substepvalues.DataSource_dataselectorid AS substep_DataSource_dataselectorid
            ,IF(IFNULL(substepvalues.variable_name,'')='',substeps.seoname,substepvalues.variable_name) AS substep_variable_name
            ,substepvalues.DataSource_connectedid AS substep_DataSource_connectedid
            ,substepvalues.isrequired AS substep_isrequired
            ,substepvalues.check_connectedid AS substep_check_connectedid
	        ,substepvalues.step_options AS substep_options

            #Get the applied regex
            ,IF(IFNULL(mainstepvalues.urlregex,'')<>'',mainstepvalues.urlregex,
                IF(IFNULL(stepvalues.urlregex,'')<>'',stepvalues.urlregex,IFNULL(substepvalues.urlregex,''))) AS urlregex

        FROM easy_items configurator
        JOIN easy_configuratorsetup configvalues ON configvalues.itemid = configurator.id
        LEFT JOIN easy_configuratorsetup configvalues_duplicate ON configvalues_duplicate.itemid=configvalues.duplicatelayoutfrom
        JOIN easy_items mainsteps ON mainsteps.moduleid=320 AND mainsteps.`level`=2 AND mainsteps.parent_id=configurator.id AND mainsteps.published=1

        LEFT JOIN easy_configuratorsetup mainstepvalues ON mainstepvalues.itemid = mainsteps.id
        LEFT JOIN easy_configuratorsetup mainstepvalues_duplicate ON mainstepvalues_duplicate.itemid=mainstepvalues.duplicatelayoutfrom

        LEFT JOIN easy_items steps ON steps.moduleid=320 AND steps.`level`=3 AND steps.parent_id=mainsteps.id AND steps.published=1
        LEFT JOIN easy_configuratorsetup stepvalues ON stepvalues.itemid = steps.id
        LEFT JOIN easy_configuratorsetup stepvalues_duplicate ON stepvalues_duplicate.itemid=stepvalues.duplicatelayoutfrom
        LEFT JOIN easy_items substeps ON substeps.moduleid=320 AND substeps.`level`=4 AND substeps.parent_id=steps.id AND substeps.published=1
        LEFT JOIN easy_configuratorsetup substepvalues ON substepvalues.itemid = substeps.id
        LEFT JOIN easy_configuratorsetup substepvalues_duplicate ON substepvalues_duplicate.itemid=substepvalues.duplicatelayoutfrom
        WHERE 
	        configurator.moduleid=320 AND
	        configurator.`level`=1 AND
	        configurator.`name`= ?name
        ORDER BY mainsteps.volgnr, steps.volgnr")]
        public string MainConfiguratorDataQuery{ get; } = "";

        /// <summary>
        /// The query that retreives the data from product categories, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT *
        FROM shop_category
        WHERE parent_cat_id = ?connectedId
        ORDER BY priority DESC, name ASC")]
        public string ProductCategoriesQuery{ get; } = "";

        /// <summary>
        /// The query that retreives the data from products, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT p.*, sc.*
        FROM shop_products p
        JOIN shop_pro_cat spc ON spc.product_id = p.id AND spc.cat_id = ?connectedId
        JOIN shop_category sc ON sc.id = spc.cat_id
        ORDER BY p.priority DESC, p.name ASC")]
        public string ProductsQuery{ get; } = "";

        /// <summary>
        /// The query that retreives the data from product variants, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT *
        FROM shop_variant
        WHERE product_id = ?connectedId
        ORDER BY priority DESC, name ASC")]
        public string ProductVariantsQuery{ get; } = "";

        /// <summary>
        /// The query that retreives the data from products that are connected to the current product, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT p.*, sc.*, k.*
        FROM shop_koppeling k
        JOIN shop_products p ON p.id = k.prod_source
        LEFT JOIN shop_pro_cat spc ON spc.product_id = p.id
        LEFT JOIN shop_category sc ON sc.id = spc.cat_id
        WHERE k.prod_dest = ?connectedId
        AND k.koppeltype = ?connectedType
        GROUP BY p.id
        ORDER BY p.priority DESC, p.name ASC")]
        public string ConnectedProductsOnProductQuery{ get; } = "";

        /// <summary>
        /// The query that retreives the data from products that are connected to the current category, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        /// </summary>
        [DefaultValue(@"SELECT p.*, sc.*, k.*
        FROM shop_koppeling k
        JOIN shop_products p ON p.id = k.prod_source
        LEFT JOIN shop_pro_cat spc ON spc.product_id = p.id
        LEFT JOIN shop_category sc ON sc.id = spc.cat_id
        WHERE k.map_dest = ?connectedId
        AND k.koppeltype = ?connectedType
        GROUP BY p.id
        ORDER BY p.priority DESC, p.name")]
        public string ConnectedProductsOnCategoryQuery{ get; } = "";

        #endregion
        #region Tab Layout properties

        /// <summary>
        ///  The HTML for for a main step, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div class='jjl_configurator_mainstep' id='jjl_configurator_step-{mainStepCount}' data-jconfigurator-name='{currentMainStepName}' data-content-id='{contentId}' data-jconfigurator-step-options='{mainstep_options}'>
            {mainStepContent}
        </div>")]
        public string MainStepHtml{ get; } = "";

        /// <summary>
        ///  The HTML for for a step, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div style='{style}' data-jconfigurator-is-required='{isrequired}' class='jjl_configurator_step' data-jconfigurator-name='{stepname}' id='jjl_configurator_step-{mainStepNumber}-{stepNumber}' data-jconfigurator-step-options='{mainstep_options}' {dependsOn}>
            {stepContent}
        </div>")]
        public string StepHtml{ get; } = "";

        /// <summary>
        ///  The HTML for for a sub step, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div data-jconfigurator-is-required='{isrequired}' class='jjl_configurator_substep' id='jjl_configurator_substep-{mainStepNumber}-{stepNumber}-{subStepNumber}' data-jconfigurator-step-options='{mainstep_options}' {dependsOn}>
            {subStepContent}
        </div>")]
        public string SubStepHtml{ get; } = "";

        /// <summary>
        ///  The HTML for the summary of the configurator, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div class='jjl_summary'>
            <div class='jjl_summary_template'>{progress_template}</div>
            <div class='jjl_summary_mainstep_template'>{progress_step_template}</div>
            <div class='jjl_summary_step_template'>{progress_substep_template}</div>
        </div>")]
        public string SummaryHtml{ get; } = "";

        /// <summary>
        ///  The HTML for the final summary of the configurator, shown on the overview page at the end, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div class='jjl_finalsummary'>
            <div class='jjl_finalsummary_template'>{summary_template}</div>
            <div class='jjl_finalsummary_mainstep_template'>{summary_mainstep_template}</div>
            <div class='jjl_finalsummary_step_template'>{summary_step_template}</div>
        </div>")]
        public string FinalSummaryHtml{ get; } = "";

        /// <summary>
        ///  The first part of HTML for showing the progress on mobile devices, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div class='jjl_summary_pre'>
            <div class='jjl_summary_pre_template'>{progress_pre_template}</div>
            <div class='jjl_summary_pre_mainstep_template'>{progress_pre_step_template}</div>
            <div class='jjl_summary_pre_step_template'>{progress_pre_substep_template}</div>
        </div>")]
        public string MobilePreProgressHtml{ get; } = "";

        /// <summary>
        ///  The last part of HTML for showing the progress on mobile devices, when the <see cref="ComponentMode"/> is <see cref="ConfiguratorComponentModes.Default" />.
        ///  </summary>
        [DefaultValue(@"<div class='jjl_summary_post'>
            <div class='jjl_summary_post_template'>{progress_post_template}</div>
            <div class='jjl_summary_post_mainstep_template'>{progress_post_step_template}</div>
            <div class='jjl_summary_post_step_template'>{progress_post_substep_template}</div>
        </div>")]
        public string MobilePostProgressHtml{ get; } = "";
        #endregion
    }
}
