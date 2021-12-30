using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Operators;

namespace GeeksCoreLibrary.Components.Configurator.Services
{
    public class ConfiguratorsService : IConfiguratorsService, IScopedService
    {
        private readonly ILogger<Configurator> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;

        private readonly string[] queryFields =
        {
            "configuratorId","mainStepId","stepId","subStepId","duplicateConfiguratorId","duplicateMainStepId","duplicateStepId","duplicateSubStepId","summary_template", "summary_mainstep_template", "summary_step_template", "progress_pre_template", "progress_pre_step_template", "progress_pre_substep_template", "progress_post_template", "progress_post_step_template", "progress_post_substep_template", "progress_template", "progress_step_template", "progress_substep_template", "name", "configurator_free_content1", "configurator_free_content2", "configurator_free_content3", "configurator_free_content4",
            "configurator_free_content5", "template", "price_calculation_query", "deliverytime_query", "custom_param_name", "custom_param_dependencies", "custom_param_query", "pre_render_steps_query", "mainstep_template", "mainstepname", "mainsteps_values_template", "mainsteps_datasource", "mainsteps_custom_query", "mainsteps_own_data_values", "mainsteps_fixed_valuelist", "mainsteps_datasource_connectedtype", "mainsteps_datasource_connectedid", "mainsteps_isrequired", "mainsteps_check_connectedid", "mainstep_free_content1", "mainstep_free_content2", "mainstep_free_content3",
            "mainstep_free_content4", "mainstep_free_content5", "step_template", "stepname", "values_template", "datasource", "custom_query", "own_data_values", "fixed_valuelist", "datasource_connectedtype", "variable_name", "step_free_content1", "step_free_content2", "step_free_content3", "step_free_content4", "step_free_content5", "datasource_connectedid", "isrequired", "check_connectedid", "substepname", "substep_template", "substep_values_template", "substep_datasource",
            "substep_custom_query", "substep_own_data_values", "substep_fixed_valuelist", "substep_datasource_connectedtype", "substep_variable_name", "substep_datasource_connectedid", "substep_isrequired", "substep_check_connectedid", "substep_free_content1", "substep_free_content2", "substep_free_content3", "substep_free_content4", "substep_free_content5", "urlregex", "configurator_step_template"
        };

        private readonly List<(string prefix, string fieldName)> configuratorFields = new List<(string prefix, string fieldName)> { ("", "name"), ("", "summary_template"), ("", "summary_mainstep_template"), ("", "summary_step_template"), ("", "progress_pre_template"), ("", "progress_pre_step_template"), ("", "progress_pre_substep_template"), ("", "progress_post_template"), ("", "progress_post_step_template"), ("", "progress_post_substep_template"), ("", "progress_template"), ("", "progress_step_template"), ("", "progress_substep_template"), ("configurator_", "free_content1"), ("configurator_", "free_content2"), ("configurator_", "free_content3"), ("configurator_", "free_content4"), ("configurator_", "free_content5"), ("", "template"), ("", "deliverytime_query"), ("", "custom_param_name"), ("", "custom_param_dependencies"), ("", "custom_param_query"), ("", "pre_render_steps_query"), ("configurator_", "step_template"), ("", "price_calculation_query") };
        //("mainstep_", "template"),
        private readonly List<(string prefix, string fieldName)> mainStepFields = new List<(string prefix, string fieldName)> { ("main", "step_template"),  ("", "mainstepname"), ("mainsteps_", "values_template"), ("mainsteps_", "datasource"), ("mainsteps_", "custom_query"), ("mainsteps_", "own_data_values"), ("mainsteps_", "fixed_valuelist"), ("mainsteps_", "datasource_connectedtype"), ("mainsteps_", "datasource_connectedid"), ("mainsteps_", "isrequired"), ("mainsteps_", "check_connectedid"), ("mainstep_", "free_content1"), ("mainstep_", "free_content2"), ("mainstep_", "free_content3"), ("mainstep_", "free_content4"), ("mainstep_", "free_content5") };
        private readonly List<(string prefix, string fieldName)> stepFields = new List<(string prefix, string fieldName)> { ("", "step_template"), ("", "stepname"), ("", "values_template"), ("", "datasource"), ("", "custom_query"), ("", "own_data_values"), ("", "fixed_valuelist"), ("", "datasource_connectedtype"), ("", "variable_name"), ("", "datasource_connectedid"), ("", "isrequired"), ("", "check_connectedid"), ("step_", "free_content1"), ("step_", "free_content2"), ("step_", "free_content3"), ("step_", "free_content4"), ("step_", "free_content5") };
        private readonly List<(string prefix, string fieldName)> subStepFields = new List<(string prefix, string fieldName)> { ("sub", "step_template"), ("", "substepname"), ("substep_", "values_template"), ("substep_", "datasource"), ("substep_", "custom_query"), ("substep_", "own_data_values"), ("substep_", "fixed_valuelist"), ("substep_", "datasource_connectedtype"), ("substep_", "variable_name"), ("substep_", "datasource_connectedid"), ("substep_", "isrequired"), ("substep_", "check_connectedid"), ("substep_", "free_content1"), ("substep_", "free_content2"), ("substep_", "free_content3"), ("substep_", "free_content4"), ("substep_", "free_content5") };

