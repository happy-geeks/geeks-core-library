using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services
{
    /// <inheritdoc />
    public class CachedWiserItemsService : IWiserItemsService
    {
        private readonly GclSettings gclSettings;
        private readonly IAppCache cache;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly ICacheService cacheService;
        private readonly IBranchesService branchesService;

        public CachedWiserItemsService(IOptions<GclSettings> gclSettings, IAppCache cache, IWiserItemsService wiserItemsService, IDatabaseConnection databaseConnection, ICacheService cacheService, IBranchesService branchesService)
        {
            this.gclSettings = gclSettings.Value;
            this.cache = cache;
            this.wiserItemsService = wiserItemsService;
            this.databaseConnection = databaseConnection;
            this.cacheService = cacheService;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null, bool alwaysSaveReadOnly = false)
        {
            return await SaveAsync(this, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck, storeTypeOverride, alwaysSaveReadOnly);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(IWiserItemsService service, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null, bool alwaysSaveReadOnly = false)
        {
            return await wiserItemsService.SaveAsync(service, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck, storeTypeOverride, alwaysSaveReadOnly);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> CreateAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null)
        {
            return await CreateAsync(this, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, saveHistory, createNewTransaction, skipPermissionsCheck, storeTypeOverride);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> CreateAsync(IWiserItemsService service, WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, StoreType? storeTypeOverride = null)
        {
            return await wiserItemsService.CreateAsync(service, wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, saveHistory, createNewTransaction, skipPermissionsCheck, storeTypeOverride);
        }

        /// <inheritdoc />
        public async Task<WiserItemDuplicationResultModel> DuplicateItemAsync(ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DuplicateItemAsync(this, itemId, parentId, username, encryptionKey, userId, entityType, parentEntityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemDuplicationResultModel> DuplicateItemAsync(IWiserItemsService service, ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.DuplicateItemAsync(service, itemId, parentId, username, encryptionKey, userId, entityType, parentEntityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> UpdateAsync(ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false, bool isNewlyCreatedItem = false, bool alwaysSaveReadOnly = false, bool skipDetails = false)
        {
            return await UpdateAsync(this, itemId, wiserItem, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck, isNewlyCreatedItem, alwaysSaveReadOnly, skipDetails);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> UpdateAsync(IWiserItemsService service, ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, bool skipPermissionsCheck = false,  bool isNewlyCreatedItem = false, bool alwaysSaveReadOnly = false, bool skipDetails = false)
        {
            return await wiserItemsService.UpdateAsync(service, itemId, wiserItem, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, skipPermissionsCheck, isNewlyCreatedItem, alwaysSaveReadOnly, skipDetails);
        }

        /// <inheritdoc />
        public async Task<int> ChangeEntityTypeAsync(ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false)
        {
            return await ChangeEntityTypeAsync(this, itemId, currentEntityType, newEntityType, username, userId, saveHistory, skipPermissionsCheck, resetAddedOnDate);
        }

        /// <inheritdoc />
        public async Task<int> ChangeEntityTypeAsync(IWiserItemsService service, ulong itemId, string currentEntityType, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, bool resetAddedOnDate = false)
        {
            return await wiserItemsService.ChangeEntityTypeAsync(service, itemId, currentEntityType, newEntityType, username, userId, saveHistory, skipPermissionsCheck, resetAddedOnDate);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DeleteAsync(this, itemId, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IWiserItemsService service, ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.DeleteAsync(service, itemId, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await DeleteAsync(this, itemIds, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(IWiserItemsService service, List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.DeleteAsync(service, itemIds, undelete, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteWorkflowAsync(ulong itemId, bool isNewItem, EntitySettingsModel entitySettingsModel, WiserItemModel wiserItem = null, ulong userId = 0, string username = "GCL", bool saveHistory = true)
        {
            return await wiserItemsService.ExecuteWorkflowAsync(itemId, isNewItem, entitySettingsModel, wiserItem, userId, username, saveHistory);
        }

        /// <inheritdoc />
        public async Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null)
        {
            return await CheckIfEntityActionIsPossibleAsync(this, itemId, action, userId, wiserItem, onlyCheckAccessRights, entityType);
        }

        /// <inheritdoc />
        public async Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(IWiserItemsService service, ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null)
        {
            return await wiserItemsService.CheckIfEntityActionIsPossibleAsync(service, itemId, action, userId, wiserItem, onlyCheckAccessRights, entityType);
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserItemPermissionsAsync(ulong itemId, ulong userId, string entityType = null)
        {
            return await GetUserItemPermissionsAsync(this, itemId, userId, entityType);
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserItemPermissionsAsync(IWiserItemsService service, ulong itemId, ulong userId, string entityType = null)
        {
            var cacheName = $"user_item_permission_{itemId}_{userId}_{entityType ?? ""}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserItemPermissionsAsync(service, itemId, userId, entityType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserModulePermissions(int moduleId, ulong userId)
        {
            var cacheName = $"user_module_permission_{moduleId}_{userId}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserModulePermissions(moduleId, userId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserQueryPermissionsAsync(int queryId, ulong userId)
        {
            var cacheName = $"user_query_permission_{queryId}_{userId}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserQueryPermissionsAsync(queryId, userId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserDataSelectorPermissionsAsync(int dataSelectorId, ulong userId)
        {
            var cacheName = $"user_data_selector_permission_{dataSelectorId}_{userId}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserDataSelectorPermissionsAsync(dataSelectorId, userId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetItemDetailsAsync(ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false)
        {
            return await GetItemDetailsAsync(this, itemId, uniqueId, languageCode, userId, detailKey, detailValue, returnNullIfDeleted, skipDetailsWithoutLanguageCode, entityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetItemDetailsAsync(IWiserItemsService service, ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.GetItemDetailsAsync(service, itemId, uniqueId, languageCode, userId, detailKey, detailValue, returnNullIfDeleted, skipDetailsWithoutLanguageCode, entityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await GetLinkedItemDetailsAsync(this, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(IWiserItemsService service, ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.GetLinkedItemDetailsAsync(service, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<ulong>> GetLinkedItemIdsAsync(ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await GetLinkedItemIdsAsync(this, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<List<ulong>> GetLinkedItemIdsAsync(IWiserItemsService service, ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.GetLinkedItemIdsAsync(service, itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0)
        {
            var cacheName = $"entity_type_settings_{entityType}_{moduleId}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetEntityTypeSettingsAsync(entityType, moduleId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, Dictionary<string, object>>> GetFieldOptionsForLinkFieldsAsync(int linkType)
        {
            var cacheName = $"link_type_field_options_{linkType}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetFieldOptionsForLinkFieldsAsync(linkType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(ulong itemId, string entityType = null)
        {
            return await GetTemplateAndDataForItemAsync(this, itemId, entityType);
        }

        /// <inheritdoc />
        public async Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(IWiserItemsService service, ulong itemId, string entityType = null)
        {
            return await wiserItemsService.GetTemplateAndDataForItemAsync(service, itemId, entityType);
        }

        /// <inheritdoc />
        public async Task<int> GetLinkTypeAsync(string destinationEntityType, string connectedEntityType)
        {
            var cacheName = $"link_type_{destinationEntityType}_{connectedEntityType}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetLinkTypeAsync(destinationEntityType, connectedEntityType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemLinkAsync(ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            return await AddItemLinkAsync(this, itemId, destinationItemId, type, ordering, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemLinkAsync(IWiserItemsService service, ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            return await wiserItemsService.AddItemLinkAsync(service, itemId, destinationItemId, type, ordering, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksAsync(ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveItemLinksAsync(this, destinationItemId, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksAsync(IWiserItemsService service, ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.RemoveItemLinksAsync(service, destinationItemId, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksByIdAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveItemLinksByIdAsync(this, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksByIdAsync(IWiserItemsService service, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.RemoveItemLinksByIdAsync(service, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveParentLinkOfItemsAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await RemoveParentLinkOfItemsAsync(this, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveParentLinkOfItemsAsync(IWiserItemsService service, List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.RemoveParentLinkOfItemsAsync(service, ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveLinkedItemsAsync(ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0UL, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            await RemoveLinkedItemsAsync(this, destinationItemId, type, exceptItemIds, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task RemoveLinkedItemsAsync(IWiserItemsService service, ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.RemoveLinkedItemsAsync(service, destinationItemId, type, exceptItemIds, username, userId, saveHistory, entityType, createNewTransaction, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeItemLinksAsync(ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeItemLinksAsync(this, oldDestinationItemId, newDestinationItemId, entityType, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeItemLinksAsync(IWiserItemsService service, ulong oldDestinationItemId, ulong newDestinationItemId, string entityType, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.ChangeItemLinksAsync(service, oldDestinationItemId, newDestinationItemId, entityType, type, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypesAsync(ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeLinkTypesAsync(this, destinationItemId, oldLinkType, newLinkType, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypesAsync(IWiserItemsService service, ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.ChangeLinkTypesAsync(service, destinationItemId, oldLinkType, newLinkType, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypeAsync(ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await ChangeLinkTypeAsync(this, destinationItemId, oldLinkType, newLinkType, sourceItemId, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypeAsync(IWiserItemsService service, ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false)
        {
            await wiserItemsService.ChangeLinkTypeAsync(service, destinationItemId, oldLinkType, newLinkType, sourceItemId, username, userId, saveHistory, skipPermissionsCheck);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemFileAsync(WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0)
        {
            return await AddItemFileAsync(this, wiserItemFile, username, userId, saveHistory, skipPermissionsCheck, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemFileAsync(IWiserItemsService service, WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true, bool skipPermissionsCheck = false, string entityType = null, int linkType = 0)
        {
            return await wiserItemsService.AddItemFileAsync(service, wiserItemFile, username, userId, saveHistory, skipPermissionsCheck, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetItemFileAsync(ulong id, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await GetItemFileAsync(this, id, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetItemFileAsync(IWiserItemsService service, ulong id, string field = "id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await wiserItemsService.GetItemFileAsync(service, id, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemFileModel>> GetItemFilesAsync(ulong[] ids, string field = "Id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await GetItemFilesAsync(this, ids, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemFileModel>> GetItemFilesAsync(IWiserItemsService service, ulong[] ids, string field = "id", string propertyName = null, string entityType = null, int linkType = 0)
        {
            return await wiserItemsService.GetItemFilesAsync(service, ids, field, propertyName, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<string>>GetDedicatedTablePrefixesAsync()
        {
            return await wiserItemsService.GetDedicatedTablePrefixesAsync();
        }
        
        /// <inheritdoc />
        public async Task<string> GetTablePrefixForEntityAsync(string entityType)
        {
            return await GetTablePrefixForEntityAsync(this, entityType);
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForEntityAsync(IWiserItemsService service, string entityType)
        {
            return await wiserItemsService.GetTablePrefixForEntityAsync(service, entityType);
        }

        /// <inheritdoc />
        public string GetTablePrefixForEntity(EntitySettingsModel entityTypeSettings)
        {
            return wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsAsync(int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
        {
            if (linkType <= 0 && String.IsNullOrWhiteSpace(sourceEntityType) && String.IsNullOrWhiteSpace(destinationEntityType))
            {
                throw new ArgumentException($"You must enter a value in at least one of the following parameters: {nameof(linkType)}, {nameof(sourceEntityType)}, {nameof(destinationEntityType)}");
            }

            IEnumerable<LinkSettingsModel> result = await GetAllLinkTypeSettingsAsync();
            if (linkType > 0)
            {
                result = result.Where(t => t.Type == linkType);
            }

            if (!String.IsNullOrWhiteSpace(sourceEntityType))
            {
                result = result.Where(t => String.Equals(t.SourceEntityType, sourceEntityType, StringComparison.OrdinalIgnoreCase));
            }

            if (!String.IsNullOrWhiteSpace(destinationEntityType))
            {
                result = result.Where(t => String.Equals(t.DestinationEntityType, destinationEntityType, StringComparison.OrdinalIgnoreCase));
            }

            return result.FirstOrDefault() ?? new LinkSettingsModel();
        }

        /// <inheritdoc />
        public async Task<List<LinkSettingsModel>> GetAllLinkTypeSettingsAsync()
        {
            var cacheName = $"all_link_type_settings_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetAllLinkTypeSettingsAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId)
        {
            return await GetLinkTypeSettingsByIdAsync(this, linkId);
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(IWiserItemsService service, int linkId)
        {
            IEnumerable<LinkSettingsModel> result = await GetAllLinkTypeSettingsAsync();
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
        public async Task<string> GetTablePrefixForLinkAsync(IWiserItemsService service, int linkType = 0, string sourceEntityType = null, string destinationEntityType = null)
        {
            return await wiserItemsService.GetTablePrefixForLinkAsync(service, linkType, sourceEntityType, destinationEntityType);
        }

        /// <inheritdoc />
        public string GetTablePrefixForLink(LinkSettingsModel linkTypeSettings)
        {
            return wiserItemsService.GetTablePrefixForLink(linkTypeSettings);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceHtmlForSavingAsync(string input, bool allowAbsoluteImageUrls = false)
        {
            return await wiserItemsService.ReplaceHtmlForSavingAsync(input, allowAbsoluteImageUrls);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceHtmlForViewingAsync(string input)
        {
            return await wiserItemsService.ReplaceHtmlForViewingAsync(input);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceRelativeImagesToAbsoluteAsync(string input, string imagesDomain)
        {
            return await wiserItemsService.ReplaceRelativeImagesToAbsoluteAsync(input, imagesDomain);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(string entityType = null, int linkType = 0)
        {
            return await GetAggregationSettingsAsync(this, entityType, linkType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemPropertyAggregateOptionsModel>> GetAggregationSettingsAsync(IWiserItemsService service, string entityType = null, int linkType = 0)
        {
            var cacheName = $"aggregation_settings_{(String.IsNullOrWhiteSpace(entityType) ? linkType.ToString() : entityType)}_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                 async cacheEntry =>
                 {
                     cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                     return await wiserItemsService.GetAggregationSettingsAsync(service, entityType);
                 }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task HandleItemAggregationAsync(WiserItemModel itemModel, string encryptionKey = "")
        {
            await HandleItemAggregationAsync(this, itemModel, encryptionKey);
        }

        /// <inheritdoc />
        public async Task HandleItemAggregationAsync(IWiserItemsService service, WiserItemModel itemModel, string encryptionKey = "")
        {
            await wiserItemsService.HandleItemAggregationAsync(service, itemModel, encryptionKey);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllEntityBlocksAsync(string template)
        {
            return await wiserItemsService.ReplaceAllEntityBlocksAsync(template);
        }

        /// <inheritdoc />
        public async Task SaveItemDetailAsync(WiserItemDetailModel itemDetail, ulong itemId = 0, ulong itemLinkId = 0, string entityType = null, string username = "GCL", bool saveHistory = true)
        {
            await wiserItemsService.SaveItemDetailAsync(itemDetail, itemId, itemLinkId, entityType, username, saveHistory);
        }
    }
}