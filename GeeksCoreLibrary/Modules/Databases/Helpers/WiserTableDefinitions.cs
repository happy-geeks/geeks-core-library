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
            }
        };
    }
}
