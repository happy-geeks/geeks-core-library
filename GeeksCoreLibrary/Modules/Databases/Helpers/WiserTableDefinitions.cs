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
                    new ("original_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new ("unique_uuid", MySqlDbType.VarChar, 200, notNull: true, defaultValue: ""),
                    new ("parent_item_id", MySqlDbType.UInt64, notNull: true, defaultValue: "0"),
                    new ("ordering", MySqlDbType.Int24, notNull: true, defaultValue: "0"),
                    new ("entity_type", MySqlDbType.VarChar, 100, notNull: true, defaultValue: "0"),
                    new ("moduleid", MySqlDbType.Int32, 11, notNull: true, defaultValue: "0"),
                    new ("published_environment", MySqlDbType.Int16, notNull: true, defaultValue: "15"),
                    new ("readonly", MySqlDbType.Int16, notNull: true, defaultValue: "0"),
                    new ("title", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new ("added_on", MySqlDbType.DateTime, notNull: true, defaultValue: "CURRENT_TIMESTAMP"),
                    new ("added_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: ""),
                    new ("changed_on", MySqlDbType.DateTime, notNull: true, updateTimeStampOnChange: true),
                    new ("changed_by", MySqlDbType.VarChar, 255, notNull: true, defaultValue: "")
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new (WiserTableNames.WiserItem, "idx_module_env", IndexTypes.Normal, new List<string> { "moduleid", "published_environment" }),
                    new (WiserTableNames.WiserItem, "idx_entity", IndexTypes.Normal, new List<string> { "entity_type", "unique_uuid" }),
                    new (WiserTableNames.WiserItem, "idx_unique_uuid", IndexTypes.Normal, new List<string> { "unique_uuid" }),
                    new (WiserTableNames.WiserItem, "idx_original_item_id", IndexTypes.Normal, new List<string> { "original_item_id" }),
                    new (WiserTableNames.WiserItem, "idx_parent", IndexTypes.Normal, new List<string> { "parent_item_id", "entity_type" })
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
                    new ("key", MySqlDbType.VarChar, 512, notNull: true),
                    new ("type", MySqlDbType.VarChar, 255, notNull: true),
                    new ("client_id", MySqlDbType.VarChar, 50, notNull: true),
                    new ("data", MySqlDbType.MediumText),
                    new ("subject_id", MySqlDbType.VarChar, 255, notNull: true),
                    new ("description", MySqlDbType.VarChar, 512),
                    new ("creation_time", MySqlDbType.DateTime, notNull: true),
                    new ("expiration", MySqlDbType.DateTime, notNull: true),
                    new ("session_id", MySqlDbType.VarChar, 255)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new (WiserTableNames.WiserGrantStore, "idx_key", IndexTypes.Unique, new List<string> { "key" })
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
                    new ("selector", MySqlDbType.VarChar, 32, notNull: true),
                    new ("hashed_validator", MySqlDbType.VarChar, 150, notNull: true),
                    new ("user_id", MySqlDbType.Int64, notNull: true),
                    new ("main_user_id", MySqlDbType.Int64, notNull: true),
                    new ("entity_type", MySqlDbType.VarChar, 255, notNull: true),
                    new ("main_user_entity_type", MySqlDbType.VarChar, 255, notNull: true),
                    new ("role", MySqlDbType.VarChar, 255),
                    new ("ip_address", MySqlDbType.VarChar, 255, notNull: true),
                    new ("user_agent", MySqlDbType.VarChar, 2000),
                    new ("login_date", MySqlDbType.DateTime, notNull: true),
                    new ("expires", MySqlDbType.DateTime, notNull: true)
                },
                Indexes = new List<IndexSettingsModel>
                {
                    new (Components.Account.Models.Constants.AuthenticationTokensTableName, "idx_selector", IndexTypes.Unique, new List<string> { "selector", "entity_type" })
                }
            }
        };
    }
}
