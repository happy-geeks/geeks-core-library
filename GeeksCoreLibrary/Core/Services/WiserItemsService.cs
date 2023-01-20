using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Exceptions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace GeeksCoreLibrary.Core.Services
{
    /// <inheritdoc cref="IWiserItemsService" />
    public class WiserItemsService : IWiserItemsService, IScopedService
    {
        #region Privates

        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IDataSelectorsService dataSelectorsService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<WiserItemsService> logger;
        private readonly GclSettings gclSettings;
        private const int MaximumLevelsToDuplicate = 25;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of <see cref="WiserItemsService"/>.
        /// </summary>
        public WiserItemsService(IDatabaseConnection databaseConnection, IObjectsService objectsService, IStringReplacementsService stringReplacementsService, IDataSelectorsService dataSelectorsService, IDatabaseHelpersService databaseHelpersService, IOptions<GclSettings> gclSettings, ILogger<WiserItemsService> logger)
        {
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.stringReplacementsService = stringReplacementsService;
            this.dataSelectorsService = dataSelectorsService;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
        }

        #endregion

        #region Public constants

        // Keys for the JSON object that we need to read, from the "options" column in the table "wiser_entityproperty".
        public const string FieldTypeKey = "_fieldType";
        public const string SaveSeoValueKey = "_alsoSaveSeoValue";
        public const string ReadOnlyKey = "_readOnly";
        public const string SecurityMethodKey = "securityMethod";
        public const string SecurityKeyKey = "securityKey";
        public const string CultureKey = "culture";
        public const string SizeKey = "size";
        public const string SeoPropertySuffix = "_SEO";
        public const string AutoIncrementPropertySuffix = "_auto_increment";
        public const string SaveValueAsItemLinkKey = "saveValueAsItemLink";
        public const string CurrentItemIsDestinationIdKey = "currentItemIsDestinationId";
        public const string LinkTypeNumberKey = "linkTypeNumber";
        public const string DefaultInputType = "text";
        public const string LinkOrderingFieldName = "__ordering";

        #endregion

        #region Implemented methods from interface

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await SaveAsync(this, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "",             bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();

            try
            {
                if (wiserItem.Id == 0)
                {
                    wiserItem = await wiserItemsService.CreateAsync(wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, saveHistory, false, skipPermissionsCheck);
                }

                var result = await wiserItemsService.UpdateAsync(wiserItem.Id, wiserItem, userId, username, encryptionKey, alwaysSaveValues, saveHistory, false, skipPermissionsCheck);

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();

                return result;
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> CreateAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await CreateAsync(this, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, saveHistory, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> CreateAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            if (String.IsNullOrWhiteSpace(wiserItem?.EntityType))
            {
                throw new ArgumentNullException(nameof(wiserItem.EntityType));
            }

            if (parentId is > 0 && !skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(parentId.Value, EntityActions.Create, userId, wiserItem);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to create items that are linked to '{parentId}'.")
                    {
                        Action = EntityActions.Create,
                        ItemId = parentId.Value,
                        UserId = userId
                    };
                }
            }

            var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync(wiserItem.EntityType);
            var tablePrefix = wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);
            if (wiserItem.ModuleId <= 0 && entityTypeSettings != null)
            {
                wiserItem.ModuleId = entityTypeSettings.ModuleId;
            }

            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();

            try
            {
                databaseConnection.AddParameter("moduleId", wiserItem.ModuleId);
                databaseConnection.AddParameter("title", wiserItem.Title ?? "");
                databaseConnection.AddParameter("entityType", wiserItem.EntityType);
                databaseConnection.AddParameter("parentId", parentId);
                databaseConnection.AddParameter("linkTypeNumber", linkTypeNumber);
                databaseConnection.AddParameter("username", username);
                databaseConnection.AddParameter("username", username);
                databaseConnection.AddParameter("userId", userId);
                databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
                var query = $@"SET @saveHistory = ?saveHistoryGcl;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryGcl;
                        INSERT INTO {tablePrefix}{WiserTableNames.WiserItem} (moduleid, title, entity_type, added_by)
                        VALUES (?moduleId, ?title, ?entityType, ?username);
                        SELECT LAST_INSERT_ID() AS newId;";
                var queryResult = await databaseConnection.GetAsync(query, true);

                if (queryResult.Rows.Count == 0)
                {
                    return null;
                }

                wiserItem.Id = Convert.ToUInt64(queryResult.Rows[0]["newId"]);
                wiserItem.EncryptedId = wiserItem.Id.ToString().EncryptWithAesWithSalt(encryptionKey, true);

                if (!parentId.HasValue)
                {
                    return wiserItem;
                }

                // Check where we need to save the link to the parent ID.
                databaseConnection.AddParameter("parentId", parentId.Value);
                queryResult = await databaseConnection.GetAsync($@"SELECT entity_type FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE id = ?parentId", true);
                var destinationEntityType = "";
                if (queryResult.Rows.Count > 0)
                {
                    destinationEntityType = queryResult.Rows[0].Field<string>("entity_type");
                }

                var linkTypeSettings = await wiserItemsService.GetLinkTypeSettingsAsync(0, wiserItem.EntityType, destinationEntityType);
                if (linkTypeSettings is { UseItemParentId: true })
                {
                    // Save parent ID in parent_item_id column of wiser_item.
                    wiserItem.ParentItemId = parentId.Value;
                    databaseConnection.AddParameter("newItemId", wiserItem.Id);
                    databaseConnection.AddParameter("parentId", parentId);
                    await databaseConnection.ExecuteAsync($@"SET @newOrdering = (SELECT IFNULL(MAX(ordering), 0) + 1 FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE parent_item_id = ?parentId);
UPDATE {tablePrefix}{WiserTableNames.WiserItem} SET parent_item_id = ?parentId, ordering = @newOrdering WHERE id = ?newItemId");
                }
                else
                {
                    var linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
                    
                    // Save parent ID in wiser_itemlink.
                    var newOrderNumber = 1;
                    queryResult = await databaseConnection.GetAsync($"SELECT IFNULL(MAX(ordering), 0) + 1 AS newOrderNumber FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE destination_item_id = ?parentId", true);
                    if (queryResult.Rows.Count > 0)
                    {
                        newOrderNumber = Convert.ToInt32(queryResult.Rows[0]["newOrderNumber"]);
                    }

                    databaseConnection.AddParameter("newId", wiserItem.Id);
                    databaseConnection.AddParameter("newOrderNumber", newOrderNumber);
                    await databaseConnection.ExecuteAsync($@"INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLink} (item_id, destination_item_id, ordering, type)
                                                            VALUES (?newId, ?parentId, ?newOrderNumber, ?linkTypeNumber)");
                }

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();

                return wiserItem;
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<WiserItemDuplicationResultModel> DuplicateItemAsync(ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DuplicateItemAsync(this, itemId, parentId, username, encryptionKey, userId, entityType, parentEntityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemDuplicationResultModel> DuplicateItemAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            if (itemId <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.");
            }

            // Keep track of items that we already duplicated, to prevent never ending functions in cases where items are linked to each other.
            var duplicatedItemIds = new List<ulong>();

            // Local function for duplicating an item.
            async Task<WiserItemDuplicationResultModel> DuplicateItemLocalAsync(ulong itemIdToDuplicate, ulong parentIdOfItemToDuplicate, int duplicationLevel, string entityTypeToDuplicate, string parentEntityTypeInner)
            {
                // Some checks to prevent a never ending recursive loop, in cases where items are linked to each other for example.
                if (duplicatedItemIds.Contains(itemIdToDuplicate) || duplicationLevel > MaximumLevelsToDuplicate)
                {
                    return null;
                }

                if (!skipPermissionsCheck)
                {
                    var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemIdToDuplicate, EntityActions.Read, userId, entityType: entityTypeToDuplicate);
                    if (!isPossible.ok)
                    {
                        throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to read item '{itemIdToDuplicate}' and therefore they cannot duplicate it.")
                        {
                            Action = EntityActions.Read,
                            ItemId = itemIdToDuplicate,
                            UserId = userId
                        };
                    }

                    isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(parentIdOfItemToDuplicate, EntityActions.Create, userId, entityType: parentEntityTypeInner);
                    if (!isPossible.ok)
                    {
                        throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to create items that are linked to '{parentIdOfItemToDuplicate}'.")
                        {
                            Action = EntityActions.Create,
                            ItemId = itemIdToDuplicate,
                            UserId = userId
                        };
                    }
                }

                duplicatedItemIds.Add(itemIdToDuplicate);

                databaseConnection.AddParameter("itemId", itemIdToDuplicate);
                databaseConnection.AddParameter("parentId", parentIdOfItemToDuplicate);
                databaseConnection.AddParameter("username", username);

                var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityTypeToDuplicate);
                var useItemParentId = false;
                var linkTablePrefix = "";
                if (!String.IsNullOrWhiteSpace(entityTypeToDuplicate) && !String.IsNullOrWhiteSpace(parentEntityTypeInner))
                {
                    var linkTypeSettings = await wiserItemsService.GetLinkTypeSettingsAsync(0, entityTypeToDuplicate, parentEntityTypeInner);
                    useItemParentId = linkTypeSettings.UseItemParentId;
                    linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
                }

                var addItemLinkQuery = useItemParentId ? "" : $@"#Duplicate item into current node
                                                                    SET @new_order = (SELECT IFNULL(MAX(ordering), 0) + 1 FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE destination_item_id = ?parentId);
                                                                    INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLink} (item_id, destination_item_id, ordering) VALUES (LAST_INSERT_ID(), ?parentId, @new_order);
                                                                    SET @newLinkId = (SELECT LAST_INSERT_ID());";

                // TODO: Duplicate item link details.
                var query = $@"#Duplicate item
                                INSERT INTO {tablePrefix}{WiserTableNames.WiserItem} (entity_type, moduleid, published_environment, readonly, title, added_on, added_by, parent_item_id)
                                (
                                    SELECT i.entity_type, i.moduleid, i.published_environment, i.readonly, {(duplicationLevel == 1 ? "CONCAT(i.title, '_duplicate')" : "i.title")}, NOW(), ?username AS added_by, {(useItemParentId ? "?parentId" : "0")}
                                    FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                    WHERE id = ?itemId
                                );

                                SET @newItemId = (SELECT LAST_INSERT_ID());

                                {addItemLinkQuery}

                                #Duplicate values
                                INSERT INTO {tablePrefix}{WiserTableNames.WiserItemDetail} (language_code, item_id, groupname, `key`, `value`, long_value)
                                (SELECT language_code, @newItemId, groupname, `key`, `value`, long_value FROM {tablePrefix}{WiserTableNames.WiserItemDetail} WHERE item_id = ?itemId);

                                #Duplicate files
                                INSERT INTO {tablePrefix}{WiserTableNames.WiserItemFile} (item_id, content_type, content, content_url, width, height, file_name, extension, title, property_name, added_on, added_by)
                                (SELECT @newItemId, content_type, content, content_url, width, height, file_name, extension, title, property_name, NOW(), ?username FROM {tablePrefix}{WiserTableNames.WiserItemFile} WHERE item_id = ?itemId);

                                SELECT @newItemId AS newItemId, we.icon, {(useItemParentId ? "0" : "@newLinkId")} AS newLinkId, i.title
                                FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                LEFT JOIN {WiserTableNames.WiserEntity} we ON we.name = i.entity_type
                                WHERE i.id = @newItemId
                                LIMIT 1;";

                var dataTable = await databaseConnection.GetAsync(query, true);
                var firstRow = dataTable.Rows[0];
                var newItemId = firstRow.Field<ulong>("newItemId");
                duplicatedItemIds.Add(newItemId);

                var result = new WiserItemDuplicationResultModel
                {
                    NewItemIdPlain = newItemId,
                    NewItemId = newItemId.ToString().EncryptWithAesWithSalt(encryptionKey, true),
                    Icon = firstRow.Field<string>("icon"),
                    NewLinkId = Convert.ToUInt64(firstRow["newLinkId"]),
                    Title = firstRow.Field<string>("title"),
                    Haschilds = await DuplicateLinksAsync(itemIdToDuplicate, newItemId, duplicationLevel + 1, entityTypeToDuplicate, parentEntityTypeInner)
                };

                return result;
            }

            // Function for duplicating all links of an item.
            async Task<bool> DuplicateLinksAsync(ulong oldItemId, ulong newItemId, int duplicationLevel, string entityTypeToDuplicate, string parentEntityTypeInner)
            {
                databaseConnection.AddParameter("oldItemId", oldItemId);
                var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityTypeToDuplicate);
                var linkTablePrefix = "";
                if (!String.IsNullOrWhiteSpace(entityTypeToDuplicate) && !String.IsNullOrWhiteSpace(parentEntityTypeInner))
                {
                    linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(0, entityTypeToDuplicate, parentEntityTypeInner);
                }

                var query = $@"(
                                    SELECT 
	                                    link_settings.duplication,
	                                    link.item_id,
	                                    link.destination_item_id,
	                                    link.type,
	                                    link.ordering,
                                        linked_item.entity_type,
                                        link_settings.use_item_parent_id
                                    FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                                    JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.destination_item_id = item.id
                                    JOIN {tablePrefix}{WiserTableNames.WiserItem} AS linked_item ON linked_item.id = link.item_id
                                    JOIN {WiserTableNames.WiserLink} AS link_settings ON link_settings.destination_entity_type = item.entity_type AND link_settings.connected_entity_type = linked_item.entity_type AND link_settings.duplication <> 'none'
                                    WHERE item.id = ?oldItemId
                                    ORDER BY link.ordering ASC
                                )
                                UNION
                                (
                                    SELECT 
	                                    link_settings.duplication,
	                                    linked_item.id AS item_id,
	                                    item.id AS destination_item_id,
	                                    1 AS type,
	                                    0 AS ordering,
	                                    linked_item.entity_type,
	                                    link_settings.use_item_parent_id
                                    FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                                    JOIN {tablePrefix}{WiserTableNames.WiserItem} AS linked_item ON linked_item.parent_item_id = item.id
                                    JOIN {WiserTableNames.WiserLink} AS link_settings ON link_settings.destination_entity_type = item.entity_type AND link_settings.connected_entity_type = linked_item.entity_type AND link_settings.duplication <> 'none'
                                    WHERE item.id = ?oldItemId
                                    ORDER BY item.title ASC
                                )";
                var dataTable = await databaseConnection.GetAsync(query, true);

                if (dataTable.Rows.Count == 0)
                {
                    return false;
                }

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var duplicationType = dataRow.Field<string>("duplication");
                    var linkedItemId = Convert.ToUInt64(dataRow["item_id"]);
                    var linkType = Convert.ToInt32(dataRow["type"]);
                    var useItemParentId = Convert.ToBoolean(dataRow["use_item_parent_id"]);
                    var useDedicatedTable = Convert.ToBoolean(dataRow["use_dedicated_table"]);
                    linkTablePrefix = !useDedicatedTable ? "" : $"{linkType}_";

                    switch (duplicationType?.ToUpperInvariant())
                    {
                        case "NONE":
                            // Do nothing.
                            continue;
                        case "COPY-LINK" when !useItemParentId:
                            // TODO: Duplicate item link details.
                            databaseConnection.AddParameter("newItemId", newItemId);
                            databaseConnection.AddParameter("linkedItemId", linkedItemId);
                            databaseConnection.AddParameter("linkType", linkType);

                            query = $@"SET @new_order = (SELECT IFNULL(MAX(ordering), 0) + 1 FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE destination_item_id = ?newItemId);
                                    INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLink} (item_id, destination_item_id, ordering, type) 
                                    VALUES (?linkedItemId, ?newItemId, @new_order, ?linkType);";
                            await databaseConnection.ExecuteAsync(query);
                            break;
                        case "COPY-ITEM":
                            await DuplicateItemLocalAsync(linkedItemId, newItemId, duplicationLevel, dataRow.Field<string>("entity_type"), entityTypeToDuplicate);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(duplicationType), duplicationType);
                    }
                }

                return true;
            }

            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();

            try
            {
                var result = await DuplicateItemLocalAsync(itemId, parentId, 1, entityType, parentEntityType);

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();

                return result;
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> UpdateAsync(ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await UpdateAsync(this, itemId, wiserItem, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> UpdateAsync(IWiserItemsService wiserItemsService, ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            if (itemId <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.");
            }

            // Check if the user has the correct permissions to do this.
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, wiserItem);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{itemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = itemId,
                        UserId = userId
                    };
                }
            }

            // Don't allow deletion of items via this method.
            if (wiserItem.Removed.HasValue && wiserItem.Removed.Value)
            {
                throw new Exception("It's not possible to change deleted items or to delete items with this method. If you want to delete an existing item, please use the Delete method. If you want to change a previously deleted item, first undelete it via the Delete method.");
            }

            // Get the settings of the entity type.
            var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync(wiserItem.EntityType, wiserItem.ModuleId);
            var tablePrefix = wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);

            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();

            try
            {
                // Set session variables with username and user id. These will be used in triggers for keeping track of the change history.
                databaseConnection.AddParameter("username", username);
                databaseConnection.AddParameter("userId", userId);
                databaseConnection.AddParameter("itemId", itemId);
                databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.

                // The word "update" at the end of the query is to force the GCL to use the write database (for customers that use multiple databases).
                // Otherwise the GCL might throw the exception that the item doesn't exist, if it has just been created and not synchronised to the slave database(s) yet.
                var dataTable = await databaseConnection.GetAsync($"SELECT readonly, entity_type FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE id = ?itemId #UPDATE", true);
                if (dataTable.Rows.Count == 0)
                {
                    throw new Exception($"Item with id '{itemId}' does not exist.");
                }

                if (Convert.ToBoolean(dataTable.Rows[0]["readonly"]))
                {
                    throw new Exception($"Item with id '{itemId}' is set to read only.");
                }

                var entityTypeInDatabase = dataTable.Rows[0].Field<string>("entity_type");

                // Remember the current changed value, because it will always be set to true when setting the Id/EncryptedId/EntityType.
                var originalChangedValue = wiserItem.Changed;

                wiserItem.Id = itemId;
                wiserItem.EncryptedId = itemId.ToString().EncryptWithAesWithSalt(encryptionKey, true);

                // Get entity type of item, we need this later in javascript for executing API work flows.
                if (String.IsNullOrWhiteSpace(wiserItem.EntityType))
                {
                    wiserItem.EntityType = entityTypeInDatabase;
                }

                wiserItem.Changed = originalChangedValue;

                var insertAndUpdateQueryBuilder = new List<string>();
                var updateQueryBuilder = new List<string>(); // For details that are updated via ID.
                var deleteQueryBuilder = new List<string>();

                // Get options from fields. Some fields need to be saved differently based on what options are set.
                var fieldOptions = entityTypeSettings.FieldOptions;

                // Check auto increment fields and save the correct value.
                if (entityTypeSettings.AutoIncrementFields != null && entityTypeSettings.AutoIncrementFields.Any())
                {
                    var fieldCounter = 0;
                    foreach (var (fieldName, languageCode) in entityTypeSettings.AutoIncrementFields)
                    {
                        fieldCounter++;
                        var findAutoIncrementValuesQuery = $@"SELECT IFNULL(MAX(d.`value`), IFNULL(ep.default_value, 0)) AS maximumValue
                                                            FROM {WiserTableNames.WiserEntityProperty} ep
                                                            LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} d ON d.key = ep.property_name AND d.language_code = ep.language_code AND d.item_id <> ?itemId
                                                            WHERE ep.entity_name = i.entity_type 
                                                            AND ep.inputtype = 'auto-increment' 
                                                            AND ep.property_name = ?propertyName{AutoIncrementPropertySuffix}{fieldCounter}
                                                            AND ((?languageCode{AutoIncrementPropertySuffix}{fieldCounter} IS NULL AND ep.language_code IS NULL) OR (?languageCode{AutoIncrementPropertySuffix}{fieldCounter} IS NOT NULL AND ep.language_code IS NOT NULL AND ep.language_code = ?languageCode{AutoIncrementPropertySuffix}{fieldCounter}))
                                                            GROUP BY ep.property_name";
                        databaseConnection.AddParameter($"propertyName{AutoIncrementPropertySuffix}{fieldCounter}", fieldName);
                        databaseConnection.AddParameter($"languageCode{AutoIncrementPropertySuffix}{fieldCounter}", languageCode);
                        dataTable = await databaseConnection.GetAsync(findAutoIncrementValuesQuery, true);

                        var dataRow = dataTable.Rows[0];
                        var propertyName = dataRow.Field<string>("property_name");
                        var previousValue = Convert.ToInt32(dataRow["maximumValue"]);

                        databaseConnection.AddParameter($"groupName{AutoIncrementPropertySuffix}{fieldCounter}", ""); // TODO
                        databaseConnection.AddParameter($"key{AutoIncrementPropertySuffix}{fieldCounter}", propertyName);
                        databaseConnection.AddParameter($"value{AutoIncrementPropertySuffix}{fieldCounter}", previousValue + 1);
                        databaseConnection.AddParameter($"longValue{AutoIncrementPropertySuffix}{fieldCounter}", "");

                        insertAndUpdateQueryBuilder.Add($"(?languageCode{AutoIncrementPropertySuffix}{fieldCounter}, ?itemId, ?groupName{AutoIncrementPropertySuffix}{fieldCounter}, ?key{AutoIncrementPropertySuffix}{fieldCounter}, ?value{AutoIncrementPropertySuffix}{fieldCounter}, ?longValue{AutoIncrementPropertySuffix}{fieldCounter})");

                        var itemDetail = wiserItem.Details.FirstOrDefault(d => d.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                        if (itemDetail == null)
                        {
                            itemDetail = new WiserItemDetailModel
                            {
                                Key = propertyName
                            };

                            wiserItem.Details.Add(itemDetail);
                        }

                        itemDetail.Value = previousValue + 1;
                        itemDetail.Changed = false;
                    }
                }

                if (wiserItem.Changed)
                {
                    var updateQueryParts = new List<string>();

                    // Save the item itself (if needed).
                    if (!String.IsNullOrEmpty(wiserItem.Title))
                    {
                        databaseConnection.AddParameter("title", wiserItem.Title);
                        updateQueryParts.Add("title = ?title");
                    }

                    if (wiserItem.OriginalItemId > 0)
                    {
                        databaseConnection.AddParameter("original_item_id", wiserItem.OriginalItemId);
                        updateQueryParts.Add("original_item_id = ?original_item_id");
                    }

                    if (wiserItem.ParentItemId > 0)
                    {
                        databaseConnection.AddParameter("parent_item_id", wiserItem.ParentItemId);
                        updateQueryParts.Add("parent_item_id = ?parent_item_id");
                    }

                    if (!String.IsNullOrEmpty(wiserItem.UniqueUuid))
                    {
                        databaseConnection.AddParameter("unique_uuid", wiserItem.UniqueUuid);
                        updateQueryParts.Add("unique_uuid = ?unique_uuid");
                    }

                    if (wiserItem.PublishedEnvironment.HasValue)
                    {
                        databaseConnection.AddParameter("published_environment", wiserItem.PublishedEnvironment);
                        updateQueryParts.Add("published_environment = ?published_environment");
                    }

                    if (wiserItem.ReadOnly.HasValue)
                    {
                        databaseConnection.AddParameter("readonly", wiserItem.ReadOnly);
                        updateQueryParts.Add("readonly = ?readonly");
                    }

                    if (!String.IsNullOrEmpty(wiserItem.ChangedBy))
                    {
                        databaseConnection.AddParameter("changed_by", wiserItem.ChangedBy);
                        updateQueryParts.Add("changed_by = ?changed_by");
                    }
                    else
                    {
                        databaseConnection.AddParameter("changed_by", username);
                        updateQueryParts.Add("changed_by = ?changed_by");
                    }

                    // You should never change the entity type of an item with this function, unless the entity type is still empty in the database.
                    if (!String.IsNullOrEmpty(wiserItem.EntityType) && String.IsNullOrEmpty(entityTypeInDatabase))
                    {
                        databaseConnection.AddParameter("entity_type", wiserItem.EntityType);
                        updateQueryParts.Add("entity_type = ?entity_type");
                    }

                    databaseConnection.AddParameter("changed_on", DateTime.Now);
                    updateQueryParts.Add("changed_on = ?changed_on");
                    var query = $@"SET @_username = ?username;
                            SET @_userId = ?userId;
                            SET @saveHistory = ?saveHistoryGcl;
                            UPDATE {tablePrefix}{WiserTableNames.WiserItem} SET {String.Join(",", updateQueryParts)} WHERE id = ?itemId";
                    await databaseConnection.ExecuteAsync(query);

                    // Save SEO value of title, if required.
                    if (!String.IsNullOrEmpty(wiserItem.Title) && entityTypeSettings.SaveTitleAsSeo)
                    {
                        var seoTitle = wiserItem.Title?.ConvertToSeo() ?? "";
                        var useLongValueColumn = seoTitle.Length > 1000;

                        databaseConnection.AddParameter("languageCode_title", "");
                        databaseConnection.AddParameter("groupName_title", "");
                        databaseConnection.AddParameter("key_title", CoreConstants.SeoTitlePropertyName);
                        databaseConnection.AddParameter("value_title", useLongValueColumn ? "" : seoTitle);
                        databaseConnection.AddParameter("longValue_title", !useLongValueColumn ? "" : seoTitle);
                        insertAndUpdateQueryBuilder.Add("(?languageCode_title, ?itemId, ?groupName_title, ?key_title, ?value_title, ?longValue_title)");
                    }

                    wiserItem.Changed = false;
                }

                if ((wiserItem.Details == null || !wiserItem.Details.Any()) && !insertAndUpdateQueryBuilder.Any())
                {
                    if (createNewTransaction) await databaseConnection.CommitTransactionAsync();
                    return wiserItem;
                }

                // Check previous values, so that we can skip fields that haven't changed.
                // This is only for Wiser. If you save items via custom code or the JCL, then the Changed property of ItemModel and ItemDetailModel will be used.
                var previousItemDetails = new List<WiserItemDetailModel>();
                if (!alwaysSaveValues)
                {
                    var previousValuesQuery = $@"SELECT 
                                                d.id,
	                                            d.key,
	                                            d.language_code,
	                                            d.value,
	                                            d.long_value,
                                                d.groupname
                                            FROM {tablePrefix}{WiserTableNames.WiserItem} i
                                            JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} d ON d.item_id = i.id
                                            WHERE i.id = ?itemId";

                    dataTable = await databaseConnection.GetAsync(previousValuesQuery, true);
                    if (dataTable.Rows.Count > 0)
                    {
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            var field = new WiserItemDetailModel
                            {
                                Id = dataRow.Field<ulong>("id"),
                                Key = dataRow.Field<string>("key"),
                                LanguageCode = dataRow.Field<string>("language_code"),
                                Value = dataRow.Field<string>("long_value"),
                                GroupName = dataRow.Field<string>("groupname")
                            };

                            if (field.Value == null || field.Value.ToString() == "")
                            {
                                field.Value = dataRow.Field<string>("value");
                            }

                            previousItemDetails.Add(field);
                        }
                    }

                    var previousValuesQueryBuilder = new List<string>();
                    var allLinkTypeSettings = await GetAllLinkTypeSettingsAsync();
                    var linksWithDedicatedTables = allLinkTypeSettings.Where(x => x.UseDedicatedTable && String.Equals(x.SourceEntityType, wiserItem.EntityType, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (!linksWithDedicatedTables.Any())
                    {
                        previousValuesQueryBuilder.Add($@"SELECT 
    detail.id,
	detail.key,
	detail.language_code,
	detail.value,
	detail.long_value,
    detail.itemlink_id,
    detail.groupname
FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
JOIN {WiserTableNames.WiserItemLink} AS link ON link.item_id = item.id
JOIN {WiserTableNames.WiserItemLinkDetail} AS detail ON detail.itemlink_id = link.id
WHERE item.id = ?itemId");
                    }
                    else
                    {
                        foreach (var linkTypeSettings in linksWithDedicatedTables)
                        {
                            var linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
                            previousValuesQueryBuilder.Add($@"SELECT 
    detail.id,
	detail.key,
	detail.language_code,
	detail.value,
	detail.long_value,
    detail.itemlink_id,
    detail.groupname
FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.item_id = item.id
JOIN {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} AS detail ON detail.itemlink_id = link.id
WHERE item.id = ?itemId");
                        }
                    }

                    var previousValuesDataTable = await databaseConnection.GetAsync(String.Join(" UNION ", previousValuesQueryBuilder), true);
                    if (previousValuesDataTable.Rows.Count > 0)
                    {
                        foreach (DataRow dataRow in previousValuesDataTable.Rows)
                        {
                            var field = new WiserItemDetailModel
                            {
                                Id = dataRow.Field<ulong>("id"),
                                Key = dataRow.Field<string>("key"),
                                LanguageCode = dataRow.Field<string>("language_code"),
                                Value = dataRow.Field<string>("long_value"),
                                IsLinkProperty = true,
                                ItemLinkId = dataRow.Field<ulong>("itemlink_id"),
                                GroupName = dataRow.Field<string>("groupname")
                            };

                            if (field.Value == null || field.Value.ToString() == "")
                            {
                                field.Value = dataRow.Field<string>("value");
                            }

                            previousItemDetails.Add(field);
                        }
                    }
                }

                // Save the item details / fields for item details.
                // If the property ReadOnly of ItemDetail is set to true, always skip it. It means it has been set to true in back-end code.
                var counter = 0;
                foreach (var itemDetail in wiserItem.Details.Where(d => !d.IsLinkProperty && d.Changed && !d.ReadOnly))
                {
                    counter++;
                    var key = $"{itemDetail.Key}_{itemDetail.LanguageCode}";

                    // If the current item detail ends with "_input", it means it's a text field that corresponds with a dropdown/combobox
                    // and we don't want to save the value if the dropdown/combobox does not have a value, to prevent saving the placeholder or optionLabel text.
                    if (itemDetail.Key.EndsWith("_input", StringComparison.OrdinalIgnoreCase))
                    {
                        var dropDownItem = wiserItem.Details.FirstOrDefault(d => d.LanguageCode == itemDetail.LanguageCode && d.Key == itemDetail.Key.ReplaceCaseInsensitive("_input", ""));
                        if (String.IsNullOrEmpty(dropDownItem?.Value?.ToString()))
                        {
                            // Don't save place holder texts.
                            itemDetail.Value = "";
                        }
                    }

                    // Skip fields that are readonly, if they already contain a value. Still allow an initial value to be saved.
                    if (fieldOptions != null && fieldOptions.ContainsKey(key) && fieldOptions[key].ContainsKey(ReadOnlyKey) && (bool)fieldOptions[key][ReadOnlyKey])
                    {
                        var previousField = previousItemDetails.FirstOrDefault(x =>
                            x.IsLinkProperty == itemDetail.IsLinkProperty &&
                            x.ItemLinkId == itemDetail.ItemLinkId &&
                            x.Key.Equals(itemDetail.Key, StringComparison.OrdinalIgnoreCase) &&
                            (x.LanguageCode ?? "").Equals(itemDetail.LanguageCode ?? "", StringComparison.OrdinalIgnoreCase));

                        if (!String.IsNullOrEmpty(previousField?.Value?.ToString()))
                        {
                            continue;
                        }
                    }

                    // Some fields need to be saved differently than normal, so check for that first.
                    if (fieldOptions != null && fieldOptions.ContainsKey(key) && fieldOptions[key].ContainsKey(FieldTypeKey))
                    {
                        switch (fieldOptions[key][FieldTypeKey]?.ToString()?.ToLowerInvariant())
                        {
                            case "combobox":
                            case "multiselect":
                                if (!fieldOptions[key].ContainsKey(SaveValueAsItemLinkKey) || !(bool)fieldOptions[key][SaveValueAsItemLinkKey])
                                {
                                    // If the option 'saveValueAsItemLinkKey' doesn't exist or is false, we have to save this field the usual way (in wiser_itemdetail).
                                    break;
                                }

                                var linkTypeNumber = !fieldOptions[key].ContainsKey(LinkTypeNumberKey) ? 0 : Convert.ToInt32(fieldOptions[key][LinkTypeNumberKey]);
                                if (linkTypeNumber <= 0)
                                {
                                    // If we have no link type number, we can't save the item link, so save this field the usual way (in wiser_itemdetail).
                                    break;
                                }

                                // Collect the new destination IDs from the itemDetail.
                                var destinationIds = new List<int>();
                                if (itemDetail.Value is JArray valueAsJsonArray)
                                {
                                    destinationIds = valueAsJsonArray.Cast<object>().Select(Convert.ToInt32).ToList();
                                }
                                else if (Int32.TryParse(itemDetail.Value?.ToString(), out var integerValue))
                                {
                                    destinationIds.Add(integerValue);
                                }

                                var linkTablePrefix = await GetTablePrefixForLinkAsync(linkTypeNumber, wiserItem.EntityType);

                                databaseConnection.AddParameter("linkTypeNumber", linkTypeNumber);
                                var currentItemIsDestinationId = fieldOptions[key].ContainsKey(CurrentItemIsDestinationIdKey) && (bool)fieldOptions[key][CurrentItemIsDestinationIdKey];

                                // Delete any previous links that were added via this field.
                                // NOTE: Here we make an assumption that the given linkTypeNumber is not used for anything else!
                                // NOTE: We can't do this without making that assumption, since we don't know what the old values were.
                                var wherePart = "";
                                if (destinationIds.Any())
                                {
                                    wherePart = $"AND {(!currentItemIsDestinationId ? "destination_item_id" : "item_id")} NOT IN ({String.Join(",", destinationIds)})";
                                }

                                var query = $"DELETE FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE {(currentItemIsDestinationId ? "destination_item_id" : "item_id")} = ?itemId AND type = ?linkTypeNumber {wherePart}";
                                await databaseConnection.ExecuteAsync(query);

                                // Save the new values as item links.
                                foreach (var destinationId in destinationIds)
                                {
                                    databaseConnection.AddParameter("destinationId", destinationId);
                                    await databaseConnection.ExecuteAsync($@"INSERT IGNORE INTO {linkTablePrefix}{WiserTableNames.WiserItemLink} ({(currentItemIsDestinationId ? "destination_item_id, item_id" : "item_id, destination_item_id")}, type)
                                                                        VALUES (?itemId, ?destinationId, ?linkTypeNumber);");
                                }

                                // Continue the foreach, so that we don't save the field normally in wiser_itemdetail.
                                itemDetail.Changed = false;
                                continue;
                        }
                    }

                    databaseConnection.AddParameter($"languageCode{counter}", itemDetail.LanguageCode ?? "");
                    databaseConnection.AddParameter($"groupName{counter}", itemDetail.GroupName ?? "");
                    databaseConnection.AddParameter($"key{counter}", itemDetail.Key);

                    var (_, valueChanged, deleteValue, alsoSaveSeoValue) = await AddValueParameterToConnectionAsync(counter, itemDetail, fieldOptions, previousItemDetails, encryptionKey, alwaysSaveValues);
                    if (!valueChanged && !alwaysSaveValues)
                    {
                        continue;
                    }

                    if (deleteValue)
                    {
                        if (itemDetail.Id > 0)
                        {
                            databaseConnection.AddParameter($"itemId{counter}", itemDetail.Id);
                            deleteQueryBuilder.Add($"id = ?itemId{counter}");
                        }
                        else
                        {
                            deleteQueryBuilder.Add($"(`key` = ?key{counter} AND language_code = ?languageCode{counter})");
                        }
                    }
                    else if (itemDetail.Id > 0)
                    {
                        databaseConnection.AddParameter($"itemId{counter}", itemDetail.Id);
                        updateQueryBuilder.Add($"UPDATE {tablePrefix}{WiserTableNames.WiserItemDetail} SET `key` = ?key{counter}, `value` = ?value{counter}, `long_value` = ?longValue{counter}, `groupname` = ?groupName{counter}, language_code = ?languageCode{counter} WHERE id = ?itemId{counter};");
                    }
                    else
                    {
                        insertAndUpdateQueryBuilder.Add($"(?languageCode{counter}, ?itemId, ?groupName{counter}, ?key{counter}, ?value{counter}, ?longValue{counter})");
                    }

                    if (alsoSaveSeoValue)
                    {
                        databaseConnection.AddParameter($"key{SeoPropertySuffix}{counter}", itemDetail.Key + SeoPropertySuffix);
                        if (deleteValue)
                        {
                            deleteQueryBuilder.Add($"(`key` = ?key{SeoPropertySuffix}{counter} AND language_code = ?languageCode{counter})");
                        }
                        else
                        {
                            insertAndUpdateQueryBuilder.Add($"(?languageCode{counter}, ?itemId, ?groupName{counter}, ?key{SeoPropertySuffix}{counter}, ?value{SeoPropertySuffix}{counter}, ?longValue{SeoPropertySuffix}{counter})");
                        }
                    }

                    itemDetail.Changed = false;
                }

                if (deleteQueryBuilder.Any())
                {
                    var query = $@"SET @_username = ?username;
                            SET @_userId = ?userId;
                            SET @saveHistory = ?saveHistoryGcl;
                            DELETE FROM {tablePrefix}{WiserTableNames.WiserItemDetail} WHERE item_id = ?itemId AND ({String.Join(" OR ", deleteQueryBuilder)});";
                    await databaseConnection.ExecuteAsync(query);
                    deleteQueryBuilder.Clear();
                }

                if (insertAndUpdateQueryBuilder.Any())
                {
                    var query = $@"SET @_username = ?username;
                            SET @_userId = ?userId;
                            SET @saveHistory = ?saveHistoryGcl;
                            INSERT INTO {tablePrefix}{WiserTableNames.WiserItemDetail} (`language_code`, `item_id`, `groupname`, `key`, `value`, `long_value`)
                            VALUES {String.Join(", ", insertAndUpdateQueryBuilder)}
                            ON DUPLICATE KEY UPDATE `value` = VALUES(`value`), `long_value` = VALUES(`long_value`), `groupname` = VALUES(`groupname`);";
                    await databaseConnection.ExecuteAsync(query);
                    insertAndUpdateQueryBuilder.Clear();
                }

                if (updateQueryBuilder.Any())
                {
                    var query = $@"SET @_username = ?username;
                            SET @_userId = ?userId;
                            SET @saveHistory = ?saveHistoryGcl;
                            {String.Join(Environment.NewLine, updateQueryBuilder)}";
                    await databaseConnection.ExecuteAsync(query);
                    updateQueryBuilder.Clear();
                }

                // Save the item details / fields for link item details.
                databaseConnection.AddParameter("itemId", itemId);
                databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
                counter = 0;

                // If the property ReadOnly of ItemDetail is set to true, always skip it. It means it has been set to true in back-end code.
                foreach (var linkTypeGroup in wiserItem.Details.Where(d => d.IsLinkProperty && !d.ReadOnly && d.Changed).GroupBy(x => x.LinkType))
                {
                    var linkTablePrefix = await GetTablePrefixForLinkAsync(linkTypeGroup.Key, wiserItem.EntityType);

                    foreach (var itemDetail in linkTypeGroup)
                    {
                        counter++;
                        var key = $"{itemDetail.Key}_{itemDetail.LanguageCode}";

                        // Skip fields that are readonly, if they already contain a value. Still allow an initial value to be saved.
                        if (fieldOptions != null && fieldOptions.ContainsKey(key) && fieldOptions[key].ContainsKey(ReadOnlyKey) && (bool) fieldOptions[key][ReadOnlyKey])
                        {
                            var previousField = previousItemDetails.FirstOrDefault(x =>
                                                                                       x.IsLinkProperty == itemDetail.IsLinkProperty &&
                                                                                       x.ItemLinkId == itemDetail.ItemLinkId &&
                                                                                       x.Key.Equals(itemDetail.Key, StringComparison.OrdinalIgnoreCase) &&
                                                                                       (x.LanguageCode ?? "").Equals(itemDetail.LanguageCode ?? "", StringComparison.OrdinalIgnoreCase));

                            if (!String.IsNullOrEmpty(previousField?.Value?.ToString()))
                            {
                                continue;
                            }
                        }

                        // If this is the ordering field, update the ordering column of the table 'wiser_itemlink'.
                        if (LinkOrderingFieldName.Equals(itemDetail.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (itemDetail.ItemLinkId > 0)
                            {
                                databaseConnection.AddParameter($"itemLinkId{counter}", itemDetail.ItemLinkId);
                                databaseConnection.AddParameter($"ordering{counter}", itemDetail.Value);

                                var updateOrderingQuery = $@"UPDATE {linkTablePrefix}{WiserTableNames.WiserItemLink} SET ordering = ?ordering{counter} WHERE id = ?itemLinkId{counter}";
                                await databaseConnection.ExecuteAsync(updateOrderingQuery);
                            }
                            else
                            {
                                databaseConnection.AddParameter($"ordering{counter}", itemDetail.Value);

                                var updateOrderingQuery = $@"UPDATE {WiserTableNames.WiserItem} SET ordering = ?ordering{counter} WHERE id = ?itemId";
                                await databaseConnection.ExecuteAsync(updateOrderingQuery);
                            }

                            continue;
                        }

                        databaseConnection.AddParameter($"languageCode{counter}", itemDetail.LanguageCode ?? "");
                        databaseConnection.AddParameter($"itemLinkId{counter}", itemDetail.ItemLinkId);
                        databaseConnection.AddParameter($"groupName{counter}", itemDetail.GroupName ?? "");
                        databaseConnection.AddParameter($"key{counter}", itemDetail.Key);

                        var (_, valueChanged, deleteValue, alsoSaveSeoValue) = await AddValueParameterToConnectionAsync(counter, itemDetail, fieldOptions, previousItemDetails, encryptionKey, alwaysSaveValues);
                        if (!valueChanged && !alwaysSaveValues)
                        {
                            continue;
                        }

                        if (deleteValue)
                        {
                            deleteQueryBuilder.Add($"(itemlink_id = ?itemLinkId{counter} AND `key` = ?key{counter} AND language_code = ?languageCode{counter})");
                        }
                        else
                        {
                            insertAndUpdateQueryBuilder.Add($"(?languageCode{counter}, ?itemLinkId{counter}, ?groupName{counter}, ?key{counter}, ?value{counter}, ?longValue{counter})");
                        }

                        if (alsoSaveSeoValue)
                        {
                            databaseConnection.AddParameter($"key{SeoPropertySuffix}{counter}", itemDetail.Key + SeoPropertySuffix);
                            if (deleteValue)
                            {
                                deleteQueryBuilder.Add($"(itemlink_id = ?itemLinkId{counter} AND `key` = ?key{SeoPropertySuffix}{counter} AND language_code = ?languageCode{counter})");
                            }
                            else
                            {
                                insertAndUpdateQueryBuilder.Add($"(?languageCode{counter}, ?itemLinkId{counter}, ?groupName{counter}, ?key{SeoPropertySuffix}{counter}, ?value{SeoPropertySuffix}{counter}, ?longValue{SeoPropertySuffix}{counter})");
                            }
                        }

                        itemDetail.Changed = false;
                    }

                    if (deleteQueryBuilder.Any())
                    {
                        var query = $@"SET @_username = ?username;
                                    SET @_userId = ?userId;
                                    SET @saveHistory = ?saveHistoryGcl;
                                    DELETE FROM {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} WHERE {String.Join(" OR ", deleteQueryBuilder)}";
                        await databaseConnection.ExecuteAsync(query);
                        deleteQueryBuilder.Clear();
                    }

                    if (insertAndUpdateQueryBuilder.Any())
                    {
                        var query = $@"SET @_username = ?username;
                                        SET @_userId = ?userId;
                                        SET @saveHistory = ?saveHistoryGcl;
                                        INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} (`language_code`, `itemlink_id`, `groupname`, `key`, `value`, `long_value`)
                                        VALUES {String.Join(", ", insertAndUpdateQueryBuilder)}
                                        ON DUPLICATE KEY UPDATE `value` = VALUES(`value`), `long_value` = VALUES(`long_value`), `groupName` = VALUES(`groupName`)";
                        await databaseConnection.ExecuteAsync(query);
                        insertAndUpdateQueryBuilder.Clear();
                    }
                }

                // Add or update item in aggregation table(s) when needed.
                await wiserItemsService.HandleItemAggregationAsync(wiserItem, encryptionKey);

                // Execute the after update query, if one is entered.
                await ExecuteWorkflowAsync(itemId, false, entityTypeSettings, wiserItem, userId, username);

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();

                return wiserItem;
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> ChangeEntityTypeAsync(ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false)
        {
            return await ChangeEntityTypeAsync(this, itemId, currentEntityType, newEntityType, username, userId, saveHistory, skipPermissionsCheck, resetAddedOnDate);
        }

        /// <inheritdoc />
        public async Task<int> ChangeEntityTypeAsync(IWiserItemsService wiserItemsService, ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Delete, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to change entity type of item '{itemId}'.")
                    {
                        Action = EntityActions.Delete,
                        ItemId = itemId,
                        UserId = userId
                    };
                }
            }

            var oldEntityTypeTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(currentEntityType);
            var newEntityTypeTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(newEntityType);

            if (!String.Equals(oldEntityTypeTablePrefix, newEntityTypeTablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"The new entity type has a different table prefix ('{newEntityTypeTablePrefix}') than the current one ('{oldEntityTypeTablePrefix}'). This means we would need to move the item to a different database table and this method does not support that (yet).");
            }

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("entityType", newEntityType);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            databaseConnection.AddParameter("now", DateTime.Now);

            var addedOnResetPart = !resetAddedOnDate ? "" : ", added_on = ?now, added_by = ?username";

            var query = $@"SET @_username = ?username;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryGcl; 
                        UPDATE {newEntityTypeTablePrefix}{WiserTableNames.WiserItem} SET entity_type = ?entityType, changed_by = ?username{addedOnResetPart} WHERE id = ?itemId LIMIT 1;";
            return await databaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DeleteAsync(this, new List<ulong> { itemId }, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IWiserItemsService wiserItemsService, ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DeleteAsync(wiserItemsService, new List<ulong> { itemId }, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DeleteAsync(this, itemIds, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IWiserItemsService wiserItemsService, List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            var filteredItemIds = itemIds.Where(id => id > 0).ToList();
            if (!filteredItemIds.Any())
            {
                return 0;
            }

            if (!skipPermissionsCheck)
            {
                var itemsWithNoPermissionToDelete = new List<ulong>();

                foreach (var itemId in filteredItemIds)
                {
                    var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Delete, userId, entityType: entityType);
                    if (!isPossible.ok)
                    {
                        itemsWithNoPermissionToDelete.Add(itemId);
                    }
                }

                if (itemsWithNoPermissionToDelete.Any())
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to delete items '{String.Join(", ", itemsWithNoPermissionToDelete)}'.")
                    {
                        Action = EntityActions.Delete,
                        ItemId = itemsWithNoPermissionToDelete.First(),
                        UserId = userId
                    };
                }
            }

            var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync(entityType);
            var tablePrefix = wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);

            if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Disallow)
            {
                throw new InvalidOperationException($"Items of type '{entityType}' can not be deleted.");
            }

            if (undelete && entityTypeSettings.DeleteAction == EntityDeletionTypes.Permanent)
            {
                throw new InvalidOperationException($"Items of type '{entityType}' are always permanently deleted and therefor cannot be undeleted.");
            }

            if (!entityTypeSettings.SaveHistory)
            {
                saveHistory = false;
            }

            var result = 0;
            try
            {
                if (createNewTransaction) await databaseConnection.BeginTransactionAsync();
                var allLinkTypeSettings = await GetAllLinkTypeSettingsAsync();

                var formattedItemIds = String.Join(",", filteredItemIds);

                databaseConnection.AddParameter("username", username);
                databaseConnection.AddParameter("userId", userId);
                databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
                databaseConnection.AddParameter("now", DateTime.Now); // Don't use MySQL time, because DigitalOcean uses a different timezone than us.

                string query;
                if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Hide)
                {
                    query = $"UPDATE {tablePrefix}{WiserTableNames.WiserItem} SET published_environment = {(undelete ? 15 : 0)} WHERE id IN ({formattedItemIds})";
                    result = await databaseConnection.ExecuteAsync(query);
                }
                else
                {
                    var linkTypeSettingsWithDedicatedTablesForSource = allLinkTypeSettings.Where(x => x.UseDedicatedTable && String.Equals(x.SourceEntityType, entityType, StringComparison.OrdinalIgnoreCase)).ToList();
                    var linkTypeSettingsWithDedicatedTablesForDestination = allLinkTypeSettings.Where(x => x.UseDedicatedTable && String.Equals(x.DestinationEntityType, entityType, StringComparison.OrdinalIgnoreCase)).ToList();

                    /*
                     * NOTE: In all queries below we have hard-coded all columns. This is on purpose and should stay this way.
                     * It's the only way we can be 100% sure that we're inserting the correct data into the correct columns.
                     * Otherwise, if someone manually created an archive table and adds the columns in a different order than the original table,
                     * we could end up inserting data in the wrong columns (if we would have used SELECT *).
                     */

                    if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                    {
                        // Copy the item itself to the archive (or vice versa, when undeleting).
                        query = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItem}{(undelete ? "" : WiserTableNames.ArchiveSuffix)} 
(
    id, 
    original_item_id, 
    parent_item_id, 
    unique_uuid, 
    entity_type, 
    moduleid, 
    published_environment, 
    readonly, 
    title, 
    added_on, 
    added_by, 
    changed_on, 
    changed_by
)
SELECT
    id, 
    original_item_id, 
    parent_item_id, 
    unique_uuid, 
    entity_type, 
    moduleid, 
    published_environment, 
    readonly, 
    title, 
    added_on, 
    added_by, 
    ?now AS changed_on, 
    ?username AS changed_by
FROM {tablePrefix}{WiserTableNames.WiserItem}{(undelete ? WiserTableNames.ArchiveSuffix : "")}
WHERE id IN({formattedItemIds})";

                        await databaseConnection.ExecuteAsync(query);

                        // Copy the item details to the archive (or vice versa, when undeleting).
                        query = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItemDetail}{(undelete ? "" : WiserTableNames.ArchiveSuffix)}
(
    id,
    language_code,
    item_id,
    groupname,
    `key`,
    value,
    long_value
)
SELECT
    id,
    language_code,
    item_id,
    groupname,
    `key`,
    value,
    long_value
FROM {tablePrefix}{WiserTableNames.WiserItemDetail}{(undelete ? WiserTableNames.ArchiveSuffix : "")}
WHERE item_id IN({formattedItemIds})";
                        await databaseConnection.ExecuteAsync(query);
                    }

                    // Copy the item files to the archive (or vice versa, when undeleting).
                    if (await databaseHelpersService.TableExistsAsync($"{tablePrefix}{WiserTableNames.WiserItemFile}"))
                    {
                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            query = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItemFile}{(undelete ? "" : WiserTableNames.ArchiveSuffix)}
(
    id,
    item_id,
    content_type,
    content,
    content_url,
    width,
    height,
    file_name,
    extension,
    added_on,
    added_by,
    title,
    property_name,
    itemlink_id,
    protected,
    ordering
)
SELECT
    id,
    item_id,
    content_type,
    content,
    content_url,
    width,
    height,
    file_name,
    extension,
    added_on,
    added_by,
    title,
    property_name,
    itemlink_id,
    protected,
    ordering
FROM {tablePrefix}{WiserTableNames.WiserItemFile}{(undelete ? WiserTableNames.ArchiveSuffix : "")}
WHERE item_id IN({formattedItemIds});";
                            await databaseConnection.ExecuteAsync(query);
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync($"DELETE FROM {tablePrefix}{WiserTableNames.WiserItemFile}{(undelete ? WiserTableNames.ArchiveSuffix : "")} WHERE item_id IN({formattedItemIds});");
                        }
                    }

                    var copyItemLinkFilesQuery = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT INTO {tablePrefix}{WiserTableNames.WiserItemFile}{(undelete ? "" : WiserTableNames.ArchiveSuffix)}
