using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    /// <inheritdoc cref="IDatabaseHelpersService" />.
    public class CachedDatabaseHelpersService : IDatabaseHelpersService
    {
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IAppCache cache;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;
        private readonly ConcurrentDictionary<string, object> parameters = new();
        private readonly GclSettings gclSettings;

        public CachedDatabaseHelpersService(IDatabaseHelpersService databaseHelpersService, IAppCache cache, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, ICacheService cacheService)
        {
            this.databaseHelpersService = databaseHelpersService;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            var cacheName = $"CachedDatabaseHelpersService_ColumnExistsAsync_{tableName}_{columnName}";
            return cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.ColumnExistsAsync(tableName, columnName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public Task<List<string>> GetColumnNamesAsync(string tableName)
        {
            var cacheName = $"CachedDatabaseHelpersService_GetColumnNamesAsync_{tableName}";
            return cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.GetColumnNamesAsync(tableName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true)
        {
            await databaseHelpersService.AddColumnToTableAsync(tableName, settings, throwExceptionIfColumnAlreadyExists);
        }

        /// <inheritdoc />
        public async Task DropColumnAsync(string tableName, string columnName)
        {
            await databaseHelpersService.DropColumnAsync(tableName, columnName);
        }

        /// <inheritdoc />
        public async Task CreateTableAsync(string tableName, IList<ColumnSettingsModel> primaryKeys, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            await databaseHelpersService.CreateTableAsync(tableName, primaryKeys, characterSet, collation);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            await databaseHelpersService.CreateOrUpdateTableAsync(tableName, columns, characterSet, collation);
        }

        /// <inheritdoc />
        public Task<bool> TableExistsAsync(string tableName, string databaseName = null)
        {
            var cacheName = $"CachedDatabaseHelpersService_TableExistsAsync_{tableName}";
            return cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;                    
                    return await databaseHelpersService.TableExistsAsync(tableName, databaseName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public Task<bool> DatabaseExistsAsync(string databaseName)
        {
            var cacheName = $"CachedDatabaseHelpersService_DatabaseExistsAsync_{databaseName}";
            return cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.DatabaseExistsAsync(databaseName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task DropTableAsync(string tableName, bool isTemporaryTable = false)
        {
            await databaseHelpersService.DropTableAsync(tableName, isTemporaryTable);
        }

        /// <inheritdoc />
        public async Task DuplicateTableAsync(string tableToDuplicate, string newTableName, bool includeData = true)
        {
            await databaseHelpersService.DuplicateTableAsync(tableToDuplicate, newTableName, includeData);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateIndexesAsync(List<IndexSettingsModel> indexes)
        {
            await databaseHelpersService.CreateOrUpdateIndexesAsync(indexes);
        }

        /// <inheritdoc />
        public async Task CreateDatabaseAsync(string databaseName, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            await databaseHelpersService.CreateDatabaseAsync(databaseName, characterSet, collation);
        }

        /// <inheritdoc />
        public Task<Dictionary<string, DateTime>> GetLastTableUpdatesAsync()
        {
            var cacheName = "CachedDatabaseHelpersService_GetLastTableUpdates";
            return cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;                    
                    return await databaseHelpersService.GetLastTableUpdatesAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task CheckAndUpdateTablesAsync(List<string> tablesToUpdate, Dictionary<string, DateTime> tableChanges = null)
        {
            tableChanges ??= await GetLastTableUpdatesAsync();

            await databaseHelpersService.CheckAndUpdateTablesAsync(tablesToUpdate, tableChanges);

            // Remove the cache for last table updates, so that they will be retrieved from database next time.
            // Otherwise we will get problems that we try to do the same changes multiple times, because the cache will have old dates then.
            cache.Remove("CachedDatabaseHelpersService_GetLastTableUpdates");
        }
    }
}
