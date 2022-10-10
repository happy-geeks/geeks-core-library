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

        public CachedDatabaseConnection(IDatabaseConnection databaseConnection, IAppCache cache, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, ICacheService cacheService)
        {
            this.databaseConnection = databaseConnection;
            this.cache = cache;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
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
        public Task<DbDataReader> GetReaderAsync(string query)
        {
            return databaseConnection.GetReaderAsync(query);
        }

        /// <inheritdoc />
        public Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            // TODO: This skipCache parameter is temporary, and will be removed once a better solution is found to skip cache.
            if (skipCache)
            {
                return databaseConnection.GetAsync(query, cleanUp: cleanUp, useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
            }

            var currentUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
            var cacheName = new StringBuilder($"GCL_QUERY_{currentUri.Host}");

            if (gclSettings.MultiLanguageBasedOnUrlSegments)
            {
                cacheName.Append(currentUri.Segments.First().Trim('/'));
            }

            cacheName.Append(query.ToSha512Simple());
            foreach (var (key, value) in parameters.OrderBy(item => item.Key))
            {
                cacheName.Append($"{key}={value}");
            }

            return cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseConnection.GetAsync(query);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false)
        {
            // TODO: This skipCache parameter is temporary, and will be removed once a better solution is found to skip cache.
            if (skipCache)
            {
                return databaseConnection.GetAsJsonAsync(query, formatResult);
            }

            var cacheName = new StringBuilder("GCL_QUERY_");

            if (gclSettings.MultiLanguageBasedOnUrlSegments)
            {
                cacheName.Append(HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).Segments.First().Trim('/'));
            }

            cacheName.Append(query.ToSha512Simple());
            foreach (var (key, value) in parameters.OrderBy(item => item.Key))
            {
                cacheName.Append($"{key}={value}");
            }

            return cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                    return await databaseConnection.GetAsJsonAsync(query, formatResult);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));
        }

        /// <inheritdoc />
        public Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            return databaseConnection.ExecuteAsync(query, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true)
        {
            return databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(tableName, id, idColumnName, ignoreErrors, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
        {
            return databaseConnection.InsertRecordAsync(query, useWritingConnectionIfAvailable);
        }

        /// <inheritdoc />
        public Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
        {
            return databaseConnection.BeginTransactionAsync(forceNewTransaction);
        }

        /// <inheritdoc />
        public Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            return databaseConnection.CommitTransactionAsync(throwErrorIfNoActiveTransaction);
        }

        /// <inheritdoc />
        public Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            return databaseConnection.RollbackTransactionAsync(throwErrorIfNoActiveTransaction);
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
        public async Task ChangeConnectionStringsAsync(string newConnectionStringForReading, string newConnectionStringForWriting)
        {
            await databaseConnection.ChangeConnectionStringsAsync(newConnectionStringForReading, newConnectionStringForWriting);
        }

        /// <inheritdoc />
        public void SetCommandTimeout(int value)
        {
            databaseConnection.SetCommandTimeout(value);
        }
    }
}
