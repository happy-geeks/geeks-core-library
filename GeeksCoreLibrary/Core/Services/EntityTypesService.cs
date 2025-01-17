using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Services;

/// <inheritdoc cref="IEntityTypesService"/>
public class EntityTypesService : IEntityTypesService, IScopedService
{
    private readonly IDatabaseConnection databaseConnection;

    /// <summary>
    /// Creates a new instance of <see cref="EntityTypesService"/>.
    /// </summary>
    public EntityTypesService(IDatabaseConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetDedicatedTablePrefixesAsync()
    {
        var prefixes = new List<string>();

        var query = $"SELECT DISTINCT dedicated_table_prefix FROM {WiserTableNames.WiserEntity} WHERE dedicated_table_prefix IS NOT NULL AND dedicated_table_prefix != ''";
        var dataTable = await databaseConnection.GetAsync(query);

        if (dataTable.Rows.Count <= 0)
        {
            return prefixes;
        }

        foreach (DataRow dataRow in dataTable.Rows)
        {
            var tablePrefix = dataRow.Field<string>("dedicated_table_prefix");
            if (!tablePrefix!.EndsWith("_"))
            {
                tablePrefix += "_";
            }

            prefixes.Add(tablePrefix);
        }

        return prefixes;
    }

    /// <inheritdoc />
    public async Task<string> GetTablePrefixForEntityAsync(string entityType)
    {
        return await GetTablePrefixForEntityAsync(this, entityType);
    }

    /// <inheritdoc />
    public async Task<string> GetTablePrefixForEntityAsync(IEntityTypesService entityTypesService, string entityType)
    {
        if (String.IsNullOrWhiteSpace(entityType))
        {
            return "";
        }

        var settings = await entityTypesService.GetEntityTypeSettingsAsync(entityType);
        return entityTypesService.GetTablePrefixForEntity(settings);
    }

    /// <inheritdoc />
    public string GetTablePrefixForEntity(EntitySettingsModel entityTypeSettings)
    {
        var result = entityTypeSettings?.DedicatedTablePrefix ?? "";
        if (!String.IsNullOrWhiteSpace(result) && !result.EndsWith("_"))
        {
            result += "_";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0)
    {
        databaseConnection.AddParameter("entityType", entityType);
        var query = $"""
                     SELECT 
                                                     entity.*,
                     	                            IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name) AS displayName,
                     
                                                     property.id AS property_id,
                                                     IF(property.property_name IS NULL OR property.property_name = '', property.display_name, property.property_name) AS property_name, 
                                                     property.inputtype,
                                                     property.language_code,
                                                     property.options,
                                                     property.also_save_seo_value,
                                                     property.readonly
                                                 # DUAL is a fake MySQL table that you can use in queries. We use it because sometimes there is no row in wiser_entity and sometimes no row in wiser_entityproperty, so both of these need to be left joins.
                                                 FROM (SELECT 1 FROM DUAL) AS d
                                                 LEFT JOIN {WiserTableNames.WiserEntity} AS entity ON entity.name = ?entityType
                                                 LEFT JOIN {WiserTableNames.WiserEntityProperty} AS property ON property.entity_name = ?entityType
                                                 ORDER BY entity.id ASC, property.ordering ASC
                     """;

        var dataTable = await databaseConnection.GetAsync(query);
        if (dataTable.Rows.Count <= 0)
        {
            return new EntitySettingsModel();
        }

        var allEntityTypeSettings = new List<EntitySettingsModel>();
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var id = dataRow.Field<int?>("id") ?? 0;
            var settings = allEntityTypeSettings.FirstOrDefault(e => e.Id == id);

            if (settings == null)
            {
                settings = new EntitySettingsModel
                {
                    Id = id,
                    ModuleId = dataRow.Field<int?>("module_id") ?? 0,
                    EntityType = entityType,
                    QueryAfterInsert = dataRow.Field<string>("query_after_insert"),
                    QueryAfterUpdate = dataRow.Field<string>("query_after_update"),
                    SaveTitleAsSeo = !dataRow.IsNull("save_title_as_seo") && Convert.ToInt16(dataRow["save_title_as_seo"]) > 0,
                    DedicatedTablePrefix = dataTable.Columns.Contains("dedicated_table_prefix") ? dataRow.Field<string>("dedicated_table_prefix") : "",
                    EnableMultipleEnvironments = dataTable.Columns.Contains("enable_multiple_environments") && !dataRow.IsNull("enable_multiple_environments") && Convert.ToInt32(dataRow["enable_multiple_environments"]) > 0,
                    AcceptedChildTypes = (dataRow.Field<string>("accepted_childtypes") ?? "").Split(',').ToList(),
                    ShowInTreeView = !dataRow.IsNull("show_in_tree_view") && Convert.ToBoolean(dataRow["show_in_tree_view"]),
                    ShowOverviewTab = !dataRow.IsNull("show_overview_tab") && Convert.ToBoolean(dataRow["show_overview_tab"]),
                    ShowTitleField = !dataRow.IsNull("show_title_field") && Convert.ToBoolean(dataRow["show_title_field"]),
                    DisplayName = dataRow.Field<string>("displayName"),
                    SaveHistory = dataRow.IsNull("save_history") || Convert.ToBoolean(dataRow["save_history"]),
                    DeleteAction = dataRow.Field<string>("delete_action")?.ToLowerInvariant() switch
                    {
                        null => EntityDeletionTypes.Archive,
                        "archive" => EntityDeletionTypes.Archive,
                        "permanent" => EntityDeletionTypes.Permanent,
                        "hide" => EntityDeletionTypes.Hide,
                        "disallow" => EntityDeletionTypes.Disallow,
                        // ReSharper disable once NotResolvedInText
                        _ => throw new ArgumentOutOfRangeException("delete_action", dataRow.Field<string>("delete_action"), null)
                    },
                    StoreType = dataRow.Field<string>("store_type")?.ToLowerInvariant() switch
                    {
                        null => StoreType.Table,
                        "" => StoreType.Table,
                        "table" => StoreType.Table,
                        "document_store" => StoreType.DocumentStore,
                        "hybrid" => StoreType.Hybrid,
                        // ReSharper disable once NotResolvedInText
                        _ => throw new ArgumentOutOfRangeException("store_type", dataRow.Field<string>("store_type"), null)
                    }
                };

                allEntityTypeSettings.Add(settings);
            }

            if (dataRow.IsNull("property_id"))
            {
                continue;
            }

            var propertyName = dataRow.Field<string>("property_name");
            var optionsJson = dataRow.Field<string>("options");
            var fieldType = dataRow.Field<string>("inputtype");
            var languageCode = dataRow.Field<string>("language_code");
            var alsoSaveSeoValue = Convert.ToBoolean(dataRow["also_save_seo_value"]);
            var readOnly = Convert.ToBoolean(dataRow["readonly"]);

            var options = new Dictionary<string, object>();
            if (!String.IsNullOrWhiteSpace(optionsJson))
            {
                options = JsonConvert.DeserializeObject<Dictionary<string, object>>(optionsJson) ?? new Dictionary<string, object>();
            }

            options.Add(Constants.FieldTypeKey, fieldType);
            options.Add(Constants.SaveSeoValueKey, alsoSaveSeoValue);
            options.Add(Constants.ReadOnlyKey, readOnly);

            settings.FieldOptions[$"{propertyName}_{languageCode}"] = options;

            if (String.Equals(fieldType, "auto-increment", StringComparison.OrdinalIgnoreCase))
            {
                settings.AutoIncrementFields.Add((propertyName, languageCode));
            }
        }

        // If there is an entity type for the specified module, prefer that one, otherwise just take the first one.
        // An entity type can exist multiple times in wiser_entity, for different modules. This almost never actually happens though.
        return allEntityTypeSettings.MinBy(e => e.ModuleId == moduleId ? 0 : 1) ?? new EntitySettingsModel();
    }
}