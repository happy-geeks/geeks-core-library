using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Models;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Modules.Databases.Helpers
{
    public class WiserTableDefinitions
    {
        public static readonly List<WiserTableDefinitionModel> TablesToUpdate = new()
        {
            // wiser_item
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserItem,
                LastUpdate = new DateTime(2021, 9, 1),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("original_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new("unique_uuid", MySqlDbType.VarChar, 200, notNull: true, defaultValue: ""),
                    new("parent_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "0"),
                    new("moduleid", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0"),
                    new("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "15"),
                    new("readonly", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                    new("title", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("added_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("changed_on", MySqlDbType.DateTime, notNull: true, updateTimeStampOnChange: true),
                    new("changed_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserItem, "idx_module_env", IndexTypes.Normal, new List<string> { "moduleid", "published_environment" }),
                    new(WiserTableNames.WiserItem, "idx_entity", IndexTypes.Normal, new List<string> { "entity_type", "unique_uuid" }),
                    new(WiserTableNames.WiserItem, "idx_unique_uuid", IndexTypes.Normal, new List<string> { "unique_uuid" }),
                    new(WiserTableNames.WiserItem, "idx_original_item_id", IndexTypes.Normal, new List<string> { "original_item_id" }),
                    new(WiserTableNames.WiserItem, "idx_parent", IndexTypes.Normal, new List<string> { "parent_item_id", "entity_type" })
                }
            },

            // wiser_grant_store
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserGrantStore,
                LastUpdate = new DateTime(2022, 1, 1),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("key", MySqlDbType.VarChar, 512, notNull: true),
                    new("type", MySqlDbType.VarChar, 255, notNull: true),
                    new("client_id", MySqlDbType.VarChar, 50, notNull: true),
                    new("data", MySqlDbType.MediumText),
                    new("subject_id", MySqlDbType.VarChar, 255, notNull: true),
                    new("description", MySqlDbType.VarChar, 512),
                    new("creation_time", MySqlDbType.DateTime, notNull: true),
                    new("expiration", MySqlDbType.DateTime, notNull: true),
                    new("session_id", MySqlDbType.VarChar, 255)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserGrantStore, "idx_key", IndexTypes.Unique, new List<string> { "key" })
                }
            },

            // wiser_grant_store
            new WiserTableDefinitionModel
            {
                Name = Components.Account.Models.Constants.AuthenticationTokensTableName,
                LastUpdate = new DateTime(2022, 1, 1),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("selector", MySqlDbType.VarChar, 32, notNull: true),
                    new("hashed_validator", MySqlDbType.VarChar, 150, notNull: true),
                    new("user_id", MySqlDbType.Int64, notNull: true),
                    new("main_user_id", MySqlDbType.Int64, notNull: true),
                    new("entity_type", MySqlDbType.VarChar, 255, notNull: true),
                    new("main_user_entity_type", MySqlDbType.VarChar, 255, notNull: true),
                    new("role", MySqlDbType.VarChar, 255),
                    new("ip_address", MySqlDbType.VarChar, 255, notNull: true),
                    new("user_agent", MySqlDbType.VarChar, 2000),
                    new("login_date", MySqlDbType.DateTime, notNull: true),
                    new("expires", MySqlDbType.DateTime, notNull: true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(Components.Account.Models.Constants.AuthenticationTokensTableName, "idx_selector", IndexTypes.Unique, new List<string> { "selector", "entity_type" })
                }
            },

            // wiser_entityproperty
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserEntityProperty,
                LastUpdate = new DateTime(2022, 4, 25),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("entity_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("link_type", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("visible_in_overview", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("overview_fieldtype", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                    new("overview_width", MySqlDbType.Int24, notNull: true, defaultValue: "100"),
                    new("tab_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("group_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("inputtype", MySqlDbType.Enum, notNull: true, defaultValue: "input", enumValues: new List<string> { "input", "secure-input", "textbox", "radiobutton", "checkbox", "combobox", "multiselect", "numeric-input", "file-upload", "HTMLeditor", "querybuilder", "date-time picker", "grid", "imagecoords", "button", "image-upload", "gpslocation", "daterange", "sub-entities-grid", "item-linker", "color-picker", "auto-increment", "linked-item", "action-button", "data-selector", "chart", "scheduler", "timeline", "empty", "iframe" }),
                    new("display_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("property_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("explanation", MySqlDbType.MediumText),
                    new("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "1"),
                    new("regex_validation", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("mandatory", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("readonly", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("default_value", MySqlDbType.MediumText),
                    new("automation", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "", comment: "E.g. upperCaseFirst, trim, replaces, etc."),
                    new("css", MySqlDbType.MediumText),
                    new("width", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("height", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("options", MySqlDbType.MediumText, comment: "The options for this item (in case of dropdown etc.)"),
                    new("data_query", MySqlDbType.MediumText, comment: "Additionally load data from a query to load the options"),
                    new("action_query", MySqlDbType.MediumText, comment: "A query for certain fields that can execute actions, such as action-button"),
                    new("search_query", MySqlDbType.MediumText, comment: "This query is used in sub-entities-grids with the option to link existing items enabled. The data from the search window will be retrieved via this query, if it contains a value."),
                    new("search_count_query", MySqlDbType.MediumText, comment: "This query is used in combination with the \"search_query\". This should be the same query except that it should return a COUNT with the total number of results."),
                    new("grid_delete_query", MySqlDbType.MediumText, comment: "The query to remove records if a node is removed"),
                    new("grid_insert_query", MySqlDbType.MediumText, comment: "The query to save each record in the grid, always proceeded by the delete query"),
                    new("grid_update_query", MySqlDbType.MediumText, comment: "The query for updating an existing record in a grid"),
                    new("depends_on_field", MySqlDbType.VarChar, 100),
                    new("depends_on_operator", MySqlDbType.Enum, enumValues: new List<string> { "eq", "neq", "contains", "doesnotcontain", "startswith", "doesnotstartwith", "endswith", "doesnotendwith", "isempty", "isnotempty", "gte", "gt", "lte", "lt" }),
                    new("depends_on_value", MySqlDbType.VarChar, 255),
                    new("depends_on_action", MySqlDbType.Enum, enumValues: new List<string> { "toggle-visibility", "refresh" }),
                    new("language_code", MySqlDbType.VarChar, 5, notNull: true, defaultValue: ""),
                    new("custom_script", MySqlDbType.MediumText),
                    new("also_save_seo_value", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("save_on_change", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("extended_explanation", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("label_style", MySqlDbType.Enum, enumValues: new List<string> { "normal", "inline", "float" }),
                    new("label_width", MySqlDbType.Enum, enumValues: new List<string> { "0", "10", "20", "30", "40", "50" }),
                    new("enable_aggregation", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("aggregate_options", MySqlDbType.MediumText)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserEntityProperty, "idx_unique", IndexTypes.Unique, new List<string> { "entity_name", "property_name", "language_code", "link_type", "display_name" }),
                    new(WiserTableNames.WiserEntityProperty, "idx_module_entity", IndexTypes.Normal, new List<string> { "module_id", "entity_name" }),
                    new(WiserTableNames.WiserEntityProperty, "idx_entity_overview", IndexTypes.Normal, new List<string> { "entity_name", "visible_in_overview" }),
                    new(WiserTableNames.WiserEntityProperty, "idx_link_overview", IndexTypes.Normal, new List<string> { "link_type", "visible_in_overview" }),
                    new(WiserTableNames.WiserEntityProperty, "idx_property", IndexTypes.Normal, new List<string> { "property_name" })
                }
            },

            // wiser_template
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserTemplate,
                LastUpdate = new DateTime(2022, 4, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("parent_id", MySqlDbType.Int32),
                    new("template_name", MySqlDbType.VarChar, 50, notNull: true),
                    new("template_data", MySqlDbType.MediumText),
                    new("template_data_minified", MySqlDbType.MediumText),
                    new("template_type", MySqlDbType.Int32, notNull: true),
                    new("version", MySqlDbType.Int24, notNull: true),
                    new("template_id", MySqlDbType.Int32, notNull: true),
                    new("changed_on", MySqlDbType.DateTime, notNull: true),
                    new("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                    new("published_environment", MySqlDbType.Int16, notNull: true),
                    new("use_cache", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("cache_minutes", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("cache_location", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("cache_regex", MySqlDbType.VarChar, 255),
                    new("handle_request", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_session", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_objects", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_standards", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_translations", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_dynamic_content", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_logic_blocks", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("handle_mutators", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("login_required", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("login_user_type", MySqlDbType.VarChar, 50),
                    new("login_session_prefix", MySqlDbType.VarChar, 255),
                    new("login_role", MySqlDbType.VarChar, 50),
                    new("linked_templates", MySqlDbType.MediumText),
                    new("ordering", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("insert_mode", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("load_always", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("url_regex", MySqlDbType.VarChar, 255),
                    new("external_files", MySqlDbType.MediumText),
                    new("grouping_create_object_instead_of_array", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("grouping_prefix", MySqlDbType.VarChar, 50),
                    new("grouping_key", MySqlDbType.VarChar, 50),
                    new("grouping_key_column_name", MySqlDbType.VarChar, 50),
                    new("grouping_value_column_name", MySqlDbType.VarChar, 50),
                    new("removed", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("is_scss_include_template", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("use_in_wiser_html_editors", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("pre_load_query", MySqlDbType.MediumText),
                    new("return_not_found_when_pre_load_query_has_no_data", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserTemplate, "idx_unique", IndexTypes.Unique, new List<string> { "template_id", "version" }),
                    new(WiserTableNames.WiserTemplate, "idx_template_id", IndexTypes.Normal, new List<string> { "template_id", "removed" }),
                    new(WiserTableNames.WiserTemplate, "idx_parent_id", IndexTypes.Normal, new List<string> { "parent_id", "removed" }),
                    new(WiserTableNames.WiserTemplate, "idx_type", IndexTypes.Normal, new List<string> { "template_type", "removed" }),
                    new(WiserTableNames.WiserTemplate, "idx_environment", IndexTypes.Normal, new List<string> { "published_environment", "removed" })
                }
            },

            // wiser_dynamic_content
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserDynamicContent,
                LastUpdate = new DateTime(2022, 3, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("content_id", MySqlDbType.Int32, notNull: true),
                    new("settings", MySqlDbType.MediumText),
                    new("component", MySqlDbType.VarChar, 255, notNull: true),
                    new("component_mode", MySqlDbType.VarChar, 255, notNull: true),
                    new("version", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("title", MySqlDbType.VarChar, 255, notNull: true),
                    new("changed_on", MySqlDbType.DateTime, notNull: true),
                    new("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                    new("published_environment", MySqlDbType.Int16, notNull: true),
                    new("removed", MySqlDbType.Int16, notNull: true, defaultValue: "0")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserDynamicContent, "idx_unique", IndexTypes.Unique, new List<string> { "content_id", "version" }),
                }
            },

            // wiser_template_dynamic_content
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserTemplateDynamicContent,
                LastUpdate = new DateTime(2022, 3, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("content_id", MySqlDbType.Int32, notNull: true),
                    new("destination_template_id", MySqlDbType.Int32, notNull: true),
                    new("added_on", MySqlDbType.DateTime, notNull: true),
                    new("added_by", MySqlDbType.VarChar, 50, notNull: true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserTemplateDynamicContent, "idx_unique", IndexTypes.Unique, new List<string> { "content_id", "destination_template_id" }),
                    new(WiserTableNames.WiserTemplateDynamicContent, "idx_destination", IndexTypes.Normal, new List<string> { "destination_template_id" })
                }
            },

            // wiser_template_publish_log
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserTemplatePublishLog,
                LastUpdate = new DateTime(2022, 3, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("template_id", MySqlDbType.Int32, notNull: true),
                    new("old_live", MySqlDbType.Int32, notNull: true),
                    new("old_accept", MySqlDbType.Int32, notNull: true),
                    new("old_test", MySqlDbType.Int32, notNull: true),
                    new("new_live", MySqlDbType.Int32, notNull: true),
                    new("new_accept", MySqlDbType.Int32, notNull: true),
                    new("new_test", MySqlDbType.Int32, notNull: true),
                    new("changed_on", MySqlDbType.DateTime, notNull: true),
                    new("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserTemplatePublishLog, "idx_template_id", IndexTypes.Normal, new List<string> { "template_id" })
                }
            },

            // wiser_preview_profiles
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserPreviewProfiles,
                LastUpdate = new DateTime(2022, 3, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("name", MySqlDbType.VarChar, 255, notNull: true),
                    new("template_id", MySqlDbType.Int32, notNull: true),
                    new("url", MySqlDbType.MediumText, notNull: true),
                    new("variables", MySqlDbType.MediumText, notNull: true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserPreviewProfiles, "idx_template_id", IndexTypes.Normal, new List<string> { "template_id" })
                }
            },

            // wiser_template_publish_log
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserDynamicContentPublishLog,
                LastUpdate = new DateTime(2022, 3, 8),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("content_id", MySqlDbType.Int32, notNull: true),
                    new("old_live", MySqlDbType.Int32, notNull: true),
                    new("old_accept", MySqlDbType.Int32, notNull: true),
                    new("old_test", MySqlDbType.Int32, notNull: true),
                    new("new_live", MySqlDbType.Int32, notNull: true),
                    new("new_accept", MySqlDbType.Int32, notNull: true),
                    new("new_test", MySqlDbType.Int32, notNull: true),
                    new("changed_on", MySqlDbType.DateTime, notNull: true),
                    new("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserDynamicContentPublishLog, "idx_content_id", IndexTypes.Normal, new List<string> { "content_id" })
                }
            },

            // wiser_data_selector
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserDataSelector,
                LastUpdate = new DateTime(2022, 3, 18),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("name", MySqlDbType.VarChar, 50, notNull: true),
                    new("removed", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("module_selection", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("request_json", MySqlDbType.MediumText),
                    new("saved_json", MySqlDbType.MediumText),
                    new("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("changed_on", MySqlDbType.DateTime),
                    new("show_in_export_module", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("available_for_rendering", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("default_template", MySqlDbType.UInt64, notNull: true, defaultValue: "0")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserDataSelector, "idx_name", IndexTypes.Unique, new List<string> { "name" })
                }
            }
        };
    }
}