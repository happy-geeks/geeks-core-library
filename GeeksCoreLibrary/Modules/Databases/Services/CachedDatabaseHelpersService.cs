using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    /// <inheritdoc cref="IDatabaseHelpersService" />.
    public class CachedDatabaseHelpersService : IDatabaseHelpersService
    {
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IAppCache cache;
        private readonly ICacheService cacheService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IBranchesService branchesService;

        public CachedDatabaseHelpersService(IDatabaseHelpersService databaseHelpersService, IAppCache cache, IOptions<GclSettings> gclSettings, ICacheService cacheService, IDatabaseConnection databaseConnection, IBranchesService branchesService)
        {
            this.databaseHelpersService = databaseHelpersService;
            this.cache = cache;
            this.cacheService = cacheService;
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
            this.branchesService = branchesService;
        }

        public List<WiserTableDefinitionModel> ExtraWiserTableDefinitions
        {
            get => databaseHelpersService.ExtraWiserTableDefinitions;
            set => databaseHelpersService.ExtraWiserTableDefinitions = value;
        }

        /// <inheritdoc />
        public async Task<bool> ColumnExistsAsync(string tableName, string columnName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var cacheName = $"CachedDatabaseHelpersService_ColumnExistsAsync_{databaseName}_{tableName}_{columnName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.ColumnExistsAsync(tableName, columnName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetColumnNamesAsync(string tableName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var cacheName = $"CachedDatabaseHelpersService_GetColumnNamesAsync_{databaseName}_{tableName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.GetColumnNamesAsync(tableName, databaseName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null)
        {
            await AddColumnToTableAsync(this, tableName, settings, throwExceptionIfColumnAlreadyExists, databaseName);
        }

        /// <inheritdoc />
        public async Task AddColumnToTableAsync(IDatabaseHelpersService service, string tableName, ColumnSettingsModel settings, bool throwExceptionIfColumnAlreadyExists = true, string databaseName = null)
        {
            await databaseHelpersService.AddColumnToTableAsync(service, tableName, settings, throwExceptionIfColumnAlreadyExists, databaseName);
        }

        /// <inheritdoc />
        public async Task DropColumnAsync(string tableName, string columnName, string databaseName = null)
        {
            await databaseHelpersService.DropColumnAsync(tableName, columnName, databaseName);
        }

        /// <inheritdoc />
        public async Task CreateTableAsync(string tableName, IList<ColumnSettingsModel> primaryKeys, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            await databaseHelpersService.CreateTableAsync(tableName, primaryKeys, characterSet, collation, databaseName);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            await CreateOrUpdateTableAsync(this, tableName, columns, characterSet, collation, databaseName);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTableAsync(IDatabaseHelpersService service, string tableName, IList<ColumnSettingsModel> columns, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci", string databaseName = null)
        {
            await databaseHelpersService.CreateOrUpdateTableAsync(service, tableName, columns, characterSet, collation, databaseName);
        }

        /// <inheritdoc />
        public async Task<bool> TableExistsAsync(string tableName, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var cacheName = $"CachedDatabaseHelpersService_TableExistsAsync_{databaseName}_{tableName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.TableExistsAsync(tableName, databaseName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task<bool> DatabaseExistsAsync(string databaseName)
        {
            var cacheName = $"CachedDatabaseHelpersService_DatabaseExistsAsync_{databaseName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseHelpersService.DatabaseExistsAsync(databaseName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task DropTableAsync(string tableName, bool isTemporaryTable = false, string databaseName = null)
        {
            await databaseHelpersService.DropTableAsync(tableName, isTemporaryTable, databaseName);
        }

        /// <inheritdoc />
        public async Task DuplicateTableAsync(string tableToDuplicate, string newTableName, bool includeData = true, string sourceDatabaseName = null, string destinationTableName = null)
        {
            await databaseHelpersService.DuplicateTableAsync(tableToDuplicate, newTableName, includeData, sourceDatabaseName, destinationTableName);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateIndexesAsync(List<IndexSettingsModel> indexes, string databaseName = null)
        {
            await databaseHelpersService.CreateOrUpdateIndexesAsync(indexes, databaseName);
        }

        /// <inheritdoc />
        public async Task CreateDatabaseAsync(string databaseName, string characterSet = "utf8mb4", string collation = "utf8mb4_general_ci")
        {
            await databaseHelpersService.CreateDatabaseAsync(databaseName, characterSet, collation);
        }

        /// <inheritdoc />
        public async Task DropDatabaseAsync(string databaseName)
        {
            await databaseHelpersService.DropDatabaseAsync(databaseName);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, DateTime>> GetLastTableUpdatesAsync(string databaseName = null)
        {
            return await GetLastTableUpdatesAsync(this, databaseName);
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, DateTime>> GetLastTableUpdatesAsync(IDatabaseHelpersService service, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var cacheName = $"CachedDatabaseHelpersService_GetLastTableUpdates_{databaseName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
               async cacheEntry =>
               {
                   cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                   return await databaseHelpersService.GetLastTableUpdatesAsync(service, databaseName);
               }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task<bool> CheckAndUpdateTablesAsync(List<string> tablesToUpdate, string databaseName = null)
        {
            return await CheckAndUpdateTablesAsync(this, tablesToUpdate, databaseName);
        }

        /// <inheritdoc />
        public async Task<bool> CheckAndUpdateTablesAsync(IDatabaseHelpersService service, List<string> tablesToUpdate, string databaseName = null)
        {
            var changesMade = await databaseHelpersService.CheckAndUpdateTablesAsync(service, tablesToUpdate, databaseName);

            if (!changesMade)
            {
                return false;
            }

            // Remove the cache for last table updates, so that they will be retrieved from database next time, but only if we have made any changes to any table.
            // Otherwise we will get problems that we try to do the same changes multiple times, because the cache will have old dates then.
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = databaseConnection.ConnectedDatabase;
            }
            cache.Remove($"CachedDatabaseHelpersService_GetLastTableUpdates_{databaseName}");

            return true;
        }

        /// <inheritdoc />
        public async Task<IList<string>> GetAllTableNamesAsync(bool includeViews = false, string databaseName = null)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                await databaseConnection.EnsureOpenConnectionForReadingAsync();
                databaseName = databaseConnection.ConnectedDatabase;
            }

            var cacheName = $"CachedDatabaseHelpersService_GetAllTableNames_{databaseName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName, async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                return await databaseHelpersService.GetAllTableNamesAsync(includeViews, databaseName);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task RenameTableAsync(string currentTableName, string newTableName)
        {
            await databaseHelpersService.RenameTableAsync(currentTableName, newTableName);
        }

        /// <inheritdoc />
        public async Task OptimizeTablesAsync(params string[] tableNames)
        {
            await databaseHelpersService.OptimizeTablesAsync(tableNames);
        }
    }
}