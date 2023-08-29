using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GeeksCoreLibrary.Components.Configurator.Services
{
    public class ConfiguratorsService : IConfiguratorsService, IScopedService
    {
        private readonly ILogger<ConfiguratorsService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly ILanguagesService languagesService;
        private readonly ITemplatesService templatesService;

        private readonly string[] queryFields =
        {
            "configuratorId", "mainStepId", "stepId", "subStepId", "duplicateConfiguratorId", "duplicateMainStepId", "duplicateStepId", "duplicateSubStepId", "summary_template", "summary_mainstep_template", "summary_step_template", "progress_pre_template", "progress_pre_step_template", "progress_pre_substep_template", "progress_post_template", "progress_post_step_template",
            "progress_post_substep_template", "progress_template", "progress_step_template", "progress_substep_template", "name", "configurator_free_content1", "configurator_free_content2", "configurator_free_content3", "configurator_free_content4",
            "configurator_free_content5", "template", "price_calculation_query", "deliverytime_query", "custom_param_name", "custom_param_dependencies", "custom_param_query", "pre_render_steps_query", "mainstep_template", "mainstepname", "mainsteps_values_template", "mainsteps_datasource", "mainsteps_custom_query", "mainsteps_own_data_values", "mainsteps_fixed_valuelist",
            "mainsteps_datasource_connectedtype", "mainsteps_datasource_connectedid", "mainsteps_isrequired", "mainsteps_check_connectedid", "mainstep_free_content1", "mainstep_free_content2", "mainstep_free_content3",
            "mainstep_free_content4", "mainstep_free_content5", "mainstep_variable_name", "step_template", "stepname", "values_template", "datasource", "custom_query", "own_data_values", "fixed_valuelist", "datasource_connectedtype", "variable_name", "step_free_content1", "step_free_content2", "step_free_content3", "step_free_content4", "step_free_content5", "datasource_connectedid", "isrequired", "check_connectedid",
            "substepname", "substep_template", "substep_values_template", "substep_datasource",
            "substep_custom_query", "substep_own_data_values", "substep_fixed_valuelist", "substep_datasource_connectedtype", "substep_variable_name", "substep_datasource_connectedid", "substep_isrequired", "substep_check_connectedid", "substep_free_content1", "substep_free_content2", "substep_free_content3", "substep_free_content4", "substep_free_content5", "urlregex",
            "configurator_step_template"
        };

        private readonly List<(string prefix, string fieldName)> configuratorFields = new()
        {
            ("", "name"), ("", "summary_template"), ("", "summary_mainstep_template"), ("", "summary_step_template"), ("", "progress_pre_template"), ("", "progress_pre_step_template"), ("", "progress_pre_substep_template"), ("", "progress_post_template"), ("", "progress_post_step_template"), ("", "progress_post_substep_template"), ("", "progress_template"), ("", "progress_step_template"),
            ("", "progress_substep_template"), ("configurator_", "free_content1"), ("configurator_", "free_content2"), ("configurator_", "free_content3"), ("configurator_", "free_content4"), ("configurator_", "free_content5"), ("", "template"), ("", "deliverytime_query"), ("", "custom_param_name"), ("", "custom_param_dependencies"), ("", "custom_param_query"), ("", "pre_render_steps_query"),
            ("configurator_", "step_template"), ("", "price_calculation_query")
        };

        private readonly List<(string prefix, string fieldName)> mainStepFields = new()
        {
            ("main", "step_template"), ("", "mainstepname"), ("mainsteps_", "values_template"), ("mainsteps_", "datasource"), ("mainsteps_", "custom_query"), ("mainsteps_", "own_data_values"), ("mainsteps_", "fixed_valuelist"), ("mainsteps_", "datasource_connectedtype"), ("mainstep_", "variable_name"), ("mainsteps_", "datasource_connectedid"), ("mainsteps_", "isrequired"), ("mainsteps_", "check_connectedid"),
            ("mainstep_", "free_content1"), ("mainstep_", "free_content2"), ("mainstep_", "free_content3"), ("mainstep_", "free_content4"), ("mainstep_", "free_content5")
        };

        private readonly List<(string prefix, string fieldName)> stepFields = new()
        {
            ("", "step_template"), ("", "stepname"), ("", "values_template"), ("", "datasource"), ("", "custom_query"), ("", "own_data_values"), ("", "fixed_valuelist"), ("", "datasource_connectedtype"), ("", "variable_name"), ("", "datasource_connectedid"), ("", "isrequired"), ("", "check_connectedid"), ("step_", "free_content1"), ("step_", "free_content2"), ("step_", "free_content3"),
            ("step_", "free_content4"), ("step_", "free_content5")
        };

        private readonly List<(string prefix, string fieldName)> subStepFields = new()
        {
            ("sub", "step_template"), ("", "substepname"), ("substep_", "values_template"), ("substep_", "datasource"), ("substep_", "custom_query"), ("substep_", "own_data_values"), ("substep_", "fixed_valuelist"), ("substep_", "datasource_connectedtype"), ("substep_", "variable_name"), ("substep_", "datasource_connectedid"), ("substep_", "isrequired"), ("substep_", "check_connectedid"),
            ("substep_", "free_content1"), ("substep_", "free_content2"), ("substep_", "free_content3"), ("substep_", "free_content4"), ("substep_", "free_content5")
        };

        private readonly Regex dependencyValuesRegex = new(@"\((?<values>[^\)]+)\)", RegexOptions.Compiled);

        public ConfiguratorsService(ILogger<ConfiguratorsService> logger,
            IDatabaseConnection databaseConnection,
            IObjectsService objectsService,
            IWiserItemsService wiserItemsService,
            IStringReplacementsService stringReplacementsService,
            ILanguagesService languagesService,
            ITemplatesService templatesService)
        {
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.languagesService = languagesService;
            this.templatesService = templatesService;
        }

        /// <inheritdoc />
        public async Task<DataTable> GetConfiguratorDataAsync(string name)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("name", name);
            // Get all connected steps/substeps
            var query = @$"
               SELECT 
                    configurator.id AS configuratorId, 
                    mainStep.id AS mainStepId, 
                    step.id AS stepId,
                    IFNULL(subStep.id, '0') AS subStepId,
                    IFNULL(duplicateConfigurator.`value`, '0') AS duplicateConfiguratorId,
                    IFNULL(duplicateMainStep.`value`, '0') AS duplicateMainStepId,
                    IFNULL(duplicateStep.`value`, '0') AS duplicateStepId,
                    IFNULL(duplicateSubStep.`value`, '0') AS duplicateSubStepId
                FROM {WiserTableNames.WiserItem} configurator
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateConfigurator ON duplicateConfigurator.item_id = configurator.id AND duplicateConfigurator.`key` = '{Constants.DuplicateLayoutProperty}'

                JOIN {WiserTableNames.WiserItemLink} mainStepLink ON mainStepLink.destination_item_id = configurator.id
                JOIN {WiserTableNames.WiserItem} mainStep ON mainStep.id = mainStepLink.item_id AND mainStep.entity_type = '{Constants.MainStepEntityType}'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateMainStep ON duplicateMainStep.item_id = mainStep.id AND duplicateMainStep.`key` = '{Constants.DuplicateLayoutProperty}'

                JOIN {WiserTableNames.WiserItemLink} stepLink ON stepLink.destination_item_id = mainStep.id
                JOIN {WiserTableNames.WiserItem} step ON step.id = stepLink.item_id AND step.entity_type = '{Constants.StepEntityType}'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateStep ON duplicateStep.item_id = step.id AND duplicateStep.`key` = '{Constants.DuplicateLayoutProperty}'

                LEFT JOIN {WiserTableNames.WiserItemLink} subStepLink ON subStepLink.destination_item_id = step.id
                LEFT JOIN {WiserTableNames.WiserItem} subStep ON subStep.id = subStepLink.item_id AND subStep.entity_type = '{Constants.SubStepEntityType}'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateSubStep ON duplicateSubStep.item_id = subStep.id AND duplicateSubStep.`key` = '{Constants.DuplicateLayoutProperty}'
                WHERE configurator.moduleid = {Constants.ConfiguratorModuleId} AND configurator.entity_type = '{Constants.ConfiguratorEntityType}' AND configurator.title = ?name
                ORDER BY mainStepLink.ordering, stepLink.ordering, subStepLink.ordering ";
            var dataTable = await databaseConnection.GetAsync(query);

            var returnDataTable = new DataTable();

            foreach (var queryField in queryFields)
            {
                returnDataTable.Columns.Add(queryField);
            }

            if (dataTable.Rows.Count == 0)
            {
                return returnDataTable;
            }

            // create one list item per step/substep
            foreach (DataRow dr in dataTable.Rows)
            {
                returnDataTable.Rows.Add(new object[]
                {
                    dr.Field<ulong>("configuratorId"),
                    dr.Field<ulong>("mainStepId"),
                    dr.Field<ulong>("stepId"),
                    dr["subStepId"],
                    dr["duplicateConfiguratorId"],
                    dr["duplicateMainStepId"],
                    dr["duplicateStepId"],
                    dr["duplicateSubStepId"]
                });
            }

            var idList = new List<ulong>();
            idList.AddRange(dataTable.Rows.Cast<DataRow>().Select(x => x.Field<ulong>("configuratorId")).Distinct());
            idList.AddRange(dataTable.Rows.Cast<DataRow>().Select(x => x.Field<ulong>("mainStepId")).Distinct());
            idList.AddRange(dataTable.Rows.Cast<DataRow>().Select(x => x.Field<ulong>("stepId")).Distinct());
            idList.AddRange(dataTable.Rows.Cast<DataRow>().Select(x => Convert.ToUInt64(x["subStepId"])).Distinct());

            var languageCode = await languagesService.GetLanguageCodeAsync();

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("gcl_languageCode", languageCode);
            dataTable = await databaseConnection.GetAsync(@$"
                SELECT item.id, IFNULL(namePart.`value`, item.title) AS title, item.entity_type, detail.`key`, CONCAT_WS('', detail.`value`, detail.long_value) AS `value`
                FROM {WiserTableNames.WiserItem} item
                JOIN {WiserTableNames.WiserItemDetail} detail ON detail.item_id = item.id
                LEFT JOIN {WiserTableNames.WiserItemDetail} namePart ON namePart.item_id = item.id AND namePart.key = 'title' AND namePart.language_code = ?gcl_languageCode
                WHERE item.id IN ({String.Join(",", idList)});");

            if (dataTable.Rows.Count == 0)
            {
                return returnDataTable;
            }

            foreach (DataRow dr in dataTable.Rows)
            {
                var idField = dr.Field<ulong>("id");
                var titleField = dr.Field<string>("title");
                var entityType = dr.Field<string>("entity_type");
                var keyField = dr.Field<string>("key");
                var valueField = dr.Field<string>("value");

                // skip field
                if (String.IsNullOrWhiteSpace(entityType) || String.IsNullOrWhiteSpace(valueField))
                {
                    continue;
                }

                // fill properties
                switch (entityType.ToLowerInvariant())
                {
                    case "configurator":
                    {
                        var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["configuratorId"]) == idField);

                        foreach (var item in items)
                        {
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["name"].ToString()))
                            {
                                item["name"] = titleField;
                            }

                            var foundField = configuratorFields.FirstOrDefault(x => x.fieldName == keyField);
                            if (foundField.prefix == null || foundField.fieldName == null)
                            {
                                break;
                            }

                            item[$"{foundField.prefix}{foundField.fieldName}"] = valueField;
                        }

                        break;
                    }
                    case "hoofdstap":
                    {
                        var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["mainStepId"]) == idField);

                        foreach (var item in items)
                        {
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["mainstepname"].ToString()))
                            {
                                item["mainstepname"] = await templatesService.DoReplacesAsync(titleField);
                            }

                            var foundField = mainStepFields.FirstOrDefault(x => x.fieldName == keyField);
                            if (foundField.prefix == null || foundField.fieldName == null)
                            {
                                break;
                            }

                            item[$"{foundField.prefix}{foundField.fieldName}"] = valueField;

                            // if variable name isn't set, fill it with title in mysql safe value.
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["mainstep_variable_name"].ToString()))
                            {
                                item["mainstep_variable_name"] = titleField;
                            }
                        }

                        break;
                    }
                    case "stap":
                    {
                        var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["stepId"]) == idField);

                        foreach (var item in items)
                        {
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["stepname"].ToString()))
                            {
                                item["stepname"] = titleField;
                            }

                            var foundField = stepFields.FirstOrDefault(x => x.fieldName == keyField);
                            if (foundField.prefix == null || foundField.fieldName == null)
                            {
                                break;
                            }

                            item[$"{foundField.prefix}{foundField.fieldName}"] = valueField;

                            // if variable name isn't set, fill it with title in mysql safe value.
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["variable_name"].ToString()))
                            {
                                item["variable_name"] = titleField;
                            }
                        }

                        break;
                    }
                    case "substap":
                    {
                        var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["subStepId"]) == idField);

                        foreach (var item in items)
                        {
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["substepname"].ToString()))
                            {
                                item["substepname"] = titleField;
                            }

                            var foundField = subStepFields.FirstOrDefault(x => x.fieldName == keyField);
                            if (foundField.prefix == null || foundField.fieldName == null)
                            {
                                break;
                            }

                            item[$"{foundField.prefix}{foundField.fieldName}"] = valueField;
                            // if variable name isn't set, fill it with title in mysql safe value.
                            if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["substep_variable_name"].ToString()))
                            {
                                item["substep_variable_name"] = titleField;
                            }
                        }

                        break;
                    }
                }
            }

            // afhandeling duplicate layout
            foreach (DataRow returnRow in returnDataTable.Rows)
            {
                if (Convert.ToUInt64(returnRow["duplicateConfiguratorId"]) > 0)
                {
                    var original = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["configuratorId"]) == Convert.ToUInt64(returnRow["duplicateConfiguratorId"]));
                    if (original != null)
                    {
                        returnRow["summary_template"] = original["summary_template"];
                        returnRow["summary_mainstep_template"] = original["summary_mainstep_template"];
                        returnRow["summary_step_template"] = original["summary_step_template"];
                        returnRow["progress_pre_template"] = original["progress_pre_template"];
                        returnRow["progress_pre_step_template"] = original["progress_pre_step_template"];
                        returnRow["progress_pre_substep_template"] = original["progress_pre_substep_template"];
                        returnRow["progress_post_template"] = original["progress_post_template"];
                        returnRow["progress_post_step_template"] = original["progress_post_step_template"];
                        returnRow["progress_post_substep_template"] = original["progress_post_substep_template"];
                        returnRow["progress_template"] = original["progress_template"];
                        returnRow["progress_step_template"] = original["progress_step_template"];
                        returnRow["progress_substep_template"] = original["progress_substep_template"];
                        returnRow["template"] = original["template"];
                        returnRow["step_template"] = original["step_template"];
                    }
                }

                if (Convert.ToUInt64(returnRow["duplicateMainStepId"]) > 0)
                {
                    var original = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["mainStepId"]) == Convert.ToUInt64(returnRow["duplicateMainStepId"]));
                    if (original != null)
                    {

                        returnRow["mainsteps_values_template"] = original["mainsteps_values_template"];
                        if (!String.IsNullOrWhiteSpace(original["mainstep_template"].ToString()))
                        {
                            returnRow["mainstep_template"] = original["mainstep_template"];
                        }
                        else
                        {
                            var originalConfiguratorDataRow = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["configuratorId"]) == Convert.ToUInt64(returnRow["configuratorId"]));
                            if (originalConfiguratorDataRow != null)
                            {
                                returnRow["mainstep_template"] = originalConfiguratorDataRow["configurator_step_template"];
                            }
                        }
                    }
                }

                if (Convert.ToUInt64(returnRow["duplicateStepId"]) > 0)
                {
                    var original = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["stepId"]) == Convert.ToUInt64(returnRow["duplicateStepId"]));
                    if (original != null)
                    {

                        returnRow["values_template"] = original["values_template"];
                        if (!String.IsNullOrWhiteSpace(original["step_template"].ToString()))
                        {
                            returnRow["step_template"] = original["step_template"];
                        }
                        else
                        {
                            var originalConfiguratorDataRow = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["configuratorId"]) == Convert.ToUInt64(returnRow["configuratorId"]));
                            if (originalConfiguratorDataRow != null)
                            {
                                returnRow["step_template"] = originalConfiguratorDataRow["template"];
                            }
                        }

                    }
                }

                if (Convert.ToUInt64(returnRow["duplicateSubStepId"]) > 0)
                {
                    var original = returnDataTable.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToUInt64(x["subStepId"]) == Convert.ToUInt64(returnRow["duplicateSubStepId"]));
                    if (original != null)
                    {
                        returnRow["substep_values_template"] = original["substep_values_template"];
                        returnRow["substep_template"] = original["substep_template"];
                    }
                }

                // fall back to configurator step template
                if (String.IsNullOrWhiteSpace(returnRow["step_template"].ToString()))
                {
                    returnRow["step_template"] = returnRow["configurator_step_template"];
                }

                if (String.IsNullOrWhiteSpace(returnRow["mainstep_template"].ToString()))
                {
                    returnRow["mainstep_template"] = returnRow["configurator_step_template"];
                }
            }

            return returnDataTable;
        }

        /// <inheritdoc />
        public async Task<VueConfiguratorDataModel> GetVueConfiguratorDataAsync(string name, bool includeStepsData = true)
        {
            // Make sure the language code has a value.
            if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                // This function fills the property "CurrentLanguageCode".
                await languagesService.GetLanguageCodeAsync();
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("name", name);
            databaseConnection.AddParameter("languageCode", languagesService.CurrentLanguageCode);

            var configuratorSettings = await databaseConnection.GetAsync($@"SELECT
    configurator.id AS configuratorId,
    CONCAT_WS('', mainTemplate.`value`, mainTemplate.long_value) AS mainTemplate,
    CONCAT_WS('', progressBarTemplate.`value`, progressBarTemplate.long_value) AS progressBarTemplate,
    CONCAT_WS('', progressBarStepTemplate.`value`, progressBarStepTemplate.long_value) AS progressBarStepTemplate,
    CONCAT_WS('', progressTemplate.`value`, progressTemplate.long_value) AS progressTemplate,
    CONCAT_WS('', summaryTemplate.`value`, summaryTemplate.long_value) AS summaryTemplate,
    summaryStepName.`value` AS summaryStepName,
    IFNULL(showSummaryProgressBarStep.`value`, '1') = '1' AS showSummaryProgressBarStep,
    priceCalculationQuery.`value` AS priceCalculationQuery,
    deliveryTimeCalculationQuery.`value` AS deliveryTimeCalculationQuery,
    apiStartConfiguration.`value` IS NOT NULL AND apiStartConfiguration.`value` <> '' AS startExternalConfigurationOnStart
FROM {WiserTableNames.WiserItem} AS configurator
LEFT JOIN {WiserTableNames.WiserItemDetail} AS mainTemplate ON mainTemplate.item_id = configurator.id AND mainTemplate.`key` = 'template'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS progressBarTemplate ON progressBarTemplate.item_id = configurator.id AND progressBarTemplate.`key` = 'progress_bar_template'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS progressBarStepTemplate ON progressBarStepTemplate.item_id = configurator.id AND progressBarStepTemplate.`key` = 'progress_bar_step_template'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS progressTemplate ON progressTemplate.item_id = configurator.id AND progressTemplate.`key` = 'progress_template'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS summaryTemplate ON summaryTemplate.item_id = configurator.id AND summaryTemplate.`key` = 'summary_template'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS showSummaryProgressBarStep ON showSummaryProgressBarStep.item_id = configurator.id AND showSummaryProgressBarStep.`key` = 'show_summary_progress_bar_step'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS summaryStepName ON summaryStepName.item_id = configurator.id AND summaryStepName.`key` = 'summary_step_name' AND summaryStepName.language_code = ?languageCode
LEFT JOIN {WiserTableNames.WiserItemDetail} AS priceCalculationQuery ON priceCalculationQuery.item_id = configurator.id AND priceCalculationQuery.`key` = 'price_calculation_query'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS deliveryTimeCalculationQuery ON deliveryTimeCalculationQuery.item_id = configurator.id AND deliveryTimeCalculationQuery.`key` = 'delivery_time_calculation_query'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS apiStartConfiguration ON apiStartConfiguration.item_id = configurator.id AND apiStartConfiguration.`key` = 'api_start'
WHERE configurator.entity_type = '{Constants.ConfiguratorEntityType}' AND configurator.title = ?name");

            if (configuratorSettings.Rows.Count == 0)
            {
                return null;
            }

            var configuratorId = Convert.ToUInt64(configuratorSettings.Rows[0]["configuratorId"]);
            var returnValue = new VueConfiguratorDataModel
            {
                ConfiguratorId = configuratorId,
                MainTemplate = configuratorSettings.Rows[0].Field<string>("mainTemplate"),
                ProgressBarTemplate = configuratorSettings.Rows[0].Field<string>("progressBarTemplate"),
                ProgressBarStepTemplate = configuratorSettings.Rows[0].Field<string>("progressBarStepTemplate"),
                ProgressTemplate = configuratorSettings.Rows[0].Field<string>("progressTemplate"),
                SummaryTemplate = configuratorSettings.Rows[0].Field<string>("summaryTemplate"),
                SummaryStepName = configuratorSettings.Rows[0].Field<string>("summaryStepName"),
                PriceCalculationQuery = configuratorSettings.Rows[0].Field<string>("priceCalculationQuery"),
                DeliveryTimeCalculationQuery = configuratorSettings.Rows[0].Field<string>("deliveryTimeCalculationQuery"),
                StartExternalConfigurationOnStart = Convert.ToBoolean(configuratorSettings.Rows[0]["startExternalConfigurationOnStart"])
            };

            if (!includeStepsData)
            {
                returnValue.StepsData = new List<VueStepDataModel>(0);
                return returnValue;
            }

            #region Query

            var query = $@"WITH RECURSIVE cte AS (
    SELECT
        step.id AS stepId,
        0 AS parentStepId,
        IFNULL(title.`value`, step.title) AS displayName,
        variableName.`value` AS stepName,
        dependencies.`value` AS dependencies,
        datasource.`value` AS datasource,
        IF(apiAnswer.id IS NULL OR apiAnswer.`value` = '', FALSE, TRUE) AS isApiAnswer,
        IFNULL(isRequired.`value`, 'true') = 'true' AS isRequired,
        IFNULL(isRequiredOnlyWithOptions.`value`, '0') = '1' AS isRequiredOnlyWithOptions,
        requiredConditions.`value` AS requiredConditions,
        minimumValue.`value` AS minimumValue,
        maximumValue.`value` AS maximumValue,
        validationRegex.`value` AS validationRegex,
        requiredErrorMessage.`value` AS requiredErrorMessage,
        minimumValueErrorMessage.`value` AS minimumValueErrorMessage,
        maximumValueErrorMessage.`value` AS maximumValueErrorMessage,
        validationRegexErrorMessage.`value` AS validationRegexErrorMessage,
        IF(
            templatesFromStepId.id IS NOT NULL AND templatesFromStepId.`value` NOT IN ('', '0'),
            (SELECT CONCAT_WS('', `value`, long_value) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = CONVERT(templatesFromStepId.`value`, UNSIGNED) AND `key` = 'step_template'),
            CONCAT_WS('', stepTemplate.`value`, stepTemplate.long_value)
        ) AS stepTemplate,
        IF(
            templatesFromStepId.id IS NOT NULL AND templatesFromStepId.`value` NOT IN ('', '0'),
            (SELECT CONCAT_WS('', `value`, long_value) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = CONVERT(templatesFromStepId.`value`, UNSIGNED) AND `key` = 'values_template'),
            CONCAT_WS('', stepOptionTemplate.`value`, stepOptionTemplate.long_value)
        ) AS stepOptionTemplate,
        CONCAT_WS('', extraDataQuery.`value`, extraDataQuery.long_value) AS extraDataQuery,
        urlRegex.`value` AS urlRegex,
        (SELECT JSON_OBJECTAGG(`key`, CONCAT_WS('', `value`, long_value)) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = step.id AND groupname = 'extra_data') AS extraData,

        IFNULL(optionsOpenModal.`value`, '0') = '1' AS optionsOpenModal,
        CONCAT_WS('', modalTemplate.`value`, modalTemplate.long_value) AS modalTemplate,
        modalContainerSelector.`value` AS modalContainerSelector,

        stepLink.ordering
    FROM {WiserTableNames.WiserItem} AS configurator

    JOIN {WiserTableNames.WiserItemLink} AS stepLink ON stepLink.destination_item_id = configurator.id
    JOIN {WiserTableNames.WiserItem} AS step ON step.id = stepLink.item_id AND step.entity_type IN ('hoofdstap', 'stap', 'substap')
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS title ON title.item_id = step.id AND title.`key` = 'title' AND title.language_code = ?languageCode
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS variableName ON variableName.item_id = step.id AND variableName.`key` = 'variable_name'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS dependencies ON dependencies.item_id = step.id AND dependencies.`key` = 'datasource_connectedid'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS datasource ON datasource.item_id = step.id AND datasource.`key` = 'datasource'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS apiAnswer ON apiAnswer.item_id = step.id AND apiAnswer.`key` = 'api_answer'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS isRequired ON isRequired.item_id = step.id AND isRequired.`key` = 'isrequired'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS isRequiredOnlyWithOptions ON isRequiredOnlyWithOptions.item_id = step.id AND isRequiredOnlyWithOptions.`key` = 'is_required_only_with_options'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS requiredConditions ON requiredConditions.item_id = step.id AND requiredConditions.`key` = 'required_conditions'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS minimumValue ON minimumValue.item_id = step.id AND minimumValue.`key` = 'min_value'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS maximumValue ON maximumValue.item_id = step.id AND maximumValue.`key` = 'max_value'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS validationRegex ON validationRegex.item_id = step.id AND validationRegex.`key` = 'validation_regex'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS requiredErrorMessage ON requiredErrorMessage.item_id = step.id AND requiredErrorMessage.`key` = 'required_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS minimumValueErrorMessage ON minimumValueErrorMessage.item_id = step.id AND minimumValueErrorMessage.`key` = 'min_value_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS maximumValueErrorMessage ON maximumValueErrorMessage.item_id = step.id AND maximumValueErrorMessage.`key` = 'max_value_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS validationRegexErrorMessage ON validationRegexErrorMessage.item_id = step.id AND validationRegexErrorMessage.`key` = 'validation_regex_error_message'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS templatesFromStepId ON templatesFromStepId.item_id = step.id AND templatesFromStepId.`key` = 'duplicatelayoutfrom'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepTemplate ON stepTemplate.item_id = step.id AND stepTemplate.`key` = 'step_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepOptionTemplate ON stepOptionTemplate.item_id = step.id AND stepOptionTemplate.`key` = 'values_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS extraDataQuery ON extraDataQuery.item_id = step.id AND extraDataQuery.`key` = 'extra_data_query'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS urlRegex ON urlRegex.item_id = step.id AND urlRegex.`key` = 'urlregex'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS optionsOpenModal ON optionsOpenModal.item_id = step.id AND optionsOpenModal.`key` = 'options_open_modal'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS modalTemplate ON modalTemplate.item_id = step.id AND modalTemplate.`key` = 'modal_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS modalContainerSelector ON modalContainerSelector.item_id = step.id AND modalContainerSelector.`key` = 'modal_container_selector'

    WHERE configurator.entity_type = '{Constants.ConfiguratorEntityType}' AND configurator.id = ?configuratorId

    UNION ALL

    SELECT
        step.id AS stepId,
        parentStep.id AS parentStepId,
        IFNULL(title.`value`, step.title) AS displayName,
        variableName.`value` AS stepName,
        dependencies.`value` AS dependencies,
        datasource.`value` AS datasource,
        IF(apiAnswer.id IS NULL OR apiAnswer.`value` = '', FALSE, TRUE) AS isApiAnswer,

        # Validation properties.
        IFNULL(isRequired.`value`, 'true') = 'true' AS isRequired,
        IFNULL(isRequiredOnlyWithOptions.`value`, '0') = '1' AS isRequiredOnlyWithOptions,
        requiredConditions.`value` AS requiredConditions,
        minimumValue.`value` AS minimumValue,
        maximumValue.`value` AS maximumValue,
        validationRegex.`value` AS validationRegex,
        requiredErrorMessage.`value` AS requiredErrorMessage,
        minimumValueErrorMessage.`value` AS minimumValueErrorMessage,
        maximumValueErrorMessage.`value` AS maximumValueErrorMessage,
        validationRegexErrorMessage.`value` AS validationRegexErrorMessage,

        # Layout properties.
        IF(
            templatesFromStepId.id IS NOT NULL AND templatesFromStepId.`value` NOT IN ('', '0'),
            (SELECT CONCAT_WS('', `value`, long_value) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = CONVERT(templatesFromStepId.`value`, UNSIGNED) AND `key` = 'step_template'),
            CONCAT_WS('', stepTemplate.`value`, stepTemplate.long_value)
        ) AS stepTemplate,
        IF(
            templatesFromStepId.id IS NOT NULL AND templatesFromStepId.`value` NOT IN ('', '0'),
            (SELECT CONCAT_WS('', `value`, long_value) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = CONVERT(templatesFromStepId.`value`, UNSIGNED) AND `key` = 'values_template'),
            CONCAT_WS('', stepOptionTemplate.`value`, stepOptionTemplate.long_value)
        ) AS stepOptionTemplate,
        CONCAT_WS('', extraDataQuery.`value`, extraDataQuery.long_value) AS extraDataQuery,
        urlRegex.`value` AS urlRegex,
        (SELECT JSON_OBJECTAGG(`key`, CONCAT_WS('', `value`, long_value)) FROM {WiserTableNames.WiserItemDetail} WHERE item_id = step.id AND groupname = 'extra_data') AS extraData,

        IFNULL(optionsOpenModal.`value`, '0') = '1' AS optionsOpenModal,
        CONCAT_WS('', modalTemplate.`value`, modalTemplate.long_value) AS modalTemplate,
        modalContainerSelector.`value` AS modalContainerSelector,

        stepLink.ordering
    FROM {WiserTableNames.WiserItem} AS parentStep
    JOIN cte ON parentStep.id = cte.stepId

    JOIN {WiserTableNames.WiserItemLink} AS stepLink ON stepLink.destination_item_id = parentStep.id
    JOIN {WiserTableNames.WiserItem} AS step ON step.id = stepLink.item_id AND step.entity_type IN ('hoofdstap', 'stap', 'substap')
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS title ON title.item_id = step.id AND title.`key` = 'title' AND title.language_code = ?languageCode
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS variableName ON variableName.item_id = step.id AND variableName.`key` = 'variable_name'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS dependencies ON dependencies.item_id = step.id AND dependencies.`key` = 'datasource_connectedid'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS datasource ON datasource.item_id = step.id AND datasource.`key` = 'datasource'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS apiAnswer ON apiAnswer.item_id = step.id AND apiAnswer.`key` = 'api_answer'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS isRequired ON isRequired.item_id = step.id AND isRequired.`key` = 'isrequired'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS isRequiredOnlyWithOptions ON isRequiredOnlyWithOptions.item_id = step.id AND isRequiredOnlyWithOptions.`key` = 'is_required_only_with_options'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS requiredConditions ON requiredConditions.item_id = step.id AND requiredConditions.`key` = 'required_conditions'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS minimumValue ON minimumValue.item_id = step.id AND minimumValue.`key` = 'min_value'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS maximumValue ON maximumValue.item_id = step.id AND maximumValue.`key` = 'max_value'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS validationRegex ON validationRegex.item_id = step.id AND validationRegex.`key` = 'validation_regex'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS requiredErrorMessage ON requiredErrorMessage.item_id = step.id AND requiredErrorMessage.`key` = 'required_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS minimumValueErrorMessage ON minimumValueErrorMessage.item_id = step.id AND minimumValueErrorMessage.`key` = 'min_value_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS maximumValueErrorMessage ON maximumValueErrorMessage.item_id = step.id AND maximumValueErrorMessage.`key` = 'max_value_error_message'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS validationRegexErrorMessage ON validationRegexErrorMessage.item_id = step.id AND validationRegexErrorMessage.`key` = 'validation_regex_error_message'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS templatesFromStepId ON templatesFromStepId.item_id = step.id AND templatesFromStepId.`key` = 'duplicatelayoutfrom'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepTemplate ON stepTemplate.item_id = step.id AND stepTemplate.`key` = 'step_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepOptionTemplate ON stepOptionTemplate.item_id = step.id AND stepOptionTemplate.`key` = 'values_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS extraDataQuery ON extraDataQuery.item_id = step.id AND extraDataQuery.`key` = 'extra_data_query'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS urlRegex ON urlRegex.item_id = step.id AND urlRegex.`key` = 'urlregex'

    LEFT JOIN {WiserTableNames.WiserItemDetail} AS optionsOpenModal ON optionsOpenModal.item_id = step.id AND optionsOpenModal.`key` = 'options_open_modal'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS modalTemplate ON modalTemplate.item_id = step.id AND modalTemplate.`key` = 'modal_template'
    LEFT JOIN {WiserTableNames.WiserItemDetail} AS modalContainerSelector ON modalContainerSelector.item_id = step.id AND modalContainerSelector.`key` = 'modal_container_selector'

    WHERE parentStep.entity_type IN ('hoofdstap', 'stap', 'substap')
)
SELECT
    stepId,
    parentStepId,
    displayName,
    stepName,
    dependencies,
    datasource,
    isApiAnswer,
    isRequired,
    isRequiredOnlyWithOptions,
    requiredConditions,
    minimumValue,
    maximumValue,
    validationRegex,
    requiredErrorMessage,
    minimumValueErrorMessage,
    maximumValueErrorMessage,
    validationRegexErrorMessage,
    stepTemplate,
    stepOptionTemplate,
    extraDataQuery,
    urlRegex,
    extraData,
    optionsOpenModal,
    modalTemplate,
    modalContainerSelector,
    ordering
FROM cte
ORDER BY parentStepId, ordering";

            #endregion

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("configuratorId", configuratorId);
            databaseConnection.AddParameter("languageCode", languagesService.CurrentLanguageCode);
            var dataTable = await databaseConnection.GetAsync(query);

            var stepsData = new List<VueStepDataModel>(dataTable.Rows.Count);
            var dataRows = dataTable.Rows.Cast<DataRow>().ToArray();
            foreach (var dataRow in dataRows)
            {
                // Create dependencies.
                var dependencies = new List<VueStepDependencyModel>();
                if (!String.IsNullOrWhiteSpace(dataRow.Field<string>("dependencies")))
                {
                    var dependencyArray = dataRow.Field<string>("dependencies").Split(';');
                    foreach (var dependency in dependencyArray)
                    {
                        var dependencyStepName = dependency;

                        // Check if the dependency should also check for the value of the dependency.
                        List<string> dependencyValues = null;
                        var dependencyValuesMatch = dependencyValuesRegex.Match(dependency);
                        if (dependencyValuesMatch.Success)
                        {
                            dependencyValues = dependencyValuesMatch.Groups["values"].Value.Split(',').ToList();
                            dependencyStepName = dependency.Replace(dependencyValuesMatch.Value, String.Empty);
                        }

                        dependencies.Add(new VueStepDependencyModel
                        {
                            StepName = dependencyStepName,
                            Values = dependencyValues ?? new List<string>()
                        });
                    }
                }

                // Create required conditions.
                var requiredConditions = new List<VueStepDependencyModel>();
                if (!String.IsNullOrWhiteSpace(dataRow.Field<string>("requiredConditions")))
                {
                    var requiredConditionsArray = dataRow.Field<string>("requiredConditions").Split(';');
                    foreach (var requiredCondition in requiredConditionsArray)
                    {
                        // Check if the dependency should also check for the value of the dependency.
                        var requiredConditionValuesMatch = dependencyValuesRegex.Match(requiredCondition);
                        if (!requiredConditionValuesMatch.Success) continue;

                        var requiredConditionValues = requiredConditionValuesMatch.Groups["values"].Value.Split(',').ToList();
                        var requiredConditionStepName = requiredCondition.Replace(requiredConditionValuesMatch.Value, String.Empty);

                        requiredConditions.Add(new VueStepDependencyModel
                        {
                            StepName = requiredConditionStepName,
                            Values = requiredConditionValues
                        });
                    }
                }

                var requiredErrorMessage = await stringReplacementsService.DoAllReplacementsAsync(dataRow.Field<string>("requiredErrorMessage"), removeUnknownVariables: false);
                var minimumValueErrorMessage = await stringReplacementsService.DoAllReplacementsAsync(dataRow.Field<string>("minimumValueErrorMessage"), removeUnknownVariables: false);
                var maximumValueErrorMessage = await stringReplacementsService.DoAllReplacementsAsync(dataRow.Field<string>("maximumValueErrorMessage"), removeUnknownVariables: false);
                var validationRegexErrorMessage = await stringReplacementsService.DoAllReplacementsAsync(dataRow.Field<string>("validationRegexErrorMessage"));

                var extraDataJson = dataRow.Field<string>("extraData");
                var extraData = !String.IsNullOrWhiteSpace(extraDataJson) ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, JToken>>(extraDataJson) : null;

                // Perform string replacements on the values of the extra data.
                if (extraData is { Count: > 0 })
                {
                    foreach (var key in extraData.Keys)
                    {
                        // Make sure the value is a string.
                        if (extraData[key] is JValue { Type: JTokenType.String } jValue)
                        {
                            jValue.Value = await stringReplacementsService.DoAllReplacementsAsync(jValue.Value<string>());
                        }
                    }
                }

                var step = new VueStepDataModel
                {
                    StepId = Convert.ToUInt64(dataRow["stepId"]),
                    ParentStepId = Convert.ToUInt64(dataRow["parentStepId"]),
                    DisplayName = dataRow.Field<string>("displayName"),
                    StepName = dataRow.Field<string>("stepName"),
                    Dependencies = dependencies,
                    MinimumValue = dataRow.Field<string>("minimumValue"),
                    MaximumValue = dataRow.Field<string>("maximumValue"),
                    ValidationRegex = dataRow.Field<string>("validationRegex"),
                    RequiredErrorMessage = requiredErrorMessage,
                    MinimumValueErrorMessage = minimumValueErrorMessage,
                    MaximumValueErrorMessage = maximumValueErrorMessage,
                    ValidationRegexErrorMessage = validationRegexErrorMessage,
                    IsRequired = Convert.ToBoolean(dataRow["isRequired"]),
                    IsRequiredOnlyWithOptions = Convert.ToBoolean(dataRow["isRequiredOnlyWithOptions"]),
                    RequiredConditions = requiredConditions,
                    StepTemplate = dataRow.Field<string>("stepTemplate"),
                    StepOptionTemplate = dataRow.Field<string>("stepOptionTemplate"),
                    Datasource = dataRow.Field<string>("datasource"),
                    IsApiAnswer = Convert.ToBoolean(dataRow["isApiAnswer"]),
                    ExtraDataQuery = dataRow.Field<string>("extraDataQuery"),
                    UrlRegex = dataRow.Field<string>("urlRegex"),
                    OptionsOpenModal = Convert.ToBoolean(dataRow["optionsOpenModal"]),
                    ModalContent = dataRow.Field<string>("modalTemplate"),
                    ModalContainerSelector = dataRow.Field<string>("modalContainerSelector"),
                    ExtraData = extraData
                };

                stepsData.Add(step);
            }

            foreach (var dataRow in dataRows)
            {
                var pos = GetStepPosition(dataRows, dataRow, new List<int>());
                stepsData.First(s => s.StepId == Convert.ToUInt64(dataRow["stepId"])).Position = String.Join("-", pos);
            }

            returnValue.StepsData = stepsData;
            return returnValue;
        }

        /// <summary>
        /// Determines the position of the step in the configurator.
        /// </summary>
        /// <param name="allSteps">Data rows containing the data of all steps.</param>
        /// <param name="stepData">Data row containing the data of the step whose position needs to be determined.</param>
        /// <param name="current">The current position value.</param>
        /// <returns>A list containing the positions of every level.</returns>
        private List<int> GetStepPosition(DataRow[] allSteps, DataRow stepData, List<int> current)
        {
            current ??= new List<int>();

            var pos = Convert.ToInt32(Convert.ToInt32(stepData["ordering"])) - 1;
            current.Insert(0, pos);

            var parentStepId = Convert.ToUInt64(stepData["parentStepId"]);
            if (parentStepId > 0)
            {
                current = GetStepPosition(allSteps, allSteps.First(dr => Convert.ToUInt64(dr["stepId"]) == parentStepId), current);
            }

            return current;
        }

        /// <inheritdoc />
        public async Task SetVueStepOptionsWithQueryAsync(VueStepDataModel stepData, List<VueStepOptionDataModel> options, VueConfigurationsModel configuration)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("stepId", stepData.StepId);
            var dataTable = await databaseConnection.GetAsync($@"SELECT CONCAT_WS('', stepOptionsQuery.`value`, stepOptionsQuery.long_value) AS stepOptionsQuery
FROM {WiserTableNames.WiserItemDetail} AS stepOptionsQuery
WHERE stepOptionsQuery.item_id = ?stepId
AND stepOptionsQuery.`key` = 'custom_query'");
            
            if (dataTable.Rows.Count == 0)
            {
                stepData.Options = options;
                return;
            }
            
            var stepOptionsQuery = dataTable.Rows[0].Field<string>("stepOptionsQuery");
            if (String.IsNullOrWhiteSpace(stepOptionsQuery))
            {
                stepData.Options = options;
                return;
            }

            stepOptionsQuery = await ReplaceConfiguratorItemsAsync(stepOptionsQuery, configuration, true);
            stepOptionsQuery = await templatesService.DoReplacesAsync(stepOptionsQuery, handleRequest: false, removeUnknownVariables: false, forQuery: true);
            var stepOptionsDataTable = await databaseConnection.GetAsync(stepOptionsQuery);
            if (stepOptionsDataTable.Rows.Count == 0)
            {
                stepData.Options = options;
                return;
            }

            // Some values of the step can be overriden through the step options query.
            if (stepOptionsDataTable.Columns.Contains("minimumValue"))
            {
                stepData.MinimumValue = Convert.ToString(stepOptionsDataTable.Rows[0]["minimumValue"]);
            }
            if (stepOptionsDataTable.Columns.Contains("maximumValue"))
            {
                stepData.MaximumValue = Convert.ToString(stepOptionsDataTable.Rows[0]["maximumValue"]);
            }
            if (stepOptionsDataTable.Columns.Contains("validationRegex"))
            {
                stepData.ValidationRegex = Convert.ToString(stepOptionsDataTable.Rows[0]["validationRegex"]);
            }

            // Handle the data rows.
            var stepOptionProperties = typeof(VueStepOptionDataModel).GetProperties();
            foreach (var dataRow in stepOptionsDataTable.Rows.Cast<DataRow>())
            {
                var stepOption = new VueStepOptionDataModel
                {
                    AdditionalData = new Dictionary<string, object>()
                };

                foreach (var dataColumn in stepOptionsDataTable.Columns.Cast<DataColumn>())
                {
                    var columnName = dataColumn.ColumnName;
                    var columnValue = dataRow[dataColumn];
                    var property = stepOptionProperties.FirstOrDefault(property => property.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                    if (property != null)
                    {
                        // Check if the property is a boolean and if so, convert the value to a boolean.
                        if (property.PropertyType == typeof(bool))
                        {
                            // String values are handled differently.
                            if (columnValue is string stringValue)
                            {
                                property.SetValue(stepOption, stringValue.InList(StringComparer.OrdinalIgnoreCase, "1", "true"));
                            }
                            else
                            {
                                property.SetValue(stepOption, Convert.ToBoolean(columnValue));
                            }
                        }
                        else
                        {
                            var type = property.PropertyType;
                            type = Nullable.GetUnderlyingType(type) ?? type;

                            // All other data types are just added as-is.
                            property.SetValue(stepOption, Convert.ChangeType(columnValue, type));
                        }
                    }
                    else
                    {
                        // Add the value to the additional data dictionary.
                        stepOption.AdditionalData.Add(columnName, columnValue);
                    }
                }

                options.Add(stepOption);
            }

            stepData.Options = options;
        }

        /// <inheritdoc />
        public async Task SetVueStepOptionsWithApiAsync(VueStepDataModel stepData, List<VueStepOptionDataModel> options, VueConfigurationsModel configuration, Dictionary<string, JToken> apiData)
        {
            // Get the correct Configurator API entity.
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("stepId", stepData.StepId);
            var dataTable = await databaseConnection.GetAsync($@"SELECT
	CAST(questionConfiguratorApiId.`value` AS UNSIGNED) AS questionConfiguratorApiId,
	apiExtraData.`key`,
	apiExtraData.`value`
FROM {WiserTableNames.WiserItemDetail} AS questionConfiguratorApiId
LEFT JOIN {WiserTableNames.WiserItemDetail} AS apiExtraData ON apiExtraData.item_id = questionConfiguratorApiId.item_id AND apiExtraData.groupname = 'api_extra_data'
WHERE questionConfiguratorApiId.item_id = ?stepId
AND questionConfiguratorApiId.`key` = 'api_question'");
            
            if (dataTable.Rows.Count == 0)
            {
                stepData.Options = options;
                return;
            }
            
            var questionApi = await wiserItemsService.GetItemDetailsAsync(dataTable.Rows[0].Field<UInt64>("questionConfiguratorApiId"), entityType: "ConfiguratorApi", skipPermissionsCheck: true);
            if (questionApi == null)
            {
                stepData.Options = options;
                return;
            }

            var endpoint = questionApi.GetDetailValue("endpoint");
            var requestJson = questionApi.GetDetailValue("request_json");
            var questionKey = questionApi.GetDetailValue("question_key");
            var answersKey = questionApi.GetDetailValue("answers_key");
            var query = questionApi.GetDetailValue("api_query");
            
            if (String.IsNullOrWhiteSpace(endpoint) || String.IsNullOrWhiteSpace(questionKey) || String.IsNullOrWhiteSpace(answersKey))
            {
                stepData.Options = options;
                return;
            }
            
            // Get the the extra data for the API based on the group name of the details that are set in Wiser.
            var apiExtraData = new Dictionary<string, string>();
            foreach (DataRow row in dataTable.Rows)
            {
                apiExtraData.Add(row.Field<string>("key"), row.Field<string>("value"));
            }
            
            DataRow extraData = null;

            // If a query is set, execute it and use the results for the replacements in the JSON.
            if (!String.IsNullOrWhiteSpace(query))
            {
                query = stringReplacementsService.DoReplacements(query, apiExtraData, forQuery: true);
                query = await ReplaceConfiguratorItemsAsync(query, configuration, true);
                query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables: false, forQuery: true);
                var extraDataTable = await databaseConnection.GetAsync(query);

                if (extraDataTable.Rows.Count > 0)
                {
                    extraData = extraDataTable.Rows[0];
                }
            }

            endpoint = stringReplacementsService.DoReplacements(endpoint, apiExtraData);
            endpoint = await ReplaceConfiguratorItemsAsync(endpoint, configuration, false);
            endpoint = await stringReplacementsService.DoAllReplacementsAsync(endpoint, removeUnknownVariables: false);

            // If not all replacements have been replaced the endpoint is not valid. Calling it will not give a result.
            // This can happen for example if the endpoint has an external ID and the external configuration has not yet started.
            if (endpoint.Contains("{"))
            {
                stepData.Options = options;
                return;
            }
            
            questionKey = stringReplacementsService.DoReplacements(questionKey, apiExtraData);
            questionKey = await ReplaceConfiguratorItemsAsync(questionKey, configuration, false);
            questionKey = await stringReplacementsService.DoAllReplacementsAsync(questionKey, removeUnknownVariables: false);
            
            answersKey = stringReplacementsService.DoReplacements(answersKey, apiExtraData);
            answersKey = await ReplaceConfiguratorItemsAsync(answersKey, configuration, false);
            answersKey = await stringReplacementsService.DoAllReplacementsAsync(answersKey, removeUnknownVariables: false);
            
            var requestMethod = (Method) questionApi.GetDetailValue<int>("request_type");
            
            var restClient = new RestClient();
            var restRequest = new RestRequest(endpoint, requestMethod);
            
            // If a body is provided perform replacements and add it to the request.
            if (!String.IsNullOrWhiteSpace(requestJson))
            {
                requestJson = await ReplaceConfiguratorItemsAsync(requestJson, configuration, false);
                requestJson = await ReplaceConfiguratorItemsAsync(requestJson, configuration, false);
                requestJson = await stringReplacementsService.DoAllReplacementsAsync(requestJson, extraData, removeUnknownVariables: false);

                // Replace all not replaced values with null to prevent invalid JSON.
                var regex = new Regex("([\"'])?{[^\\]}\\s]*}([\"'])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                requestJson = regex.Replace(requestJson, "null");

                restRequest.AddBody(requestJson, MediaTypeNames.Application.Json);
            }
            
            await AddAuthenticationToApiCall(restRequest, questionApi);
            await AddAcceptLanguageToApiCall(restRequest);
            await AddCustomHeadersAsync(restRequest, questionApi, vueConfiguration: configuration, extraData: extraData);

            // Create a hash of the endpoint and request JSON to use as key for the API data to reuse the response if possible.
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{endpoint}{requestJson}"));
            var apiDataHash = string.Join("", hash.Select(b => b.ToString("x2")));

            // If the data is not yet retrieved, get it from the API.
            if (!apiData.ContainsKey(apiDataHash))
            {
                var restResponse = await DoExternalConfiguratorApiCallAsync(restClient, restRequest);
                if (!restResponse.IsSuccessful || restResponse.Content == null)
                {
                    logger.LogWarning("Error while trying to get the configuration from API ({questionApi}). The API response HTTP code was '{restResponseStatusCode}' and the result was: {restResponseContent}.\n\n{requestJson}", questionApi.Title, restResponse.StatusCode, restResponse.Content, requestJson);
                    stepData.Options = options;
                    return;
                }
                
                apiData[apiDataHash] = JToken.Parse(restResponse.Content);
            }
            
            // Get the question from the response.
            var responseData = apiData[apiDataHash];
            var keyParts = new List<string>(questionKey.Split('.'));
            var currentObject = responseData;

            // Step into the object till the correct object is found or the path leads to an invalid object.
            while (keyParts.Count > 0)
            {
                // Check if the key is an array selector.
                if (keyParts[0].StartsWith("["))
                {
                    // Remove the surrounding brackets [] to get the selector from the key.
                    var selector = keyParts[0].Substring(1, keyParts[0].Length - 2);
                    var selectorParts = selector.Split('=');
                    
                    if (selectorParts.Length != 2)
                    {
                        throw new Exception($"Selector '{selector}' is not valid. It should be in the format [key=value]. This selector is part of the question key '{questionKey}' in the question API '{questionApi.Title}'.");
                    }

                    var matchFound = false;

                    // Check for each object in the array if the property matches the first selector part and if its value matches the second selector part.
                    foreach (JToken jToken in currentObject)
                    {
                        if (jToken.Type != JTokenType.Object || !jToken[selectorParts[0]].Value<string>().Equals(selectorParts[1], StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        currentObject = jToken;
                        matchFound = true;
                        break;
                    }
                    
                    // If no match is found set the current object to null so the question is not found and will not have answers.
                    if (!matchFound)
                    {
                        currentObject = null;
                    }
                }
                else
                {
                    currentObject = currentObject[keyParts[0]];
                }
                
                keyParts.RemoveAt(0);
                
                // If the current object is null, the question is not found and will not have answers.
                if (currentObject == null)
                {
                    stepData.Options = options;
                    return;
                }
            }

            if (!String.IsNullOrWhiteSpace(stepData.MinimumValue) && stepData.MinimumValue.StartsWith("JSON:", StringComparison.InvariantCultureIgnoreCase))
            {
                stepData.MinimumValue = currentObject.SelectToken(stepData.MinimumValue.Substring(5)).Value<string>();
                
                // If the property could not be found while being expected the question is not complete and will not have answers.
                if (stepData.MinimumValue == null)
                {
                    stepData.Options = options;
                    return;
                }
            }
            
            if (!String.IsNullOrWhiteSpace(stepData.MaximumValue) && stepData.MaximumValue.StartsWith("JSON:", StringComparison.InvariantCultureIgnoreCase))
            {
                stepData.MaximumValue = currentObject.SelectToken(stepData.MaximumValue.Substring(5)).Value<string>();
                
                // If the property could not be found while being expected the question is not complete and will not have answers.
                if (stepData.MaximumValue == null)
                {
                    stepData.Options = options;
                    return;
                }
            }
            
            // Get properties from question as extra data if it is not an object or array.
            foreach (var property in (JObject)currentObject)
            {
                if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
                {
                    continue;
                }
                
                stepData.ExtraData.Add(property);
            }
            
            // Setup mapping for option ID, value and name.
            var mappings = new Dictionary<string, string>();
            foreach (var detail in questionApi.Details)
            {
                if (String.IsNullOrWhiteSpace(detail.GroupName) || !detail.GroupName.Equals("mapping", StringComparison.InvariantCultureIgnoreCase) || String.IsNullOrWhiteSpace(detail.Key))
                {
                    continue;
                }

                mappings.Add(detail.Key, detail.Value.ToString());
            }

            keyParts = new List<string>(answersKey.Split('.'));

            // Step into the object till the correct object is found or the path leads to an invalid object.
            while (keyParts.Count > 0)
            {
                currentObject = currentObject[keyParts[0]];
                keyParts.RemoveAt(0);
                
                // If the current object is null, the answers are not found.
                if (currentObject == null)
                {
                    stepData.Options = options;
                    return;
                }
            }
            
            var stepOptions = new List<VueStepOptionDataModel>();

            // Select the answers from the response.
            JObject[] answers = null;
            if (currentObject.Type == JTokenType.Object)
            {
                answers = new[] { (JObject)currentObject };
            }
            else if (currentObject.Type == JTokenType.Array)
            {
                answers = currentObject.Select(x => (JObject)x).ToArray();
            }

            if (answers == null || !answers.Any())
            {
                stepData.Options = options;
                return;
            }
            
            // Add each answer to the step options.
            var optionId = 1;
            foreach (var jObject in answers)
            {
                var stepOption = new VueStepOptionDataModel
                {
                    Id = optionId.ToString(),
                    Value = optionId.ToString(),
                    Name = $"Answer {optionId}",
                    AdditionalData = new Dictionary<string, object>()
                };
                
                foreach (var property in jObject)
                {
                    var propertyIsMapped = false;
                
                    foreach (var mapping in mappings)
                    {
                        if (!property.Key.Equals(mapping.Value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        switch (mapping.Key)
                        {
                            case "id":
                                stepOption.Id = property.Value.Value<string>();
                                propertyIsMapped = true;
                                break;
                            case "value":
                                stepOption.Value = property.Value.Value<string>();
                                propertyIsMapped = true;
                                break;
                            case "name":
                                stepOption.Name = property.Value.Value<string>();
                                propertyIsMapped = true;
                                break;
                        }
                    }
                
                    if (propertyIsMapped)
                    {
                        continue;
                    }
                    
                    stepOption.AdditionalData.Add(property.Key, property.Value);
                }

                stepOptions.Add(stepOption);
                optionId++;
            }

            stepData.Options = stepOptions;
        }
        
        /// <inheritdoc />
        public async Task<string> ReplaceConfiguratorItemsAsync(string templateOrQuery, ConfigurationsModel configuration, bool isQuery)
        {
            if (configuration == null || !templateOrQuery.Contains('{'))
            {
                return templateOrQuery;
            }

            foreach (var queryStringItem in configuration.QueryStringItems.Where(queryStringItem => templateOrQuery.Contains($"{{{queryStringItem.Key}}}", StringComparison.OrdinalIgnoreCase)))
            {
                if (!isQuery)
                {
                    templateOrQuery = templateOrQuery.Replace($"{{{queryStringItem.Key}}}", queryStringItem.Value);
                }
                else
                {
                    var parameterName = DatabaseHelpers.CreateValidParameterName(queryStringItem.Key);
                    databaseConnection.AddParameter(parameterName, queryStringItem.Value);
                    templateOrQuery = templateOrQuery.Replace($"'{{{queryStringItem.Key}}}'", $"?{parameterName}").Replace($"{{{queryStringItem.Key}}}", $"?{parameterName}");
                }
            }

            foreach (var key in configuration.Items.Keys)
            {
                if (!templateOrQuery.Contains($"{{{configuration.Items[key].Id}}}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parameterName = DatabaseHelpers.CreateValidParameterName(configuration.Items[key].Id);
                var valuesCanContainDashes = await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_ValuesCanContainDashes");
                if (!String.IsNullOrWhiteSpace(valuesCanContainDashes) && valuesCanContainDashes.Equals("true", StringComparison.OrdinalIgnoreCase) && configuration.Items[key].Value.Contains("-"))
                {
                    var value = configuration.Items[key].Value.Split('-')[1];
                    if (!isQuery)
                    {
                        templateOrQuery = templateOrQuery.Replace($"{{{configuration.Items[key].Id}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        templateOrQuery = templateOrQuery.Replace($"'{{{configuration.Items[key].Id}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].Id}}}", $"?{parameterName}");
                    }
                }
                else if (configuration.Items[key].Value == "-1")
                {
                    var value = configuration.Items[key].Value.Split('-')[1];
                    if (!isQuery)
                    {
                        templateOrQuery = templateOrQuery.Replace($"{{{configuration.Items[key].Id}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        templateOrQuery = templateOrQuery.Replace($"'{{{configuration.Items[key].Id}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].Id}}}", $"?{parameterName}");
                    }
                }
                else
                {
                    var value = configuration.Items[key].Value;
                    if (!isQuery)
                    {
                        templateOrQuery = templateOrQuery.Replace($"{{{configuration.Items[key].Id}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        templateOrQuery = templateOrQuery.Replace($"'{{{configuration.Items[key].Id}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].Id}}}", $"?{parameterName}");
                    }
                }
            }

            return templateOrQuery;
        }

        /// <inheritdoc />
        public async Task<string> ReplaceConfiguratorItemsAsync(string template, VueConfigurationsModel configuration, bool isDataQuery)
        {
            if (configuration == null || !template.Contains('{'))
            {
                return template;
            }
            
            // Replace external configurator ID if one is provided.
            if (configuration.ExternalConfiguration != null && !String.IsNullOrWhiteSpace(configuration.ExternalConfiguration.Id))
            {
                if (!isDataQuery)
                {
                    template = template.Replace("{externalConfiguration.id}", configuration.ExternalConfiguration.Id);
                }
                else
                {
                    var parameterName = "externalConfigurationId";
                    databaseConnection.AddParameter(parameterName, configuration.ExternalConfiguration.Id);
                    template = template.Replace("'{externalConfiguration.id}'", $"?{parameterName}").Replace("{externalConfiguration.id}", $"?{parameterName}");
                }
            }

            foreach (var queryStringItem in configuration.QueryStringItems.Where(queryStringItem => template.Contains($"{{{queryStringItem.Key}}}", StringComparison.OrdinalIgnoreCase)))
            {
                if (!isDataQuery)
                {
                    template = template.Replace($"{{{queryStringItem.Key}}}", queryStringItem.Value);
                }
                else
                {
                    var parameterName = DatabaseHelpers.CreateValidParameterName(queryStringItem.Key);
                    databaseConnection.AddParameter(parameterName, queryStringItem.Value);
                    template = template.Replace($"'{{{queryStringItem.Key}}}'", $"?{parameterName}").Replace($"{{{queryStringItem.Key}}}", $"?{parameterName}");
                }
            }

            foreach (var key in configuration.Items.Keys)
            {
                if (!template.Contains($"{{{configuration.Items[key].StepName}}}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parameterName = DatabaseHelpers.CreateValidParameterName(configuration.Items[key].StepName);
                var valuesCanContainDashes = await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_ValuesCanContainDashes");
                if (!String.IsNullOrWhiteSpace(valuesCanContainDashes) && valuesCanContainDashes.Equals("true", StringComparison.OrdinalIgnoreCase) && configuration.Items[key].CurrentValue.Contains("-"))
                {
                    var value = configuration.Items[key].CurrentValue.Split('-')[1];
                    if (!isDataQuery)
                    {
                        template = template.Replace($"{{{configuration.Items[key].StepName}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        template = template.Replace($"'{{{configuration.Items[key].StepName}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].StepName}}}", $"?{parameterName}");
                    }
                }
                else if (configuration.Items[key].CurrentValue == "-1")
                {
                    var value = configuration.Items[key].CurrentValue.Split('-')[1];
                    if (!isDataQuery)
                    {
                        template = template.Replace($"{{{configuration.Items[key].StepName}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        template = template.Replace($"'{{{configuration.Items[key].StepName}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].StepName}}}", $"?{parameterName}");
                    }
                }
                else
                {
                    var value = configuration.Items[key].CurrentValue;
                    if (!isDataQuery)
                    {
                        template = template.Replace($"{{{configuration.Items[key].StepName}}}", value);
                    }
                    else
                    {
                        databaseConnection.AddParameter(parameterName, value);
                        template = template.Replace($"'{{{configuration.Items[key].StepName}}}'", $"?{parameterName}").Replace($"{{{configuration.Items[key].StepName}}}", $"?{parameterName}");
                    }
                }
            }

            return template;
        }

        /// <inheritdoc />
        public async Task<(string deliveryTime, string deliveryExtra)> GetDeliveryTimeAsync(ConfigurationsModel configuration)
        {
            var dataTable = await GetConfiguratorDataAsync(configuration.Configurator);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return ("", "");
            }

            var query = dataTable.Rows[0].Field<string>("deliverytime_query");

            if (String.IsNullOrWhiteSpace(query))
            {
                return ("", "");
            }

            query = await ReplaceConfiguratorItemsAsync(query, configuration, true);

            var deliveryTimeResultDataTable = await databaseConnection.GetAsync(query);
            if (deliveryTimeResultDataTable.Rows.Count == 0)
            {
                return ("", "");
            }

            var deliveryTime = deliveryTimeResultDataTable.Rows[0][0].ToString();
            var deliveryTimeExtra = "";

            if (deliveryTimeResultDataTable.Columns.Count > 1)
            {
                deliveryTimeExtra = deliveryTimeResultDataTable.Rows[0][1].ToString();
            }

            return (deliveryTime, deliveryTimeExtra);
        }

        /// <inheritdoc />
        public async Task<ExternalConfigurationModel> StartConfigurationExternallyAsync(VueConfigurationsModel vueConfiguration)
        {
            var query = $@"SELECT CAST(externalStartConfiguratorApi.`value` AS UNSIGNED) AS externalStartConfiguratorApi
FROM {WiserTableNames.WiserItem} AS configurator
JOIN {WiserTableNames.WiserItemDetail} AS externalStartConfiguratorApi ON externalStartConfiguratorApi.item_id = configurator.id AND externalStartConfiguratorApi.`key` = 'api_start'
WHERE configurator.entity_type = 'configurator' AND configurator.title = ?configuratorName";
            
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("configuratorName", vueConfiguration.ConfiguratorName);
            var dataTable = await databaseConnection.GetAsync(query);
            
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var startApi = await wiserItemsService.GetItemDetailsAsync(dataTable.Rows[0].Field<ulong>("externalStartConfiguratorApi"), entityType: "ConfiguratorApi", skipPermissionsCheck: true);
            
            var endpoint = startApi.GetDetailValue("endpoint");
            var requestJson = startApi.GetDetailValue("request_json");
            var externalConfigurationIdLey = startApi.GetDetailValue("external_configuration_id_key");
            query = startApi.GetDetailValue("api_query");

            if (String.IsNullOrWhiteSpace(endpoint) || String.IsNullOrWhiteSpace(externalConfigurationIdLey))
            {
                return null;
            }

            DataRow extraData = null;

            // If a query is set handle it to add extra information for the replacements in the JSON.
            if (!String.IsNullOrWhiteSpace(query))
            {
                query = await ReplaceConfiguratorItemsAsync(query, vueConfiguration, true);
                query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables: false, forQuery: true);
                var extraDataTable = await databaseConnection.GetAsync(query);

                if (extraDataTable.Rows.Count > 0)
                {
                    extraData = extraDataTable.Rows[0];
                }
            }

            endpoint = await ReplaceConfiguratorItemsAsync(endpoint, vueConfiguration, false);
            endpoint = await stringReplacementsService.DoAllReplacementsAsync(endpoint, extraData, removeUnknownVariables: false);

            var requestMethod = (Method) startApi.GetDetailValue<int>("request_type");

            var restClient = new RestClient();
            var restRequest = new RestRequest(endpoint, requestMethod);

            // If a body is provided perform replacements and add it to the request.
            if (!String.IsNullOrWhiteSpace(requestJson))
            {
                requestJson = await ReplaceConfiguratorItemsAsync(requestJson, vueConfiguration, false);
                requestJson = await stringReplacementsService.DoAllReplacementsAsync(requestJson, extraData, removeUnknownVariables: false);

                var regex = new Regex("([\"'])?{[^\\]}\\s]*}([\"'])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                requestJson = regex.Replace(requestJson, "null");

                restRequest.AddBody(requestJson, MediaTypeNames.Application.Json);
            }

            await AddAuthenticationToApiCall(restRequest, startApi);
            await AddAcceptLanguageToApiCall(restRequest);
            await AddCustomHeadersAsync(restRequest, startApi, vueConfiguration: vueConfiguration, extraData: extraData);

            var restResponse = await DoExternalConfiguratorApiCallAsync(restClient, restRequest);
            if (!restResponse.IsSuccessful || restResponse.Content == null)
            {
                logger.LogWarning("Error while trying to start the configuration at an API ({startApi}). The API response HTTP code was '{restResponseStatusCode}' and the result was: {restResponseContent}.\n\n{requestJson}",  startApi.Title, restResponse.StatusCode, restResponse.Content, requestJson);
                return null;
            }
            
            // Get the external configuration ID from the response.
            var responseData = JObject.Parse(restResponse.Content);
            var keyParts = new List<string>(externalConfigurationIdLey.Split('.'));
            var currentObject = responseData;

            // Step into the object till only 1 key part is left.
            while (keyParts.Count > 1)
            {
                currentObject = (JObject) currentObject[keyParts[0]];
                keyParts.RemoveAt(0);
                
                // If the current object is null, the key is not found.
                if (currentObject == null)
                {
                    return null;
                }
            }

            return new ExternalConfigurationModel()
            {
                Id = currentObject?[keyParts[0]]?.ToString()
            };
        }

        /// <inheritdoc />
        public async Task<bool> SendAnswerToExternalApiAsync(VueConfigurationsModel configuration, int stepId)
        {
            var query = $@"SELECT
	CAST(externalAnswerConfiguratorApi.`value` AS UNSIGNED) AS externalAnswerConfiguratorApi,
	apiExtraData.`key`,
	apiExtraData.`value`
FROM {WiserTableNames.WiserItemDetail} AS externalAnswerConfiguratorApi
LEFT JOIN {WiserTableNames.WiserItemDetail} AS apiExtraData ON apiExtraData.item_id = externalAnswerConfiguratorApi.item_id AND apiExtraData.groupname = 'api_extra_data'
WHERE externalAnswerConfiguratorApi.item_id = ?stepId
AND externalAnswerConfiguratorApi.`key` = 'api_answer'";
            
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("stepId", stepId);
            var dataTable = await databaseConnection.GetAsync(query);
            
            if (dataTable.Rows.Count == 0)
            {
                return false;
            }

            var answerApi = await wiserItemsService.GetItemDetailsAsync(dataTable.Rows[0].Field<UInt64>("externalAnswerConfiguratorApi"), entityType: "ConfiguratorApi", skipPermissionsCheck: true);
            
            var endpoint = answerApi.GetDetailValue("endpoint");
            var requestJson = answerApi.GetDetailValue("request_json");
            query = answerApi.GetDetailValue("api_query");

            if (String.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }
            
            // Get the the extra data for the API based on the group name of the details that are set in Wiser.
            var apiExtraData = new Dictionary<string, string>();
            foreach (DataRow row in dataTable.Rows)
            {
                apiExtraData.Add(row.Field<string>("key"), row.Field<string>("value"));
            }

            DataRow extraData = null;

            // If a query is set handle it to add extra information for the replacements in the JSON.
            if (!String.IsNullOrWhiteSpace(query))
            {
                query = stringReplacementsService.DoReplacements(query, apiExtraData, forQuery: true);
                query = await ReplaceConfiguratorItemsAsync(query, configuration, true);
                query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables: false, forQuery: true);
                var extraDataTable = await databaseConnection.GetAsync(query);

                if (extraDataTable.Rows.Count > 0)
                {
                    extraData = extraDataTable.Rows[0];
                }
            }
            
            endpoint = stringReplacementsService.DoReplacements(endpoint, apiExtraData);
            endpoint = await ReplaceConfiguratorItemsAsync(endpoint, configuration, false);
            endpoint = await stringReplacementsService.DoAllReplacementsAsync(endpoint, extraData, removeUnknownVariables: false);

            var requestMethod = (Method) answerApi.GetDetailValue<int>("request_type");

            var restClient = new RestClient();
            var restRequest = new RestRequest(endpoint, requestMethod);

            // If a body is provided perform replacements and add it to the request.
            if (!String.IsNullOrWhiteSpace(requestJson))
            {
                requestJson = stringReplacementsService.DoReplacements(requestJson, apiExtraData);
                requestJson = await ReplaceConfiguratorItemsAsync(requestJson, configuration, false);
                requestJson = await stringReplacementsService.DoAllReplacementsAsync(requestJson, extraData, removeUnknownVariables: false);

                var regex = new Regex("([\"'])?{[^\\]}\\s]*}([\"'])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                requestJson = regex.Replace(requestJson, "null");

                restRequest.AddBody(requestJson, MediaTypeNames.Application.Json);
            }

            await AddAuthenticationToApiCall(restRequest, answerApi);
            await AddAcceptLanguageToApiCall(restRequest);
            await AddCustomHeadersAsync(restRequest, answerApi, vueConfiguration: configuration, extraData: extraData);

            var restResponse = await DoExternalConfiguratorApiCallAsync(restClient, restRequest);
            if (!restResponse.IsSuccessful || restResponse.Content == null)
            {
                logger.LogWarning("Error while trying to send the answer to an API ({answerApi}). The API response HTTP code was '{restResponseStatusCode}' and the result was: {restResponseContent}.\n\n{requestJson}",  answerApi.Title, restResponse.StatusCode, restResponse.Content, requestJson);
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input)
        {
            (decimal purchasePrice, decimal customerPrice, decimal fromPrice) result = (0, 0, 0);

            var dataTable = await GetConfiguratorDataAsync(input.Configurator);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return result;
            }

            try
            {
                // Get the price from an API if available. If not it will return 0, 0, 0.
                var configuratorId = Convert.ToUInt64(dataTable.Rows[0].Field<object>("configuratorId"));
                var priceFromApi = await GetPriceFromApiAsync(configuratorId, input);
                result.purchasePrice += priceFromApi.purchasePrice;
                result.customerPrice += priceFromApi.customerPrice;
                result.fromPrice += priceFromApi.fromPrice;
            }
            // If an exception is thrown during the retrieval of the price from an API consider the full price to be invalid.
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to get price from an API.");
                return result;
            }

            var query = dataTable.Rows[0].Field<string>("price_calculation_query");

            if (String.IsNullOrEmpty(query))
            {
                return result;
            }

            query = await ReplaceConfiguratorItemsAsync(query, input, true);
            query = await stringReplacementsService.DoAllReplacementsAsync(query, null, true, true, false, true);

            var priceResultDataTable = await databaseConnection.GetAsync(query);
            if (priceResultDataTable.Rows.Count == 0)
            {
                return result;
            }

            var price = priceResultDataTable.Rows[0].IsNull(0) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][0]);
            var purchasePrice = 0m;
            var fromPrice = price;

            if (priceResultDataTable.Columns.Count > 1)
            {
                purchasePrice = priceResultDataTable.Rows[0].IsNull(1) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][1]);
            }

            if (priceResultDataTable.Columns.Count > 2)
            {
                fromPrice = priceResultDataTable.Rows[0].IsNull(2) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][2]);
            }

            result.purchasePrice += purchasePrice;
            result.customerPrice += price;
            result.fromPrice += fromPrice;

            return result;
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(VueConfigurationsModel input)
        {
            (decimal purchasePrice, decimal customerPrice, decimal fromPrice) result = (0, 0, 0);

            var configuratorData = await GetVueConfiguratorDataAsync(input.ConfiguratorName, false);
            if (configuratorData == null)
            {
                return result;
            }

            try
            {
                // Get the price from an API if available. If not it will return 0, 0, 0.
                var priceFromApi = await GetPriceFromApiAsync(configuratorData.ConfiguratorId, vueConfiguration: input);
                result.purchasePrice += priceFromApi.purchasePrice;
                result.customerPrice += priceFromApi.customerPrice;
                result.fromPrice += priceFromApi.fromPrice;
            }
            // If an exception is thrown during the retrieval of the price from an API consider the full price to be invalid.
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to get price from an API.");
                return result;
            }

            var query = configuratorData.PriceCalculationQuery;

            if (String.IsNullOrEmpty(query))
            {
                return result;
            }

            query = await ReplaceConfiguratorItemsAsync(query, input, true);
            query = await stringReplacementsService.DoAllReplacementsAsync(query, null, true, true, false, true);

            var priceResultDataTable = await databaseConnection.GetAsync(query);
            if (priceResultDataTable.Rows.Count == 0)
            {
                return result;
            }

            var price = priceResultDataTable.Rows[0].IsNull(0) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][0]);
            var purchasePrice = 0m;
            var fromPrice = price;

            if (priceResultDataTable.Columns.Count > 1)
            {
                purchasePrice = priceResultDataTable.Rows[0].IsNull(1) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][1]);
            }

            if (priceResultDataTable.Columns.Count > 2)
            {
                fromPrice = priceResultDataTable.Rows[0].IsNull(2) ? 0 : Convert.ToDecimal(priceResultDataTable.Rows[0][2]);
            }

            result.purchasePrice += purchasePrice;
            result.customerPrice += price;
            result.fromPrice += fromPrice;

            return result;
        }

        /// <summary>
        /// Retrieve the price from an external API. It's not necessary to pass both configuration and vueConfiguration, only one of them is required.
        /// </summary>
        /// <param name="configuratorId">The Wiser ID of the configurator settings.</param>
        /// <param name="configuration">The configuration of a legacy configurator.</param>
        /// <param name="vueConfiguration">The configuration of a configurator running in "Vue" mode.</param>
        /// <returns>The purchase price, customer price and from price as a <see cref="ValueTuple{T1,T2,T3}"/>.</returns>
        private async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> GetPriceFromApiAsync(ulong configuratorId, ConfigurationsModel configuration = null, VueConfigurationsModel vueConfiguration = null)
        {
            (decimal purchasePrice, decimal customerPrice, decimal fromPrice) result = (0, 0, 0);

            if (configuration == null && vueConfiguration == null)
            {
                return result;
            }

            var priceApis = await wiserItemsService.GetLinkedItemDetailsAsync(configuratorId, 41, "ConfiguratorApi");

            foreach (var priceApi in priceApis)
            {
                try
                {
                    var endpoint = priceApi.GetDetailValue("endpoint");
                    var requestJson = priceApi.GetDetailValue("request_json");
                    var purchasePriceKey = priceApi.GetDetailValue("price_calculation_purchase_price_key");
                    var customerPriceKey = priceApi.GetDetailValue("price_calculation_customer_price_key");
                    var fromPriceKey = priceApi.GetDetailValue("price_calculation_from_price_key");
                    var query = priceApi.GetDetailValue("api_query");
                    var requestMethod = (Method) priceApi.GetDetailValue<int>("request_type");

                    if (String.IsNullOrWhiteSpace(endpoint) || (requestMethod != Method.Get && String.IsNullOrWhiteSpace(requestJson)) || String.IsNullOrWhiteSpace(purchasePriceKey) || String.IsNullOrWhiteSpace(customerPriceKey) || String.IsNullOrWhiteSpace(fromPriceKey))
                    {
                        continue;
                    }

                    DataRow extraData = null;

                    // If a query is set handle it to add extra information for the replacements in the JSON.
                    if (!String.IsNullOrWhiteSpace(query))
                    {
                        query = await ReplaceConfiguratorItemsAsync(query, configuration, true);
                        query = await ReplaceConfiguratorItemsAsync(query, vueConfiguration, true);
                        query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables: false, forQuery: true);
                        var extraDataTable = await databaseConnection.GetAsync(query);

                        if (extraDataTable.Rows.Count > 0)
                        {
                            extraData = extraDataTable.Rows[0];
                        }
                    }

                    endpoint = await ReplaceConfiguratorItemsAsync(endpoint, configuration, false);
                    endpoint = await ReplaceConfiguratorItemsAsync(endpoint, vueConfiguration, false);
                    endpoint = await stringReplacementsService.DoAllReplacementsAsync(endpoint, extraData, removeUnknownVariables: false);
                    
                    var restClient = new RestClient();
                    var restRequest = new RestRequest(endpoint, requestMethod);
                    
                    // If a body is provided perform replacements and add it to the request.
                    if (!String.IsNullOrWhiteSpace(requestJson))
                    {
                        requestJson = await ReplaceConfiguratorItemsAsync(requestJson, configuration, false);
                        requestJson = await ReplaceConfiguratorItemsAsync(requestJson, vueConfiguration, false);
                        requestJson = await stringReplacementsService.DoAllReplacementsAsync(requestJson, extraData, removeUnknownVariables: false);

                        var regex = new Regex("([\"'])?{[^\\]}\\s]*}([\"'])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                        requestJson = regex.Replace(requestJson, "null");

                        // If there is no request JSON if a body is expected it is useless to do an API call.
                        if (requestMethod != Method.Get && String.IsNullOrWhiteSpace(requestJson))
                        {
                            continue;
                        }

                        restRequest.AddBody(requestJson, MediaTypeNames.Application.Json);
                    }

                    await AddAuthenticationToApiCall(restRequest, priceApi);
                    await AddAcceptLanguageToApiCall(restRequest);
                    await AddCustomHeadersAsync(restRequest, priceApi, configuration, extraData: extraData);

                    var restResponse = await DoExternalConfiguratorApiCallAsync(restClient, restRequest);
                    if (!restResponse.IsSuccessful || restResponse.Content == null)
                    {
                        logger.LogWarning("Error while trying to get price from an API ({priceApi}). The API response HTTP code was '{restResponseStatusCode}' and the result was: {restResponseContent}.\n\n{requestJson}",  priceApi.Title, restResponse.StatusCode, restResponse.Content, requestJson);
                        continue;
                    }

                    // Get the three different prices from the response.
                    var responseData = JObject.Parse(restResponse.Content);
                    result.purchasePrice += GetPriceValueFromResponse(responseData, purchasePriceKey);
                    result.customerPrice += GetPriceValueFromResponse(responseData, customerPriceKey);
                    result.fromPrice += GetPriceValueFromResponse(responseData, fromPriceKey);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error while trying to get price from an API.");
                }
            }

            return result;
        }

        /// <summary>
        /// Get the price value from the response based on the given key.
        /// </summary>
        /// <param name="responseData">The JObject from the response content.</param>
        /// <param name="key">The key the final value, separated by a comma.</param>
        /// <returns>Returns the decimal value from the response based on the given key.</returns>
        private decimal GetPriceValueFromResponse(JObject responseData, string key)
        {
            var keyParts = new List<string>(key.Split('.'));
            var currentObject = responseData;

            // Step into the object till only 1 key part is left.
            while (keyParts.Count > 1)
            {
                currentObject = (JObject)currentObject[keyParts[0]];
                keyParts.RemoveAt(0);
            }

            return Convert.ToDecimal(currentObject[keyParts[0]]);
        }

        /// <inheritdoc />
        public async Task<ulong> SaveConfigurationAsync(ConfigurationsModel input, ulong? parentId = null)
        {
            var prices = await CalculatePriceAsync(input);
            var saveZeroPriceConfigurations = (await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_SaveZeroPriceConfigurations")).Equals("true", StringComparison.OrdinalIgnoreCase);

            // Return if price of configuration is 0 and configurations with zero price must not be saved
            if (!saveZeroPriceConfigurations && prices.customerPrice <= 0)
            {
                logger.LogInformation("Saving configuration skipped because of zero price.");
                return 0;
            }

            var configuration = new WiserItemModel
            {
                Title = "Configuration",
                EntityType = "configuration",
                ModuleId = 854
            };
            var deliveryTime = await GetDeliveryTimeAsync(input);

            // add optional
            var saveConfigQuery = await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_SaveConfigurationQuery");
            await AddItemDetailsFromQueryToWiserItemModelAsync(saveConfigQuery, configuration, input.QueryStringItems);

            // set up details
            configuration.Details.AddRange(new List<WiserItemDetailModel>
            {
                new() { Key = "quantity", Value = input.Quantity },
                new() { Key = "purchase_price", Value = prices.purchasePrice },
                new() { Key = "sales_price", Value = prices.customerPrice },
                new() { Key = "from_price", Value = prices.fromPrice },
                new() { Key = "delivery_time", Value = deliveryTime.deliveryTime },
                new() { Key = "delivery_time_extra", Value = deliveryTime.deliveryExtra },
                new() { Key = "image_url", Value = input.Image },
            });

            // add all querystring items
            configuration.Details.AddRange(input.QueryStringItems.Select(x => new WiserItemDetailModel { Key = x.Key, Value = x.Value }));

            // save main item
            await wiserItemsService.SaveAsync(configuration, parentId, skipPermissionsCheck: true, saveHistory: false);

            // save configuration line query, we run this query to get all the other variables that need to be added to the configuration line like ean, purchaseprice etc.
            var saveConfigLineQuery = await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_SaveConfigurationLineQuery");

            // loop through input items
            foreach (var item in input.Items.Select(item => item.Value))
            {
                // create new WiserItem per input item
                var configurationItem = new WiserItemModel
                {
                    ParentItemId = configuration.Id,
                    Title = $"{item.Name}: {item.ValueName}",
                    EntityType = "configurationline",
                    ModuleId = 854
                };

                // add optional details, replace the id and value in saveConfigLineQuery
                await AddItemDetailsFromQueryToWiserItemModelAsync(saveConfigLineQuery,
                    configurationItem,
                    new Dictionary<string, string>()
                    {
                        { "id", item.Id },
                        { "value", item.Value }
                    });

                // add details
                configurationItem.Details.AddRange(new List<WiserItemDetailModel>
                {
                    new() { Key = "id", Value = item.Id },
                    new() { Key = "name", Value = item.Name },
                    new() { Key = "value_name", Value = item.ValueName },
                    new() { Key = "value", Value = item.Value },
                    new() { Key = "main_step", Value = item.MainStep },
                    new() { Key = "step", Value = item.Step },
                    new() { Key = "sub_step", Value = item.SubStep }
                });

                foreach (var extraDataDictionary in item.ExtraData.Where(extraDataDictionary => extraDataDictionary.Any()))
                {
                    configurationItem.Details.AddRange(extraDataDictionary.Select(x => new WiserItemDetailModel { Key = x.Key, Value = x.Value }));
                }

                // save configuration line
                await wiserItemsService.SaveAsync(configurationItem, skipPermissionsCheck: true, saveHistory: false);
            }

            var dataTable = await GetConfiguratorDataAsync(input.Configurator);
            var configuratorId = Convert.ToUInt64(dataTable.Rows[0].Field<object>("configuratorId"));
            var saveApis = await wiserItemsService.GetLinkedItemDetailsAsync(configuratorId, 42, "ConfiguratorApi");

            foreach (var saveApi in saveApis)
            {
                try
                {
                    var endpoint = saveApi.GetDetailValue("endpoint");
                    var requestJson = saveApi.GetDetailValue("request_json");
                    var supplierIdKey = saveApi.GetDetailValue("supplier_id_key");
                    var query = saveApi.GetDetailValue("api_query");

                    if (String.IsNullOrWhiteSpace(endpoint) || String.IsNullOrWhiteSpace(requestJson))
                    {
                        continue;
                    }

                    DataRow extraData = null;

                    // If a query is set handle it to add extra information for the replacements in the JSON.
                    if (!String.IsNullOrWhiteSpace(query))
                    {
                        if (parentId != null)
                        {
                            var data = new Dictionary<string, object>()
                            {
                                {"gcl_configuration_parent_id", parentId}
                            };

                            query = stringReplacementsService.DoReplacements(query, data, forQuery: true);
                        }

                        query = await ReplaceConfiguratorItemsAsync(query, input, true);
                        query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables: false, forQuery: true);
                        var extraDataTable = await databaseConnection.GetAsync(query);

                        if (extraDataTable.Rows.Count > 0)
                        {
                            extraData = extraDataTable.Rows[0];
                        }
                    }

                    endpoint = await ReplaceConfiguratorItemsAsync(endpoint, input, false);
                    endpoint = await stringReplacementsService.DoAllReplacementsAsync(endpoint, extraData, removeUnknownVariables: false);

                    requestJson = await ReplaceConfiguratorItemsAsync(requestJson, input, false);
                    requestJson = await stringReplacementsService.DoAllReplacementsAsync(requestJson, extraData, removeUnknownVariables: false);

                    var regex = new Regex("([\"'])?{[^\\]}\\s]*}([\"'])?", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
                    requestJson = regex.Replace(requestJson, "null");

                    // If there is no request JSON it is useless to do an API call.
                    if (String.IsNullOrWhiteSpace(requestJson))
                    {
                        continue;
                    }

                    configuration.Details.Add(new WiserItemDetailModel
                    {
                        Key = "gcl_endpoint",
                        Value = endpoint,
                        GroupName = saveApi.Title
                    });

                    var requestMethod = (Method) saveApi.GetDetailValue<int>("request_type");

                    var restClient = new RestClient();
                    var restRequest = new RestRequest(endpoint, requestMethod);

                    await AddAuthenticationToApiCall(restRequest, saveApi);
                    await AddAcceptLanguageToApiCall(restRequest);
                    await AddCustomHeadersAsync(restRequest, saveApi, input, extraData: extraData);

                    restRequest.AddBody(requestJson, MediaTypeNames.Application.Json);

                    configuration.Details.Add(new WiserItemDetailModel()
                    {
                        Key = "gcl_request",
                        Value = requestJson,
                        GroupName = saveApi.Title
                    });

                    var restResponse = await DoExternalConfiguratorApiCallAsync(restClient, restRequest);

                    configuration.Details.Add(new WiserItemDetailModel()
                    {
                        Key = "gcl_response",
                        Value = restResponse.Content,
                        GroupName = saveApi.Title
                    });

                    if (!restResponse.IsSuccessful || restResponse.Content == null)
                    {
                        // Save the configuration so the response is logged.
                        await wiserItemsService.SaveAsync(configuration, skipPermissionsCheck: true, saveHistory: false);

                        // Log the error and throw an exception.
                        var messageResponsePart = String.IsNullOrWhiteSpace(restResponse.Content) ? "<empty>" : restResponse.Content;

                        logger.LogError("Error while trying to execute Save API '{saveApiTitle}' - Status code: '{restResponseStatusCode}' - Response from API: {restResponseContent}", saveApi.Title, restResponse.StatusCode.ToString("D"), messageResponsePart);
                        throw new Exception($"Error while trying to execute Save API '{saveApi.Title}' - Status code: '{restResponse.StatusCode:D}' - Response from API: {messageResponsePart}");
                    }

                    // If the call only needed to be made and no supplier ID needs to be retrieved the last part can be skipped.
                    if (String.IsNullOrWhiteSpace(supplierIdKey))
                    {
                        await wiserItemsService.SaveAsync(configuration, skipPermissionsCheck: true, saveHistory: false);
                        continue;
                    }

                    // Get the supplier ID from the response.
                    var responseData = JObject.Parse(restResponse.Content);
                    var keyParts = new List<string>(supplierIdKey.Split('.'));
                    var currentObject = responseData;

                    // Step into the object till only 1 key part is left.
                    while (keyParts.Count > 1)
                    {
                        currentObject = (JObject) currentObject[keyParts[0]];
                        keyParts.RemoveAt(0);
                    }

                    var supplierId = currentObject[keyParts[0]].ToString();
                    configuration.Details.Add(new WiserItemDetailModel()
                    {
                        Key = "gcl_supplier_id",
                        Value = supplierId,
                        GroupName = saveApi.Title
                    });
                    
                    await wiserItemsService.SaveAsync(configuration, skipPermissionsCheck: true, saveHistory: false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error thrown during the saving of a configuration at an external API.");

                    configuration.Details.Add(new WiserItemDetailModel
                    {
                        Key = "gcl_save_configuration_exception",
                        Value = e.ToString(),
                        GroupName = saveApi.Title
                    });
                    
                    await wiserItemsService.SaveAsync(configuration, skipPermissionsCheck: true, saveHistory: false);
                }
            }

            return configuration.Id;
        }

        /// <summary>
        /// Save extra item details based on other query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="item"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task AddItemDetailsFromQueryToWiserItemModelAsync(string query, WiserItemModel item, Dictionary<string, string> parameters = null)
        {
            // if save query is not empty, run query and save result
            if (String.IsNullOrWhiteSpace(query))
            {
                return;
            }

            // replace system objects in query
            query = await stringReplacementsService.DoAllReplacementsAsync(query, removeUnknownVariables:false, forQuery:true);

            if (parameters is {Count: > 0})
            {
                query = stringReplacementsService.DoReplacements(query, parameters, forQuery: true);

                foreach (var parameter in parameters)
                {
                    databaseConnection.AddParameter(parameter.Key, parameter.Value);
                }
            }

            var saveConfigLineDataTable = await databaseConnection.GetAsync(query);
            if (saveConfigLineDataTable.Rows.Count <= 0)
            {
                return;
            }

            foreach (DataRow row in saveConfigLineDataTable.Rows)
            {
                var itemDetail = new WiserItemDetailModel
                {
                    Key = row.Field<string>("itemdetail_name"),

                    // we dont know the type that is returned from the query, so we save as is without .field<>
                    Value = row["itemdetail_value"]
                };
                item.Details.Add(itemDetail);
            }
        }

        /// <summary>
        /// Add the correct authentication to the API request.
        /// </summary>
        /// <param name="request">The request to add the authentication to.</param>
        /// <param name="configuratorApi">A ConfiguratorApi entity item to determine the authentication from.</param>
        private async Task AddAuthenticationToApiCall(RestRequest request, WiserItemModel configuratorApi)
        {
            var authenticationType = configuratorApi.GetDetailValue<int>("authentication_type");

            switch (authenticationType)
            {
                case 0: // When not set skip authentication. Either no authentication is needed or done by custom header.
                    return;
                case 1: // OAuth 2.0
                    // TODO handle OAuth 2
                    request.AddHeader("Authorization", $"Bearer {await objectsService.GetSystemObjectValueAsync("configurator_api_token")}");
                    throw new ArgumentOutOfRangeException($"Token type '{authenticationType}' is not yet implemented.");
                case 2: // Token
                    var token = configuratorApi.GetDetailValue("token");
                    token = await stringReplacementsService.DoAllReplacementsAsync(token);
                    request.AddHeader("Authorization", $"Token {token}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Token type '{authenticationType}' is not yet implemented.");
            }
        }

        /// <summary>
        /// Add the correct Accept-Language header to the API request if one is provided as system object.
        /// </summary>
        /// <param name="request">The request to add the Accept-Language header to.</param>
        private async Task AddAcceptLanguageToApiCall(RestRequest request)
        {
            var languageCode = await objectsService.FindSystemObjectByDomainNameAsync("configurator_api_language_code");
            if (!String.IsNullOrWhiteSpace(languageCode))
            {
                request.AddHeader("Accept-Language", languageCode);
            }
        }
        
        /// <summary>
        /// Add the custom headers to the request that are set in the <see cref="configuratorApi"/>.
        /// </summary>
        /// <param name="request">The request to add the headers to.</param>
        /// <param name="configuratorApi">The configurator API settings to set the headers from.</param>
        /// <param name="configuration">The configuration to use for configuration replacements in the header's value.</param>
        /// <param name="vueConfiguration">The Vue configuration to use for configuration replacements in the header's value.</param>
        /// <param name="extraData">Extra data to use for replacements in the header's value.</param>
        private async Task AddCustomHeadersAsync(RestRequest request, WiserItemModel configuratorApi, ConfigurationsModel configuration = null, VueConfigurationsModel vueConfiguration = null, DataRow extraData = null)
        {
            foreach (var detail in configuratorApi.Details)
            {
                if (!String.Equals(detail.GroupName, "headers", StringComparison.CurrentCultureIgnoreCase) || String.IsNullOrWhiteSpace(detail.Key)) continue;

                var value = detail.Value.ToString();
                if (String.IsNullOrWhiteSpace(value)) continue;

                if (configuration != null)
                {
                    value = await ReplaceConfiguratorItemsAsync(value, configuration, false);
                }

                if (vueConfiguration != null)
                {
                    value = await ReplaceConfiguratorItemsAsync(value, vueConfiguration, false);
                }

                value = await stringReplacementsService.DoAllReplacementsAsync(value, extraData, removeUnknownVariables: false);
                
                // Check if there is still a value after replacements.
                if (String.IsNullOrWhiteSpace(value)) continue;

                request.AddHeader(detail.Key, value);
            }
        }

        /// <summary>
        /// Execute the API call and return the response.
        /// If the retry settings are set they will be applied.
        /// </summary>
        /// <param name="client">The client to use to make the request.</param>
        /// <param name="request">The request to send to the external API.</param>
        /// <returns>The response of the request.</returns>
        private async Task<RestResponse> DoExternalConfiguratorApiCallAsync(RestClient client, RestRequest request)
        {
            var retrySettings = await GetConfiguratorApiRetrySettingsAsync();
            var retryCount = 0;

            RestResponse response;
            
            do
            {
                response = await client.ExecuteAsync(request);

                // If there is no retry enabled or the response code does not need to be retried directly return the response.
                if (retrySettings.retryCount == 0 || !retrySettings.statusCodes.Contains((int)response.StatusCode))
                {
                    return response;
                }

                if (retryCount++ >= retrySettings.retryCount)
                {
                    return response;
                }
                
                Thread.Sleep(retrySettings.retryDelay);    
            } while (retryCount <= retrySettings.retryCount);

            return response;
        }
        
        /// <summary>
        /// Get the settings for the configurator API retry from the database if they are set.
        /// </summary>
        /// <returns>Returns the retry count, delay and the status codes to retry.</returns>
        private async Task<(int retryCount, int retryDelay, IReadOnlyList<int> statusCodes)> GetConfiguratorApiRetrySettingsAsync()
        {
            var retryCount = await objectsService.GetSystemObjectValueAsync("configurator_api_retry_count");
            Int32.TryParse(retryCount, out var retryCountValue);

            var retryDelay = await objectsService.GetSystemObjectValueAsync("configurator_api_retry_delay");
            Int32.TryParse(retryDelay, out var retryDelayValue);

            // Get the status codes that need to be retried.
            var retryStatusCodes = (await objectsService.GetSystemObjectValueAsync("configurator_api_retry_status_codes")).Split(",");
            var retryStatusCodesList = new List<int>();

            foreach(var statusCode in retryStatusCodes)
            {
                if (String.IsNullOrWhiteSpace(statusCode)) continue;
                
                Int32.TryParse(statusCode, out var statusCodeValue);
                retryStatusCodesList.Add(statusCodeValue);
            }
            
            return (retryCountValue, retryDelayValue, retryStatusCodesList);
        }
    }
}