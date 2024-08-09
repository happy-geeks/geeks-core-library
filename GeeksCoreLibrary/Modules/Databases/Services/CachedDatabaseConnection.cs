using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Databases.Services
{
    public class CachedDatabaseConnection : IDatabaseConnection
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IAppCache cache;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;
        private readonly ConcurrentDictionary<string, object> parameters = new();
        private readonly GclSettings gclSettings;
        private readonly IBranchesService branchesService;

        public CachedDatabaseConnection(IDatabaseConnection databaseConnection,
            IAppCache cache,
            IOptions<GclSettings> gclSettings,
            ICacheService cacheService,
            IBranchesService branchesService,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.databaseConnection = databaseConnection;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await databaseConnection.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            databaseConnection.Dispose();
        }

        public DbConnection ConnectionForReading { get; set; }
        public DbConnection ConnectionForWriting { get; set; }

        /// <inheritdoc />
        public string ConnectedDatabase => databaseConnection.ConnectedDatabase;

        /// <inheritdoc />
        public string ConnectedDatabaseForWriting => databaseConnection.ConnectedDatabaseForWriting;

        /// <inheritdoc />
        public async Task<DbDataReader> GetReaderAsync(string query)
        {
            return await databaseConnection.GetReaderAsync(query);
        }

        /// <inheritdoc />
        public async Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            // TODO: This skipCache parameter is temporary, and will be removed once a better solution is found to skip cache.
            if (skipCache)
            {
                return await databaseConnection.GetAsync(query, cleanUp: cleanUp, useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
            }

            var currentUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);
            var cacheName = new StringBuilder($"GCL_QUERY_{currentUri.Host}");

            if (gclSettings.MultiLanguageBasedOnUrlSegments && currentUri.Segments.Length > gclSettings.IndexOfLanguagePartInUrl)
            {
                cacheName.Append(currentUri.Segments[gclSettings.IndexOfLanguagePartInUrl].Trim('/'));
            }

            cacheName.Append(query.ToSha512Simple());
            foreach (var (key, value) in parameters.OrderBy(item => item.Key))
            {
                if (!query.Contains($"?{key}", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't include parameters that are not used in the query.
                    continue;
                }

                cacheName.Append($"{key}={value}");
            }

            cacheName.Append('_').Append(branchesService.GetDatabaseNameFromCookie());
            return await cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseConnection.GetAsync(query, cleanUp: cleanUp, useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false)
        {
            // TODO: This skipCache parameter is temporary, and will be removed once a better solution is found to skip cache.
            if (skipCache)
            {
                return await databaseConnection.GetAsJsonAsync(query, formatResult);
            }

            var cacheName = new StringBuilder("GCL_QUERY_");

            var currentUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);
            if (gclSettings.MultiLanguageBasedOnUrlSegments && currentUri.Segments.Length > gclSettings.IndexOfLanguagePartInUrl)
            {
                cacheName.Append(currentUri.Segments[gclSettings.IndexOfLanguagePartInUrl].Trim('/'));
            }

            cacheName.Append(query.ToSha512Simple());
            foreach (var (key, value) in parameters.OrderBy(item => item.Key))
            {
                cacheName.Append($"{key}={value}");
            }

            cacheName.Append('_').Append(branchesService.GetDatabaseNameFromCookie());
            return await cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseConnection.GetAsJsonAsync(query, formatResult);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public async Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            return await databaseConnection.ExecuteAsync(query, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public async Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true)
        {
            return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(tableName, id, idColumnName, ignoreErrors, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public async Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
        {
            return await databaseConnection.InsertRecordAsync(query, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public async Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
        {
            return await databaseConnection.BeginTransactionAsync(forceNewTransaction);
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            await databaseConnection.CommitTransactionAsync(throwErrorIfNoActiveTransaction);
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            await databaseConnection.RollbackTransactionAsync(throwErrorIfNoActiveTransaction);
        }

        /// <inheritdoc />
        public void AddParameter(string key, object value)
        {
            databaseConnection.AddParameter(key, value);

            if (parameters.ContainsKey(key))
            {
                parameters.TryRemove(key, out _);
            }

            parameters.TryAdd(key, value);
        }

        /// <inheritdoc />
        public void ClearParameters()
        {
            databaseConnection.ClearParameters();
            parameters.Clear();
        }

        /// <inheritdoc />
        public string GetDatabaseNameForCaching(bool writeDatabase = false)
        {
            return databaseConnection.GetDatabaseNameForCaching(writeDatabase);
        }

        /// <inheritdoc />
        public async Task EnsureOpenConnectionForReadingAsync()
        {
            await databaseConnection.EnsureOpenConnectionForReadingAsync();
        }

        /// <inheritdoc />
        public async Task EnsureOpenConnectionForWritingAsync()
        {
            await databaseConnection.EnsureOpenConnectionForWritingAsync();
        }

        /// <inheritdoc />
        public async Task ChangeConnectionStringsAsync(string newConnectionStringForReading, string newConnectionStringForWriting, SshSettings sshSettingsForReading = null, SshSettings sshSettingsForWriting = null)
        {
            await databaseConnection.ChangeConnectionStringsAsync(newConnectionStringForReading, newConnectionStringForWriting, sshSettingsForReading, sshSettingsForWriting);
        }

        /// <inheritdoc />
        public void SetCommandTimeout(int value)
        {
            databaseConnection.SetCommandTimeout(value);
        }

        /// <inheritdoc />
        public bool HasActiveTransaction()
        {
            return databaseConnection.HasActiveTransaction();
        }

        /// <inheritdoc />
        public DbConnection GetConnectionForReading()
        {
            return databaseConnection.GetConnectionForReading();
        }

        /// <inheritdoc />
        public DbConnection GetConnectionForWriting()
        {
            return databaseConnection.GetConnectionForWriting();
        }

        /// <inheritdoc />
        public async Task<int> BulkInsertAsync(DataTable dataTable, string tableName, bool useWritingConnectionIfAvailable = true, bool useInsertIgnore = false)
        {
            return await databaseConnection.BulkInsertAsync(dataTable, tableName, useWritingConnectionIfAvailable, useInsertIgnore);
        }
    }
}