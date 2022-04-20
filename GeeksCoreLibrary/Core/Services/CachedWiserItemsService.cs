using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services
{
    public class CachedWiserItemsService : IWiserItemsService
    {
        private readonly GclSettings gclSettings;
        private readonly IAppCache cache;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly ICacheService cacheService;

        public CachedWiserItemsService(IOptions<GclSettings> gclSettings, IAppCache cache, IWiserItemsService wiserItemsService, IDatabaseConnection databaseConnection, ICacheService cacheService)
        {
            this.gclSettings = gclSettings.Value;
            this.cache = cache;
            this.wiserItemsService = wiserItemsService;
            this.databaseConnection = databaseConnection;
            this.cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 0, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, EntitySettingsModel entityTypeSettings = null)
        {
            entityTypeSettings ??= await GetEntityTypeSettingsAsync(wiserItem.EntityType);
            return await wiserItemsService.SaveAsync(wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> CreateAsync(WiserItemModel wiserItem, ulong? parentId = null, int linkTypeNumber = 1, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool saveHistory = true, bool createNewTransaction = true, EntitySettingsModel entityTypeSettings = null)
        {
            if (String.IsNullOrWhiteSpace(wiserItem?.EntityType))
            {
                throw new ArgumentNullException(nameof(wiserItem.EntityType));
            }

            entityTypeSettings ??= await GetEntityTypeSettingsAsync(wiserItem.EntityType);
            return await wiserItemsService.CreateAsync(wiserItem, parentId, linkTypeNumber, userId, username, encryptionKey, saveHistory, createNewTransaction, entityTypeSettings);
        }

        /// <inheritdoc />
        public async Task<WiserItemDuplicationResultModel> DuplicateItemAsync(ulong itemId, ulong parentId, string username = "GCL", string encryptionKey = "", ulong userId = 0, string entityType = null, string parentEntityType = null, bool createNewTransaction = true)
        {
            return await wiserItemsService.DuplicateItemAsync(itemId, parentId, username, encryptionKey, userId, entityType, parentEntityType, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> UpdateAsync(ulong itemId, WiserItemModel wiserItem, ulong userId = 0, string username = "GCL", string encryptionKey = "", bool alwaysSaveValues = false, bool saveHistory = true, bool createNewTransaction = true, EntitySettingsModel entityTypeSettings = null)
        {
            entityTypeSettings ??= await GetEntityTypeSettingsAsync(wiserItem.EntityType);
            return await wiserItemsService.UpdateAsync(itemId, wiserItem, userId, username, encryptionKey, alwaysSaveValues, saveHistory, createNewTransaction, entityTypeSettings);
        }

        /// <inheritdoc />
        public async Task<int> ChangeEntityTypeAsync(ulong itemId, string newEntityType, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            return await wiserItemsService.ChangeEntityTypeAsync(itemId, newEntityType, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(ulong itemId, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true)
        {
            return await wiserItemsService.DeleteAsync(itemId, undelete, username, userId, saveHistory, entityType, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAsync(List<ulong> itemIds, bool undelete = false, string username = "GCL", ulong userId = 0, bool saveHistory = true, string entityType = null, bool createNewTransaction = true)
        {
            return await wiserItemsService.DeleteAsync(itemIds, undelete, username, userId, saveHistory, entityType, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteWorkflowAsync(ulong itemId, bool isNewItem, EntitySettingsModel entitySettingsModel, WiserItemModel wiserItem = null, ulong userId = 0, string username = "GCL", bool saveHistory = true)
        {
            return await wiserItemsService.ExecuteWorkflowAsync(itemId, isNewItem, entitySettingsModel, wiserItem, userId, username, saveHistory);
        }

        /// <inheritdoc />
        public async Task<(bool ok, string errorMessage, AccessRights permissions)> CheckIfEntityActionIsPossibleAsync(ulong itemId, EntityActions action, ulong userId, WiserItemModel wiserItem = null, bool onlyCheckAccessRights = false, string entityType = null, AccessRights? permissions = null)
        {
            permissions ??= await GetUserItemPermissionsAsync(itemId, userId, entityType);
            return await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, action, userId, wiserItem, onlyCheckAccessRights, entityType, permissions);
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserItemPermissionsAsync(ulong itemId, ulong userId, string entityType = null)
        {
            var cacheKey = $"user_item_permission_{itemId}_{userId}_{entityType ?? ""}_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserItemPermissionsAsync(itemId, userId, entityType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<AccessRights> GetUserModulePermissions(int moduleId, ulong userId)
        {
            var cacheKey = $"user_module_permission_{moduleId}_{userId}_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetUserModulePermissions(moduleId, userId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetItemDetailsAsync(ulong itemId = 0, string uniqueId = "", string languageCode = "", ulong userId = 0, string detailKey = "", string detailValue = "", bool returnNullIfDeleted = true, bool skipDetailsWithoutLanguageCode = false, string entityType = null)
        {
            return await wiserItemsService.GetItemDetailsAsync(itemId, uniqueId, languageCode, userId, detailKey, detailValue, returnNullIfDeleted, skipDetailsWithoutLanguageCode, entityType);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> GetLinkedItemDetailsAsync(ulong itemId, int linkType = -1, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null)
        {
            return await wiserItemsService.GetLinkedItemDetailsAsync(itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType);
        }

        /// <inheritdoc />
        public async Task<List<ulong>> GetLinkedItemIdsAsync(ulong itemId, int linkType, string entityType = null, bool includeDeletedItems = false, ulong userId = 0, bool reverse = false, string itemIdEntityType = null)
        {
            return await wiserItemsService.GetLinkedItemIdsAsync(itemId, linkType, entityType, includeDeletedItems, userId, reverse, itemIdEntityType);
        }

        /// <inheritdoc />
        public async Task<EntitySettingsModel> GetEntityTypeSettingsAsync(string entityType, int moduleId = 0)
        {
            var cacheKey = $"entity_type_settings{entityType}_{moduleId}_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;                    
                    return await wiserItemsService.GetEntityTypeSettingsAsync(entityType, moduleId);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<(string template, DataRow dataRow)> GetTemplateAndDataForItemAsync(ulong itemId, string entityType = null)
        {
            return await wiserItemsService.GetTemplateAndDataForItemAsync(itemId, entityType);
        }

        /// <inheritdoc />
        public async Task<int> GetLinkTypeAsync(string destinationEntityType, string connectedEntityType)
        {
            var cacheKey = $"link_type_{destinationEntityType}_{connectedEntityType}_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetLinkTypeAsync(destinationEntityType, connectedEntityType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemLinkAsync(ulong itemId, ulong destinationItemId, int type, int ordering = 1, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            return await wiserItemsService.AddItemLinkAsync(itemId, destinationItemId, type, ordering, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksAsync(ulong destinationItemId, int type, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.RemoveItemLinksAsync(destinationItemId, type, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task RemoveItemLinksByIdAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.RemoveItemLinksByIdAsync(ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task RemoveParentLinkOfItemsAsync(List<ulong> ids, string sourceEntityType, List<ulong> sourceIds, string destinationEntityType, List<ulong> destinationIds, string username = "JCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.RemoveParentLinkOfItemsAsync(ids, sourceEntityType, sourceIds, destinationEntityType, destinationIds, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task RemoveLinkedItemsAsync(ulong destinationItemId, int type = 0, List<ulong> exceptItemIds = null, string username = "GCL", ulong userId = 0UL, bool saveHistory = true, string entityType = null, bool createNewTransaction = true)
        {
            await wiserItemsService.RemoveLinkedItemsAsync(destinationItemId, type, exceptItemIds, username, userId, saveHistory, entityType, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task ChangeItemLinksAsync(ulong oldDestinationItemId, ulong newDestinationItemId, int type = 0, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.ChangeItemLinksAsync(oldDestinationItemId, newDestinationItemId, type, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypesAsync(ulong destinationItemId, int oldLinkType, int newLinkType, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.ChangeLinkTypesAsync(destinationItemId, oldLinkType, newLinkType, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task ChangeLinkTypeAsync(ulong destinationItemId, int oldLinkType, int newLinkType, ulong sourceItemId, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            await wiserItemsService.ChangeLinkTypeAsync(destinationItemId, oldLinkType, newLinkType, sourceItemId, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task<ulong> AddItemFileAsync(WiserItemFileModel wiserItemFile, string username = "GCL", ulong userId = 0, bool saveHistory = true)
        {
            return await wiserItemsService.AddItemFileAsync(wiserItemFile, username, userId, saveHistory);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetItemFileAsync(ulong id, string field = "Id")
        {
            return await wiserItemsService.GetItemFileAsync(id, field);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemFileModel>> GetItemFilesAsync(ulong[] ids, string field = "Id")
        {
            return await wiserItemsService.GetItemFilesAsync(ids, field);
        }

        /// <inheritdoc />
        public async Task<string> GetTablePrefixForEntityAsync(string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType))
            {
                return "";
            }

            var settings = await GetEntityTypeSettingsAsync(entityType);
            return GetTablePrefixForEntity(settings);
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
            var cacheKey = $"all_link_type_settings_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWiserItemsCacheDuration;
                    return await wiserItemsService.GetAllLinkTypeSettingsAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WiserItems));
        }

        /// <inheritdoc />
        public async Task<LinkSettingsModel> GetLinkTypeSettingsByIdAsync(int linkId)
        {
            IEnumerable<LinkSettingsModel> result = await GetAllLinkTypeSettingsAsync();
            if (linkId > 0)
            {
                result = result.Where(t => t.Id == linkId);
            }

            return result.FirstOrDefault() ?? new LinkSettingsModel();
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
    }
}
