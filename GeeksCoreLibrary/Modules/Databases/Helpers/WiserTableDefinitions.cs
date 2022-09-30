using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Models;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Modules.Databases.Helpers
{
    /*************************************************************
     ** Important note:                                         **
     ** When adding/removing/renaming columns in a table here,  **
     ** make sure to also do the same changes in the files      **
     ** "CreateTriggers.sql" and "CreateTables.sql" in Wiser 3. **
     *************************************************************/
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
            
            // wiser_entity
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserEntity,
                LastUpdate = new DateTime(2022, 6, 17),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("customer_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("accepted_childtypes", MySqlDbType.VarChar, 1000, notNull: true, defaultValue: ""),
                    new("icon", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                    new("icon_add", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                    new("show_in_tree_view", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("query_after_insert", MySqlDbType.MediumText),
                    new("query_after_update", MySqlDbType.MediumText),
                    new("query_before_update", MySqlDbType.MediumText),
                    new("query_before_delete", MySqlDbType.MediumText),
                    new("color", MySqlDbType.Enum, notNull: true, defaultValue: "blue", enumValues: new List<string> {"blue", "orange", "yellow", "green", "red"}),
                    new("show_in_search", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("show_overview_tab", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("save_title_as_seo", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("api_after_insert", MySqlDbType.Int32),
                    new("api_after_update", MySqlDbType.Int32),
                    new("api_before_update", MySqlDbType.Int32),
                    new("api_before_delete", MySqlDbType.Int32),
                    new("show_title_field", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("friendly_name", MySqlDbType.VarChar, 255),
                    new("save_history", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                    new("default_ordering", MySqlDbType.Enum, notNull: true, defaultValue: "link_ordering", enumValues: new List<string> {"link_ordering", "item_title"}),
                    new("template_query", MySqlDbType.MediumText),
                    new("template_html", MySqlDbType.MediumText),
                    new("enable_multiple_environments", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("icon_expanded", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                    new("use_dedicated_table", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("dedicated_table_prefix", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                    new("delete_action", MySqlDbType.Enum, notNull: true, defaultValue: "archive", enumValues: new List<string> {"archive", "permanent", "hide", "disallow"})
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserEntity, "name_module_id", IndexTypes.Unique, new List<string> {"name", "module_id"}),
                    new(WiserTableNames.WiserEntity, "name", IndexTypes.Normal, new List<string> {"name", "show_in_tree_view"}),
                    new(WiserTableNames.WiserEntity, "module_id", IndexTypes.Normal, new List<string> {"module_id"})
                }
            },

            // wiser_entityproperty
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserEntityProperty,
                LastUpdate = new DateTime(2022, 8, 5),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("entity_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("link_type", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("visible_in_overview", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
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
                    new("aggregate_options", MySqlDbType.MediumText),
                    new("access_key", MySqlDbType.VarChar, 1, notNull: true, defaultValue: ""),
                    new("visibility_path_regex", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "")
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
                LastUpdate = new DateTime(2022, 5, 31),
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
                    new("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
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
                    new("login_redirect_url", MySqlDbType.VarChar, 255),
                    new("linked_templates", MySqlDbType.MediumText),
                    new("ordering", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("insert_mode", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("load_always", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("disable_minifier", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
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
                    new("return_not_found_when_pre_load_query_has_no_data", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("routine_type", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "For routine templates only"),
                    new("routine_parameters", MySqlDbType.Text, comment: "For routine templates only"),
                    new("routine_return_type", MySqlDbType.VarChar, 25, comment: "For routine templates only"),
                    new("is_default_header", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("is_default_footer", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                    new("default_header_footer_regex", MySqlDbType.VarChar, 255),
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
                LastUpdate = new DateTime(2022, 5, 17),
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
                    new("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
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
            },

            // ais_logs
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.AisLogs,
                LastUpdate = new DateTime(2022, 5, 10),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("message", MySqlDbType.MediumText, notNull: true),
                    new("level", MySqlDbType.VarChar, 64, notNull: true),
                    new("scope", MySqlDbType.VarChar, 64, notNull: true),
                    new("source", MySqlDbType.VarChar, 256, notNull: true),
                    new("configuration", MySqlDbType.VarChar, 256),
                    new("time_id", MySqlDbType.Int32),
                    new("order", MySqlDbType.Int32),
                    new("added_on", MySqlDbType.DateTime, notNull:true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.AisLogs, "idx_configuration", IndexTypes.Normal, new List<string> { "configuration", "time_id", "order" }),
                    new(WiserTableNames.AisLogs, "idx_level", IndexTypes.Normal, new List<string> { "level", "configuration", "time_id", "order" })
                }
            },
            
            // wiser_id_mappings
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserIdMappings,
                LastUpdate = new DateTime(2022, 5, 19),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("table_name", MySqlDbType.VarChar, 255, notNull: true),
                    new("our_id", MySqlDbType.UInt64, notNull: true),
                    new("production_id", MySqlDbType.UInt64, notNull: true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserIdMappings, "idx_unique", IndexTypes.Unique, new List<string> { "table_name", "our_id" })
                }
            },
            
            // wiser_itemfile
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserItemFile,
                LastUpdate = new DateTime(2022, 6, 2),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new("itemlink_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new("content_type", MySqlDbType.VarChar, 100, notNull: true),
                    new("content", MySqlDbType.LongBlob),
                    new("content_url", MySqlDbType.VarChar, 1024),
                    new("width", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("height", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new("file_name", MySqlDbType.VarChar, 255),
                    new("extension", MySqlDbType.VarChar, 20),
                    new("title", MySqlDbType.VarChar, 255),
                    new("property_name", MySqlDbType.VarChar, 255),
                    new("protected", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                    new("ordering", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                    new("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("added_by", MySqlDbType.VarChar, 255),
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserItemFile, "idx_item_id", IndexTypes.Normal, new List<string> { "item_id", "property_name" }),
                    new(WiserTableNames.WiserItemFile, "idx_item_link_id", IndexTypes.Normal, new List<string> { "itemlink_id", "property_name" })
                }
            },
            
            // wiser_link
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserLink,
                LastUpdate = new DateTime(2022, 7, 19),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("type", MySqlDbType.Int32, notNull: true),
                    new("destination_entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new ("connected_entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new("show_in_tree_view", MySqlDbType.Int16, notNull: true, defaultValue: "1"),
                    new("show_in_data_selector", MySqlDbType.Int16, notNull: true, defaultValue: "1"),
                    new("relationship", MySqlDbType.Enum, enumValues: new List<string> { "one-to-one", "one-to-many", "many-to-many" }, defaultValue: "one-to-many"),
                    new("relationship", MySqlDbType.Enum, enumValues: new List<string> { "none", "copy-link", "copy-item" }, defaultValue: "none", comment: "What to do with this link, when an item is being duplicated. None means that links of this type will not be copied/duplicatied to the new item. Copy-link means that the linked item will also be linked to the new item. Copy-item means that the linked item will also be duplicated and then that duplicated item will be linked to the new item."),
                    new("use_item_parent_id", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to use the column \"parent_item_id\" from wiser_item for these links. This will then no longer use or need the table wiser_itemlink for these links."),
                    new("use_dedicated_table", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to use a dedicated table for links of this type. The GCL and Wiser expect there to be a table \"[linkType]_wiser_itemlink\" to store the links in. So if your link type is \"1\", we will use the table \"1_wiser_itemlink\" instead of \"wiser_itemlink\". This table will not be created automatically. To create this table, make a copy of wiser_itemlink (including triggers, but the the name of the table in the triggers too)."),
                    new("cascade_delete", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to also delete children when a parent is being deleted.")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserLink, "idx_link", IndexTypes.Unique, new List<string> { "type", "destination_entity_type", "connected_entity_type" })
                }
            },
            
            // wiser_branches_queue
            new WiserTableDefinitionModel
            {
                Name = WiserTableNames.WiserBranchesQueue,
                LastUpdate = new DateTime(2022, 6, 17),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("name", MySqlDbType.VarChar, 255, notNull: true),
                    new("branch_id", MySqlDbType.Int32),
                    new("action", MySqlDbType.Enum, notNull: true, enumValues: new List<string> { "create", "merge", "delete" }),
                    new("data", MySqlDbType.MediumText),
                    new("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("added_by", MySqlDbType.VarChar, 255, notNull: true),
                    new("user_id", MySqlDbType.UInt64, notNull: true),
                    new("start_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("started_on", MySqlDbType.DateTime),
                    new("finished_on", MySqlDbType.DateTime),
                    new("success", MySqlDbType.Int16),
                    new("errors", MySqlDbType.MediumText)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new(WiserTableNames.WiserBranchesQueue, "idx_branch_id", IndexTypes.Normal, new List<string> { "branch_id" }),
                    new(WiserTableNames.WiserBranchesQueue, "idx_started_on", IndexTypes.Normal, new List<string> { "started_on" })
                }
            },
            
            // log_psp
            new WiserTableDefinitionModel
            {
                Name = Payments.Models.Constants.PaymentServiceProviderLogTableName,
                LastUpdate = new DateTime(2022, 9, 30),
                Columns = new List<ColumnSettingsModel>
                {
                    new("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                    new("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new("payment_service_provider", MySqlDbType.VarChar, 50, notNull: true, defaultValue: ""),
                    new("unique_payment_number", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                    new("status", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0"),
                    new("request_headers", MySqlDbType.Text, 0),
                    new("request_query_string", MySqlDbType.Text, 0),
                    new("request_form_values", MySqlDbType.MediumText, 0),
                    new("request_body", MySqlDbType.MediumText, 0),
                    new("response_body", MySqlDbType.MediumText, 0),
                    new("error", MySqlDbType.Text, 0),
                    new("url", MySqlDbType.Text, 0),
                    new("type", MySqlDbType.Enum, enumValues: new List<string> { "incoming", "outgoing" })
                }
            }
        };
    }
}