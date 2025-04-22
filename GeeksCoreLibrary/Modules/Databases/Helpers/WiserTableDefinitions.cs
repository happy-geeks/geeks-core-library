using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Enums;
using GeeksCoreLibrary.Modules.Databases.Models;
using MySqlConnector;
using Constants = GeeksCoreLibrary.Modules.Databases.Models.Constants;

namespace GeeksCoreLibrary.Modules.Databases.Helpers;

/*************************************************************
 ** Important note:                                         **
 ** When adding/removing/renaming columns in a table here,  **
 ** make sure to also do the same changes in the files      **
 ** "CreateTriggers.sql" and "CreateTables.sql" in Wiser 3. **
 *************************************************************/
public class WiserTableDefinitions
{
    public static readonly List<WiserTableDefinitionModel> TablesToUpdate =
    [
        // wiser_item
        new()
        {
            Name = WiserTableNames.WiserItem,
            LastUpdate = new DateTime(2024, 10, 28),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("original_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("unique_uuid", MySqlDbType.VarChar, 200, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("parent_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("moduleid", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("published_environment", MySqlDbType.Int24, notNull: true, defaultValue: "15"),
                new ColumnSettingsModel("readonly", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("title", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true, updateTimeStampOnChange: true),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("json", MySqlDbType.JSON),
                new ColumnSettingsModel("json_last_processed_date", MySqlDbType.DateTime)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserItem, "idx_module_env", IndexTypes.Normal, ["moduleid", "published_environment"]),
                new IndexSettingsModel(WiserTableNames.WiserItem, "idx_entity", IndexTypes.Normal, ["entity_type", "unique_uuid"]),
                new IndexSettingsModel(WiserTableNames.WiserItem, "idx_unique_uuid", IndexTypes.Normal, ["unique_uuid"]),
                new IndexSettingsModel(WiserTableNames.WiserItem, "idx_original_item_id", IndexTypes.Normal, ["original_item_id"]),
                new IndexSettingsModel(WiserTableNames.WiserItem, "idx_parent", IndexTypes.Normal, ["parent_item_id", "entity_type"])
            ]
        },

        // wiser_itemdetail
        new()
        {
            Name = WiserTableNames.WiserItemDetail,
            LastUpdate = new DateTime(2024, 2, 19),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 5, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("groupname", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("key", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("value", MySqlDbType.VarChar, 1000, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("long_value", MySqlDbType.MediumText)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserItemDetail, "item_key", IndexTypes.Unique, ["item_id", "key", "language_code"]),
                new IndexSettingsModel(WiserTableNames.WiserItemDetail, "key_value", IndexTypes.Normal, ["key(50)", "value(100)"]),
                new IndexSettingsModel(WiserTableNames.WiserItemDetail, "item_id_key_value", IndexTypes.Normal, ["item_id", "key(40)", "value(40)"]),
                new IndexSettingsModel(WiserTableNames.WiserItemDetail, "item_id_group", IndexTypes.Normal, ["item_id", "groupname", "key(40)"])
            ]
        },

        // wiser_itemlink
        new()
        {
            Name = WiserTableNames.WiserItemLink,
            LastUpdate = new DateTime(2022, 1, 1),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("destination_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("type", MySqlDbType.Int24, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserItemLink, "uniquelink", IndexTypes.Unique, ["item_id", "destination_item_id", "type"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLink, "type", IndexTypes.Normal, ["type", "destination_item_id", "ordering"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLink, "item_id", IndexTypes.Normal, ["item_id"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLink, "destination_item_id", IndexTypes.Normal, ["destination_item_id", "item_id", "ordering"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLink, "destination_item_id_2", IndexTypes.Normal, ["destination_item_id", "type", "ordering"])
            ]
        },

        // wiser_itemlinkdetail
        new()
        {
            Name = WiserTableNames.WiserItemLinkDetail,
            LastUpdate = new DateTime(2024, 2, 19),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 5, notNull: true, defaultValue: "", comment: "leeg betekent beschikbaar voor alle talen"),
                new ColumnSettingsModel("itemlink_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("groupname", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "", comment: "optionele groepering van items, zoals een 'specs' tabel"),
                new ColumnSettingsModel("key", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("value", MySqlDbType.VarChar, 1000, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("long_value", MySqlDbType.MediumText, comment: "Voor waardes die niet in 'value' passen, zoals van HTMLeditors")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserItemLinkDetail, "itemlink_key", IndexTypes.Unique, ["itemlink_id", "key", "language_code"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLinkDetail, "key_value", IndexTypes.Normal, ["key(50)", "value(100)"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLinkDetail, "itemlink_id_key_value", IndexTypes.Normal, ["itemlink_id", "key(40)", "value(40)"]),
                new IndexSettingsModel(WiserTableNames.WiserItemLinkDetail, "itemlink_id_group", IndexTypes.Normal, ["itemlink_id", "groupname", "key(40)"])
            ]
        },

        // wiser_grant_store
        new()
        {
            Name = WiserTableNames.WiserGrantStore,
            LastUpdate = new DateTime(2022, 1, 1),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("key", MySqlDbType.VarChar, 512, notNull: true),
                new ColumnSettingsModel("type", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("client_id", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("data", MySqlDbType.MediumText),
                new ColumnSettingsModel("subject_id", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("description", MySqlDbType.VarChar, 512),
                new ColumnSettingsModel("creation_time", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("expiration", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("session_id", MySqlDbType.VarChar, 255)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserGrantStore, "idx_key", IndexTypes.Unique, ["key"])]
        },

        // wiser_grant_store
        new()
        {
            Name = Components.Account.Models.Constants.AuthenticationTokensTableName,
            LastUpdate = new DateTime(2022, 1, 1),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("selector", MySqlDbType.VarChar, 32, notNull: true),
                new ColumnSettingsModel("hashed_validator", MySqlDbType.VarChar, 150, notNull: true),
                new ColumnSettingsModel("user_id", MySqlDbType.Int64, notNull: true),
                new ColumnSettingsModel("main_user_id", MySqlDbType.Int64, notNull: true),
                new ColumnSettingsModel("entity_type", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("main_user_entity_type", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("role", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("ip_address", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("user_agent", MySqlDbType.VarChar, 2000),
                new ColumnSettingsModel("login_date", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("expires", MySqlDbType.DateTime, notNull: true)
            ],
            Indexes = [new IndexSettingsModel(Components.Account.Models.Constants.AuthenticationTokensTableName, "idx_selector", IndexTypes.Unique, ["selector", "entity_type"])]
        },

        // wiser_entity
        new()
        {
            Name = WiserTableNames.WiserEntity,
            LastUpdate = new DateTime(2025, 1, 30),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("accepted_childtypes", MySqlDbType.VarChar, 1000, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("icon", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("icon_add", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("show_in_tree_view", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("query_after_insert", MySqlDbType.MediumText),
                new ColumnSettingsModel("query_after_update", MySqlDbType.MediumText),
                new ColumnSettingsModel("query_before_update", MySqlDbType.MediumText),
                new ColumnSettingsModel("query_before_delete", MySqlDbType.MediumText),
                new ColumnSettingsModel("color", MySqlDbType.Enum, notNull: true, defaultValue: "blue", enumValues: new List<string> {"blue", "orange", "yellow", "green", "red"}),
                new ColumnSettingsModel("show_in_search", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("show_overview_tab", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("save_title_as_seo", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("api_after_insert", MySqlDbType.Int32),
                new ColumnSettingsModel("api_after_update", MySqlDbType.Int32),
                new ColumnSettingsModel("api_before_update", MySqlDbType.Int32),
                new ColumnSettingsModel("api_before_delete", MySqlDbType.Int32),
                new ColumnSettingsModel("show_title_field", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("friendly_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("save_history", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("default_ordering", MySqlDbType.Enum, notNull: true, defaultValue: "link_ordering", enumValues: new List<string> {"link_ordering", "item_title"}),
                new ColumnSettingsModel("template_query", MySqlDbType.MediumText),
                new ColumnSettingsModel("template_html", MySqlDbType.MediumText),
                new ColumnSettingsModel("store_type", MySqlDbType.Enum, notNull: true, defaultValue: "table", enumValues: new List<string> {"table", "document_store", "hybrid"}),
                new ColumnSettingsModel("enable_multiple_environments", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("icon_expanded", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("dedicated_table_prefix", MySqlDbType.VarChar, 25, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("delete_action", MySqlDbType.Enum, notNull: true, defaultValue: "archive", enumValues: new List<string> {"archive", "permanent", "hide", "disallow"}),
                new ColumnSettingsModel("show_in_dashboard", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("allow_creation_on_main_from_branch", MySqlDbType.Int16, 1, notNull: true, defaultValue: "1")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserEntity, "name_module_id", IndexTypes.Unique, ["name", "module_id"]),
                new IndexSettingsModel(WiserTableNames.WiserEntity, "name", IndexTypes.Normal, ["name", "show_in_tree_view"]),
                new IndexSettingsModel(WiserTableNames.WiserEntity, "module_id", IndexTypes.Normal, ["module_id"]),
                new IndexSettingsModel(WiserTableNames.WiserEntity, "show_in_dashboard", IndexTypes.Normal, ["show_in_dashboard"])
            ]
        },

        // wiser_entityproperty
        new()
        {
            Name = WiserTableNames.WiserEntityProperty,
            LastUpdate = new DateTime(2022, 8, 5),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("entity_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("link_type", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("visible_in_overview", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("overview_width", MySqlDbType.Int24, notNull: true, defaultValue: "100"),
                new ColumnSettingsModel("tab_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("group_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("inputtype", MySqlDbType.Enum, notNull: true, defaultValue: "input", enumValues: new List<string> {"input", "secure-input", "textbox", "radiobutton", "checkbox", "combobox", "multiselect", "numeric-input", "file-upload", "HTMLeditor", "querybuilder", "date-time picker", "grid", "imagecoords", "button", "image-upload", "gpslocation", "daterange", "sub-entities-grid", "item-linker", "color-picker", "auto-increment", "linked-item", "action-button", "data-selector", "chart", "scheduler", "timeline", "empty", "iframe"}),
                new ColumnSettingsModel("display_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("property_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("explanation", MySqlDbType.MediumText),
                new ColumnSettingsModel("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("regex_validation", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("mandatory", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("readonly", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("default_value", MySqlDbType.MediumText),
                new ColumnSettingsModel("automation", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "", comment: "E.g. upperCaseFirst, trim, replaces, etc."),
                new ColumnSettingsModel("css", MySqlDbType.MediumText),
                new ColumnSettingsModel("width", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("height", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("options", MySqlDbType.MediumText, comment: "The options for this item (in case of dropdown etc.)"),
                new ColumnSettingsModel("data_query", MySqlDbType.MediumText, comment: "Additionally load data from a query to load the options"),
                new ColumnSettingsModel("action_query", MySqlDbType.MediumText, comment: "A query for certain fields that can execute actions, such as action-button"),
                new ColumnSettingsModel("search_query", MySqlDbType.MediumText, comment: "This query is used in sub-entities-grids with the option to link existing items enabled. The data from the search window will be retrieved via this query, if it contains a value."),
                new ColumnSettingsModel("search_count_query", MySqlDbType.MediumText, comment: "This query is used in combination with the \"search_query\". This should be the same query except that it should return a COUNT with the total number of results."),
                new ColumnSettingsModel("grid_delete_query", MySqlDbType.MediumText, comment: "The query to remove records if a node is removed"),
                new ColumnSettingsModel("grid_insert_query", MySqlDbType.MediumText, comment: "The query to save each record in the grid, always proceeded by the delete query"),
                new ColumnSettingsModel("grid_update_query", MySqlDbType.MediumText, comment: "The query for updating an existing record in a grid"),
                new ColumnSettingsModel("depends_on_field", MySqlDbType.VarChar, 100),
                new ColumnSettingsModel("depends_on_operator", MySqlDbType.Enum, enumValues: new List<string> {"eq", "neq", "contains", "doesnotcontain", "startswith", "doesnotstartwith", "endswith", "doesnotendwith", "isempty", "isnotempty", "gte", "gt", "lte", "lt"}),
                new ColumnSettingsModel("depends_on_value", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("depends_on_action", MySqlDbType.Enum, enumValues: new List<string> {"toggle-visibility", "refresh"}),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 5, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("custom_script", MySqlDbType.MediumText),
                new ColumnSettingsModel("also_save_seo_value", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("save_on_change", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("extended_explanation", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("label_style", MySqlDbType.Enum, enumValues: new List<string> {"normal", "inline", "float"}),
                new ColumnSettingsModel("label_width", MySqlDbType.Enum, enumValues: new List<string> {"0", "10", "20", "30", "40", "50"}),
                new ColumnSettingsModel("enable_aggregation", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("aggregate_options", MySqlDbType.MediumText),
                new ColumnSettingsModel("access_key", MySqlDbType.VarChar, 1, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("visibility_path_regex", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserEntityProperty, "idx_unique", IndexTypes.Unique, ["entity_name", "property_name", "language_code", "link_type", "display_name"]),
                new IndexSettingsModel(WiserTableNames.WiserEntityProperty, "idx_module_entity", IndexTypes.Normal, ["module_id", "entity_name"]),
                new IndexSettingsModel(WiserTableNames.WiserEntityProperty, "idx_entity_overview", IndexTypes.Normal, ["entity_name", "visible_in_overview"]),
                new IndexSettingsModel(WiserTableNames.WiserEntityProperty, "idx_link_overview", IndexTypes.Normal, ["link_type", "visible_in_overview"]),
                new IndexSettingsModel(WiserTableNames.WiserEntityProperty, "idx_property", IndexTypes.Normal, ["property_name"])
            ]
        },

        // wiser_template
        new()
        {
            Name = WiserTableNames.WiserTemplate,
            LastUpdate = new DateTime(2024, 12, 5),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("parent_id", MySqlDbType.Int32),
                new ColumnSettingsModel("template_name", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("template_data", MySqlDbType.MediumText),
                new ColumnSettingsModel("template_data_minified", MySqlDbType.MediumText),
                new ColumnSettingsModel("template_type", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("version", MySqlDbType.Int24, notNull: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_per_url", MySqlDbType.Int16, length: 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_per_querystring", MySqlDbType.Int16, length: 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_per_hostname", MySqlDbType.Int16, length: 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_per_user", MySqlDbType.Int16, length: 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_using_regex", MySqlDbType.Int16, length: 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_minutes", MySqlDbType.Int32, notNull: true, defaultValue: "-1"),
                new ColumnSettingsModel("cache_location", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("cache_regex", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("login_required", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("login_role", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("login_redirect_url", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("linked_templates", MySqlDbType.MediumText),
                new ColumnSettingsModel("ordering", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("insert_mode", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("load_always", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("disable_minifier", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("url_regex", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("external_files", MySqlDbType.MediumText),
                new ColumnSettingsModel("grouping_create_object_instead_of_array", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("grouping_prefix", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("grouping_key", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("grouping_key_column_name", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("grouping_value_column_name", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("removed", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("is_scss_include_template", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("use_in_wiser_html_editors", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("pre_load_query", MySqlDbType.MediumText),
                new ColumnSettingsModel("return_not_found_when_pre_load_query_has_no_data", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("routine_type", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "For routine templates only"),
                new ColumnSettingsModel("routine_parameters", MySqlDbType.Text, comment: "For routine templates only"),
                new ColumnSettingsModel("routine_return_type", MySqlDbType.VarChar, 25, comment: "For routine templates only"),
                new ColumnSettingsModel("trigger_timing", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "For trigger templates only"),
                new ColumnSettingsModel("trigger_event", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "For trigger templates only"),
                new ColumnSettingsModel("trigger_table_name", MySqlDbType.VarChar, 100, comment: "For trigger templates only"),
                new ColumnSettingsModel("is_default_header", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("is_default_footer", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("default_header_footer_regex", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("is_partial", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("widget_content", MySqlDbType.MediumText),
                new ColumnSettingsModel("widget_location", MySqlDbType.Int16, 4, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("is_dirty", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("robots_no_index", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("robots_no_follow", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserTemplate, "idx_unique", IndexTypes.Unique, ["template_id", "version"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplate, "idx_template_id", IndexTypes.Normal, ["template_id", "removed"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplate, "idx_parent_id", IndexTypes.Normal, ["parent_id", "removed"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplate, "idx_type", IndexTypes.Normal, ["template_type", "removed"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplate, "idx_environment", IndexTypes.Normal, ["published_environment", "removed"])
            ]
        },

        // wiser_template_external_files
        new()
        {
            Name = WiserTableNames.WiserTemplateExternalFiles,
            LastUpdate = new DateTime(2024, 12, 25),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("external_file", MySqlDbType.VarChar, 1000, notNull: true),
                new ColumnSettingsModel("hash", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("ordering", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0")
            ],
        },

        // wiser_commit
        new()
        {
            Name = WiserTableNames.WiserCommit,
            LastUpdate = new DateTime(2023, 4, 5),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("description", MySqlDbType.MediumText),
                new ColumnSettingsModel("external_id", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("deployed_to_development_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("deployed_to_development_by", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("deployed_to_test_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("deployed_to_test_by", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("deployed_to_acceptance_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("deployed_to_acceptance_by", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("deployed_to_live_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("deployed_to_live_by", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("completed", MySqlDbType.Int16, notNull: true, defaultValue: "0")
            ]
        },

        // wiser_commit_dynamic_content
        new()
        {
            Name = WiserTableNames.WiserCommitDynamicContent,
            LastUpdate = new DateTime(2022, 11, 4),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("dynamic_content_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("version", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("commit_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255)
            ]
        },

        // wiser_commit_template
        new()
        {
            Name = WiserTableNames.WiserCommitTemplate,
            LastUpdate = new DateTime(2022, 11, 4),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("version", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("commit_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255)
            ]
        },

        // wiser_commit_reviews
        new()
        {
            Name = WiserTableNames.WiserCommitReviews,
            LastUpdate = new DateTime(2023, 3, 22),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("commit_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("requested_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("requested_by", MySqlDbType.Int64, notNull: true, comment: "Negative numbers are IDs of admins"),
                new ColumnSettingsModel("requested_by_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("reviewed_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("reviewed_by", MySqlDbType.Int64, notNull: true, defaultValue: "0", comment: "Negative numbers are IDs of admins"),
                new ColumnSettingsModel("reviewed_by_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("status", MySqlDbType.Enum, enumValues: new List<string> {"Pending", "Approved", "RequestChanges"})
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserCommitReviews, "idx_commit_id", IndexTypes.Unique, ["commit_id"]),
                new IndexSettingsModel(WiserTableNames.WiserCommitReviews, "idx_requested_by", IndexTypes.Normal, ["requested_by"]),
                new IndexSettingsModel(WiserTableNames.WiserCommitReviews, "idx_reviewed_by", IndexTypes.Normal, ["reviewed_by"])
            ]
        },

        // wiser_commit_review_comments
        new()
        {
            Name = WiserTableNames.WiserCommitReviewComments,
            LastUpdate = new DateTime(2023, 3, 22),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("review_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.Int64, notNull: true, comment: "Negative numbers are IDs of admins"),
                new ColumnSettingsModel("added_by_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("text", MySqlDbType.MediumText)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserCommitReviewComments, "idx_review_id", IndexTypes.Normal, ["review_id"]),
                new IndexSettingsModel(WiserTableNames.WiserCommitReviewComments, "idx_added_by", IndexTypes.Normal, ["added_by"])
            ]
        },

        // wiser_commit_review_requests
        new()
        {
            Name = WiserTableNames.WiserCommitReviewRequests,
            LastUpdate = new DateTime(2023, 3, 22),
            Columns =
            [
                new ColumnSettingsModel("review_id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true),
                new ColumnSettingsModel("requested_user", MySqlDbType.Int64, notNull: true, isPrimaryKey: true, comment: "Negative numbers are IDs of admins")
            ]
        },

        // wiser_dynamic_content
        new()
        {
            Name = WiserTableNames.WiserDynamicContent,
            LastUpdate = new DateTime(2023, 10, 20),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("content_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("settings", MySqlDbType.MediumText),
                new ColumnSettingsModel("component", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("component_mode", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("version", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("title", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("removed", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("is_dirty", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserDynamicContent, "idx_unique", IndexTypes.Unique, ["content_id", "version"])
            ]
        },

        // wiser_template_dynamic_content
        new()
        {
            Name = WiserTableNames.WiserTemplateDynamicContent,
            LastUpdate = new DateTime(2022, 3, 8),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("content_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("destination_template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 50, notNull: true)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserTemplateDynamicContent, "idx_unique", IndexTypes.Unique, ["content_id", "destination_template_id"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplateDynamicContent, "idx_destination", IndexTypes.Normal, ["destination_template_id"])
            ]
        },

        // wiser_template_publish_log
        new()
        {
            Name = WiserTableNames.WiserTemplatePublishLog,
            LastUpdate = new DateTime(2022, 3, 8),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_live", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_accept", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_test", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_live", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_accept", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_test", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50, notNull: true)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserTemplatePublishLog, "idx_template_id", IndexTypes.Normal, ["template_id"])]
        },

        // wiser_preview_profiles
        new()
        {
            Name = WiserTableNames.WiserPreviewProfiles,
            LastUpdate = new DateTime(2022, 3, 8),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("url", MySqlDbType.MediumText, notNull: true),
                new ColumnSettingsModel("variables", MySqlDbType.MediumText, notNull: true)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserPreviewProfiles, "idx_template_id", IndexTypes.Normal, ["template_id"])]
        },

        // wiser_dynamic_content_publish_log
        new()
        {
            Name = WiserTableNames.WiserDynamicContentPublishLog,
            LastUpdate = new DateTime(2022, 3, 8),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("content_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_live", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_accept", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("old_test", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_live", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_accept", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("new_test", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50, notNull: true)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserDynamicContentPublishLog, "idx_content_id", IndexTypes.Normal, ["content_id"])]
        },

        // wiser_data_selector
        new()
        {
            Name = WiserTableNames.WiserDataSelector,
            LastUpdate = new DateTime(2023, 3, 24),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("removed", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("module_selection", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("request_json", MySqlDbType.MediumText),
                new ColumnSettingsModel("saved_json", MySqlDbType.MediumText),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("show_in_export_module", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("show_in_communication_module", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("available_for_rendering", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("default_template", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("show_in_dashboard", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("available_for_branches", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0")
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserDataSelector, "idx_name", IndexTypes.Unique, ["name"])]
        },

        // wts_logs
        new()
        {
            Name = WiserTableNames.WtsLogs,
            LastUpdate = new DateTime(2022, 9, 14),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("message", MySqlDbType.MediumText, notNull: true),
                new ColumnSettingsModel("level", MySqlDbType.VarChar, 64, notNull: true),
                new ColumnSettingsModel("scope", MySqlDbType.VarChar, 64, notNull: true),
                new ColumnSettingsModel("source", MySqlDbType.VarChar, 256, notNull: true),
                new ColumnSettingsModel("configuration", MySqlDbType.VarChar, 256),
                new ColumnSettingsModel("time_id", MySqlDbType.Int32),
                new ColumnSettingsModel("order", MySqlDbType.Int32),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("is_test", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WtsLogs, "idx_configuration", IndexTypes.Normal, ["configuration", "time_id", "order", "is_test"]),
                new IndexSettingsModel(WiserTableNames.WtsLogs, "idx_level", IndexTypes.Normal, ["level", "configuration", "time_id", "order", "is_test"]),
                new IndexSettingsModel(WiserTableNames.WtsLogs, "idx_dated_configuration", IndexTypes.Normal, ["added_on", "configuration", "time_id", "is_test"])
            ]
        },

        // wts_services
        new()
        {
            Name = WiserTableNames.WtsServices,
            LastUpdate = new DateTime(2022, 12, 20),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("configuration", MySqlDbType.VarChar, 256, notNull: true),
                new ColumnSettingsModel("time_id", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("action", MySqlDbType.VarChar, 256),
                new ColumnSettingsModel("scheme", MySqlDbType.Enum, notNull: true, enumValues: new List<string> {"continuous", "daily", "weekly", "monthly"}),
                new ColumnSettingsModel("last_run", MySqlDbType.DateTime),
                new ColumnSettingsModel("next_run", MySqlDbType.DateTime),
                new ColumnSettingsModel("run_time", MySqlDbType.Double),
                new ColumnSettingsModel("state", MySqlDbType.Enum, notNull: true, enumValues: new List<string> {"active", "success", "warning", "failed", "paused", "stopped", "crashed", "running"}, defaultValue: "active"),
                new ColumnSettingsModel("paused", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("extra_run", MySqlDbType.Int16, 1, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WtsServices, "idx_time", IndexTypes.Normal, ["configuration", "time_id"]),
                new IndexSettingsModel(WiserTableNames.WtsServices, "idx_action", IndexTypes.Normal, ["configuration", "action"])
            ]
        },

        // wiser_id_mappings
        new()
        {
            Name = WiserTableNames.WiserIdMappings,
            LastUpdate = new DateTime(2025, 3, 17),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("table_name", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("our_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("production_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP")
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserIdMappings, "idx_unique", IndexTypes.Unique, ["table_name", "our_id"])]
        },

        // wiser_itemfile
        new()
        {
            Name = WiserTableNames.WiserItemFile,
            LastUpdate = new DateTime(2025, 2, 11),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("itemlink_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("content_type", MySqlDbType.VarChar, 100, notNull: true),
                new ColumnSettingsModel("content", MySqlDbType.LongBlob),
                new ColumnSettingsModel("content_url", MySqlDbType.VarChar, 1024),
                new ColumnSettingsModel("file_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("extension", MySqlDbType.VarChar, 20),
                new ColumnSettingsModel("title", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("property_name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("extra_data", MySqlDbType.MediumText),
                new ColumnSettingsModel("protected", MySqlDbType.Int16, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("ordering", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserItemFile, "idx_item_id", IndexTypes.Normal, ["item_id", "property_name"]),
                new IndexSettingsModel(WiserTableNames.WiserItemFile, "idx_item_link_id", IndexTypes.Normal, ["itemlink_id", "property_name"])
            ]
        },

        // wiser_link
        new()
        {
            Name = WiserTableNames.WiserLink,
            LastUpdate = new DateTime(2022, 7, 19),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("type", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("destination_entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("connected_entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("show_in_tree_view", MySqlDbType.Int16, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("show_in_data_selector", MySqlDbType.Int16, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("relationship", MySqlDbType.Enum, enumValues: new List<string> {"one-to-one", "one-to-many", "many-to-many"}, defaultValue: "one-to-many"),
                new ColumnSettingsModel("relationship", MySqlDbType.Enum, enumValues: new List<string> {"none", "copy-link", "copy-item"}, defaultValue: "none", comment: "What to do with this link, when an item is being duplicated. None means that links of this type will not be copied/duplicatied to the new item. Copy-link means that the linked item will also be linked to the new item. Copy-item means that the linked item will also be duplicated and then that duplicated item will be linked to the new item."),
                new ColumnSettingsModel("use_item_parent_id", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to use the column \"parent_item_id\" from wiser_item for these links. This will then no longer use or need the table wiser_itemlink for these links."),
                new ColumnSettingsModel("use_dedicated_table", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to use a dedicated table for links of this type. The GCL and Wiser expect there to be a table \"[linkType]_wiser_itemlink\" to store the links in. So if your link type is \"1\", we will use the table \"1_wiser_itemlink\" instead of \"wiser_itemlink\". This table will not be created automatically. To create this table, make a copy of wiser_itemlink (including triggers, but the the name of the table in the triggers too)."),
                new ColumnSettingsModel("cascade_delete", MySqlDbType.Int16, notNull: true, defaultValue: "0", comment: "Set this to 1 to also delete children when a parent is being deleted.")
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserLink, "idx_link", IndexTypes.Unique, ["type", "destination_entity_type", "connected_entity_type"])]
        },

        // wiser_branches_queue
        new()
        {
            Name = WiserTableNames.WiserBranchesQueue,
            LastUpdate = new DateTime(2024, 11, 20),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("branch_id", MySqlDbType.Int32),
                new ColumnSettingsModel("action", MySqlDbType.Enum, notNull: true, enumValues: new List<string> {"create", "merge", "delete"}),
                new ColumnSettingsModel("data", MySqlDbType.MediumText),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("start_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("started_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("finished_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("success", MySqlDbType.Int16),
                new ColumnSettingsModel("errors", MySqlDbType.MediumText),
                new ColumnSettingsModel("total_items", MySqlDbType.Int32),
                new ColumnSettingsModel("items_processed", MySqlDbType.Int32),
                new ColumnSettingsModel("is_template", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("is_for_automatic_deploy", MySqlDbType.Int16, notNull: true, defaultValue: "0")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserBranchesQueue, "idx_branch_id", IndexTypes.Normal, ["branch_id"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchesQueue, "idx_started_on", IndexTypes.Normal, ["started_on"])
            ]
        },

        // wiser_dashboard
        new()
        {
            Name = WiserTableNames.WiserDashboard,
            LastUpdate = new DateTime(2023, 2, 23),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("last_update", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("items_data", MySqlDbType.MediumText),
                new ColumnSettingsModel("entities_data", MySqlDbType.MediumText),
                new ColumnSettingsModel("user_login_count_top10", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("user_login_count_other", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("user_login_active_top10", MySqlDbType.Int64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("user_login_active_other", MySqlDbType.Int64, notNull: true, defaultValue: "0")
            ]
        },

        // wiser_login_log
        new()
        {
            Name = WiserTableNames.WiserLoginLog,
            LastUpdate = new DateTime(2023, 2, 23),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("time_active_in_seconds", MySqlDbType.Int64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("time_active_changed_on", MySqlDbType.DateTime, notNull: true)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserLoginLog, "idx_added_on", IndexTypes.Normal, ["added_on"]),
                new IndexSettingsModel(WiserTableNames.WiserLoginLog, "idx_user_Id", IndexTypes.Normal, ["user_id"])
            ]
        },

        // wiser_query
        new()
        {
            Name = WiserTableNames.WiserQuery,
            LastUpdate = new DateTime(2023, 9, 12),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("description", MySqlDbType.VarChar, 512, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("query", MySqlDbType.MediumText),
                new ColumnSettingsModel("show_in_export_module", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("show_in_communication_module", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserQuery, "idx_show_in_export_module", IndexTypes.Normal, ["show_in_export_module"])]
        },

        // wiser_styled_output
        new()
        {
            Name = WiserTableNames.WiserStyledOutput,
            LastUpdate = new DateTime(2024, 06, 06),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("format_begin", MySqlDbType.MediumText),
                new ColumnSettingsModel("format_item", MySqlDbType.MediumText),
                new ColumnSettingsModel("format_end", MySqlDbType.MediumText),
                new ColumnSettingsModel("format_empty", MySqlDbType.MediumText),
                new ColumnSettingsModel("query_id", MySqlDbType.Int32),
                new ColumnSettingsModel("return_type", MySqlDbType.VarChar, 10),
                new ColumnSettingsModel("options", MySqlDbType.JSON),
                new ColumnSettingsModel("log_average_runtime", MySqlDbType.Double),
                new ColumnSettingsModel("log_run_count", MySqlDbType.Int32)
            ]
        },

        // wiser_parent_updates
        new()
        {
            Name = WiserTableNames.WiserParentUpdates,
            LastUpdate = new DateTime(2024, 3, 7),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("target_id", MySqlDbType.UInt64),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50),
                new ColumnSettingsModel("target_table", MySqlDbType.VarChar, 50)
            ]
        },

        // wiser_permission
        new()
        {
            Name = WiserTableNames.WiserPermission,
            LastUpdate = new DateTime(2025, 4, 4),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("role_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("entity_name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("entity_property_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("permissions", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: """
                                                                                                                     0 = Nothing
                                                                                                                     1 = Read
                                                                                                                     2 = Create
                                                                                                                     4 = Update
                                                                                                                     8 = Delete
                                                                                                                     """),

                new ColumnSettingsModel("module_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("query_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("data_selector_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("endpoint_url", MySqlDbType.VarChar, 500, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("endpoint_http_method", MySqlDbType.Enum, notNull: true, defaultValue: "GET", enumValues: new List<string> {"GET", "HEAD", "POST", "PUT", "DELETE", "CONNECT", "OPTIONS", "TRACE", "PATCH"})
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserPermission, "role_id", IndexTypes.Unique, ["role_id", "entity_name", "item_id", "entity_property_id", "module_id", "query_id", "data_selector_id", "endpoint_url", "endpoint_http_method"])]
        },

        // log_psp
        new()
        {
            Name = Payments.Models.Constants.PaymentServiceProviderLogTableName,
            LastUpdate = new DateTime(2022, 9, 30),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("payment_service_provider", MySqlDbType.VarChar, 50, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("unique_payment_number", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("status", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("request_headers", MySqlDbType.Text),
                new ColumnSettingsModel("request_query_string", MySqlDbType.Text),
                new ColumnSettingsModel("request_form_values", MySqlDbType.MediumText),
                new ColumnSettingsModel("request_body", MySqlDbType.MediumText),
                new ColumnSettingsModel("response_body", MySqlDbType.MediumText),
                new ColumnSettingsModel("error", MySqlDbType.Text),
                new ColumnSettingsModel("url", MySqlDbType.Text),
                new ColumnSettingsModel("type", MySqlDbType.Enum, enumValues: new List<string> {"incoming", "outgoing"})
            ]
        },

        // wiser_communication
        new()
        {
            Name = WiserTableNames.WiserCommunication,
            LastUpdate = new DateTime(2022, 10, 28),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 50, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("receiver_list", MySqlDbType.MediumText),
                new ColumnSettingsModel("receivers_data_selector_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("receivers_query_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("content_data_selector_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("content_query_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("settings", MySqlDbType.MediumText),
                new ColumnSettingsModel("send_trigger_type", MySqlDbType.Enum, enumValues: new List<string> {"direct", "fixed", "recurring"}),
                new ColumnSettingsModel("trigger_start", MySqlDbType.Date),
                new ColumnSettingsModel("trigger_end", MySqlDbType.Date),
                new ColumnSettingsModel("trigger_time", MySqlDbType.Time),
                new ColumnSettingsModel("trigger_period_value", MySqlDbType.Int16, 4, notNull: true, defaultValue: "1"),
                new ColumnSettingsModel("trigger_period_type", MySqlDbType.Enum, enumValues: new List<string> {"minute", "hour", "day", "week", "month", "year"}),
                new ColumnSettingsModel("trigger_week_days", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("trigger_day_of_month", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("last_processed", MySqlDbType.MediumText),
                new ColumnSettingsModel("added_by", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 100),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime)
            ],
            Indexes = [new IndexSettingsModel(WiserTableNames.WiserCommunication, "idx_name", IndexTypes.Unique, ["name"])]
        },

        // wiser_dynamic_content_render_log
        new()
        {
            Name = WiserTableNames.WiserDynamicContentRenderLog,
            LastUpdate = new DateTime(2023, 1, 6),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("content_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("version", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("url", MySqlDbType.VarChar, 1000, notNull: true),
                new ColumnSettingsModel("environment", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("start", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("end", MySqlDbType.DateTime),
                new ColumnSettingsModel("time_taken", MySqlDbType.Int32, comment: "Time in milliseconds it took to render the component this time"),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 10, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("error", MySqlDbType.MediumText)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserDynamicContentRenderLog, "idx_content_id_version", IndexTypes.Normal, ["content_id", "version"]),
                new IndexSettingsModel(WiserTableNames.WiserDynamicContentRenderLog, "idx_environment", IndexTypes.Normal, ["environment", "content_id", "version"])
            ]
        },

        // wiser_template_render_log
        new()
        {
            Name = WiserTableNames.WiserTemplateRenderLog,
            LastUpdate = new DateTime(2023, 1, 6),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("template_id", MySqlDbType.Int32, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("version", MySqlDbType.Int32, notNull: true),
                new ColumnSettingsModel("url", MySqlDbType.VarChar, 1000, notNull: true),
                new ColumnSettingsModel("environment", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("start", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("end", MySqlDbType.DateTime),
                new ColumnSettingsModel("time_taken", MySqlDbType.UInt64, comment: "Time in milliseconds it took to render the component this time"),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 10, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("error", MySqlDbType.MediumText)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserTemplateRenderLog, "idx_template_id_version", IndexTypes.Normal, ["template_id", "version"]),
                new IndexSettingsModel(WiserTableNames.WiserTemplateRenderLog, "idx_environment", IndexTypes.Normal, ["environment", "template_id", "version"])
            ]
        },

        // gcl_database_connection_log
        new()
        {
            Name = Constants.DatabaseConnectionLogTableName,
            LastUpdate = new DateTime(2023, 2, 1),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.Int32, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("opened", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("closed", MySqlDbType.DateTime),
                new ColumnSettingsModel("url", MySqlDbType.MediumText),
                new ColumnSettingsModel("http_method", MySqlDbType.VarChar, 20),
                new ColumnSettingsModel("database_service_instance_id", MySqlDbType.VarChar, 40),
                new ColumnSettingsModel("type", MySqlDbType.Enum, enumValues: new List<string> {"read", "write"})
            ],
            Indexes =
            [
                new IndexSettingsModel(Constants.DatabaseConnectionLogTableName, "idx_instance_id", IndexTypes.Normal, ["database_service_instance_id"]),
                new IndexSettingsModel(Constants.DatabaseConnectionLogTableName, "idx_closed", IndexTypes.Normal, ["closed"])
            ]
        },

        // gcl_request_log
        new()
        {
            Name = WiserTableNames.GclRequestLog,
            LastUpdate = new DateTime(2024, 6, 12),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("host", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("path", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("query_string", MySqlDbType.MediumText),
                new ColumnSettingsModel("scheme", MySqlDbType.VarChar, 10),
                new ColumnSettingsModel("method", MySqlDbType.VarChar, 10),
                new ColumnSettingsModel("protocol", MySqlDbType.VarChar, 20),
                new ColumnSettingsModel("request_headers", MySqlDbType.MediumText),
                new ColumnSettingsModel("request_body", MySqlDbType.MediumText),
                new ColumnSettingsModel("response_headers", MySqlDbType.MediumText),
                new ColumnSettingsModel("response_body", MySqlDbType.MediumText),
                new ColumnSettingsModel("status_code", MySqlDbType.Int24),
                new ColumnSettingsModel("environment", MySqlDbType.VarChar, 50, notNull: true),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64),
                new ColumnSettingsModel("ip_address", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("extra_data", MySqlDbType.JSON),
                new ColumnSettingsModel("start_datetime", MySqlDbType.DateTime, notNull: true),
                new ColumnSettingsModel("end_datetime", MySqlDbType.DateTime)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.GclRequestLog, "idx_environment", IndexTypes.Normal, ["environment", "user_id", "status_code"]),
                new IndexSettingsModel(WiserTableNames.GclRequestLog, "idx_user_id", IndexTypes.Normal, ["user_id", "status_code"]),
                new IndexSettingsModel(WiserTableNames.GclRequestLog, "idx_status_code", IndexTypes.Normal, ["status_code"]),
                new IndexSettingsModel(WiserTableNames.GclRequestLog, "idx_host", IndexTypes.Normal, ["host", "path", "method"]),
                new IndexSettingsModel(WiserTableNames.GclRequestLog, "idx_path", IndexTypes.Normal, ["path", "method"])
            ]
        },

        // wiser_history
        new()
        {
            Name = WiserTableNames.WiserHistory,
            LastUpdate = new DateTime(2024, 7, 18),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("action", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("tablename", MySqlDbType.VarChar, 255, notNull: true),
                new ColumnSettingsModel("item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                new ColumnSettingsModel("changed_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                new ColumnSettingsModel("changed_by", MySqlDbType.VarChar, 50, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("field", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("oldvalue", MySqlDbType.MediumText),
                new ColumnSettingsModel("newvalue", MySqlDbType.MediumText),
                new ColumnSettingsModel("language_code", MySqlDbType.VarChar, 5, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("groupname", MySqlDbType.VarChar, 100, notNull: true, defaultValue: ""),
                new ColumnSettingsModel("target_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0")
            ]
        },

        // wiser_user_roles
        new()
        {
            Name = WiserTableNames.WiserUserRoles,
            LastUpdate = new DateTime(2025, 1, 24),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("user_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("role_id", MySqlDbType.UInt64, notNull: true),
                new ColumnSettingsModel("ip_addresses", MySqlDbType.JSON)
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserUserRoles, "idx_user_id", IndexTypes.Unique, ["user_id", "role_id"])
            ]
        },

        // wiser_module
        new()
        {
            Name = WiserTableNames.WiserModule,
            LastUpdate = new DateTime(2022, 1, 1),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("custom_query", MySqlDbType.MediumText),
                new ColumnSettingsModel("count_query", MySqlDbType.MediumText),
                new ColumnSettingsModel("options", MySqlDbType.MediumText),
                new ColumnSettingsModel("name", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("icon", MySqlDbType.VarChar, 100),
                new ColumnSettingsModel("color", MySqlDbType.VarChar, 8),
                new ColumnSettingsModel("type", MySqlDbType.VarChar, 255),
                new ColumnSettingsModel("group", MySqlDbType.VarChar, 100)
            ]
        },

        // wiser_branch_merge_log
        new()
        {
            Name = WiserTableNames.WiserBranchMergeLog,
            LastUpdate = new DateTime(2025, 4, 16),
            Columns =
            [
                new ColumnSettingsModel("id", MySqlDbType.UInt64, notNull: true, isPrimaryKey: true, autoIncrement: true),
                new ColumnSettingsModel("branch_queue_id", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "The ID of the merge action from the wiser_branches_queue table in the production database."),
                new ColumnSettingsModel("branch_queue_name", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "", comment: "The name of the merge action from the wiser_branches_queue table in the production database."),
                new ColumnSettingsModel("branch_id", MySqlDbType.Int32, notNull: true, defaultValue: "0", comment: "The tenant ID of the branch, from the easy_customers table of the main Wiser database."),
                new ColumnSettingsModel("date_time", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP", comment: "The date and time that the current action was executed."),

                new ColumnSettingsModel("history_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "The ID from the wiser_history table in the branch database that was being merged."),
                new ColumnSettingsModel("table_name", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "The table name as it was stored in the wiser_history table of the branch database."),
                new ColumnSettingsModel("field", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "", comment: "The field name as it was stored in the wiser_history table of the branch database. This can be the `key` from wiser_itemdetail tables, or a column name of any other table."),
                new ColumnSettingsModel("action", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "", comment: "The action as it was stored in the wiser_history table of the branch database."),
                new ColumnSettingsModel("old_value", MySqlDbType.MediumText, notNull: true, defaultValue: "", comment: "The value as it was before the change."),
                new ColumnSettingsModel("new_value", MySqlDbType.MediumText, notNull: true, defaultValue: "", comment: "The value that it was changed to in the branch database, which we attempted to merge to the production database."),

                new ColumnSettingsModel("object_id_original", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "The id of the object in the branch database. This is the original value of the column `item_id` from wiser_history."),
                new ColumnSettingsModel("object_id_mapped", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "The id of the object in the production database."),

                new ColumnSettingsModel("item_id_original", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "If this change was for a Wiser item or something related to a Wiser item (such as a link), then this will contain the ID of the Wiser item in the branch database. If this is a change for a link, this is the ID of the source item."),
                new ColumnSettingsModel("item_id_mapped", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "Same as `original_item_id`, but then for the production database."),
                new ColumnSettingsModel("item_entity_type", MySqlDbType.VarChar, 25, notNull: true, defaultValue: "", comment: "If this change was for a Wiser item or something related to a Wiser item (such as a link), then this will contain the entity type of that item, as we found it. If this is a change for a link, this is the entity type of the source item."),
                new ColumnSettingsModel("item_table_name", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "If this change was for a Wiser item or something related to a Wiser item (such as a link), then this will contain the full name of the `wiser_item` table that we used. If this is a change for a link, this is the table of the source item."),

                new ColumnSettingsModel("link_id_original", MySqlDbType.UInt64, defaultValue: "0", notNull: true, comment: "If this is a change for a link, this is the value of the `id` column of `wiser_itemlink`, in the branch database."),
                new ColumnSettingsModel("link_id_mapped", MySqlDbType.UInt64, defaultValue: "0", notNull: true, comment: "Same as `link_id_original`, but then for the production database."),
                new ColumnSettingsModel("link_destination_item_id_original", MySqlDbType.UInt64, defaultValue: "0", notNull: true, comment: "If this is a change for a link, this is the ID of the destination Wiser item of that link, in the branch database."),
                new ColumnSettingsModel("link_destination_item_id_mapped", MySqlDbType.UInt64, defaultValue: "0", notNull: true, comment: "Same as `original_destination_item_id`, but then for the production database."),
                new ColumnSettingsModel("link_destination_item_entity_type", MySqlDbType.VarChar, 25, notNull: true, defaultValue: "", comment: "If this is a change for a link, this is the entity type of the destination Wiser item of that link."),
                new ColumnSettingsModel("link_destination_item_table_name", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "If this is a change for a link, this is the table name of the destination Wiser item of that link."),
                new ColumnSettingsModel("link_type", MySqlDbType.Int32, defaultValue: "0", notNull: true, comment: "If this is a change for a link, then this is the value of the `type` column of `wiser_itemlink`."),
                new ColumnSettingsModel("link_ordering", MySqlDbType.Int32, defaultValue: "0", notNull: true, comment: "If this is a change for a link, then this is the value of the `ordering` column of `wiser_itemlink`."),
                new ColumnSettingsModel("link_table_name", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "If this is a change for a link, or something related to a link, then this is the full table name of the link table that we used."),

                new ColumnSettingsModel("item_detail_id_original", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "If this is a change for a Wiser item detail, this will contain the value of the `id` column of `wiser_itemdetail`, in the branch database."),
                new ColumnSettingsModel("item_detail_id_mapped", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "Same as `item_detail_id_original`, but for the production database."),
                new ColumnSettingsModel("item_detail_language_code", MySqlDbType.VarChar, 10, notNull: true, defaultValue: "", comment: "If this is a change for a Wiser item detail, this will contain the value of the `language_code` column of `wiser_itemdetail`, in the branch database."),
                new ColumnSettingsModel("item_detail_group_name", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "", comment: "If this is a change for a Wiser item detail, this will contain the value of the `groupname` column of `wiser_itemdetail`, in the branch database."),

                new ColumnSettingsModel("file_id_original", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "If this change was for a file in the database, then this will contain the value of the `id` column of `wiser_itemfile` in the branch database."),
                new ColumnSettingsModel("file_id_mapped", MySqlDbType.UInt64, notNull: true, defaultValue: "0", comment: "Same as `file_id_original`, but then for the production database."),

                new ColumnSettingsModel("used_merge_settings", MySqlDbType.JSON, comment: "The merge settings that were used for this specific action/object."),
                new ColumnSettingsModel("used_conflict_settings", MySqlDbType.JSON, comment: "The conflict settings that were used for this specific action/object."),

                new ColumnSettingsModel("production_host", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "", comment: "The hostname of the production database server."),
                new ColumnSettingsModel("production_database", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "The name of the production database schema."),
                new ColumnSettingsModel("branch_host", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "", comment: "The hostname of the branch database server."),
                new ColumnSettingsModel("branch_database", MySqlDbType.VarChar, 64, notNull: true, defaultValue: "", comment: "The name of the branch database schema."),

                new ColumnSettingsModel("status", MySqlDbType.Enum, notNull: true, enumValues: ["None", "Merged", "Skipped", "SkippedAndRemoved", "Failed"], defaultValue: "none", comment: "The merge status of this object."),
                new ColumnSettingsModel("message", MySqlDbType.MediumText, notNull: true, defaultValue: "", comment: "This will contain debug information that explains what happened, where information was taken from etc. If the merge failed, it will explain the reason and/or contain the error message. If the merge was skipped, it will explain why."),

                new ColumnSettingsModel("developer_comment", MySqlDbType.MediumText, notNull: true, defaultValue: "", comment: "A column that is not used by the system. It can be used by developers to add comments about this log entry, to note down their findings when checking whether why a merge failed or was skipped.")
            ],
            Indexes =
            [
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_branch_queue_id", IndexTypes.Normal, ["branch_queue_id"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_history_id", IndexTypes.Normal, ["history_id"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_object_id_original", IndexTypes.Normal, ["object_id_original"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_object_id_mapped", IndexTypes.Normal, ["object_id_mapped"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_item_id_original", IndexTypes.Normal, ["item_id_original"]),
                new IndexSettingsModel(WiserTableNames.WiserBranchMergeLog, "idx_item_id_mapped", IndexTypes.Normal, ["item_id_mapped"])
            ]
        }
    ];
}