(
    id,
    item_id,
    content_type,
    content,
    content_url,
    width,
    height,
    file_name,
    extension,
    added_on,
    added_by,
    title,
    property_name,
    itemlink_id,
    protected,
    ordering
)
SELECT
    file.id,
    file.item_id,
    file.content_type,
    file.content,
    file.content_url,
    file.width,
    file.height,
    file.file_name,
    file.extension,
    file.added_on,
    file.added_by,
    file.title,
    file.property_name,
    file.itemlink_id,
    file.protected,
    file.ordering
FROM {{0}}{WiserTableNames.WiserItemFile}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS file
JOIN {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS link ON link.id = file.itemlink_id AND {{1}}";

                    var deleteItemLinkFilesQuery = $@"DELETE file.* FROM {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS link 
JOIN {{0}}{WiserTableNames.WiserItemFile}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS file ON file.itemlink_id = link.id 
WHERE {{1}};";

                    var copyItemLinksQuery = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT IGNORE INTO {{0}}{WiserTableNames.WiserItemLink}{(undelete ? "" : WiserTableNames.ArchiveSuffix)}
(
    id,
    item_id,
    destination_item_id,
    ordering,
    type,
    added_on
)
SELECT
    id,
    item_id,
    destination_item_id,
    ordering,
    type,
    added_on
FROM {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")}
WHERE {{1}}";

                    var copyItemLinkDetailsQuery = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
INSERT INTO {{0}}{WiserTableNames.WiserItemLinkDetail}{(undelete ? "" : WiserTableNames.ArchiveSuffix)}
(
    id,
    language_code,
    itemlink_id,
    groupname,
    `key`,
    value,
    long_value
)
SELECT
    detail.id,
    detail.language_code,
    detail.itemlink_id,
    detail.groupname,
    detail.`key`,
    detail.value,
    detail.long_value
FROM {{0}}{WiserTableNames.WiserItemLinkDetail}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS detail
JOIN {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS link ON link.id = detail.itemlink_id AND {{1}}";

                    var deleteItemLinksQuery = $@"SET @saveHistory = FALSE; # Don't save the history when deleting the item details, otherwise we will get UPDATE_ITEM lines in the history and that will cause problems for branches.
DELETE detail.* FROM {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS link 
JOIN {{0}}{WiserTableNames.WiserItemLinkDetail}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS detail ON detail.itemlink_id = link.id 
WHERE {{1}};
SET @saveHistory = ?saveHistoryGcl;
DELETE FROM {{0}}{WiserTableNames.WiserItemLink}{(undelete ? WiserTableNames.ArchiveSuffix : "")} AS link WHERE {{1}};";

                    // If there are not dedicated link tables for this entity type, then copy from the base table.
                    if (!linkTypeSettingsWithDedicatedTablesForSource.Any() && !linkTypeSettingsWithDedicatedTablesForDestination.Any())
                    {
                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkFilesQuery, "", $"(link.item_id IN({formattedItemIds}) OR link.destination_item_id IN({formattedItemIds}))"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinkFilesQuery, "", $"(link.item_id IN({formattedItemIds}) OR link.destination_item_id IN({formattedItemIds}))"));
                        }

                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinksQuery, "", $"(item_id IN({formattedItemIds}) OR destination_item_id IN({formattedItemIds}))"));
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkDetailsQuery, "", $"(link.item_id IN({formattedItemIds}) OR link.destination_item_id IN({formattedItemIds}))"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinksQuery, "", $"(link.item_id IN({formattedItemIds}) OR link.destination_item_id IN({formattedItemIds}))"));
                        }
                    }

                    // Copy from dedicated link table, where the current entity type is the source. 
                    foreach (var linkSettings in linkTypeSettingsWithDedicatedTablesForSource)
                    {
                        var tablePrefixForLink = wiserItemsService.GetTablePrefixForLink(linkSettings);
                        if (!await databaseHelpersService.TableExistsAsync($"{tablePrefixForLink}{WiserTableNames.WiserItemFile}"))
                        {
                            continue;
                        }

                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkFilesQuery, tablePrefixForLink, $"link.item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinkFilesQuery, tablePrefixForLink, $"link.item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinksQuery, tablePrefixForLink, $"item_id IN({formattedItemIds})"));
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkDetailsQuery, tablePrefixForLink, $"link.item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinksQuery, tablePrefixForLink, $"link.item_id IN({formattedItemIds})"));
                        }
                    }

                    // Copy from dedicated link table, where the current entity type is the destination.
                    foreach (var linkSettings in linkTypeSettingsWithDedicatedTablesForDestination)
                    {
                        var tablePrefixForLink = wiserItemsService.GetTablePrefixForLink(linkSettings);
                        if (!await databaseHelpersService.TableExistsAsync($"{tablePrefixForLink}{WiserTableNames.WiserItemFile}"))
                        {
                            continue;
                        }

                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkFilesQuery, tablePrefixForLink, $"link.destination_item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinkFilesQuery, tablePrefixForLink, $"link.destination_item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction == EntityDeletionTypes.Archive)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinksQuery, tablePrefixForLink, $"destination_item_id IN({formattedItemIds})"));
                            await databaseConnection.ExecuteAsync(String.Format(copyItemLinkDetailsQuery, tablePrefixForLink, $"link.destination_item_id IN({formattedItemIds})"));
                        }

                        if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                        {
                            await databaseConnection.ExecuteAsync(String.Format(deleteItemLinksQuery, tablePrefixForLink, $"link.destination_item_id IN({formattedItemIds})"));
                        }
                    }

                    // And then delete the item from the original table (or vice versa, when undeleting).
                    if (entityTypeSettings.DeleteAction is EntityDeletionTypes.Archive or EntityDeletionTypes.Permanent)
                    {
                        query = $@"SET @_username = ?username;
SET @_userId = ?userId;
SET @saveHistory = ?saveHistoryGcl;
DELETE FROM {tablePrefix}{WiserTableNames.WiserItem}{(undelete ? WiserTableNames.ArchiveSuffix : "")} WHERE id IN({formattedItemIds});
SET @saveHistory = FALSE; # Don't save the history when deleting the item details, otherwise we will get UPDATE_ITEM lines in the history and that will cause problems for branches.
DELETE FROM {tablePrefix}{WiserTableNames.WiserItemDetail}{(undelete ? WiserTableNames.ArchiveSuffix : "")} WHERE item_id IN({formattedItemIds});
SET @saveHistory = ?saveHistoryGcl;";
                        if (undelete)
                        {
                            databaseConnection.AddParameter("entityType", entityType);
                            query += $@" INSERT INTO {WiserTableNames.WiserHistory} (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
VALUES ('UNDELETE_ITEM', 'wiser_item', ?itemId, IFNULL(@_username, USER()), ?entityType, '', '');";
                        }

                        result = await databaseConnection.ExecuteAsync(query);
                    }
                }

                if (!String.IsNullOrWhiteSpace(entityType))
                {
                    // Now (un)delete the item from the aggregation table, if applicable.
                    var aggregationSettings = await GetAggregationSettingsAsync(entityType);
                    if (aggregationSettings != null && aggregationSettings.Any())
                    {
                        if (undelete)
                        {
                            foreach (var itemId in itemIds)
                            {
                                var item = await wiserItemsService.GetItemDetailsAsync(itemId, userId: userId, entityType: entityType, skipPermissionsCheck: skipPermissionsCheck, returnNullIfDeleted: false);
                                await wiserItemsService.HandleItemAggregationAsync(item);
                            }
                        }
                        else
                        {
                            await databaseConnection.ExecuteAsync($"DELETE FROM `{aggregationSettings.First().TableName}` WHERE id IN ({formattedItemIds})");
                        }
                    }
                
                    // Also delete children of this item, if applicable.
                    foreach (var linkSettings in allLinkTypeSettings.Where(l => l.CascadeDelete && String.Equals(l.DestinationEntityType, entityType)))
                    {
                        var linkTablePrefix = GetTablePrefixForLink(linkSettings);
                        var archiveSuffix = undelete ? WiserTableNames.ArchiveSuffix : "";
                        query = $@"SELECT item_id FROM {linkTablePrefix}{WiserTableNames.WiserItemLink}{archiveSuffix} WHERE destination_item_id IN ({formattedItemIds})";
                        var dataTable = await databaseConnection.GetAsync(query);
                        var children = dataTable.Rows.Cast<DataRow>().Select(dataRow => Convert.ToUInt64(dataRow["item_id"])).ToList();
                        await DeleteAsync(wiserItemsService, children, undelete, username, userId, saveHistory, linkSettings.SourceEntityType, false, skipPermissionsCheck);
                    }
                }

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();

                return result;
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteWorkflowAsync(ulong itemId, bool isNewItem, EntitySettingsModel entitySettingsModel, WiserItemModel wiserItem = null, ulong userId = 0, string username = "GCL", bool saveHistory = true)
        {
            if (itemId <= 0)
            {
                throw new ArgumentException("Id must be greater than zero.");
            }
            if (entitySettingsModel == null)
            {
                throw new ArgumentNullException(nameof(entitySettingsModel));
            }

            var workFlowQuery = isNewItem ? entitySettingsModel.QueryAfterInsert : entitySettingsModel.QueryAfterUpdate;
            if (String.IsNullOrWhiteSpace(workFlowQuery))
            {
                return false;
            }

            // Create list of replacements to make it a bit easier to add new replacements.
            // When adding an item, make sure the argument of the constructor of the dictionary reflects the amount of entries!
            var replacements = new Dictionary<string, string>(6)
            {
                { "id", itemId.ToString() },
                { "itemId", itemId.ToString() },
                { "title", "?workFlowItemTitle" },
                { "moduleId", wiserItem?.ModuleId.ToString() },
                { "userId", userId.ToString() },
                { "username", "?username" }
            };

            foreach (var replacement in replacements)
            {
                // Replace the version with quotes around it and the version without quotes around it.
                workFlowQuery = workFlowQuery
                    .ReplaceCaseInsensitive($"'{{{replacement.Key}}}'", replacement.Value)
                    .ReplaceCaseInsensitive($"\"{{{replacement.Key}}}\"", replacement.Value)
                    .ReplaceCaseInsensitive($"{{{replacement.Key}}}", replacement.Value);
            }

            if (wiserItem?.Details != null)
            {
                var dictionary = wiserItem.Details.ToDictionary(d => d.Key, d => d.Value ?? "");
                workFlowQuery = stringReplacementsService.DoReplacements(workFlowQuery, dictionary, forQuery: true);
            }

            databaseConnection.AddParameter("workFlowItemTitle", wiserItem?.Title ?? "");
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            await databaseConnection.ExecuteAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl;
                                                        {workFlowQuery}");

            return true;
        }

        /// <inheritdoc />
        public async Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null)
        {
            return await CheckIfEntityActionIsPossibleAsync(this, itemId, action, userId, wiserItem, onlyCheckAccessRights, entityType);
        }

        /// <inheritdoc />
        public async Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(IWiserItemsService wiserItemsService, ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null)
        {
            // First check the actual permissions of the item.
            var permissions = await wiserItemsService.GetUserItemPermissionsAsync(itemId, userId, entityType);
            if (permissions == AccessRights.Nothing)
            {
                // If the user has no permissions at all, we can stop and return an error.
                return (false, "U heeft geen rechten om deze actie uit te voeren.", permissions);
            }
            
            // Check if the item itself is set to read only.
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(String.IsNullOrWhiteSpace(entityType) ? wiserItem?.EntityType : entityType);
            databaseConnection.AddParameter("itemId", itemId);

            var query = $@"SELECT readonly
                        FROM {tablePrefix}{WiserTableNames.WiserItem}
                        WHERE id = ?itemId";

            var queryResult = await databaseConnection.GetAsync(query, true);
            var readOnly = queryResult.Rows.Count > 0 && Convert.ToBoolean(queryResult.Rows[0]["readonly"]);

            if (readOnly)
            {
                permissions = AccessRights.Read;        

                if (action != EntityActions.Read)
                {
                    return (false, "Dit item staat ingesteld als alleen lezen.", permissions);
                }
            }

            // Check if the user has the correct permissions.
            var hasPermission = action switch
            {
                EntityActions.Delete => (permissions & AccessRights.Delete) == AccessRights.Delete,
                EntityActions.Update => (permissions & AccessRights.Update) == AccessRights.Update,
                EntityActions.Read => (permissions & AccessRights.Read) == AccessRights.Read,
                EntityActions.Create => (permissions & AccessRights.Create) == AccessRights.Create,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action.ToString())
            };

            if (!hasPermission)
            {
                return (false, "U heeft geen rechten om deze actie uit te voeren.", permissions);
            }

            /* TODO: I was working on this code for also implementing permissions based on entity type, but since that was not the assignment and I was running out of time, I stopped with that.
               TODO: We can continue with this commented code (instead of the code above), if and when we want to implement permissions based on entity type.
            databaseConnection.AddParameter("userId", IdentityHelpers.GetCustomId(identity));

            var permissionsQuery = $@"SELECT 
	                                        permission.entity_name,
	                                        permission.permissions
                                        FROM {WiserTableNames.WiserPermission} AS permission
                                        JOIN {WiserTableNames.WiserRoles} AS role ON role.id = permission.role_id
                                        JOIN {WiserTableNames.WiserUserRoles} AS user_role ON user_role.role_id = role.id AND user_role.user_id = ?userId
                                        JOIN {tablePrefix}{WiserTableNames.WiserItem} AS item ON item.id = ?itemId AND (permission.item_id = item.id OR permission.entity_name = item.entity_type)";
            await databaseConnection.GetAsync(permissionsQuery, true);

            // If the query returns no results, it means the user is not denied this action due to permissions, because the default permission is to allow everything.
            if (databaseConnection.RowCount() > 0)
            {
                var temporaryPermissionsList = databaseConnection.Rows().Cast<DataRow>()
                    .Select(dataRow => (isViaEntityType: !String.IsNullOrEmpty(dataRow.Field<string>("entityName")), permissionsBitMask: (AccessRights) dataRow.Field<int>("permissions")))
                    .ToList();
                
                var entityTypePermissions = temporaryPermissionsList.Where(p => p.isViaEntityType).ToList();
                var itemIdPermissions = temporaryPermissionsList.Where(p => !p.isViaEntityType).ToList();

                var isAllowedViaEntityType = entityTypePermissions.Any(p =>
                {
                    switch (action)
                    {
                        case EntityActions.Delete:
                            return (p.permissionsBitMask & AccessRights.Delete) == AccessRights.Delete;
                        case EntityActions.Update:
                            return (p.permissionsBitMask & AccessRights.Update) == AccessRights.Update;
                        case EntityActions.Read:
                            return (p.permissionsBitMask & AccessRights.Read) == AccessRights.Read;
                        case EntityActions.Create:
                            return (p.permissionsBitMask & AccessRights.Create) == AccessRights.Create;
                        default:
                            throw new Exception($"Unknown entity action '{action.ToString()}'.");
                    }
                });

                var isAllowedViaItemId = itemIdPermissions.Any(p =>
                {
                    switch (action)
                    {
                        case EntityActions.Delete:
                            return (p.permissionsBitMask & AccessRights.Delete) == AccessRights.Delete;
                        case EntityActions.Update:
                            return (p.permissionsBitMask & AccessRights.Update) == AccessRights.Update;
                        case EntityActions.Read:
                            return (p.permissionsBitMask & AccessRights.Read) == AccessRights.Read;
                        case EntityActions.Create:
                            return (p.permissionsBitMask & AccessRights.Create) == AccessRights.Create;
                        default:
                            throw new Exception($"Unknown entity action '{action.ToString()}'.");
                    }
                });

                // If the user does have item ID permissions for this item, but not for the current action, then deny the action.
                if (itemIdPermissions.Any() && !isAllowedViaItemId)
                {
                    return (false, "U heeft geen rechten om deze actie uit te voeren.");
                }

                // If the user does have entity type permissions for this item, but not for the current action, and it has no item ID permissions that override this, then deny the action.
                if (entityTypePermissions.Any() && !isAllowedViaEntityType && (!itemIdPermissions.Any() || (itemIdPermissions.Any() && !isAllowedViaItemId)))
                {
                    return (false, "U heeft geen rechten om deze actie uit te voeren.");
                }
            }*/

            if (onlyCheckAccessRights)
            {
                return (true, "", permissions);
            }

            // Check if there is a check query set and execute that query if that is the case.
            string columnName;
            switch (action)
            {
                case EntityActions.Delete:
                    columnName = "query_before_delete";
                    break;
                case EntityActions.Update:
                    columnName = "query_before_update";
                    break;
                default:
                    return (true, "", permissions);
            }

            query = $@"SELECT e.{columnName} AS `query`
                    FROM {tablePrefix}{WiserTableNames.WiserItem} i
                    JOIN {WiserTableNames.WiserEntity} e ON e.name = i.entity_type
                    WHERE i.id = ?itemId
                    ORDER BY IF(e.module_id = i.moduleid, 1, 0) DESC
                    LIMIT 1";

            queryResult = await databaseConnection.GetAsync(query, true);
            if (queryResult.Rows.Count == 0)
            {
                // If there is no query, then we don't need to check anything, so return true.
                return (true, "", permissions);
            }

            var queryToExecute = queryResult.Rows[0].Field<string>("query");
            if (String.IsNullOrWhiteSpace(queryToExecute))
            {
                // If there is no query, then we don't need to check anything, so return true.
                return (true, "", permissions);
            }

            // Execute the check query.
            queryToExecute = queryToExecute.ReplaceCaseInsensitive("{itemId}", "?itemId");

            if (wiserItem?.Details != null)
            {
                var dictionary = wiserItem.Details.ToDictionary(d => d.Key, d => d.Value ?? "");
                queryToExecute = stringReplacementsService.DoReplacements(queryToExecute, dictionary, forQuery: true);
            }

            var dataTable = await databaseConnection.GetAsync(queryToExecute, true);
            if (dataTable.Rows.Count == 0)
            {
                // If the check query returned no results, the user is allowed to execute this action, so return true.
                return (true, null, permissions);
            }

            var success = Convert.ToInt32(dataTable.Rows[0][0]);
            var errorMessage = "";
            if (dataTable.Columns.Count > 1)
            {
                errorMessage = dataTable.Rows[0].Field<string>(1);
            }

            return (success > 0, errorMessage, permissions);
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserItemPermissionsAsync(ulong itemId, ulong userId, string entityType = null)
        {
            return await GetUserItemPermissionsAsync(this, itemId, userId, entityType);
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserItemPermissionsAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong userId, string entityType = null)
        {
            // If someone is not logged in, they will have no permissions by default. If someone is logged in, then they have all permissions by default.
            var defaultPermissions = userId == 0 ? AccessRights.Nothing : AccessRights.Read | AccessRights.Create | AccessRights.Update | AccessRights.Delete;
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            // First check permissions based on module ID.
            var permissionsQuery = $@"SELECT permission.permissions
                                    FROM {WiserTableNames.WiserUserRoles} AS user_role
                                    JOIN {tablePrefix}{WiserTableNames.WiserItem} AS item ON item.id = ?itemId AND item.moduleid > 0
                                    LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = user_role.role_id AND permission.module_id = item.moduleid
                                    WHERE user_role.user_id = ?userId";

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("userId", userId);
            var dataTable = await databaseConnection.GetAsync(permissionsQuery, true);

            var modulePermissionsFound = false;
            var userItemPermissions = AccessRights.Nothing;

            if (dataTable.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    if (dataRow.IsNull("permissions"))
                    {
                        break;
                    }

                    modulePermissionsFound = true;
                    var currentPermissions = (AccessRights)dataRow.Field<int>("permissions");
                    if ((currentPermissions & AccessRights.Read) == AccessRights.Read)
                    {
                        userItemPermissions |= AccessRights.Read;
                    }

                    if ((currentPermissions & AccessRights.Create) == AccessRights.Create)
                    {
                        userItemPermissions |= AccessRights.Create;
                    }

                    if ((currentPermissions & AccessRights.Update) == AccessRights.Update)
                    {
                        userItemPermissions |= AccessRights.Update;
                    }

                    if ((currentPermissions & AccessRights.Delete) == AccessRights.Delete)
                    {
                        userItemPermissions |= AccessRights.Delete;
                    }
                }
            }

            // Then check the permissions for the specific item, they overwrite permissions of the module.
            permissionsQuery = $@"SELECT permission.permissions
                                FROM {WiserTableNames.WiserUserRoles} AS user_role
                                LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = user_role.role_id AND permission.item_id = ?itemId
                                WHERE user_role.user_id = ?userId";
            dataTable = await databaseConnection.GetAsync(permissionsQuery, true);

            if (dataTable.Rows.Count == 0)
            {
                if (!modulePermissionsFound)
                {
                    userItemPermissions = defaultPermissions;
                }

                return userItemPermissions;
            }

            var firstRow = true;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull("permissions"))
                {
                    if (!modulePermissionsFound)
                    {
                        userItemPermissions = defaultPermissions;
                        break;
                    }

                    continue;
                }

                // If the user has permissions via the module, but also via the specific item, reset the permissions so that only the permissions set on the current item will be counted.
                if (firstRow)
                {
                    firstRow = false;
                    if (modulePermissionsFound)
                    {
                        userItemPermissions = AccessRights.Nothing;
                    }
                }

                var currentPermissions = (AccessRights)dataRow.Field<int>("permissions");
                if ((currentPermissions & AccessRights.Read) == AccessRights.Read)
                {
                    userItemPermissions |= AccessRights.Read;
                }

                if ((currentPermissions & AccessRights.Create) == AccessRights.Create)
                {
                    userItemPermissions |= AccessRights.Create;
                }

                if ((currentPermissions & AccessRights.Update) == AccessRights.Update)
                {
                    userItemPermissions |= AccessRights.Update;
                }

                if ((currentPermissions & AccessRights.Delete) == AccessRights.Delete)
                {
                    userItemPermissions |= AccessRights.Delete;
                }
            }

            return userItemPermissions;
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserModulePermissions(int moduleId, ulong userId)
        {
            // First check permissions based on module ID.
            var permissionsQuery = $@"SELECT permission.permissions
                                    FROM {WiserTableNames.WiserUserRoles} user_role
                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.module_id = ?moduleId
                                    WHERE user_role.user_id = ?userId";

            databaseConnection.AddParameter("moduleId", moduleId);
            databaseConnection.AddParameter("userId", userId);
            var dataTable = await databaseConnection.GetAsync(permissionsQuery);

            var userItemPermissions = AccessRights.Nothing;

            if (dataTable.Rows.Count == 0)
            {
                userItemPermissions = AccessRights.Read | AccessRights.Create | AccessRights.Update | AccessRights.Delete;
                return userItemPermissions;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull("permissions"))
                {
                    userItemPermissions = AccessRights.Read | AccessRights.Create | AccessRights.Update | AccessRights.Delete;
                    break;
                }

                var currentPermissions = (AccessRights)dataRow.Field<int>("permissions");
                if ((currentPermissions & AccessRights.Read) == AccessRights.Read)
                {
                    userItemPermissions |= AccessRights.Read;
                }

                if ((currentPermissions & AccessRights.Create) == AccessRights.Create)
                {
                    userItemPermissions |= AccessRights.Create;
                }

                if ((currentPermissions & AccessRights.Update) == AccessRights.Update)
                {
                    userItemPermissions |= AccessRights.Update;
                }

                if ((currentPermissions & AccessRights.Delete) == AccessRights.Delete)
                {
                    userItemPermissions |= AccessRights.Delete;
                }
            }

            return userItemPermissions;
        }
        
        /// <inheritdoc />
        public async Task<AccessRights> GetUserQueryPermissionsAsync(int queryId, ulong userId)
        {
            databaseConnection.AddParameter("queryId", queryId);
            // First check permissions based on module ID.
            var permissionsQuery = $@"SELECT permission.permissions
                                    FROM {WiserTableNames.WiserUserRoles} user_role
                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.query_id = ?queryId
                                    WHERE user_role.user_id = ?userId";
            
            databaseConnection.AddParameter("userId", userId);
            var dataTable = await databaseConnection.GetAsync(permissionsQuery);

            var userItemPermissions = AccessRights.Nothing;

            if (dataTable.Rows.Count == 0)
            {
                userItemPermissions = AccessRights.Nothing;
                return userItemPermissions;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull("permissions"))
                {
                    userItemPermissions = AccessRights.Nothing;
                    break;
                }

                var currentPermissions = (AccessRights)dataRow.Field<int>("permissions");
                if ((currentPermissions & AccessRights.Read) == AccessRights.Read)
                {
                    userItemPermissions |= AccessRights.Read;
                }

                if ((currentPermissions & AccessRights.Create) == AccessRights.Create)
                {
                    userItemPermissions |= AccessRights.Create;
                }

                if ((currentPermissions & AccessRights.Update) == AccessRights.Update)
                {
                    userItemPermissions |= AccessRights.Update;
                }

                if ((currentPermissions & AccessRights.Delete) == AccessRights.Delete)
                {
                    userItemPermissions |= AccessRights.Delete;
                }
            }

            return userItemPermissions;
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserDataSelectorPermissionsAsync(int dataSelectorId, ulong userId)
        {
            databaseConnection.AddParameter("dataSelectorId", dataSelectorId);
            // First check permissions based on module ID.
            var permissionsQuery = $@"SELECT permission.permissions
                                    FROM {WiserTableNames.WiserUserRoles} user_role
                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.data_selector_id = ?dataSelectorId
                                    WHERE user_role.user_id = ?userId";
            
            databaseConnection.AddParameter("userId", userId);
            var dataTable = await databaseConnection.GetAsync(permissionsQuery);

            var userItemPermissions = AccessRights.Nothing;

            if (dataTable.Rows.Count == 0)
            {
                userItemPermissions = AccessRights.Nothing;
                return userItemPermissions;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull("permissions"))
                {
                    userItemPermissions = AccessRights.Nothing;
                    break;
                }

                var currentPermissions = (AccessRights)dataRow.Field<int>("permissions");
                if ((currentPermissions & AccessRights.Read) == AccessRights.Read)
                {
                    userItemPermissions |= AccessRights.Read;
                }

                if ((currentPermissions & AccessRights.Create) == AccessRights.Create)
                {
                    userItemPermissions |= AccessRights.Create;
                }

                if ((currentPermissions & AccessRights.Update) == AccessRights.Update)
                {
                    userItemPermissions |= AccessRights.Update;
                }

                if ((currentPermissions & AccessRights.Delete) == AccessRights.Delete)
                {
                    userItemPermissions |= AccessRights.Delete;
                }
            }

            return userItemPermissions;
        }
        
        /// <inheritdoc />
        public async Task<WiserItemModel> GetItemDetailsAsync(ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false)
        {
            return await GetItemDetailsAsync(this, itemId, uniqueId, languageCode, userId, detailKey, detailValue, returnNullIfDeleted, skipDetailsWithoutLanguageCode, entityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false)
        {
            if (itemId == 0 && String.IsNullOrEmpty(uniqueId))
            {
                return null;
            }

            if (!skipPermissionsCheck && itemId > 0)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Read, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to read item '{itemId}'.")
                    {
                        Action = EntityActions.Read,
                        ItemId = itemId,
                        UserId = userId
                    };
                }
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var where = new List<string>();
            var join = "";
            var joinDeleted = "";
            if (itemId > 0)
            {
                where.Add("item.id = ?itemId");
            }

            if (!String.IsNullOrEmpty(uniqueId))
            {
                where.Add("item.unique_uuid = ?uniqueId");
            }

            if (!String.IsNullOrEmpty(languageCode))
            {
                where.Add(skipDetailsWithoutLanguageCode ? "details.language_code = ?languageCode" : "(details.language_code = ?languageCode OR details.language_code IS NULL OR details.language_code = '')");
            }

            if (!String.IsNullOrEmpty(detailKey) && !String.IsNullOrEmpty(detailValue)) // Get item by key-value of detail
            {
                databaseConnection.AddParameter("detailKey", detailKey);
                databaseConnection.AddParameter("detailValue", detailValue);
                join = $"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS matchDetail ON matchDetail.item_id = item.id AND matchDetail.`key` = ?detailKey AND matchDetail.`value` = ?detailValue";
                joinDeleted = $"JOIN {tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix} AS matchDetail ON matchDetail.item_id = item.id AND matchDetail.`key` = ?detailKey AND matchDetail.`value` = ?detailValue";
            }

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("uniqueId", uniqueId);
            databaseConnection.AddParameter("languageCode", languageCode);
            var query = $@"SELECT 
	item.*,
	details.`key`,	
	CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
    details.language_code
FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
{join}
LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS details ON details.item_id = item.id                                        
{(where.Count > 0 ? $"WHERE {String.Join(" AND ", where)}" : "")}";

            if (!returnNullIfDeleted)
            {
                query += $@"
UNION
SELECT 
	item.*,
	details.`key`,	
	CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
    details.language_code
FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} AS item
{joinDeleted}
LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix} AS details ON details.item_id = item.id                                        
{(where.Count > 0 ? $"WHERE {String.Join(" AND ", where)}" : "")}";
            }

            var dataTable = await databaseConnection.GetAsync(query, true);

            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            // Add all columns from wiser_item table to list.
            var firstRow = dataTable.Rows[0];
            var result = DataRowToItem(firstRow);

            // Add all details of item to list.
            foreach (DataRow row in dataTable.Rows)
            {
                AddDetailFromDataRow(result, row);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await GetLinkedItemDetailsAsync(this, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            var result = new List<WiserItemModel>();

            LinkSettingsModel linkSettings;
            if (linkType > -1)
            {
                linkSettings = await wiserItemsService.GetLinkTypeSettingsAsync(linkType, reverse ? itemIdEntityType : entityType, reverse ? entityType : itemIdEntityType);
            }
            else
            {
                linkSettings = new LinkSettingsModel();
            }

            var linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkSettings);
            var permissionsQueryPart = "";
            var linkTypePart = linkType > -1 ? " AND link.type = ?linkType" : "";
            var where = new List<string> { "TRUE" };
            if (!skipPermissionsCheck)
            {
                databaseConnection.AddParameter("userId", userId);
                permissionsQueryPart = $@"# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                    LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = item.id";
                where.Add("(permission.id IS NULL OR (permission.permissions & 1) > 0)");
            }

            var tablePrefix = "";
            if (!String.IsNullOrWhiteSpace(entityType))
            {
                tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
                databaseConnection.AddParameter("entityType", entityType);
                where.Add("item.entity_type = ?entityType");
            }
            else if (!String.IsNullOrWhiteSpace(itemIdEntityType))
            {
                tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(itemIdEntityType);
            }

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("linkType", linkType);

            var itemLinkJoin = "";
            var itemLinkDetailsPart = "";

            if (linkSettings.UseItemParentId)
            {
                if (reverse)
                {
                    itemLinkJoin = $"JOIN {tablePrefix}{WiserTableNames.WiserItem} AS linkedItem ON linkedItem.id = ?itemId AND linkedItem.parent_item_id = item.id";
                }
                else
                {
                    where.Add("item.parent_item_id = ?itemId");
                }
            }
            else
            {
                itemLinkJoin = reverse
                    ? $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.destination_item_id = item.id AND link.item_id = ?itemId{linkTypePart}"
                    : $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.item_id = item.id AND link.destination_item_id = ?itemId{linkTypePart}";

                itemLinkDetailsPart = $@"
                    UNION ALL

                    # Item link details.
                    SELECT 
	                    item.*,
	                    details.`key`,	
	                    CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
                        details.language_code,
                        link.id AS itemLinkId
                    FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                    {itemLinkJoin}
                    LEFT JOIN {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} AS details ON details.itemlink_id = link.id
                    {permissionsQueryPart}
                    WHERE {String.Join(" AND ", where)}";
            }

            var query = $@"# Item details.
                        SELECT 
	                        item.*,
	                        details.`key`,	
	                        CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
                            details.language_code,
                            0 AS itemLinkId
                        FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                        {itemLinkJoin}
                        LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS details ON details.item_id = item.id
                        {permissionsQueryPart}
                        WHERE {String.Join(" AND ", where)}
                        {itemLinkDetailsPart}";

            if (includeDeletedItems)
            {
                if (linkSettings.UseItemParentId)
                {
                    if (reverse)
                    {
                        itemLinkJoin = $"JOIN {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} AS mainItem ON mainItem.parent_item_id = item.id AND mainItem.id = ?itemId";
                    }
                    else
                    {
                        where.Add("item.parent_item_id = ?itemId");
                    }
                }
                else
                {
                    itemLinkJoin = reverse
                        ? $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix} AS link ON link.destination_item_id = item.id AND link.item_id = ?itemId{linkTypePart}"
                        : $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix} AS link ON link.item_id = item.id AND link.destination_item_id = ?itemId{linkTypePart}";

                    itemLinkDetailsPart = $@"
                        UNION ALL

                        # Item link details.
                        SELECT 
	                        item.*,
	                        details.`key`,	
	                        CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
                            details.language_code,
                            link.id AS itemLinkId
                        FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} AS item
                        {itemLinkJoin}
                        LEFT JOIN {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail}{WiserTableNames.ArchiveSuffix} AS details ON details.itemlink_id = link.id
                        {permissionsQueryPart}
                        WHERE {String.Join(" AND ", where)}";
                }

                query += $@"
                            UNION
                            SELECT 
	                            item.*,
	                            details.`key`,	
	                            CONCAT_WS('', details.`value`, details.`long_value`) AS `value`,
                                details.language_code,
                                0 AS itemLinkId
                            FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} AS item
                            {itemLinkJoin}
                            LEFT JOIN {tablePrefix}{WiserTableNames.WiserItemDetail}{WiserTableNames.ArchiveSuffix} AS details ON details.item_id = item.id
                            {permissionsQueryPart}
                            WHERE {String.Join(" AND ", where)}
                            {itemLinkDetailsPart}";
            }

            var dataTable = await databaseConnection.GetAsync(query, true);

            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var linkedItemId = dataRow.Field<ulong>("id");
                var item = result.FirstOrDefault(i => i.Id == linkedItemId);
                if (item == null)
                {
                    item = DataRowToItem(dataRow);
                    result.Add(item);
                }

                AddDetailFromDataRow(item, dataRow);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<ulong>> GetLinkedItemIdsAsync(ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await GetLinkedItemIdsAsync(this, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<ulong>> GetLinkedItemIdsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            var result = new List<ulong>();
            var linkSettings = await wiserItemsService.GetLinkTypeSettingsAsync(linkType, reverse ? itemIdEntityType : entityType, reverse ? entityType : itemIdEntityType);
            var permissionsQueryPart = "";
            var itemLinkJoin = "";

            var where = new List<string>();
            if (!skipPermissionsCheck)
            {
                databaseConnection.AddParameter("userId", userId);
                permissionsQueryPart = $@"# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                    LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                    LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = item.id";
                where.Add("(permission.id IS NULL OR (permission.permissions & 1) > 0)");
            }

            var linkTablePrefix = wiserItemsService.GetTablePrefixForLink(linkSettings);
            var tablePrefix = "";
            if (!String.IsNullOrWhiteSpace(entityType))
            {
                tablePrefix = await GetTablePrefixForEntityAsync(entityType);
                databaseConnection.AddParameter("entityType", entityType);
                where.Add("item.entity_type = ?entityType");
            }
            else if (!String.IsNullOrWhiteSpace(itemIdEntityType))
            {
                tablePrefix = await GetTablePrefixForEntityAsync(itemIdEntityType);
            }

            if (linkSettings.UseItemParentId)
            {
                if (reverse)
                {
                    itemLinkJoin = $"JOIN {tablePrefix}{WiserTableNames.WiserItem} AS linkedItem ON linkedItem.id = ?itemId AND linkedItem.parent_item_id = item.id";
                }
                else
                {
                    where.Add("item.parent_item_id = ?itemId");
                }
            }
            else
            {
                itemLinkJoin = reverse
                    ? $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.destination_item_id = item.id AND link.item_id = ?itemId AND link.type = ?linkType"
                    : $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.item_id = item.id AND link.destination_item_id = ?itemId AND link.type = ?linkType";
            }

            // Create where part.
            var wherePart = where.Count > 0 ? $"WHERE {String.Join(" AND ", where)}" : "";

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("linkType", linkType);
            var query = $@"SELECT item.id
                        FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                        {itemLinkJoin}
                        {permissionsQueryPart}
                        {wherePart}
                        ORDER BY item.id DESC;";

            if (includeDeletedItems)
            {
                if (linkSettings.UseItemParentId)
                {
                    if (reverse)
                    {
                        itemLinkJoin = $"JOIN {tablePrefix}{WiserTableNames.WiserItem} AS linkedItem ON linkedItem.id = ?itemId AND linkedItem.parent_item_id = item.id";
                    }
                }
                else
                {
                    itemLinkJoin = reverse
                        ? $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix} AS link ON link.destination_item_id = item.id AND link.item_id = ?itemId AND link.type = ?linkType"
                        : $"JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix} AS link ON link.item_id = item.id AND link.destination_item_id = ?itemId AND link.type = ?linkType";
                }

                query += $@"UNION
                            SELECT item.id
                            FROM {tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} AS item
                            {itemLinkJoin}
                            {permissionsQueryPart}
                            WHERE {String.Join(" AND ", where)}
                            ORDER BY item.id DESC;";
            }

            var dataTable = await databaseConnection.GetAsync(query, true);
            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<ulong>("id")));

            return result;
        }

        /// <inheritdoc />
        public async Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0)
        {
            databaseConnection.AddParameter("entityType", entityType);
            var query = $@"SELECT 
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
                            ORDER BY entity.id ASC, property.ordering ASC";

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
                        DeleteAction = dataRow.Field<string>("delete_action")?.ToLowerInvariant() switch
                        {
                            null => EntityDeletionTypes.Archive,
                            "archive" => EntityDeletionTypes.Archive,
                            "permanent" => EntityDeletionTypes.Permanent,
                            "hide" => EntityDeletionTypes.Hide,
                            "disallow" => EntityDeletionTypes.Disallow,
                            _ => throw new ArgumentOutOfRangeException("delete_action", dataRow.Field<string>("delete_action"))
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

                options.Add(FieldTypeKey, fieldType);
                options.Add(SaveSeoValueKey, alsoSaveSeoValue);
                options.Add(ReadOnlyKey, readOnly);

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

        /// <inheritdoc />
        public async Task<Dictionary<string, Dictionary<string, object>>> GetFieldOptionsForLinkFieldsAsync(int linkType)
        {
            var results = new Dictionary<string, Dictionary<string, object>>();
            databaseConnection.AddParameter("linkType", linkType);
            var query = $@"SELECT 
                                property.id AS property_id,
                                IF(property.property_name IS NULL OR property.property_name = '', property.display_name, property.property_name) AS property_name, 
                                property.inputtype,
                                property.language_code,
                                property.options,
                                property.also_save_seo_value,
                                property.readonly
                            FROM {WiserTableNames.WiserEntityProperty} AS property
                            WHERE property.link_type = ?linkType
                            ORDER BY property.ordering ASC";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count <= 0)
            {
                return results;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
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

                options.Add(FieldTypeKey, fieldType);
                options.Add(SaveSeoValueKey, alsoSaveSeoValue);
                options.Add(ReadOnlyKey, readOnly);

                results[$"{propertyName}_{languageCode}"] = options;
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(ulong itemId, string entityType = null)
        {
            return await GetTemplateAndDataForItemAsync(this, itemId, entityType);
        }

        /// <inheritdoc />
        public async Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(IWiserItemsService wiserItemsService, ulong itemId, string entityType = null)
        {
            try
            {
                var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

                databaseConnection.AddParameter("itemId", itemId);
                var dataTable = await databaseConnection.GetAsync($@"SELECT
                                                                            item.entity_type,
	                                                                        entity.template_query,
	                                                                        entity.template_html
                                                                        FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                                                                        JOIN {WiserTableNames.WiserEntity} AS entity ON entity.`name` = item.entity_type
                                                                        WHERE item.id = ?itemId", true);

                var query = "";
                var template = "";
                var firstRow = dataTable.Rows[0];
                if (dataTable.Rows.Count > 0)
                {
                    entityType = firstRow.Field<string>("entity_type");
                    query = firstRow.Field<string>("template_query");
                    template = firstRow.Field<string>("template_html");
                }

                if (String.IsNullOrWhiteSpace(template))
                {
                    return ($"<!-- No template found for item with ID '{itemId}' -->", null);
                }

                query = String.IsNullOrWhiteSpace(query) ? $"SELECT * FROM {tablePrefix}{WiserTableNames.WiserItem} WHERE id = ?itemId" : query.ReplaceCaseInsensitive("{itemId}", "?itemId");

                dataTable = await databaseConnection.GetAsync(query, true);
                return dataTable.Rows.Count == 0 ? ($"<!-- Query for entity type '{entityType}' and item '{itemId}' returned no results. -->", null) : (template, dataTable.Rows[0]);
            }
            catch (Exception exception)
            {
                return ($"<!-- An error occurred while rendering template for item '{itemId}': {exception} -->", null);
            }
        }

        /// <inheritdoc />
        public async Task<int> GetLinkTypeAsync(string destinationEntityType, string connectedEntityType)
        {
            databaseConnection.AddParameter("destinationEntityType", destinationEntityType);
            databaseConnection.AddParameter("connectedEntityType", connectedEntityType);
            var getResult = await databaseConnection.GetAsync($"SELECT type FROM {WiserTableNames.WiserLink} WHERE destination_entity_type = ?destinationEntityType AND connected_entity_type = ?connectedEntityType");

            return getResult.Rows.Count == 0 ? 0 : getResult.Rows[0].Field<int>("type");
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemLinkAsync(ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            return await AddItemLinkAsync(this, itemId, destinationItemId, type, ordering, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemLinkAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{itemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = itemId,
                        UserId = userId
                    };
                }
                isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(destinationItemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{destinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = destinationItemId,
                        UserId = userId
                    };
                }
            }

            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(type);

            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("destinationItemId", destinationItemId);
            databaseConnection.AddParameter("type", type);
            databaseConnection.AddParameter("ordering", ordering);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            var dataTable = await databaseConnection.GetAsync($@"SELECT id FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE item_id = ?itemId AND destination_item_id = ?destinationItemId AND type = ?type", true);
            if (dataTable.Rows.Count > 0)
            {
                return Convert.ToUInt64(dataTable.Rows[0]["id"]);
            }

            dataTable = await databaseConnection.GetAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl; 
                                                        INSERT IGNORE INTO {linkTablePrefix}{WiserTableNames.WiserItemLink} (item_id, destination_item_id, type, ordering) 
                                                        VALUES (?itemId, ?destinationItemId, ?type, ?ordering);
                                                        SELECT LAST_INSERT_ID();", true);
            return Convert.ToUInt64(dataTable.Rows[0][0]);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksAsync(ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveItemLinksAsync(this, destinationItemId, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(destinationItemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{destinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = destinationItemId,
                        UserId = userId
                    };
                }
            }
            
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(type);

            databaseConnection.AddParameter("destinationItemId", destinationItemId);
            databaseConnection.AddParameter("type", type);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            await databaseConnection.ExecuteAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl; 
                                                        DELETE FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} 
                                                        WHERE destination_item_id = ?destinationItemId 
                                                        AND type = ?type");
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksByIdAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveItemLinksByIdAsync(this, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksByIdAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var itemsWithNoPermissionToUpdate = await GetItemIdsWithNoPermissionToUpdateLinkAsync(wiserItemsService, sourceEntityType, sourceIds, destinationEntityType, destinationIds, userId);

                if (itemsWithNoPermissionToUpdate.Any())
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to change the links attached to items '{String.Join(", ", itemsWithNoPermissionToUpdate)}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = itemsWithNoPermissionToUpdate.First(),
                        UserId = userId
                    };
                }
            }
            
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(0, sourceEntityType, destinationEntityType);
            
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryJcl", saveHistory); // This is used in triggers.

            // Copy the item links to the archive.
            var query = $@"SET @_username = ?username;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryJcl;
                        INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix}
                        (
                            id,
                            item_id,
                            destination_item_id,
                            ordering,
                            type,
                            added_on
                        )
                        SELECT
                            id,
                            item_id,
                            destination_item_id,
                            ordering,
                            type,
                            added_on
                        FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} AS itemLink
                        WHERE itemLink.id IN({String.Join(",", ids)})
                        ON DUPLICATE KEY UPDATE added_on = itemLink.added_on";
            await databaseConnection.ExecuteAsync(query);

            // Copy the item links details to the archive.
            query = $@"SET @_username = ?username;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryJcl;
                        INSERT INTO {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail}{WiserTableNames.ArchiveSuffix}
                        (
                            id,
                            language_code,
                            itemlink_id,
                            groupname,
                            `key`,
                            value,
                            long_value
                        )
                        SELECT
                            detail.id,
                            detail.language_code,
                            detail.itemlink_id,
                            detail.groupname,
                            detail.`key`,
                            detail.value,
                            detail.long_value
                        FROM {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} AS detail
                        WHERE detail.itemlink_id IN({String.Join(",", ids)})";
            await databaseConnection.ExecuteAsync(query);

            query = $@"SET @_username = ?username;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryJcl;
                        DELETE FROM {linkTablePrefix}{WiserTableNames.WiserItemLinkDetail} AS d WHERE d.itemlink_id IN({String.Join(",", ids)});
                        DELETE FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE id IN({String.Join(",", ids)})";
            await databaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task RemoveParentLinkOfItemsAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveParentLinkOfItemsAsync(this, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveParentLinkOfItemsAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var itemsWithNoPermissionToUpdate = await GetItemIdsWithNoPermissionToUpdateLinkAsync(wiserItemsService, sourceEntityType, sourceIds, destinationEntityType, destinationIds, userId);

                if (itemsWithNoPermissionToUpdate.Any())
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to change the links attached to items '{String.Join(", ", itemsWithNoPermissionToUpdate)}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = itemsWithNoPermissionToUpdate.First(),
                        UserId = userId
                    };
                }
            }

            var sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(sourceEntityType);
            
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryJcl", saveHistory); // This is used in triggers.

            // Save the change to the history.
            var query = $@"SET @_username = ?username;
                        SET @_userId = ?userId;
                        SET @saveHistory = ?saveHistoryJcl;
                        INSERT INTO {WiserTableNames.WiserHistory} (action, tablename, item_id, changed_by, field, oldvalue, newvalue)
                        SELECT 'REMOVE_LINK', '{sourceTablePrefix}{WiserTableNames.WiserItem}', id, @_username, 'parent_item_id', 0, parent_item_id
			            FROM {sourceTablePrefix}{WiserTableNames.WiserItem}
                        WHERE id IN({String.Join(",", ids)})";
            await databaseConnection.ExecuteAsync(query);

            query = $@"SET @_username = ?username;
                    SET @_userId = ?userId;
                    SET @saveHistory = ?saveHistoryJcl;
                    UPDATE {sourceTablePrefix}{WiserTableNames.WiserItem}
                    SET parent_item_id = 0
                    WHERE id IN({String.Join(",", ids)})";
            await databaseConnection.ExecuteAsync(query);
        }

        /// <summary>
        /// Get all ids from items that the user does not have the permission to update a link from.
        /// Check both source as destination items.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <returns>Returns a <see cref="List{T}"/> of ids of the items the user is not allowed to update.</returns>
        private async Task<List<ulong>> GetItemIdsWithNoPermissionToUpdateLinkAsync(IWiserItemsService wiserItemsService, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, ulong userId)
        {
            var itemsWithNoPermissionToUpdate = new List<ulong>();

            //Check if the user is allowed to update the source item.
            foreach (var itemId in sourceIds)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: sourceEntityType);
                if (!isPossible.ok)
                {
                    itemsWithNoPermissionToUpdate.Add(itemId);
                }
            }

            //Check if the user is allowed to update the destination item.
            foreach (var itemId in destinationIds)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: destinationEntityType);
                if (!isPossible.ok)
                {
                    itemsWithNoPermissionToUpdate.Add(itemId);
                }
            }

            return itemsWithNoPermissionToUpdate;
        }

        /// <inheritdoc />
        public async Task RemoveLinkedItemsAsync(ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0UL, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            await RemoveLinkedItemsAsync(this, destinationItemId, type, exceptItemIds, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveLinkedItemsAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(type, null, entityType);

            databaseConnection.AddParameter("destinationItemId", destinationItemId);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.

            // Query for removing links in the table wiser_itemlinks.
            var wiserItemLinkQueryBuilder = new StringBuilder($@"SET @_username = ?username;
                                                                SET @_userId = ?userId;
                                                                SET @saveHistory = ?saveHistoryGcl;
                                                                SELECT item.id, item.entity_type 
                                                                FROM {tablePrefix}{WiserTableNames.WiserItem} AS item 
                                                                JOIN {linkTablePrefix}{WiserTableNames.WiserItemLink} AS link ON link.item_id = item.id AND link.destination_item_id = ?destinationItemId");

            // Query for removing links of the column parent_item_id from the table wiser_item.
            var parentItemIdQueryBuilder = new StringBuilder($@"SELECT item.id, item.entity_type
                                                                FROM {tablePrefix}{WiserTableNames.WiserItem} AS item");

            if (type > 0)
            {
                databaseConnection.AddParameter("type", type);
                wiserItemLinkQueryBuilder.Append(" AND link.type = ?type");
                parentItemIdQueryBuilder.Append($@" JOIN {tablePrefix}{WiserTableNames.WiserItem} AS parent ON parent.id = ?destinationItemId 
                                                    JOIN {linkTablePrefix}{WiserTableNames.WiserLink} AS linkSettings ON linkSettings.destination_entity_type = parent.entity_type AND linkSettings.connected_entity_type = item.entity_type AND linkSettings.use_item_parent_id = 1");
            }

            if (!skipPermissionsCheck)
            {
                wiserItemLinkQueryBuilder.Append($@" # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                                LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                                LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = item.id");
                parentItemIdQueryBuilder.Append($@" # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	                                                LEFT JOIN {WiserTableNames.WiserUserRoles} user_role ON user_role.user_id = ?userId
	                                                LEFT JOIN {WiserTableNames.WiserPermission} permission ON permission.role_id = user_role.role_id AND permission.item_id = item.id");
            }

            parentItemIdQueryBuilder.Append(@" WHERE item.parent_item_id = ?destinationItemId");

            if (!String.IsNullOrWhiteSpace(entityType))
            {
                databaseConnection.AddParameter("entityType", entityType);
                wiserItemLinkQueryBuilder.Append(" AND item.entity_type = ?entityType");
                parentItemIdQueryBuilder.Append(" AND item.entity_type = ?entityType");
            }

            if (exceptItemIds != null && exceptItemIds.Any())
            {
                wiserItemLinkQueryBuilder.Append($" AND item.id NOT IN ({String.Join(",", exceptItemIds)})");
                parentItemIdQueryBuilder.Append($" AND item.id NOT IN ({String.Join(",", exceptItemIds)})");
            }

            if (!skipPermissionsCheck)
            {
                wiserItemLinkQueryBuilder.Append(" AND (permission.id IS NULL OR (permission.permissions & 8) > 0)");
                parentItemIdQueryBuilder.Append(" AND (permission.id IS NULL OR (permission.permissions & 8) > 0)");
            }

            var dataTable = await databaseConnection.GetAsync($"{wiserItemLinkQueryBuilder} UNION {parentItemIdQueryBuilder}", true);
            if (dataTable.Rows.Count == 0)
            {
                return;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                await wiserItemsService.DeleteAsync(dataRow.Field<ulong>("id"), username: username, userId: userId, saveHistory: saveHistory, entityType: dataRow.Field<string>("entity_type"), createNewTransaction: createNewTransaction, skipPermissionsCheck: skipPermissionsCheck);
            }
        }

        /// <inheritdoc />
        public async Task ChangeItemLinksAsync(ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeItemLinksAsync(this, oldDestinationItemId, newDestinationItemId, entityType, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeItemLinksAsync(IWiserItemsService wiserItemsService, ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(oldDestinationItemId, EntityActions.Update, userId, entityType: entityType);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{oldDestinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = oldDestinationItemId,
                        UserId = userId
                    };
                }
                isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(newDestinationItemId, EntityActions.Update, userId, entityType: entityType);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{newDestinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = newDestinationItemId,
                        UserId = userId
                    };
                }
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(type, null, entityType);
            
            databaseConnection.AddParameter("oldDestinationItemId", oldDestinationItemId);
            databaseConnection.AddParameter("newDestinationItemId", newDestinationItemId);
            databaseConnection.AddParameter("type", type);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            await databaseConnection.ExecuteAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl;
                                                        # Update links via {WiserTableNames.WiserItemLink}.
                                                        UPDATE {linkTablePrefix}{WiserTableNames.WiserItemLink} SET destination_item_id = ?newDestinationItemId 
                                                        WHERE destination_item_id = ?oldDestinationItemId 
                                                        AND (?type = 0 OR type = ?type);

                                                        # Update links via parent_item_id from {WiserTableNames.WiserItem}.
                                                        UPDATE {tablePrefix}{WiserTableNames.WiserItem} AS item
                                                        JOIN {tablePrefix}{WiserTableNames.WiserItem} AS parent ON parent.id = ?oldDestinationItemId
                                                        JOIN {linkTablePrefix}{WiserTableNames.WiserLink} AS linkSettings ON linkSettings.destination_entity_type = parent.entity_type AND linkSettings.connected_entity_type = item.entity_type AND linkSettings.use_item_parent_id = 1
                                                        SET item.parent_item_id = ?newDestinationItemId
                                                        WHERE item.parent_item_id = ?oldDestinationItemId");
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypesAsync(ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeLinkTypesAsync(this, destinationItemId, oldLinkType, newLinkType, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypesAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(destinationItemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{destinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = destinationItemId,
                        UserId = userId
                    };
                }
            }

            var oldLinkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(oldLinkType);
            var newLinkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(newLinkType);
            if (!String.Equals(oldLinkTablePrefix, newLinkTablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"The old link type has the table prefix '{oldLinkTablePrefix}' and the new link type has the table prefix '{newLinkTablePrefix}'.");
            }

            databaseConnection.AddParameter("destinationItemId", destinationItemId);
            databaseConnection.AddParameter("oldLinkType", oldLinkType);
            databaseConnection.AddParameter("newLinkType", newLinkType);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            await databaseConnection.ExecuteAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl;
                                                        UPDATE {newLinkTablePrefix}{WiserTableNames.WiserItemLink} SET type = ?newLinkType 
                                                        WHERE destination_item_id = ?destinationItemId AND type = ?oldLinkType");
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypeAsync(ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeLinkTypeAsync(this, destinationItemId, oldLinkType, newLinkType, sourceItemId, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypeAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            if (!skipPermissionsCheck)
            {
                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(destinationItemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{destinationItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = destinationItemId,
                        UserId = userId
                    };
                }
                isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(sourceItemId, EntityActions.Update, userId);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{sourceItemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = sourceItemId,
                        UserId = userId
                    };
                }
            }

            var oldLinkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(oldLinkType);
            var newLinkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(newLinkType);
            if (!String.Equals(oldLinkTablePrefix, newLinkTablePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"The old link type has the table prefix '{oldLinkTablePrefix}' and the new link type has the table prefix '{newLinkTablePrefix}'.");
            }

            databaseConnection.AddParameter("destinationItemId", destinationItemId);
            databaseConnection.AddParameter("sourceItemId", sourceItemId);
            databaseConnection.AddParameter("oldLinkType", oldLinkType);
            databaseConnection.AddParameter("newLinkType", newLinkType);
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            await databaseConnection.ExecuteAsync($@"SET @_username = ?username;
                                                        SET @_userId = ?userId;
                                                        SET @saveHistory = ?saveHistoryGcl;
                                                        UPDATE {newLinkTablePrefix}{WiserTableNames.WiserItemLink} SET type = ?newLinkType 
                                                        WHERE destination_item_id = ?destinationItemId AND type = ?oldLinkType AND item_id = ?sourceItemId");
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemFileAsync(WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0)
        {
            return await AddItemFileAsync(this, wiserItemFile, username, userId, saveHistory, skipPermissionsCheck, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemFileAsync(IWiserItemsService wiserItemsService, WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0)
        {
            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
            var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType, entityType);
            
            if (!skipPermissionsCheck)
            {
                var itemId = wiserItemFile.ItemId;
                ulong destinationItemId = 0;

                // If we have an item link id instead of an item ID, check which items are part of that link and check the rights for those items.
                if (itemId == 0)
                {
                    databaseConnection.AddParameter("itemLinkId", wiserItemFile.ItemLinkId);
                    var queryResult = await databaseConnection.GetAsync($"SELECT item_id, destination_item_id FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE id = ?itemLinkId", true);
                    if (queryResult.Rows.Count == 0)
                    {
                        throw new Exception($"Item link with id '{wiserItemFile.ItemLinkId}' not found.");
                    }

                    itemId = Convert.ToUInt64(queryResult.Rows[0]["item_id"]);
                    destinationItemId = Convert.ToUInt64(queryResult.Rows[0]["destination_item_id"]);
                }

                var isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, userId, entityType: entityType);
                if (!isPossible.ok)
                {
                    throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{itemId}'.")
                    {
                        Action = EntityActions.Update,
                        ItemId = itemId,
                        UserId = userId
                    };
                }

                if (destinationItemId > 0)
                {
                    isPossible = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(destinationItemId, EntityActions.Update, userId);
                    if (!isPossible.ok)
                    {
                        throw new InvalidAccessPermissionsException($"User '{userId}' is not allowed to update item '{destinationItemId}'.")
                        {
                            Action = EntityActions.Update,
                            ItemId = destinationItemId,
                            UserId = userId
                        };
                    }
                }
            }

            databaseConnection.AddParameter("itemId", wiserItemFile.ItemId);
            databaseConnection.AddParameter("itemLinkId", wiserItemFile.ItemLinkId);
            databaseConnection.AddParameter("content", wiserItemFile.Content);
            databaseConnection.AddParameter("contentType", wiserItemFile.ContentType);
            databaseConnection.AddParameter("contentUrl", wiserItemFile.ContentUrl);
            databaseConnection.AddParameter("width", wiserItemFile.Width);
            databaseConnection.AddParameter("height", wiserItemFile.Height);
            databaseConnection.AddParameter("fileName", Path.GetFileNameWithoutExtension(wiserItemFile.FileName).ConvertToSeo() + Path.GetExtension(wiserItemFile.FileName)?.ToLowerInvariant());
            databaseConnection.AddParameter("extension", wiserItemFile.Extension);
            databaseConnection.AddParameter("title", wiserItemFile.Title);
            databaseConnection.AddParameter("propertyName", wiserItemFile.PropertyName);
            databaseConnection.AddParameter("extraData", wiserItemFile.ExtraData == null ? null : JsonConvert.SerializeObject(wiserItemFile.ExtraData));
            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("userId", userId);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            var addItemFileResult = await databaseConnection.GetAsync($@"
                SET @_username = ?username;
                SET @_userId = ?userId;
                SET @saveHistory = ?saveHistoryGcl;
                INSERT IGNORE INTO {(linkType > 0 ? linkTablePrefix : tablePrefix)}{WiserTableNames.WiserItemFile} (item_id, content_type, content, content_url, width, height, file_name, extension, added_by, title, property_name, itemlink_id, extra_data) 
                VALUES (?itemId, ?contentType, ?content, ?contentUrl, ?width, ?height, ?fileName, ?extension, ?username, ?title, ?propertyName, ?itemLinkId, ?extraData);
                SELECT LAST_INSERT_ID();", true);

            return Convert.ToUInt64(addItemFileResult.Rows[0][0]);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetItemFileAsync(ulong id, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await GetItemFileAsync(this, id, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetItemFileAsync(IWiserItemsService wiserItemsService, ulong id, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            var list = await wiserItemsService.GetItemFilesAsync(new[] { id }, field, propertyName, entityType, linkType);
            return list.FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<List<WiserItemFileModel>> GetItemFilesAsync(ulong[] ids, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await GetItemFilesAsync(this, ids, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemFileModel>> GetItemFilesAsync(IWiserItemsService wiserItemsService, ulong[] ids, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            var result = new List<WiserItemFileModel>();

            string columnName;

            // Make sure we can't use non-existing column names.
            switch (field.ToLowerInvariant())
            {
                case "id":
                case "item_id":
                case "itemlink_id":
                    columnName = field;
                    break;
                default:
                    throw new NotImplementedException($"Unknown field '{field}' given.");
            }

            var tablePrefix = linkType > 0 
                ? await wiserItemsService.GetTablePrefixForLinkAsync(linkType, entityType) 
                : await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            var propertyNameClause = "";
            if (!String.IsNullOrWhiteSpace(propertyName))
            {
                databaseConnection.AddParameter("propertyName", propertyName);
                propertyNameClause = "AND property_name = ?propertyName";
            }

            databaseConnection.AddParameter("Ids", String.Join(",", ids));
            var queryResult = await databaseConnection.GetAsync($@"
                SELECT `id`, `item_id`, `content_type`, `content`, `content_url`, `width`, `height`, `file_name`, `extension`, `added_on`, `added_by`, `title`, `property_name`, `itemlink_id`, extra_data
                FROM {tablePrefix}{WiserTableNames.WiserItemFile}
                WHERE {columnName} IN ({String.Join(",", ids)})
                {propertyNameClause}", true);

            if (queryResult.Rows.Count == 0)
            {
                return result;
            }

            foreach (DataRow dataRow in queryResult.Rows)
            {
                var itemFile = DataRowToItemFile(dataRow);
                result.Add(itemFile);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForEntityAsync(string entityType)
        {
            return await GetTablePrefixForEntityAsync(this, entityType);
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForEntityAsync(IWiserItemsService wiserItemsService, string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                return "";
            }

            var settings = await wiserItemsService.GetEntityTypeSettingsAsync(entityType);
            return wiserItemsService.GetTablePrefixForEntity(settings);
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
        public async Task<LinkSettingsModel> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
        {
            if (linkType <= 0 && String.IsNullOrWhiteSpace(sourceEntityType) && String.IsNullOrWhiteSpace(destinationEntityType))
            {
                throw new ArgumentException($"You must enter a value in at least one of the following parameters: {nameof(linkType)}, {nameof(sourceEntityType)}, {nameof(destinationEntityType)}");
            }

            var whereClause = new List<string>();
            if (linkType > 0)
            {
                databaseConnection.AddParameter("type", linkType);
                whereClause.Add("type = ?type");
            }

            if (!String.IsNullOrWhiteSpace(sourceEntityType))
            {
                databaseConnection.AddParameter("sourceEntityType", sourceEntityType);
                whereClause.Add("connected_entity_type = ?sourceEntityType");
            }

            if (!String.IsNullOrWhiteSpace(destinationEntityType))
            {
                databaseConnection.AddParameter("destinationEntityType", destinationEntityType);
                whereClause.Add("destination_entity_type = ?destinationEntityType");
            }

            var query = $@"SELECT * FROM {WiserTableNames.WiserLink} WHERE {String.Join(" AND ", whereClause)}";

            var dataTable = await databaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? new LinkSettingsModel() : DataRowToLinkSettingsModel(dataTable.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<LinkSettingsModel>> GetAllLinkTypeSettingsAsync()
        {
            var allLinkSettings = new List<LinkSettingsModel>();

            var query = $@"SELECT * FROM {WiserTableNames.WiserLink} ORDER BY name";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return allLinkSettings;
            }

            allLinkSettings.AddRange(dataTable.Rows.Cast<DataRow>().Select(DataRowToLinkSettingsModel));

            return allLinkSettings;
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId)
        {
            return await GetLinkTypeSettingsByIdAsync(this, linkId);
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(IWiserItemsService wiserItemsService, int linkId)
        {
            IEnumerable<LinkSettingsModel> result = await wiserItemsService.GetAllLinkTypeSettingsAsync();
            if (linkId > 0)
            {
                result = result.Where(t => t.Id == linkId);
            }

            return result.FirstOrDefault() ?? new LinkSettingsModel();
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForLinkAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
        {
            return await GetTablePrefixForLinkAsync(this, linkType, sourceEntityType, destinationEntityType);
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForLinkAsync(IWiserItemsService wiserItemsService, int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
        {
            if (linkType == 0 && String.IsNullOrEmpty(sourceEntityType) && String.IsNullOrEmpty(destinationEntityType))
            {
                return "";
            }

            var linkTypeSettings = await wiserItemsService.GetLinkTypeSettingsAsync(linkType, sourceEntityType, destinationEntityType);
            return wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
        }

        /// <inheritdoc />
        public string GetTablePrefixForLink(LinkSettingsModel linkTypeSettings)
        {
            return linkTypeSettings == null || !linkTypeSettings.UseDedicatedTable || linkTypeSettings.UseItemParentId ? "" : $"{linkTypeSettings.Type}_";
        }

        /// <inheritdoc />
        public async Task<string> ReplaceHtmlForSavingAsync(string input, bool allowAbsoluteImageUrls = false)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = input.Replace("€", "&euro;");

            // Fix form tags, multiple form tags are not allowed on the page, thus the editor will throw an exception.
            // FIX: Replace the form tag with JFORM and replace it back later
            var tags = new[] { "html", "body", "head", "title", "form", "textarea" }; // , "iframe"
            foreach (var tag in tags)
            {
                output = output.Replace($"<j{tag}", $"<{tag}");
                output = output.Replace($"</j{tag}", $"</{tag}");
            }

            output = output.Replace("</br>", ""); // IE 10+ Fix

            // Determine main domain, using either the "maindomain" object or the "maindomain_wiser" object.
            var mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
            var mainDomainForWiser = await objectsService.FindSystemObjectByDomainNameAsync("maindomain_wiser");
            if (!String.IsNullOrWhiteSpace(mainDomainForWiser))
            {
                mainDomain = mainDomainForWiser;
            }

            // Replace the use of the full domain name for the image if setup.
            var testDomain = await objectsService.FindSystemObjectByDomainNameAsync("testdomainjuice");
            var requireSsl = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("requiressl"), "true", StringComparison.OrdinalIgnoreCase);
            if (!String.IsNullOrWhiteSpace(testDomain))
            {
                output = output.Replace($"src=\"http://{testDomain}",
                    !String.IsNullOrWhiteSpace(mainDomain)
                    ? $"src=\"{(requireSsl ? "https" : "http")}://{mainDomain}"
                    : "src=\"");
                output = output.Replace($"srcset=\"http://{testDomain}",
                    !String.IsNullOrWhiteSpace(mainDomain)
                    ? $"srcset=\"{(requireSsl ? "https" : "http")}://{mainDomain}"
                    : "srcset=\"");
            }

            // If images should be saved with a relative path.
            var saveImagesRelative = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("wiser_save_images_relative"), "true", StringComparison.OrdinalIgnoreCase);
            if (!allowAbsoluteImageUrls && !String.IsNullOrWhiteSpace(mainDomain) && saveImagesRelative)
            {
                output = Regex.Replace(output, $@"src=""https?://{Regex.Escape(mainDomain)}", "src=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"src=""//{Regex.Escape(mainDomain)}", "src=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"srcset=""https?://{Regex.Escape(mainDomain)}", "srcset=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"srcset=""//{Regex.Escape(mainDomain)}", "srcset=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }
            if (!allowAbsoluteImageUrls && !String.IsNullOrWhiteSpace(mainDomainForWiser) && saveImagesRelative)
            {
                output = Regex.Replace(output, $@"src=""https?://{Regex.Escape(mainDomainForWiser)}", "src=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"src=""//{Regex.Escape(mainDomainForWiser)}", "src=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"srcset=""https?://{Regex.Escape(mainDomainForWiser)}", "srcset=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                output = Regex.Replace(output, $@"srcset=""//{Regex.Escape(mainDomainForWiser)}", "srcset=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }

            // Make extra sure there's no juicedev domain saved in the image URLs.
            output = Regex.Replace(output, @"src=""http://.+?\.juicedev\.nl", "src=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            output = Regex.Replace(output, @"srcset=""http://.+?\.juicedev\.nl", "srcset=\"", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));

            Regex regex;
            if (gclSettings.UseLegacyWiser1TemplateModule)
            {
                regex = new Regex(@"<table[^>]*?(?:data=['""](?<data>.*?)['""][^>]*?)?(contentid|pageid|item-id)=['""](?<contentId>\d+)['""][^>]*?>.+<\/table>", RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                var matchResults = regex.Match(output);
                while (matchResults.Success)
                {
                    var contentId = Int32.Parse(matchResults.Groups[3].Value);
                    var type = matchResults.Groups[1].Value;
                    output = output.Replace(GetFullTableHtml(matchResults.Value, matchResults), $"<img src=\"/preview_image.aspx?{type}={contentId}\" {type}=\"{contentId}\" />");

                    matchResults = regex.Match(output);
                }
            }

            output = output.Replace("~~table", "<table");

            // Replace Office Word HTML XML.
            output = Regex.Replace(output, @"<\!--\[if.*?mso.*?\]>.*?<\!\[endif\]-->", "", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
            
            // Replace data selectors.
            regex = new Regex(@"<!-- Start data selector with id (?<dataSelectorId>\d+) and template (?<templateId>\d+) -->.+?<!-- End data selector with id \d+ and template \d+ -->", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
            var matches = regex.Matches(output);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var dataSelectorId = match.Groups["dataSelectorId"].Value;
                var templateId = match.Groups["templateId"].Value;

                output = output.Replace(match.Value, $"<div class=\"dynamic-content\" data-selector-id=\"{dataSelectorId}\" template-id=\"{templateId}\"><h2>Data selector</h2></div>");
            }
            
            // Replace entity blocks.
            regex = new Regex(@"<!-- Start entity block with id (?<itemId>\d+) -->.+?<!-- End entity block with id \d+ -->", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(200));
            matches = regex.Matches(output);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var itemId = match.Groups["itemId"].Value;

                output = output.Replace(match.Value, $"<div class=\"dynamic-content\" entity-block-item-id=\"{itemId}\"><h2>Entity block</h2></div>");
            }

            return output;
        }

        /// <inheritdoc />
        public async Task<string> ReplaceHtmlForViewingAsync(string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = input;
            // Fix form tags, multiple form tags are not allowed on the page, thus the editor will throw an exception.
            // FIX: Replace the form tag with JFORM and replace it back later
            var tags = new[] { "html", "body", "head", "title", "form", "textarea" }; // , "iframe"
            foreach (var tag in tags)
            {
                output = output.Replace($"<{tag}", $"<j{tag}");
                output = output.Replace($"</{tag}", $"</j{tag}");
            }

            // Get the domain that will be used to prefix the image URLs
            var imagesDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain_wiser");
            if (String.IsNullOrEmpty(imagesDomain))
            {
                imagesDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
            }

            output = await dataSelectorsService.ReplaceAllDataSelectorsAsync(output);
            output = await ReplaceAllEntityBlocksAsync(output);
            
            output = await ReplaceRelativeImagesToAbsoluteAsync(output, imagesDomain);

            return output;
        }

        /// <inheritcDoc />
        public async Task<string> ReplaceRelativeImagesToAbsoluteAsync(string input, string imagesDomain)
        {
            var output = input;
            
            if (!String.IsNullOrWhiteSpace(imagesDomain))
            {
                if (!imagesDomain.StartsWith("//") && !imagesDomain.StartsWith("http"))
                {
                    imagesDomain = $"//{imagesDomain}";
                }

                if (!imagesDomain.EndsWith("/"))
                {
                    imagesDomain += "/";
                }

                output = output.Replace("src=\"//", "src=\"~//").Replace("srcset=\"//", "srcset=\"~//");
                output = output.Replace("src=\"/", $"src=\"{imagesDomain}").Replace("srcset=\"/", $"srcset=\"{imagesDomain}");
                output = output.Replace("src=\"~//", "src=\"//").Replace("srcset=\"~//", "srcset=\"//");
            }

            // Replace with HTTPS
            foreach (Match imageMatch in Regex.Matches(output, @"<img.*?src=[""'](http:\/\/.*?)[""']", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                output = output.Replace(imageMatch.Groups[1].Value, imageMatch.Groups[1].Value.Replace("http://", "//"));
            }
            foreach (Match imageMatch in Regex.Matches(output, @"<source.*?srcset=[""'](http:\/\/.*?)[""']", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                output = output.Replace(imageMatch.Groups[1].Value, imageMatch.Groups[1].Value.Replace("http://", "//"));
            }

            return output;
        }

        /// <inheritdoc />
        public async Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(string entityType = null, int linkType = 0)
        {
            return await GetAggregationSettingsAsync(this, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(IWiserItemsService wiserItemsService, string entityType = null, int linkType = 0)
        {
            if (String.IsNullOrWhiteSpace(entityType) && linkType <= 0)
            {
                throw new ArgumentException("Entity type and link type are both empty.");
            }

            var results = new List<WiserItemPropertyAggregateOptionsModel>();
            databaseConnection.AddParameter("entityType", entityType);
            databaseConnection.AddParameter("linkType", linkType);

            var whereClause = new List<string>();
            if (!String.IsNullOrWhiteSpace(entityType))
            {
                whereClause.Add("entity_name = ?entityType");
            }

            if (linkType > 0)
            {
                whereClause.Add("link_type = ?linkType");
            }

            var query = $@"SELECT property_name, display_name, language_code, aggregate_options, inputtype, entity_name, link_type FROM {WiserTableNames.WiserEntityProperty} WHERE ({String.Join(" OR ", whereClause)}) AND enable_aggregation = 1";
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return results;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var optionsModel = new WiserItemPropertyAggregateOptionsModel();
                var options = dataRow.Field<string>("aggregate_options");
                if (!String.IsNullOrWhiteSpace(options))
                {
                    optionsModel = JsonConvert.DeserializeObject<WiserItemPropertyAggregateOptionsModel>(options) ?? new WiserItemPropertyAggregateOptionsModel();
                }

                optionsModel.EntityType = dataRow.Field<string>("entity_name");
                optionsModel.LinkType = dataRow.Field<int>("link_type");
                
                // Use a default table name if none is set in the options.
                if (String.IsNullOrWhiteSpace(optionsModel.TableName))
                {
                    if (optionsModel.LinkType > 0)
                    {
                        var linkTypeSettings = await wiserItemsService.GetLinkTypeSettingsAsync(optionsModel.LinkType);
                        if (!String.IsNullOrWhiteSpace(linkTypeSettings?.DestinationEntityType) && !String.IsNullOrWhiteSpace(linkTypeSettings.SourceEntityType))
                        {
                            optionsModel.TableName = $"aggregate_{linkTypeSettings.SourceEntityType.ToMySqlSafeValue(false)}_to_{linkTypeSettings.DestinationEntityType.ToMySqlSafeValue(false)}";
                        }
                        else
                        {
                            optionsModel.TableName = $"aggregate_link_{optionsModel.LinkType}";
                        }
                    }
                    else
                    {
                        optionsModel.TableName = $"aggregate_{optionsModel.EntityType.ToMySqlSafeValue(false)}";
                    }
                }

                // Get property name and use display name if there is no property name.
                optionsModel.PropertyName = dataRow.Field<string>("property_name");
                if (String.IsNullOrWhiteSpace(optionsModel.PropertyName))
                {
                    optionsModel.PropertyName = dataRow.Field<string>("display_name");
                }
                
                optionsModel.LanguageCode = dataRow.Field<string>("language_code");

                // If there is no column name set, use the property name as column name.
                if (String.IsNullOrWhiteSpace(optionsModel.ColumnName))
                {
                    optionsModel.ColumnName = optionsModel.PropertyName;
                    if (!String.IsNullOrWhiteSpace(optionsModel.LanguageCode))
                    {
                        optionsModel.ColumnName += $"_{optionsModel.LanguageCode}";
                    }
                }
                
                // Setup the column settings.
                optionsModel.ColumnSettings = new ColumnSettingsModel
                {
                    Name = optionsModel.ColumnName
                };

                var inputType = dataRow.Field<string>("inputtype") ?? "";
                switch (inputType.ToLowerInvariant())
                {
                    case "secure-input":
                    case "input":
                    case "radiobutton":
                    case "combobox":
                    case "multiselect":
                    case "gpslocation":
                    case "daterange":
                    case "color-picker":
                    case "qr":
                        optionsModel.ColumnSettings.Type = MySqlDbType.VarChar;
                        optionsModel.ColumnSettings.Length = 255;
                        break;
                    case "textbox":
                    case "htmleditor":
                        optionsModel.ColumnSettings.Type = MySqlDbType.MediumText;
                        break;
                    case "checkbox":
                        optionsModel.ColumnSettings.Type = MySqlDbType.Int16;
                        optionsModel.ColumnSettings.Length = 1;
                        optionsModel.ColumnSettings.DefaultValue = "0";
                        optionsModel.ColumnSettings.NotNull = true;
                        break;
                    case "numeric-input":
                    case "auto-increment":
                        optionsModel.ColumnSettings.Type = MySqlDbType.Decimal;
                        optionsModel.ColumnSettings.Length = 11;
                        break;
                    case "date-time picker":
                        optionsModel.ColumnSettings.Type = MySqlDbType.DateTime;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputType), inputType, $"Field with input type '{inputType}' is not supported for aggregation");
                }


                results.Add(optionsModel);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task HandleItemAggregationAsync(WiserItemModel wiserItem, string encryptionKey = "")
        {
            await HandleItemAggregationAsync(this, wiserItem);
        }

        /// <inheritdoc />
        public async Task HandleItemAggregationAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, string encryptionKey = "")
        {
            if (wiserItem == null)
            {
                throw new ArgumentNullException(nameof(wiserItem));
            }

            // Get options from fields. Some fields need to be saved differently based on what options are set.
            var entityTypeSettings = await GetEntityTypeSettingsAsync(wiserItem.EntityType);
            var fieldOptions = entityTypeSettings.FieldOptions;

            // Get the settings for aggregation.
            var settings = await wiserItemsService.GetAggregationSettingsAsync(wiserItem.EntityType);
            var detailsForLinks = wiserItem.Details.Where(detail => detail.ItemLinkId > 0).ToList();
            
            // Get aggregation settings for all link types.
            foreach (var linkType in detailsForLinks.Where(detail => detail.LinkType > 0).Select(detail => detail.LinkType).Distinct())
            {
                if (settings.Any(setting => setting.LinkType == linkType))
                {
                    continue;
                }

                settings.AddRange(await wiserItemsService.GetAggregationSettingsAsync(linkType: linkType));
            }
            
            // If we don't have a link type, try to get the link type from wiser_itemlink and then the aggregation settings for that.
            foreach (var itemLinkId in detailsForLinks.Where(detail => detail.LinkType <= 0).Select(detail => detail.ItemLinkId).Distinct())
            {
                databaseConnection.AddParameter("linkId", itemLinkId);
                var dataTable = await databaseConnection.GetAsync($"SELECT type FROM {WiserTableNames.WiserItemLink} WHERE id = ?linkId");
                if (dataTable.Rows.Count == 0)
                {
                    continue;
                }

                var linkType = dataTable.Rows[0].Field<int>("type");
                if (settings.Any(setting => setting.LinkType == linkType))
                {
                    continue;
                }

                settings.AddRange(await wiserItemsService.GetAggregationSettingsAsync(linkType: linkType));
            }

            if (!settings.Any())
            {
                return;
            }

            // Create tables if they don't exist yet.
            foreach (var tableName in settings.Select(setting => setting.TableName).Distinct())
            {
                if (await databaseHelpersService.TableExistsAsync(tableName))
                {
                    continue;
                }

                await databaseHelpersService.CreateTableAsync(tableName, new List<ColumnSettingsModel> { new("id", MySqlDbType.UInt64) });
                await databaseHelpersService.AddColumnToTableAsync(tableName, new ColumnSettingsModel("title", MySqlDbType.VarChar, 255));
                
                var settingsForThisTable = settings.Where(setting => setting.TableName == tableName).ToList();
                if (settingsForThisTable.First().LinkType > 0)
                {
                    await databaseHelpersService.AddColumnToTableAsync(tableName, new ColumnSettingsModel("source_item_id", MySqlDbType.UInt64));
                    await databaseHelpersService.AddColumnToTableAsync(tableName, new ColumnSettingsModel("destination_item_id", MySqlDbType.UInt64));
                    await databaseHelpersService.AddColumnToTableAsync(tableName, new ColumnSettingsModel("link_type", MySqlDbType.Int32));
                }

                foreach (var setting in settingsForThisTable)
                {
                    await databaseHelpersService.AddColumnToTableAsync(tableName, setting.ColumnSettings);

                    var fieldOptionsToUse = fieldOptions;

                    if (setting.LinkType > 0)
                    {
                        fieldOptionsToUse = await wiserItemsService.GetFieldOptionsForLinkFieldsAsync(setting.LinkType);
                    }
                    
                    var key = $"{setting.PropertyName}_{setting.LanguageCode}";
                    if (fieldOptionsToUse == null || !fieldOptionsToUse.ContainsKey(key))
                    {
                        continue;
                    }

                    var options = fieldOptionsToUse[key];
                    if (!(bool)options[SaveSeoValueKey])
                    {
                        continue;
                    }
                    
                    await databaseHelpersService.AddColumnToTableAsync(tableName, new ColumnSettingsModel($"{setting.ColumnSettings.Name}{SeoPropertySuffix}", setting.ColumnSettings.Type, setting.ColumnSettings.Length, setting.ColumnSettings.Decimals, setting.ColumnSettings.DefaultValue, setting.ColumnSettings.NotNull));
                }
            }

            // Insert and/or update data in the table.
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", wiserItem.Id);
            databaseConnection.AddParameter("title", wiserItem.Title);
            var columnsForQuery = new Dictionary<string, List<string>>();
            var parametersForQuery = new Dictionary<string, List<string>>();
            var counter = 0;
            foreach (var setting in settings)
            {
                if (!columnsForQuery.ContainsKey(setting.TableName))
                {
                    columnsForQuery.Add(setting.TableName, new List<string> { "id", "title" });
                    parametersForQuery.Add(setting.TableName, new List<string> { "?id", "?title" });
                }
                
                var itemDetail = wiserItem.Details.FirstOrDefault(detail => String.Equals(detail.Key, setting.PropertyName, StringComparison.OrdinalIgnoreCase) && String.Equals(detail.LanguageCode ?? "", setting.LanguageCode ?? "", StringComparison.OrdinalIgnoreCase));
                if (itemDetail == null)
                {
                    continue;
                }

                if (setting.LinkType <= 0)
                {
                    databaseConnection.AddParameter("id", wiserItem.Id);
                }
                else
                {
                    var linkTablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(setting.LinkType);
                    
                    databaseConnection.AddParameter("id", itemDetail.ItemLinkId);
                    databaseConnection.AddParameter("linkType", setting.LinkType);
                    columnsForQuery[setting.TableName].Add("link_type");
                    parametersForQuery[setting.TableName].Add("?linkType");

                    var dataTable = await databaseConnection.GetAsync($"SELECT item_id, destination_item_id FROM {linkTablePrefix}{WiserTableNames.WiserItemLink} WHERE id = ?id");
                    if (dataTable.Rows.Count > 0)
                    {
                        databaseConnection.AddParameter("sourceItemId", dataTable.Rows[0]["item_id"]);
                        columnsForQuery[setting.TableName].Add("source_item_id");
                        parametersForQuery[setting.TableName].Add("?sourceItemId");

                        databaseConnection.AddParameter("destinationItemId", dataTable.Rows[0]["destination_item_id"]);
                        columnsForQuery[setting.TableName].Add("destination_item_id");
                        parametersForQuery[setting.TableName].Add("?destinationItemId");
                    }
                }

                columnsForQuery[setting.TableName].Add($"`{setting.ColumnName.ToMySqlSafeValue(false)}`");

                var (useLongValueColumn, _, deleteValue, alsoSaveSeoValue) = await AddValueParameterToConnectionAsync(counter, itemDetail, fieldOptions, new List<WiserItemDetailModel>(), encryptionKey, true);
                parametersForQuery[setting.TableName].Add(deleteValue ? "NULL" : $"?{(useLongValueColumn ? "longValue" : "value")}{counter}");

                if (alsoSaveSeoValue)
                {
                    columnsForQuery[setting.TableName].Add($"`{setting.ColumnName.ToMySqlSafeValue(false)}{SeoPropertySuffix}`");
                    parametersForQuery[setting.TableName].Add(deleteValue ? "NULL" : $"?{(useLongValueColumn ? "longValue" : "value")}{SeoPropertySuffix}{counter}");
                }

                counter++;
            }

            var query = String.Join(";", columnsForQuery.Select(x => 
                $@"INSERT INTO `{x.Key}` ({String.Join(",", x.Value)})
                VALUES ({String.Join(",", parametersForQuery[x.Key])})
                ON DUPLICATE KEY UPDATE {String.Join(",", x.Value.Select(column => $"{column} = VALUES({column})"))}"));
            
            await databaseConnection.ExecuteAsync(query);
            
            // Handle any aggregation functions.
            foreach (var setting in settings.Where(setting => setting.AggregationMethods != null))
            {
                foreach (var aggregationMethod in setting.AggregationMethods)
                {
                    var parentItems = await wiserItemsService.GetLinkedItemDetailsAsync(wiserItem.Id, aggregationMethod.ParentLinkType, reverse: true, skipPermissionsCheck: true);
                    foreach (var parentItem in parentItems)
                    {
                        var parentAggregationSettings = await wiserItemsService.GetAggregationSettingsAsync(parentItem.EntityType);
                        if (parentAggregationSettings == null || !parentAggregationSettings.Any())
                        {
                            continue;
                        }

                        foreach (var parentTableName in parentAggregationSettings.Select(x => x.TableName).Distinct())
                        {
                            if (String.IsNullOrWhiteSpace(parentTableName))
                            {
                                continue;
                            }

                            var aggregateColumnName = $"{wiserItem.EntityType}_{setting.ColumnName}_{aggregationMethod.Method.ToString()}".ToLowerInvariant();
                            if (!await databaseHelpersService.ColumnExistsAsync(parentTableName, aggregateColumnName))
                            {
                                await databaseHelpersService.AddColumnToTableAsync(parentTableName, new ColumnSettingsModel(aggregateColumnName, setting.ColumnSettings.Type, setting.ColumnSettings.Length, setting.ColumnSettings.Decimals, setting.ColumnSettings.DefaultValue, setting.ColumnSettings.NotNull));
                            }

                            var allChildren = await wiserItemsService.GetLinkedItemDetailsAsync(parentItem.Id, aggregationMethod.ParentLinkType, skipPermissionsCheck: true);

                            var value = aggregationMethod.Method switch
                            {
                                WiserItemPropertyAggregateMethods.None => wiserItem.GetDetailValue<decimal>(setting.PropertyName),
                                WiserItemPropertyAggregateMethods.Sum => allChildren.Sum(child => child.Id == wiserItem.Id ? wiserItem.GetDetailValue<decimal>(setting.PropertyName) : child.GetDetailValue<decimal>(setting.PropertyName)),
                                WiserItemPropertyAggregateMethods.Min => allChildren.Min(child => child.Id == wiserItem.Id ? wiserItem.GetDetailValue<decimal>(setting.PropertyName) : child.GetDetailValue<decimal>(setting.PropertyName)),
                                WiserItemPropertyAggregateMethods.Max => allChildren.Max(child => child.Id == wiserItem.Id ? wiserItem.GetDetailValue<decimal>(setting.PropertyName) : child.GetDetailValue<decimal>(setting.PropertyName)),
                                WiserItemPropertyAggregateMethods.Average => allChildren.Average(child => child.Id == wiserItem.Id ? wiserItem.GetDetailValue<decimal>(setting.PropertyName) : child.GetDetailValue<decimal>(setting.PropertyName)),
                                _ => throw new ArgumentOutOfRangeException(nameof(aggregationMethod.Method), aggregationMethod.Method.ToString())
                            };

                            databaseConnection.AddParameter("parentId", parentItem.Id);
                            databaseConnection.AddParameter("value", value);
                            query = $"UPDATE `{parentTableName}` SET `{aggregateColumnName}` = ?value WHERE id = ?parentId";
                            await databaseConnection.ExecuteAsync(query);
                        }
                    }
                }
            }
        }
        
        /// <inheritdoc />
        public async Task<string> ReplaceAllEntityBlocksAsync(string template)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            // Entity blocks with templates.
            var regEx = new Regex(@"<div[^<>]*?(?:class=['""]dynamic-content['""][^<>]*?)?(entity-block-item-id)=['""](?<itemId>\d+)['""]([^<>]*?)?>[^<>]*?<h2>[^<>]*?(?<title>[^<>]*?)<\/h2>[^<>]*?<\/div>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMinutes(3));

            var matches = regEx.Matches(template);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!UInt64.TryParse(match.Groups["itemId"].Value, out var itemId) || itemId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid dataSelectorId of '{match.Groups["dataSelectorId"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    
                    await databaseConnection.EnsureOpenConnectionForReadingAsync();
                    var (entityTemplate, dataRow) = await GetTemplateAndDataForItemAsync(itemId);
                    var html = await stringReplacementsService.DoAllReplacementsAsync(entityTemplate, dataRow, removeUnknownVariables: false);

                    template = template.Replace(match.Value, $"<!-- Start entity block with id {itemId} -->{html}<!-- End entity block with id {itemId} -->");
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error while generating entity block with id '{itemId}'': {exception}");
                    var errorOnPage = $"An error occurred while generating entity block with id '{itemId}'";
                    if (gclSettings.Environment is Environments.Development or Environments.Test)
                    {
                        errorOnPage += $": {exception.Message}";
                    }

                    template = template.Replace(match.Value, errorOnPage);
                }
            }

            return template;
        }

        #endregion

        #region Static methods of this class

        /// <summary>
        /// This will get the full table HTML, including any possible sub tables.
        /// We need this to find the end of the current table, since we couldn't find a way to do this via only RegEx.
        /// With the RegEx we always either get too much HTML, or too little.
        /// </summary>
        /// <param name="html">The HTML get get the table in.</param>
        /// <param name="match">The original regular expression match.</param>
        /// <returns>The full table to replace with an image.</returns>
        private static string GetFullTableHtml(string html, Match match)
        {
            var startIndex = html.IndexOf(match.Value, StringComparison.OrdinalIgnoreCase);
            var endIndex = 0;
            var value = match.Groups[0].Value;

            var tableCount = 0;
            var currentString = "";

            for (var i = 0; i <= value.Length - 1; i++)
            {
                currentString += value[i];
                if ("</table>".Equals(currentString))
                {
                    tableCount -= 1;
                    currentString = "";
                    // Found close tag
                    if (tableCount == 0)
                    {
                        endIndex = i + startIndex + 1;
                        break;
                    }
                }
                else if ("<table".Equals(currentString))
                {
                    tableCount += 1;
                    currentString = "";
                }
                else if ("<table".StartsWith(currentString) || "</table>".StartsWith(currentString))
                {
                    // Do Nothing
                }
                else
                {
                    currentString = "";
                }
            }

            return html.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// Add an <see cref="WiserItemDetailModel"/> to an <see cref="WiserItemModel"/>, from a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="wiserItem"></param>
        /// <param name="dataRow"></param>
        private static void AddDetailFromDataRow(WiserItemModel wiserItem, DataRow dataRow)
        {
            var key = dataRow.Field<string>("key");
            if (String.IsNullOrWhiteSpace(key))
            {
                return;
            }

            wiserItem.Details.Add(new WiserItemDetailModel
            {
                Key = key,
                Value = dataRow["value"],
                LanguageCode = dataRow.Field<string>("language_code"),
                Changed = false
            });
        }

        /// <summary>
        /// Fills an existing <see cref="WiserItemModel"/> with data from a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="wiserItem"></param>
        /// <param name="dataRow"></param>
        private static WiserItemModel DataRowToItem(DataRow dataRow)
        {
            var wiserItem = new WiserItemModel();
            wiserItem.Id = dataRow.Field<ulong>("id");
            if (dataRow.Table.Columns.Contains("original_item_id"))
            {
                wiserItem.OriginalItemId = Convert.ToUInt64(dataRow["original_item_id"]);
            }
            if (dataRow.Table.Columns.Contains("parent_item_id"))
            {
                wiserItem.ParentItemId = Convert.ToUInt64(dataRow["parent_item_id"]);
            }
            wiserItem.AddedBy = dataRow.Field<string>("added_by");
            wiserItem.AddedOn = dataRow.Field<DateTime>("added_on");
            wiserItem.ChangedBy = dataRow.Field<string>("changed_by");
            if (!dataRow.IsNull("changed_on"))
            {
                wiserItem.ChangedOn = dataRow.Field<DateTime>("changed_on");
            }

            wiserItem.EntityType = dataRow.Field<string>("entity_type");
            wiserItem.ModuleId = dataRow.Field<int>("moduleid");
            wiserItem.PublishedEnvironment = (Environments)dataRow.Field<int>("published_environment");
            wiserItem.ReadOnly = Convert.ToInt32(dataRow["readonly"]) > 0;
            wiserItem.Removed = dataRow.Table.Columns.Contains("removed") && Convert.ToInt32(dataRow["removed"]) > 0;
            wiserItem.Title = dataRow.Field<string>("title");
            wiserItem.UniqueUuid = dataRow.Field<string>("unique_uuid");

            wiserItem.Changed = false;
            return wiserItem;
        }

        /// <summary>
        /// Fills an existing <see cref="WiserItemFileModel"/> with data from a <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dataRow"></param>
        public static WiserItemFileModel DataRowToItemFile(DataRow dataRow)
        {
            return new WiserItemFileModel
            {
                Id = Convert.ToUInt64(dataRow["id"]),
                AddedBy = dataRow.Field<string>("added_by"),
                AddedOn = dataRow.Field<DateTime>("added_on"),
                ItemId = Convert.ToUInt64(dataRow["item_id"]),
                ItemLinkId = Convert.ToUInt64(dataRow["itemlink_id"]),
                ContentType = dataRow.Field<string>("content_type"),
                Content = dataRow.Field<byte[]>("content"),
                ContentUrl = dataRow.Field<string>("content_url"),
                Width = Convert.ToInt32(dataRow["width"]),
                Height = Convert.ToInt32(dataRow["height"]),
                FileName = dataRow.Field<string>("file_name"),
                Extension = dataRow.Field<string>("extension"),
                Title = dataRow.Field<string>("title"),
                PropertyName = dataRow.Field<string>("property_name"),
                ExtraData = dataRow.IsNull("extra_data") ? null : JsonConvert.DeserializeObject<WiserItemFileExtraDataModel>(dataRow.Field<string>("extra_data")!)
            };
        }

        /// <summary>
        /// This function adds parameters "value" and "longValue" to the <see cref="IDatabaseConnection"/>, that can be used in a query.
        /// This will format different types as a string to save in wiser_itemdetail.
        /// </summary>
        /// <param name="counter">The counter, for creating unique parameter names.</param>
        /// <param name="wiserItemDetail">The <see cref="WiserItemDetailModel"/> with the key, value etc.</param>
        /// <param name="fieldOptions">The Wiser 2.0 options for the field that is being used.</param>
        /// <param name="previousItemDetails">A list of details as the item originally was, before updating it.</param>
        /// <param name="encryptionKey">The encryption key used for encrypting values for secure-input fields.</param>
        /// <returns></returns>
        private async Task<(bool useLongValueColumn, bool valueChanged, bool deleteValue, bool alsoSaveSeoValue)> AddValueParameterToConnectionAsync(int counter, WiserItemDetailModel wiserItemDetail, IReadOnlyDictionary<string, Dictionary<string, object>> fieldOptions, IEnumerable<WiserItemDetailModel> previousItemDetails, string encryptionKey, bool alwaysSaveValues)
        {
            var useLongValueColumn = false;
            var deleteValue = false;
            bool valueChanged;
            var alsoSaveSeoValue = false;
            var options = new Dictionary<string, object>();
            var key = $"{wiserItemDetail.Key}_{wiserItemDetail.LanguageCode}";
            if (fieldOptions != null && fieldOptions.ContainsKey(key))
            {
                options = fieldOptions[key];
            }

            var hasGroupName = !String.IsNullOrWhiteSpace(wiserItemDetail.GroupName);
            var previousFields = previousItemDetails.Where(x =>
                x.Id == wiserItemDetail.Id ||
                (
                    x.IsLinkProperty == wiserItemDetail.IsLinkProperty &&
                    x.ItemLinkId == wiserItemDetail.ItemLinkId &&
                    String.Equals(x.Key, wiserItemDetail.Key, StringComparison.OrdinalIgnoreCase)
                )).ToList();

            WiserItemDetailModel previousField = null;

            // If we don't have a group name, we only want to compare a field with the same language code, because the language code can't be changed for normal fields anyway and we don't want to accidentally overwrite the wrong language code.
            if (!hasGroupName)
            {
                previousField = previousFields.FirstOrDefault(f => f.Id == wiserItemDetail.Id || String.Equals(f.LanguageCode, wiserItemDetail.LanguageCode, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // If we do have a group name, only get a field with the same group name and then figure out which field we're changing, if there are multiple. Because the language code for grouped fields can be changed in Wiser.
                previousFields = previousFields.Where(f => String.Equals(f.GroupName, wiserItemDetail.GroupName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (previousFields.Count == 1)
                {
                    previousField = previousFields.Single();
                }
                else if (previousFields.Count > 1)
                {
                    if (wiserItemDetail.Id > 0)
                    {
                        previousField = previousFields.FirstOrDefault(f => f.Id == wiserItemDetail.Id);
                    }

                    if (previousField == null)
                    {
                        previousField = previousFields.FirstOrDefault(f => String.Equals(f.LanguageCode, wiserItemDetail.LanguageCode, StringComparison.OrdinalIgnoreCase));
                    }

                    if (previousField == null)
                    {
                        previousField = previousFields.FirstOrDefault();
                    }
                }

                if (previousField != null && wiserItemDetail.Id == 0)
                {
                    wiserItemDetail.Id = previousField.Id;
                }
            }

            switch (wiserItemDetail.Value)
            {
                case "":
                case null:
                {
                    valueChanged = !String.IsNullOrEmpty(previousField?.Value?.ToString()) || !String.Equals(wiserItemDetail.GroupName, previousField?.GroupName, StringComparison.OrdinalIgnoreCase);

                    if (String.IsNullOrWhiteSpace(wiserItemDetail.GroupName) || String.IsNullOrWhiteSpace(wiserItemDetail.Key))
                    {
                        // Empty values will be deleted from database, so no need to add a parameter to the connection.
                        deleteValue = true;
                    }
                    else
                    {
                        databaseConnection.AddParameter($"value{counter}", "");
                        databaseConnection.AddParameter($"longValue{counter}", "");
                    }

                    break;
                }

                case JArray valueAsJsonArray:
                {
                    var valueAsList = valueAsJsonArray.Cast<object>().ToList();
                    var value = String.Join(",", valueAsList);
                    valueChanged = previousField?.Value?.ToString() != value;
                    useLongValueColumn = value.Length > 1000;
                    databaseConnection.AddParameter($"value{counter}", useLongValueColumn ? "" : value);
                    databaseConnection.AddParameter($"longValue{counter}", useLongValueColumn ? value : "");

                    if ((valueChanged || alwaysSaveValues) && options.Any() && (bool) options[SaveSeoValueKey])
                    {
                        value = String.Join(",", valueAsList.Select(v => v.ToString().ConvertToSeo()));
                        databaseConnection.AddParameter($"value{SeoPropertySuffix}{counter}", useLongValueColumn ? "" : value);
                        databaseConnection.AddParameter($"longValue{SeoPropertySuffix}{counter}", useLongValueColumn ? value : "");
                        alsoSaveSeoValue = true;
                    }

                    break;
                }

                default:
                {
                    valueChanged = previousField?.Value?.ToString() != wiserItemDetail.Value?.ToString();
                    useLongValueColumn = wiserItemDetail.Value?.ToString()?.Length > 1000;

                    // Check if we need to adjust the value that gets saved in the database, such as encrypting or hashing it.
                    if ((valueChanged || alwaysSaveValues) && options.Any())
                    {
                        switch (options[FieldTypeKey].ToString().ToLowerInvariant())
                        {
                            case "secure-input":
                            {
                                var securityMethod = "GCL_SHA512";

                                if (options.ContainsKey(SecurityMethodKey))
                                {
                                    securityMethod = options[SecurityMethodKey]?.ToString()?.ToUpperInvariant();
                                }

                                var securityKey = "";
                                if (securityMethod.InList("GCL_AES", "JCL_AES", "AES"))
                                {
                                    if (options.ContainsKey(SecurityKeyKey))
                                    {
                                        securityKey = options[SecurityKeyKey]?.ToString() ?? "";
                                    }

                                    if (String.IsNullOrEmpty(securityKey))
                                    {
                                        securityKey = encryptionKey;
                                    }
                                }

                                switch (securityMethod)
                                {
                                    case "GCL_SHA512":
                                    case "JCL_SHA512":
                                        wiserItemDetail.Value = wiserItemDetail.Value.ToString().ToSha512ForPasswords();
                                        break;
                                    case "GCL_AES":
                                    case "JCL_AES":
                                        wiserItemDetail.Value = wiserItemDetail.Value.ToString().EncryptWithAesWithSalt(securityKey);
                                        break;
                                    case "AES":
                                        wiserItemDetail.Value = wiserItemDetail.Value.ToString().EncryptWithAes(securityKey);
                                        break;
                                    default:
                                        throw new Exception($"Unsupported security method used ({options[SecurityMethodKey]} for field '{wiserItemDetail.Key}' with language '{wiserItemDetail.LanguageCode}'!");
                                }

                                break;
                            }
                            case "date-time picker":
                            {
                                if (!options.ContainsKey("type") || !(wiserItemDetail.Value is DateTime dateTimeValue))
                                {
                                    break;
                                }

                                switch (options["type"]?.ToString()?.ToLowerInvariant())
                                {
                                    case "time":
                                        wiserItemDetail.Value = dateTimeValue.ToString("HH:mm");
                                        break;
                                    case "date":
                                        wiserItemDetail.Value = dateTimeValue.ToString("yyyy-MM-dd");
                                        break;
                                }

                                break;
                            }
                            case "numeric-input":
                            {
                                // Make sure decimal values are always saved with a dot separator.
                                if (wiserItemDetail.Value is decimal valueAsDecimal)
                                {
                                    wiserItemDetail.Value = valueAsDecimal.ToString(new CultureInfo("en-US"));
                                }
                                else if (wiserItemDetail.Value is double valueAsDouble)
                                {
                                    wiserItemDetail.Value = valueAsDouble.ToString(new CultureInfo("en-US"));
                                }
                                else if (wiserItemDetail.Value is string valueAsString
                                         && !String.IsNullOrWhiteSpace(valueAsString)
                                         && valueAsString.Contains(',')
                                         && Decimal.TryParse(valueAsString, NumberStyles.Any, new CultureInfo("nl-NL"), out var parsedValueAsDecimal))
                                {
                                    wiserItemDetail.Value = parsedValueAsDecimal.ToString(new CultureInfo("en-US"));
                                }

                                break;
                            }
                            case "htmleditor":
                            {
                                var valueAsString = wiserItemDetail.Value as string;
                                if (!String.IsNullOrEmpty(valueAsString))
                                {
                                    wiserItemDetail.Value = await ReplaceHtmlForSavingAsync(valueAsString, options.ContainsKey("allowAbsoluteImageUrls") && (options["allowAbsoluteImageUrls"]?.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ?? false));
                                }

                                break;
                            }
                        }

                        if ((bool) options[SaveSeoValueKey])
                        {
                            databaseConnection.AddParameter($"value{SeoPropertySuffix}{counter}", useLongValueColumn ? "" : wiserItemDetail.Value.ToString().ConvertToSeo());
                            databaseConnection.AddParameter($"longValue{SeoPropertySuffix}{counter}", useLongValueColumn ? wiserItemDetail.Value.ToString().ConvertToSeo() : "");
                            alsoSaveSeoValue = true;
                        }
                    }

                    databaseConnection.AddParameter($"value{counter}", useLongValueColumn ? "" : wiserItemDetail.Value);
                    databaseConnection.AddParameter($"longValue{counter}", useLongValueColumn ? wiserItemDetail.Value : "");

                    break;
                }
            }

            // If the value itself hasn't changed, check if the key, language code or group name has been changed, but only if we found the field based on ID.
            if (!valueChanged && previousField != null && previousField.Id == wiserItemDetail.Id)
            {
                valueChanged = !String.Equals(previousField.Key, wiserItemDetail.Key, StringComparison.OrdinalIgnoreCase)
                               || !String.Equals(previousField.GroupName, wiserItemDetail.GroupName, StringComparison.OrdinalIgnoreCase)
                               || !String.Equals(previousField.LanguageCode, wiserItemDetail.LanguageCode, StringComparison.OrdinalIgnoreCase);
            }

            return (useLongValueColumn, valueChanged, deleteValue, alsoSaveSeoValue);
        }

        /// <summary>
        /// Converts a <see cref="DataRow"/> to a  <see cref="LinkSettingsModel"/>.
        /// </summary>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        private static LinkSettingsModel DataRowToLinkSettingsModel(DataRow dataRow)
        {
            var linkSettings = new LinkSettingsModel
            {
                Id = dataRow.Field<int>("id"),
                Type = dataRow.Field<int>("type"),
                Name = dataRow.Field<string>("name"),
                DestinationEntityType = dataRow.Field<string>("destination_entity_type"),
                SourceEntityType = dataRow.Field<string>("connected_entity_type"),
                ShowInDataSelector = Convert.ToBoolean(dataRow["show_in_data_selector"]),
                ShowInTreeView = Convert.ToBoolean(dataRow["show_in_tree_view"]),
                UseItemParentId = dataRow.Table.Columns.Contains("use_item_parent_id") && Convert.ToBoolean(dataRow["use_item_parent_id"])
            };
            
            var relationship = dataRow.Field<string>("relationship");
            var duplication = dataRow.Field<string>("duplication");

            linkSettings.Relationship = relationship switch
            {
                "one-to-one" => LinkRelationships.OneToOne,
                "one-to-many" => LinkRelationships.OneToMany,
                "many-to-many" => LinkRelationships.ManyToMany,
                _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
            };

            linkSettings.DuplicationMethod = duplication switch
            {
                "none" => LinkDuplicationMethods.None,
                "copy-link" => LinkDuplicationMethods.CopyLink,
                "copy-item" => LinkDuplicationMethods.CopyItem,
                _ => throw new ArgumentOutOfRangeException(nameof(duplication), duplication, null)
            };

            if (dataRow.Table.Columns.Contains("use_dedicated_table"))
            {
                linkSettings.UseDedicatedTable = Convert.ToBoolean(dataRow["use_dedicated_table"]);
            }

            if (dataRow.Table.Columns.Contains("cascade_delete"))
            {
                linkSettings.CascadeDelete = Convert.ToBoolean(dataRow["cascade_delete"]);
            }

            return linkSettings;
        }

        #endregion
    }
}