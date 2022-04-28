using System;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Core.Services;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Core.Interfaces
{
    public interface IWiserItemsService
    {
        /// <summary>
        /// Saves an item. If the item has an ID of 0, a new item will be created.
        /// </summary>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="parentId">Optional: The ID of the parent to link the new item to. If NULL, it will not be linked to any item. Use 0 if an item needs to be added to the root of a module. Default is NULL.</param>
        /// <param name="linkTypeNumber">Optional: The link type number for the link to the parent. Default is 1.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the logged in (Wiser) user. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="alwaysSaveValues">Optional: This function gets the current values in the database and only saves values that have been changed. Set this parameter to true to disable that functionality and force the function to always save all values (except read only fields).</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        Task<WiserItemModel> SaveAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true);
        
        /// <summary>
        /// Saves an item. If the item has an ID of 0, a new item will be created.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="parentId">Optional: The ID of the parent to link the new item to. If NULL, it will not be linked to any item. Use 0 if an item needs to be added to the root of a module. Default is NULL.</param>
        /// <param name="linkTypeNumber">Optional: The link type number for the link to the parent. Default is 1.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the logged in (Wiser) user. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="alwaysSaveValues">Optional: This function gets the current values in the database and only saves values that have been changed. Set this parameter to true to disable that functionality and force the function to always save all values (except read only fields).</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        Task<WiserItemModel> SaveAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true);

        /// <summary>
        /// Creates an item.
        /// This will create an empty item, if you want to save item details as well, use the <see cref="WiserItemsService.SaveAsync"/> function instead, or call the <see cref="WiserItemsService.UpdateAsync"/> function after this.
        /// </summary>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="parentId">Optional: The ID of the parent to link the new item to. If NULL, it will not be linked to any item. Use 0 if an item needs to be added to the root of a module. Default is NULL.</param>
        /// <param name="linkTypeNumber">Optional: The link type number for the link to the parent. Default is 1.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the logged in (Wiser) user. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        /// <exception cref="System.ArgumentNullException">If wiserItem or entityType is <see langword="null"/>.</exception>
        Task<WiserItemModel> CreateAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true);

        /// <summary>
        /// Creates an item.
        /// This will create an empty item, if you want to save item details as well, use the <see cref="WiserItemsService.SaveAsync"/> function instead, or call the <see cref="WiserItemsService.UpdateAsync"/> function after this.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="parentId">Optional: The ID of the parent to link the new item to. If NULL, it will not be linked to any item. Use 0 if an item needs to be added to the root of a module. Default is NULL.</param>
        /// <param name="linkTypeNumber">Optional: The link type number for the link to the parent. Default is 1.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the logged in (Wiser) user. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        /// <exception cref="System.ArgumentNullException">If wiserItem or entityType is <see langword="null"/>.</exception>
        Task<WiserItemModel> CreateAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true);

        /// <summary>
        /// Duplicate an item. This could also duplicate links and linked items, depending on the settings in wiser_link.
        /// </summary>
        /// <param name="itemId">The ID of the item to duplicate.</param>
        /// <param name="parentId">The ID of the parent for the duplicated item. This can be the same as the original item, or a different parent.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're duplicating. This is needed for entities that have a dedicated table, instead of wiser_item.</param>
        /// <param name="parentEntityType">Optional: The entity type of the parent of the item that you're duplicating. This is needed for entities that have a dedicated table, instead of wiser_item.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>An <see cref="WiserItemDuplicationResultModel"/> with the results.</returns>
        Task<WiserItemDuplicationResultModel> DuplicateItemAsync(ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Duplicate an item. This could also duplicate links and linked items, depending on the settings in wiser_link.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item to duplicate.</param>
        /// <param name="parentId">The ID of the parent for the duplicated item. This can be the same as the original item, or a different parent.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're duplicating. This is needed for entities that have a dedicated table, instead of wiser_item.</param>
        /// <param name="parentEntityType">Optional: The entity type of the parent of the item that you're duplicating. This is needed for entities that have a dedicated table, instead of wiser_item.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>An <see cref="WiserItemDuplicationResultModel"/> with the results.</returns>
        Task<WiserItemDuplicationResultModel> DuplicateItemAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="itemId">The ID of the item to update.</param>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="alwaysSaveValues">Optional: This function gets the current values in the database and only saves values that have been changed. Set this parameter to true to disable that functionality and force the function to always save all values (except read only fields).</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again.</returns>
        Task<WiserItemModel> UpdateAsync(ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true);

        /// <summary>
        /// Updates an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item to update.</param>
        /// <param name="wiserItem">An <see cref="WiserItemModel"/> with the values to save to the database.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        /// <param name="alwaysSaveValues">Optional: This function gets the current values in the database and only saves values that have been changed. Set this parameter to true to disable that functionality and force the function to always save all values (except read only fields).</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again.</returns>
        Task<WiserItemModel> UpdateAsync(IWiserItemsService wiserItemsService, ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true);

        /// <summary>
        /// Changes an entity type of an item.
        /// </summary>
        /// <param name="itemId">The ID of the item to change entity type from.</param>
        /// <param name="currentEntityType">The name of the entity type that the item currently has.</param>
        /// <param name="newEntityType">The new entity type for the item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> ChangeEntityTypeAsync(ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Deletes or un-deletes an item.
        /// </summary>
        /// <param name="itemId">The ID of the item to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Deletes or un-deletes an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(IWiserItemsService wiserItemsService, ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Deletes or un-deletes items.
        /// Deleting items will move them to an archive table, such as wiser_item_archive.
        /// Undeleting items will move them back into the original table they were in.
        /// </summary>
        /// <param name="itemIds">The list with IDs of the items to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Deletes or un-deletes items.
        /// Deleting items will move them to an archive table, such as wiser_item_archive.
        /// Undeleting items will move them back into the original table they were in.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemIds">The list with IDs of the items to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(IWiserItemsService wiserItemsService, List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Executes the workflow of an item after it has been created or updated.
        /// This executes any query in either 'query_after_insert' or 'query_after_update' from the table 'wiser_entity', depending on the parameter <see cref="isNewItem"/>.
        /// </summary>
        /// <param name="itemId">The ID of the item to execute the workflow for.</param>
        /// <param name="isNewItem">A boolean indicating whether this item was just created (true) or if it was an existing item that has been updated (false).</param>
        /// <param name="entitySettingsModel">Settings for the entity type. Can be retrieved via the function "GetEntityTypeSettings".</param>
        /// <param name="wiserItem">Optional: The <see cref="WiserItemModel"/>. If not null, the details of this item will be replaced in the query.</param>
        /// <param name="userId">Optional: The ID of the logged in Wiser user. Set to 0 if calling this from a website or GCL.</param>
        /// <param name="username">Optional: The name of the logged in (Wiser) user. Default value is "GCL".</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <returns></returns>
        Task<bool> ExecuteWorkflowAsync(ulong itemId, bool isNewItem, EntitySettingsModel entitySettingsModel, WiserItemModel wiserItem = null, ulong userId = 0, string username = "GCL", bool saveHistory = true);

        /// <summary>
        /// Check if a certain action on an item and/or entity is possible.
        /// This will first check the rights of the user and if the user is allowed to execute this action,
        /// it will check other things, like the 'query_before_delete' and 'query_before_update'.
        /// </summary>
        /// <param name="itemId">The ID of the item that is being processed.</param>
        /// <param name="action">The action that the user is executing.</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <param name="wiserItem">Optional: The details of the item. Can be used when updating an item, to check if a new value can be used for example.</param>
        /// <param name="onlyCheckAccessRights">If this is set to true, only access rights will be checked. Any extra stuff, such as "query_before_delete" will be ignored.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        /// <returns>A Tuple with a boolean and an error message. If 'ok' is 'true', the action is allowed. Otherwise it's not and the reason will be given in the error message.</returns>
        Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null);

        /// <summary>
        /// Check if a certain action on an item and/or entity is possible.
        /// This will first check the rights of the user and if the user is allowed to execute this action,
        /// it will check other things, like the 'query_before_delete' and 'query_before_update'.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item that is being processed.</param>
        /// <param name="action">The action that the user is executing.</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <param name="wiserItem">Optional: The details of the item. Can be used when updating an item, to check if a new value can be used for example.</param>
        /// <param name="onlyCheckAccessRights">If this is set to true, only access rights will be checked. Any extra stuff, such as "query_before_delete" will be ignored.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        /// <returns>A Tuple with a boolean and an error message. If 'ok' is 'true', the action is allowed. Otherwise it's not and the reason will be given in the error message.</returns>
        Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(IWiserItemsService wiserItemsService, ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null);

        /// <summary>
        /// Get item permissions for a user. This can be used for Wiser users or website users.
        /// </summary>
        /// <param name="itemId">The ID of the item that that you want to have the permissions of..</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        /// <returns><see cref="AccessRights"/></returns>
        Task<AccessRights> GetUserItemPermissionsAsync(ulong itemId, ulong userId, string entityType = null);

        /// <summary>
        /// Get item permissions for a user. This can be used for Wiser users or website users.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item that that you want to have the permissions of..</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        /// <returns><see cref="AccessRights"/></returns>
        Task<AccessRights> GetUserItemPermissionsAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong userId, string entityType = null);

        /// <summary>
        /// Get item permissions for a user. This can be used for Wiser users or website users.
        /// </summary>
        /// <param name="moduleId">The ID of the module that that you want to have the permissions of.</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <returns><see cref="AccessRights"/></returns>
        Task<AccessRights> GetUserModulePermissions(int moduleId, ulong userId);

        /// <summary>
        /// Function gets an item from the database and returns all details in list.
        /// </summary>
        /// <param name="itemId">Optional: The ID of the item to get the details of.</param>
        /// <param name="uniqueId">Optional: The unique_uuid of the item.</param>
        /// <param name="languageCode">Optional: The language code, for if you only want values of a certain language.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param> 
        /// <param name="detailKey">Optional: The key of the detail to which the item must match to retrieve the item, always used in combination with detailValue.</param>
        /// <param name="detailValue">Optional: The value of the detail, which must match to retrieve the item details. Always used in combination with detailKey.</param>
        /// <param name="returnNullIfDeleted">Optional: Whether to return nothing/null if the item has been marked as deleted. Default is true.</param>
        /// <param name="skipDetailsWithoutLanguageCode">Optional: Set to true to ONLY get details with the given language code. Otherwise it will get all details with the given language code and all details without a specified language code.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        Task<WiserItemModel> GetItemDetailsAsync(ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null);

        /// <summary>
        /// Function gets an item from the database and returns all details in list.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">Optional: The ID of the item to get the details of.</param>
        /// <param name="uniqueId">Optional: The unique_uuid of the item.</param>
        /// <param name="languageCode">Optional: The language code, for if you only want values of a certain language.</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param> 
        /// <param name="detailKey">Optional: The key of the detail to which the item must match to retrieve the item, always used in combination with detailValue.</param>
        /// <param name="detailValue">Optional: The value of the detail, which must match to retrieve the item details. Always used in combination with detailKey.</param>
        /// <param name="returnNullIfDeleted">Optional: Whether to return nothing/null if the item has been marked as deleted. Default is true.</param>
        /// <param name="skipDetailsWithoutLanguageCode">Optional: Set to true to ONLY get details with the given language code. Otherwise it will get all details with the given language code and all details without a specified language code.</param>
        /// <param name="entityType">Optional: Enter an entity type here. This is required for entities that have a dedicated table. Default is null.</param>
        Task<WiserItemModel> GetItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null);

        /// <summary>
        /// By default this function gets all items linked to the given <see cref="itemId"/>, unless the parameter <see cref="reverse"/> is set to true,
        /// then this function will return all items that the <see cref="itemId"/> is linked to.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        /// <param name="linkType">Optional: The type number of links to get.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="includeDeletedItems">Optional: Set to true to include removed items, default is false.</param>
        /// <param name="userId">Optional: The ID of the user. If this is greater than 0, this function will check permissions and only return items that this user is allowed to see.</param>
        /// <param name="reverse">Optional: Set to true to get the items that this item is linked to, instead of the items linked to this item. Default is false.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null);

        /// <summary>
        /// By default this function gets all items linked to the given <see cref="itemId"/>, unless the parameter <see cref="reverse"/> is set to true,
        /// then this function will return all items that the <see cref="itemId"/> is linked to.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The item ID.</param>
        /// <param name="linkType">Optional: The type number of links to get.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="includeDeletedItems">Optional: Set to true to include removed items, default is false.</param>
        /// <param name="userId">Optional: The ID of the user. If this is greater than 0, this function will check permissions and only return items that this user is allowed to see.</param>
        /// <param name="reverse">Optional: Set to true to get the items that this item is linked to, instead of the items linked to this item. Default is false.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null);

        /// <summary>
        /// By default this function gets the IDs of all items linked to the given <see cref="itemId"/>, unless the parameter <see cref="reverse"/> is set to tue,
        /// then this function will return all items that the <see cref="itemId"/> is linked to.
        /// </summary>
        /// <param name="itemId">The item ID.</param>
        /// <param name="linkType">The type number of links to get.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="includeDeletedItems">Optional: Set to true to include removed items, default is false.</param>
        /// <param name="userId">Optional: The ID of the user. If this is greater than 0, this function will check permissions and only return items that this user is allowed to see.</param>
        /// <param name="reverse">Optional: Set to true to get the items that this item is linked to, instead of the items linked to this item. Default is false.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<ulong>> GetLinkedItemIdsAsync(ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null);

        /// <summary>
        /// By default this function gets the IDs of all items linked to the given <see cref="itemId"/>, unless the parameter <see cref="reverse"/> is set to tue,
        /// then this function will return all items that the <see cref="itemId"/> is linked to.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The item ID.</param>
        /// <param name="linkType">The type number of links to get.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="includeDeletedItems">Optional: Set to true to include removed items, default is false.</param>
        /// <param name="userId">Optional: The ID of the user. If this is greater than 0, this function will check permissions and only return items that this user is allowed to see.</param>
        /// <param name="reverse">Optional: Set to true to get the items that this item is linked to, instead of the items linked to this item. Default is false.</param>
        /// <param name="itemIdEntityType">Optional: You can enter the entity type of the given itemId here, if you want to get items from a dedicated table and those items can have multiple different entity types. This only works if all those items exist in the same table. Default is null.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<ulong>> GetLinkedItemIdsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null);

        /// <summary>
        /// Gets the settings for an entity type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0);

        /// <summary>
        /// Gets the HTML template and a <see cref="DataRow"/> with the data for an item, so that it can be added anywhere on the page.
        /// </summary>
        /// <param name="itemId">The ID of the Wiser 2 item.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're getting the template of. This is needed for entities that have a dedicated table.</param>
        /// <returns>A Tuple containing the HTML template and DataRow.</returns>
        Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(ulong itemId, string entityType = null);

        /// <summary>
        /// Gets the HTML template and a <see cref="DataRow"/> with the data for an item, so that it can be added anywhere on the page.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the Wiser 2 item.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're getting the template of. This is needed for entities that have a dedicated table.</param>
        /// <returns>A Tuple containing the HTML template and DataRow.</returns>
        Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(IWiserItemsService wiserItemsService, ulong itemId, string entityType = null);

        /// <summary>
        /// Get the link type number for wiser_itemlink based on 2 connecting entity types.
        /// This will look in the table wiser_link, so make sure that table contains the correct data.
        /// </summary>
        /// <param name="destinationEntityType">The entity type of the destination item.</param>
        /// <param name="connectedEntityType">The entity type of the source item.</param>
        /// <returns>The type number, or 0 if the link type has not been found.</returns>
        Task<int> GetLinkTypeAsync(string destinationEntityType, string connectedEntityType);

        /// <summary>
        /// Adds a link between 2 items. If a link already exists with the exact same parameters, nothing happens.
        /// </summary>
        /// <param name="itemId">The ID of the source item.</param>
        /// <param name="destinationItemId">The ID of the destination item.</param>
        /// <param name="type">The type number of the link.</param>
        /// <param name="ordering">Optional: The ordering number, this will decide in which order linked items are shown. Default value is 1.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task<ulong> AddItemLinkAsync(ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Adds a link between 2 items. If a link already exists with the exact same parameters, nothing happens.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the source item.</param>
        /// <param name="destinationItemId">The ID of the destination item.</param>
        /// <param name="type">The type number of the link.</param>
        /// <param name="ordering">Optional: The ordering number, this will decide in which order linked items are shown. Default value is 1.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task<ulong> AddItemLinkAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Deletes all links to an item with a specific type number.
        /// </summary>
        /// <param name="destinationItemId">The item to delete the links of.</param>
        /// <param name="type">The type of links to delete.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task RemoveItemLinksAsync(ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Deletes all links to an item with a specific type number.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="destinationItemId">The item to delete the links of.</param>
        /// <param name="type">The type of links to delete.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task RemoveItemLinksAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Remove the item link between items based on the item link id.
        /// </summary>
        /// <param name="ids">The ids of the item link to delete.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task RemoveItemLinksByIdAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Remove the item link between items based on the item link id.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="ids">The ids of the item link to delete.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task RemoveItemLinksByIdAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Remove a parent link of an item.
        /// </summary>
        /// <param name="ids">The ids of the items containing the parent id.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task RemoveParentLinkOfItemsAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Remove a parent link of an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="ids">The ids of the items containing the parent id.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "JCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task RemoveParentLinkOfItemsAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Marks items that are linked to the given destination item as removed.
        /// </summary>
        /// <param name="destinationItemId">The item ID to remove the linked items of.</param>
        /// <param name="type">Optional: The type number of linked items to remove. Use 0 for ALL linked items. Default value is 0.</param>
        /// <param name="exceptItemIds">Optional: A list with exceptions, items in this list will not be removed. Default value is <see langword="null" />.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want the DeleteAsync function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        Task RemoveLinkedItemsAsync(ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Marks items that are linked to the given destination item as removed.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="destinationItemId">The item ID to remove the linked items of.</param>
        /// <param name="type">Optional: The type number of linked items to remove. Use 0 for ALL linked items. Default value is 0.</param>
        /// <param name="exceptItemIds">Optional: A list with exceptions, items in this list will not be removed. Default value is <see langword="null" />.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: Enter an entity type here to only get items of that type. Default is null.</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want the DeleteAsync function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        Task RemoveLinkedItemsAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true);

        /// <summary>
        /// Moves all linked items of an item to a different destination item.
        /// </summary>
        /// <param name="oldDestinationItemId">The current destination item ID.</param>
        /// <param name="newDestinationItemId">The new destination item ID.</param>
        /// <param name="type">Optional: The type number of the links to move. If 0, all links will be moved. Default value is 0.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeItemLinksAsync(ulong oldDestinationItemId, ulong newDestinationItemId, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Moves all linked items of an item to a different destination item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="oldDestinationItemId">The current destination item ID.</param>
        /// <param name="newDestinationItemId">The new destination item ID.</param>
        /// <param name="type">Optional: The type number of the links to move. If 0, all links will be moved. Default value is 0.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeItemLinksAsync(IWiserItemsService wiserItemsService, ulong oldDestinationItemId, ulong newDestinationItemId, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Changes the type of all items linked to the given destination item from one specific type to another.
        /// </summary>
        /// <param name="destinationItemId">The destination item ID.</param>
        /// <param name="oldLinkType">The current link type number.</param>
        /// <param name="newLinkType">The new link type number.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeLinkTypesAsync(ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Changes the type of all items linked to the given destination item from one specific type to another.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="destinationItemId">The destination item ID.</param>
        /// <param name="oldLinkType">The current link type number.</param>
        /// <param name="newLinkType">The new link type number.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeLinkTypesAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Changes the type number of a specific link between two items.
        /// </summary>
        /// <param name="destinationItemId">The destination item ID.</param>
        /// <param name="oldLinkType">The current link type number.</param>
        /// <param name="newLinkType">The new link type number.</param>
        /// <param name="sourceItemId">The source item ID.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeLinkTypeAsync(ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Changes the type number of a specific link between two items.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="destinationItemId">The destination item ID.</param>
        /// <param name="oldLinkType">The current link type number.</param>
        /// <param name="newLinkType">The new link type number.</param>
        /// <param name="sourceItemId">The source item ID.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        Task ChangeLinkTypeAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Adds a file to an item.
        /// </summary>
        /// <param name="wiserItemFile">The ID of the destination item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task<ulong> AddItemFileAsync(WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Adds a file to an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="wiserItemFile">The ID of the destination item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        Task<ulong> AddItemFileAsync(IWiserItemsService wiserItemsService, WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true);

        /// <summary>
        /// Gets a file from the database.
        /// </summary>
        Task<WiserItemFileModel> GetItemFileAsync(ulong id, string field = "Id");

        /// <summary>
        /// Gets multiple files from the database.
        /// </summary>
        /// <param name="ids">The IDs of the files to get.</param>
        /// <param name="field"></param>
        /// <returns></returns>
        Task<List<WiserItemFileModel>> GetItemFilesAsync(ulong[] ids, string field = "Id");

        /// <summary>
        /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
        /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
        /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
        /// If it doesn't have a dedicated table, an empty string will be returned.
        /// </summary>
        /// <param name="entityType">The entity type name.</param>
        /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
        Task<string> GetTablePrefixForEntityAsync(string entityType);

        /// <summary>
        /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
        /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
        /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
        /// If it doesn't have a dedicated table, an empty string will be returned.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="entityType">The entity type name.</param>
        /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
        Task<string> GetTablePrefixForEntityAsync(IWiserItemsService wiserItemsService, string entityType);

        /// <summary>
        /// Gets the prefix for the wiser_item and wiser_itemdetail tables for a specific entity type.
        /// Certain entity types can have dedicated tables, they won't use wiser_item and wiser_itemdetail, but something like basket_wiser_item and basket_wiser_itemdetail instead.
        /// This function checks wiser_entity if the entity type has dedicated tables and returns the prefix for those tables.
        /// If it doesn't have a dedicated table, an empty string will be returned.
        /// </summary>
        /// <param name="entityTypeSettings">A <see cref="EntitySettingsModel"/> with the settings of the entity type.</param>
        /// <returns>The table prefix for the given entity type. Returns an empty string if the entity type uses the default tables.</returns>
        string GetTablePrefixForEntity(EntitySettingsModel entityTypeSettings);

        /// <summary>
        /// Gets the settings for a link type. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="linkType">Optional: The type number of the link type.</param>
        /// <param name="sourceEntityType">Optional: The entity type of the source item.</param>
        /// <param name="destinationEntityType">Optional: The entity type of the destination item.</param>
        /// <exception cref="ArgumentException">If linkType, sourceEntityType and destinationEntityType are all empty.</exception>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<LinkSettingsModel> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null);

        /// <summary>
        /// Gets the settings for a link type. These settings will be cached for 1 hour.
        /// </summary>
        /// <returns>A List of <see cref="EntitySettingsModel"/> containing all link settings.</returns>
        Task<List<LinkSettingsModel>> GetAllLinkTypeSettingsAsync();

        /// <summary>
        /// Gets the settings for a link type by id. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="linkId">The id of the link type</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId);

        /// <summary>
        /// Gets the settings for a link type by id. These settings will be cached for 1 hour.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="linkId">The id of the link type</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(IWiserItemsService wiserItemsService, int linkId);

        /// <summary>
        /// Replace HTML for saving via Wiser. This will change things like &lt;table data-contentid="x" to &lt;img src="x".
        /// </summary>
        /// <param name="input">The HTML that you want to save.</param>
        /// <param name="allowAbsoluteImageUrls"></param>
        /// <returns>The HTML that you should save.</returns>
        Task<string> ReplaceHtmlForSavingAsync(string input, bool allowAbsoluteImageUrls = false);

        /// <summary>
        /// Replace HTML for viewing via Wiser. This will change the URLs of images if they are relative, to absolute URLs, so that the images can be visible in Wiser.
        /// </summary>
        /// <param name="input">The HTML for a HTML editor in Wiser.</param>
        /// <returns></returns>
        Task<string> ReplaceHtmlForViewingAsync(string input);

        /// <summary>
        /// Gets the aggregation settings of all fields/properties of an entity type.
        /// </summary>
        /// <param name="entityType">The name of the entity type.</param>
        /// <returns>A list of <see cref="WiserItemPropertyAggregateOptionsModel"/> of the settings per field.</returns>
        Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(string entityType);

        /// <summary>
        /// Handles aggregation settings for an item.
        /// </summary>
        /// <param name="itemModel">The item to handle the aggregation of.</param>
        Task HandleItemAggregationAsync(WiserItemModel itemModel);

        /// <summary>
        /// Handles aggregation settings for an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemModel">The item to handle the aggregation of.</param>
        Task HandleItemAggregationAsync(IWiserItemsService wiserItemsService, WiserItemModel itemModel);
    }
}