        public ConfiguratorsService(ILogger<Configurator> logger, IDatabaseConnection databaseConnection, IObjectsService objectsService)
        {
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
        }

        /// <summary>
        /// Get configurator data from cache or database.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="componentId"></param>
        /// <returns></returns>
        public async Task<DataTable> GetConfiguratorDataAsync(string name, int componentId)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("name", name);
            // Get all connected steps/substeps
            var dataTable = await databaseConnection.GetAsync(@$"
               SELECT 
                    configurator.id AS configuratorId, 
                    mainstep.id AS mainStepId, 
                    step.id AS stepId,
                    IFNULL(substep.id, '0') AS subStepId,
                    IFNULL(duplicateConfigurator.`value`, '0') AS duplicateConfiguratorId,
                    IFNULL(duplicateMainStep.`value`, '0') AS duplicateMainStepId,
                    IFNULL(duplicateStep.`value`, '0') AS duplicateStepId,
                    IFNULL(duplicateSubStep.`value`, '0') AS duplicateSubStepId
                FROM {WiserTableNames.WiserItem} configurator
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateConfigurator ON duplicateConfigurator.item_id = configurator.id AND duplicateConfigurator.`key` = 'duplicatelayoutfrom'

                JOIN {WiserTableNames.WiserItemLink} mainStepLink ON mainStepLink.destination_item_id = configurator.id
                JOIN {WiserTableNames.WiserItem} mainStep ON mainStep.id = mainStepLink.item_id AND mainStep.entity_type = 'hoofdstap'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateMainStep ON duplicateMainStep.item_id = mainStep.id AND duplicateMainStep.`key` = 'duplicatelayoutfrom'

                JOIN {WiserTableNames.WiserItemLink} stepLink ON stepLink.destination_item_id = mainStep.id
                JOIN {WiserTableNames.WiserItem} step ON step.id = stepLink.item_id AND step.entity_type = 'stap'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateStep ON duplicateStep.item_id = step.id AND duplicateStep.`key` = 'duplicatelayoutfrom'

                LEFT JOIN {WiserTableNames.WiserItemLink} subStepLink ON subStepLink.destination_item_id = step.id
                LEFT JOIN {WiserTableNames.WiserItem} subStep ON subStep.id = subStepLink.item_id AND subStep.entity_type = 'substap'
                LEFT JOIN {WiserTableNames.WiserItemDetail} duplicateSubStep ON duplicateSubStep.item_id = subStep.id AND duplicateSubStep.`key` = 'duplicatelayoutfrom'
                WHERE configurator.moduleid = 800 AND configurator.entity_type = 'configurator' AND configurator.title = ?name
                ORDER BY mainStepLink.ordering, stepLink.ordering, subStepLink.ordering ");

            var returnDataTable = new DataTable();

            foreach (var queryField in queryFields)
            {
                returnDataTable.Columns.Add(queryField);
            }

            //test.Columns.AddRange(new List<DataColumn>());
            if (dataTable.Rows.Count == 0)
            {
                return returnDataTable;
            }

            // create one list item per step/substep
            foreach (DataRow dr in dataTable.Rows)
            {
                returnDataTable.Rows.Add(new object[] {
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

            dataTable = await databaseConnection.GetAsync(@$"
                    SELECT item.id, item.title, item.entity_type, detail.`key`, CONCAT_WS('', detail.`value`, detail.long_value) AS `value`
                    FROM {WiserTableNames.WiserItem} item
                    JOIN {WiserTableNames.WiserItemDetail} detail ON detail.item_id = item.id
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

                            foreach (DataRow item in items)
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

                            foreach (DataRow item in items)
                            {
                                if (!String.IsNullOrWhiteSpace(titleField) && String.IsNullOrWhiteSpace(item["mainstepname"].ToString()))
                                {
                                    item["mainstepname"] = titleField;
                                }
                                var foundField = mainStepFields.FirstOrDefault(x => x.fieldName == keyField);
                                if (foundField.prefix == null || foundField.fieldName == null)
                                {
                                    break;
                                }
                                item[$"{foundField.prefix}{foundField.fieldName}"] = valueField;
                            }
                            break;
                        }
                    case "stap":
                        {
                            var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["stepId"]) == idField);

                            foreach (DataRow item in items)
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
                                    item["variable_name"] = titleField.ToMySqlSafeValue();
                                }
                            }
                            break;
                        }
                    case "substap":
                        {
                            var items = returnDataTable.AsEnumerable().Where(x => Convert.ToUInt64(x["subStepId"]) == idField);

                            foreach (DataRow item in items)
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
                                    item["substep_variable_name"] = titleField.ToMySqlSafeValue();
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
                if (String.IsNullOrWhiteSpace(returnRow["step_template"].ToString())) // || returnRow.IsNull("mainstep_template")
                {
                    returnRow["step_template"] = returnRow["configurator_step_template"];
                }

                if (String.IsNullOrWhiteSpace(returnRow["mainstep_template"].ToString())) // || returnRow.IsNull("mainstep_template")
                {
                    returnRow["mainstep_template"] = returnRow["configurator_step_template"];
                }

               
            }

            return returnDataTable;
        }
    }
}
