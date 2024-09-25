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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="storeTypeOverride">Optional: Override the storeType of the item.</param>
        /// <param name="alwaysSaveReadOnly">Save the value even if it is marked as readonly.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        Task<WiserItemModel> SaveAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null, bool alwaysSaveReadOnly = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="storeTypeOverride">Optional: Override the storeType of the item.</param>
        /// <param name="alwaysSaveReadOnly">Save the value even if it is marked as readonly.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        Task<WiserItemModel> SaveAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null, bool alwaysSaveReadOnly = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="storeTypeOverride">Optional: Override the storeType of the item.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        /// <exception cref="System.ArgumentNullException">If wiserItem or entityType is <see langword="null"/>.</exception>
        Task<WiserItemModel> CreateAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="storeTypeOverride">Optional: Override the storeType of the item.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again, with the new ID.</returns>
        /// <exception cref="System.ArgumentNullException">If wiserItem or entityType is <see langword="null"/>.</exception>
        Task<WiserItemModel> CreateAsync(IWiserItemsService wiserItemsService, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>An <see cref="WiserItemDuplicationResultModel"/> with the results.</returns>
        Task<WiserItemDuplicationResultModel> DuplicateItemAsync(ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>An <see cref="WiserItemDuplicationResultModel"/> with the results.</returns>
        Task<WiserItemDuplicationResultModel> DuplicateItemAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="isNewlyCreatedItem">Whether this item has just been created in code and contains no details yet. If this is set to true, then this function will just insert all the details without checking if they already exist.</param>
        /// <param name="alwaysSaveReadOnly">Save the value even if it is marked as readonly.</param>
        /// <param name="skipDetails">Skip the details and only update the main item.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again.</returns>
        Task<WiserItemModel> UpdateAsync(ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, bool isNewlyCreatedItem = false, bool alwaysSaveReadOnly = false, bool skipDetails = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="isNewlyCreatedItem">Whether this item has just been created in code and contains no details yet. If this is set to true, then this function will just insert all the details without checking if they already exist.</param>
        /// <param name="alwaysSaveReadOnly">Save the value even if it is marked as readonly.</param>
        /// <param name="skipDetails">Skip the details and only update the main item.</param>
        /// <returns>The same <see cref="WiserItemModel"/> again.</returns>
        Task<WiserItemModel> UpdateAsync(IWiserItemsService wiserItemsService, ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, bool isNewlyCreatedItem = false, bool alwaysSaveReadOnly = false, bool skipDetails = false);

        /// <summary>
        /// Changes an entity type of an item.
        /// </summary>
        /// <param name="itemId">The ID of the item to change entity type from.</param>
        /// <param name="currentEntityType">The name of the entity type that the item currently has.</param>
        /// <param name="newEntityType">The new entity type for the item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Set to true to force the change, without checking if the logged in user has permissions to do this. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="resetAddedOnDate">Optional: Set to true to reset the added on date and time of the item. This is useful for when converting a basket to an order for example, so that the added on date will be the moment the order was created, instead of when the original basket was created.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> ChangeEntityTypeAsync(ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false);

        /// <summary>
        /// Changes an entity type of an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item to change entity type from.</param>
        /// <param name="currentEntityType">The name of the entity type that the item currently has.</param>
        /// <param name="newEntityType">The new entity type for the item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Set to true to force the change, without checking if the logged in user has permissions to do this. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="resetAddedOnDate">Optional: Set to true to reset the added on date and time of the item. This is useful for when converting a basket to an order for example, so that the added on date will be the moment the order was created, instead of when the original basket was created.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> ChangeEntityTypeAsync(IWiserItemsService wiserItemsService, ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false);

        /// <summary>
        /// Deletes or un-deletes an item.
        /// </summary>
        /// <param name="itemId">The ID of the item to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table and to check how the item should be deleted (delete permanently or move to archive for example).</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Deletes or un-deletes an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemId">The ID of the item to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table and to check how the item should be deleted (delete permanently or move to archive for example).</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(IWiserItemsService wiserItemsService, ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Deletes or un-deletes items.
        /// Deleting items will move them to an archive table, such as wiser_item_archive.
        /// Undeleting items will move them back into the original table they were in.
        /// </summary>
        /// <param name="itemIds">The list with IDs of the items to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table and to check how the item should be deleted (delete permanently or move to archive for example).</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Deletes or un-deletes items.
        /// Deleting items will move them to an archive table, such as wiser_item_archive.
        /// Undeleting items will move them back into the original table they were in.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemIds">The list with IDs of the items to (un)delete.</param>
        /// <param name="undelete">Optional: Indicates whether to un-delete an item instead of deleting it. Default is false.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="entityType">Optional: The entity type of the item that you're (un)deleting. This is needed for entities that have a dedicated table and to check how the item should be deleted (delete permanently or move to archive for example).</param>
        /// <param name="createNewTransaction">Optional: Set to false if you don't want this function to try and create a new database transaction. Be warned that this will then also not rollback any changes if an error occurred. It's recommended to only set this to false if you already created a transaction in your code, before calling this function. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>The amount of affected rows.</returns>
        Task<int> DeleteAsync(IWiserItemsService wiserItemsService, List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

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
        /// Get the query permissions for a user.
        /// </summary>
        /// <param name="queryId">The ID of the query that you want to have the permissions of.</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <returns></returns>
        Task<AccessRights> GetUserQueryPermissionsAsync(int queryId, ulong userId);

        /// <summary>
        /// Get the data selector permissions for a user.
        /// </summary>
        /// <param name="dataSelectorId">The ID of the data selector that you want to have the permissions of.</param>
        /// <param name="userId">The ID of the logged in Wiser user.</param>
        /// <returns></returns>
        Task<AccessRights> GetUserDataSelectorPermissionsAsync(int dataSelectorId, ulong userId);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task<WiserItemModel> GetItemDetailsAsync(ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task<WiserItemModel> GetItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<ulong>> GetLinkedItemIdsAsync(ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <returns>A list of <see cref="WiserItemModel"/>. Empty list if no items have been found.</returns>
        Task<List<ulong>> GetLinkedItemIdsAsync(IWiserItemsService wiserItemsService, ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false);

        /// <summary>
        /// Gets the settings for an entity type.
        /// </summary>
        /// <param name="entityType">The name of the entity type.</param>
        /// <param name="moduleId">Optional: The ID of the module, in case the entity type has different settings for different modules.</param>
        /// <returns>A <see cref="EntitySettingsModel"/> containing all settings of the entity type.</returns>
        Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0);

        /// <summary>
        /// Gets the field options for a link type.
        /// </summary>
        /// <param name="linkType">The link type.</param>
        /// <returns>The options of all fields set for this link type.</returns>
        Task<Dictionary<string, Dictionary<string, object>>> GetFieldOptionsForLinkFieldsAsync(int linkType);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task<ulong> AddItemLinkAsync(ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task<ulong> AddItemLinkAsync(IWiserItemsService wiserItemsService, ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Deletes all links to an item with a specific type number.
        /// </summary>
        /// <param name="destinationItemId">The item to delete the links of.</param>
        /// <param name="type">The type of links to delete.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveItemLinksAsync(ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Deletes all links to an item with a specific type number.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="destinationItemId">The item to delete the links of.</param>
        /// <param name="type">The type of links to delete.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveItemLinksAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Remove the item link between items based on the item link id.
        /// </summary>
        /// <param name="ids">The ids of the item link to delete.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveItemLinksByIdAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Remove the item link between items based on the item link id.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="ids">The ids of the item link to delete.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveItemLinksByIdAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Remove a parent link of an item.
        /// </summary>
        /// <param name="ids">The ids of the items containing the parent id.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveParentLinkOfItemsAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Remove a parent link of an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="ids">The ids of the items containing the parent id.</param>
        /// <param name="sourceEntityType">The entity type of the source.</param>
        /// <param name="sourceIds">The ids of the source for permissions.</param>
        /// <param name="destinationEntityType">The entity type of the destination.</param>
        /// <param name="destinationIds">The dis of the destination for permissions.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveParentLinkOfItemsAsync(IWiserItemsService wiserItemsService, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveLinkedItemsAsync(ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task RemoveLinkedItemsAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Moves all linked items of an item to a different destination item.
        /// </summary>
        /// <param name="oldDestinationItemId">The current destination item ID.</param>
        /// <param name="newDestinationItemId">The new destination item ID.</param>
        /// <param name="entityType">The entity type of the destination item. This is needed to check whether the items are saved in a different table than wiser_item. This method assumes that the old destination, new destination and all linked items are all saved in the same table. If that is not the case, this might not work properly.</param>
        /// <param name="type">Optional: The type number of the links to move. If 0, all links will be moved. Default value is 0.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeItemLinksAsync(ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Moves all linked items of an item to a different destination item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="oldDestinationItemId">The current destination item ID.</param>
        /// <param name="newDestinationItemId">The new destination item ID.</param>
        /// <param name="entityType">The entity type of the destination item. This is needed to check whether the items are saved in a different table than wiser_item. This method assumes that the old destination, new destination and all linked items are all saved in the same table. If that is not the case, this might not work properly.</param>
        /// <param name="type">Optional: The type number of the links to move. If 0, all links will be moved. Default value is 0.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeItemLinksAsync(IWiserItemsService wiserItemsService, ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Changes the type of all items linked to the given destination item from one specific type to another.
        /// </summary>
        /// <param name="destinationItemId">The destination item ID.</param>
        /// <param name="oldLinkType">The current link type number.</param>
        /// <param name="newLinkType">The new link type number.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is false.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeLinkTypesAsync(ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeLinkTypesAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeLinkTypeAsync(ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

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
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        Task ChangeLinkTypeAsync(IWiserItemsService wiserItemsService, ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false);

        /// <summary>
        /// Adds a file to an item.
        /// </summary>
        /// <param name="wiserItemFile">The ID of the destination item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<ulong> AddItemFileAsync(WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0);

        /// <summary>
        /// Adds a file to an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="wiserItemFile">The ID of the destination item.</param>
        /// <param name="username">Optional: The name of the user that is executing the action. Default value is "GCL".</param>
        /// <param name="userId">Optional: The ID of the user that is trying to execute this action. Make sure a value is entered here if you need to check for access rights. This can be a Wiser user or a website user.</param>
        /// <param name="saveHistory">Optional: Set to false if you don't want the current changes to be saved in wiser_history. Default value is true.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether to skip the check for permissions. Only do this for things that should always be possible by anyone, such as creating a basket.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<ulong> AddItemFileAsync(IWiserItemsService wiserItemsService, WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets a file from the database.
        /// </summary>
        /// <param name="id">The ID of the file, or the ID of the item the file belongs to or the ID of the link the file belongs to.</param>
        /// <param name="field">Optional: The field that contains the the ID from the <see cref="id"/> parameter. This can be either "id", "item_id" or "itemlink_id".</param>
        /// <param name="propertyName">Optional: The property name from wiser_entityproperty of the field where this file was uploaded.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<WiserItemFileModel> GetItemFileAsync(ulong id, string field = "id", string propertyName = null, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets a file from the database.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="id">The ID of the file, or the ID of the item the file belongs to or the ID of the link the file belongs to.</param>
        /// <param name="field">Optional: The field that contains the the ID from the <see cref="id"/> parameter. This can be either "id", "item_id" or "itemlink_id".</param>
        /// <param name="propertyName">Optional: The property name from wiser_entityproperty of the field where this file was uploaded.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<WiserItemFileModel> GetItemFileAsync(IWiserItemsService wiserItemsService, ulong id, string field = "id", string propertyName = null, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets multiple files from the database.
        /// </summary>
        /// <param name="ids">The IDs of the files, or the IDs of the items the files belong to or the IDs of the links the files belong to.</param>
        /// <param name="field">Optional: The field that contains the the ID from the <see cref="id"/> parameter. This can be either "id", "item_id" or "itemlink_id".</param>
        /// <param name="propertyName">Optional: The property name from wiser_entityproperty of the field where this file was uploaded.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<List<WiserItemFileModel>> GetItemFilesAsync(ulong[] ids, string field = "id", string propertyName = null, string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets multiple files from the database.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="ids">The IDs of the files, or the IDs of the items the files belong to or the IDs of the links the files belong to.</param>
        /// <param name="field">Optional: The field that contains the the ID from the <see cref="id"/> parameter. This can be either "id", "item_id" or "itemlink_id".</param>
        /// <param name="propertyName">Optional: The property name from wiser_entityproperty of the field where this file was uploaded.</param>
        /// <param name="entityType">Optional: If you're adding a file to an item and that entity type has a dedicated table prefix, enter the entity type here so that we can use the same prefix for wiser_itemfile.</param>
        /// <param name="linkType">Optional: If you're adding a file to a link and that link has a dedicated table prefix, enter the link type here so that we can use the same prefix for wiser_itemfile.</param>
        Task<List<WiserItemFileModel>> GetItemFilesAsync(IWiserItemsService wiserItemsService, ulong[] ids, string field = "id", string propertyName = null, string entityType = null, int linkType = 0);

        /// <summary>
        /// Get a list of all dedicated prefixes used on the server
        /// </summary>
        /// <returns>a list of dedicated prefixes strings, or an empty list none have been found.</returns>
        Task<List<string>> GetDedicatedTablePrefixesAsync();

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
        /// Gets the prefix for the wiser_itemlink and wiser_itemlinkdetail tables for a specific link type.
        /// Certain link types can have dedicated tables, they won't use wiser_itemlink and wiser_itemlinkdetail, but something like 123_wiser_itemlink and 123_wiser_itemlinkdetail instead.
        /// This function checks wiser_link if the entity type has dedicated tables and returns the prefix for those tables.
        /// If it doesn't have a dedicated table, an empty string will be returned.
        /// </summary>
        /// <param name="linkType">Optional: The type number of the link type.</param>
        /// <param name="sourceEntityType">Optional: The entity type of the source item.</param>
        /// <param name="destinationEntityType">Optional: The entity type of the destination item.</param>
        /// <exception cref="ArgumentException">If linkType, sourceEntityType and destinationEntityType are all empty.</exception>
        /// <returns>The table prefix for the given link type. Returns an empty string if the link type uses the default tables.</returns>
        Task<string> GetTablePrefixForLinkAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null);

        /// <summary>
        /// Gets the prefix for the wiser_itemlink and wiser_itemlinkdetail tables for a specific link type.
        /// Certain link types can have dedicated tables, they won't use wiser_itemlink and wiser_itemlinkdetail, but something like 123_wiser_itemlink and 123_wiser_itemlinkdetail instead.
        /// This function checks wiser_link if the entity type has dedicated tables and returns the prefix for those tables.
        /// If it doesn't have a dedicated table, an empty string will be returned.
        /// </summary>
        /// <param name="linkTypeSettings">A <see cref="LinkSettingsModel"/> with the settings of the link type.</param>
        /// <returns>The table prefix for the given link type. Returns an empty string if the link type uses the default tables.</returns>
        string GetTablePrefixForLink(LinkSettingsModel linkTypeSettings);

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
        /// Replace HTML to make relative images absolute with the given images domain.
        /// </summary>
        /// <param name="input">The HTML to make make the images absolute.</param>
        /// <param name="imagesDomain">The domain to use when making the images absolute.</param>
        /// <returns></returns>
        Task<string> ReplaceRelativeImagesToAbsoluteAsync(string input, string imagesDomain);

        /// <summary>
        /// Gets the aggregation settings of all fields/properties of an entity type and/or link type.
        /// </summary>
        /// <param name="entityType">The name of the entity type, if this is a property for an entity.</param>
        /// <param name="linkType">The type of the link, if this is for properties of a link between items.</param>
        /// <returns>A list of <see cref="WiserItemPropertyAggregateOptionsModel"/> of the settings per field.</returns>
        Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(string entityType = null, int linkType = 0);

        /// <summary>
        /// Gets the aggregation settings of all fields/properties of an entity type and/or link type.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="entityType">The name of the entity type, if this is a property for an entity.</param>
        /// <param name="linkType">The type of the link, if this is for properties of a link between items.</param>
        /// <returns>A list of <see cref="WiserItemPropertyAggregateOptionsModel"/> of the settings per field.</returns>
        Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(IWiserItemsService wiserItemsService, string entityType = null, int linkType = 0);

        /// <summary>
        /// Handles aggregation settings for an item.
        /// </summary>
        /// <param name="itemModel">The item to handle the aggregation of.</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting the new item ID. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        Task HandleItemAggregationAsync(WiserItemModel itemModel, string encryptionKey = "");

        /// <summary>
        /// Handles aggregation settings for an item.
        /// </summary>
        /// <param name="wiserItemsService">The <see cref="IWiserItemsService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="itemModel">The item to handle the aggregation of.</param>
        /// <param name="encryptionKey">Optional: The key used for encrypting values of secure-input fields. This will only be used the there is no specific encryption key set for the secure-input field. Default value is the key from the web.config setting "QueryTemplatesDecryptionKey".</param>
        Task HandleItemAggregationAsync(IWiserItemsService wiserItemsService, WiserItemModel itemModel, string encryptionKey = "");

        /// <summary>
        /// Replaces all entity blocks in a HTML template with the rendered versions.
        /// </summary>
        /// <param name="template">The HTML template that might contain one or more entity blocks.</param>
        /// <returns>The same template but with all entity blocks fully rendered.</returns>
        Task<string> ReplaceAllEntityBlocksAsync(string template);

        /// <summary>
        /// Save a single item detail to the database. This will check if the item detail already exists (based on key and language code) and updates the row if it does, or insert one if it doesn't.
        /// This function will not check for permissions and will not do any conversions for saving dates and whatnot.
        /// This is only meant for saving simple string values. In other cases, you should use "UpdateAsync".
        /// </summary>
        /// <param name="itemDetail">The <see cref="WiserItemDetailModel"/> with the data to save.</param>
        /// <param name="itemId">The ID of the item, if this is a detail for an item.</param>
        /// <param name="itemLinkId">The ID of the item link, if this is a detail for a link.</param>
        /// <param name="entityType">Optional: The entity type of the corresponding item. This is needed when that entity type uses dedicated tables.</param>
        /// <param name="username">Optional: The username of the user that is making the change. This is used in wiser_history. Default value is "GCL".</param>
        /// <param name="saveHistory">Optional: Whether or not to log this change in wiser_history. Default value is "true".</param>
        Task SaveItemDetailAsync(WiserItemDetailModel itemDetail, ulong itemId = 0, ulong itemLinkId = 0, string entityType = null, string username = "GCL", bool saveHistory = true);
    }
}