using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace GeeksCoreLibrary.Core.Services
{
    public class CacheService : ICacheService, ISingletonService
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<CacheService> logger;

        /// <inheritdoc />
        public ConcurrentDictionary<CacheAreas, CancellationTokenSource> CancellationTokenSources { get; }

        public CacheService(IWebHostEnvironment webHostEnvironment, ILogger<CacheService> logger)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;

            // The default concurrency of a concurrent dictionary if the processor count.
            CancellationTokenSources = new ConcurrentDictionary<CacheAreas, CancellationTokenSource>(Environment.ProcessorCount, Enum.GetNames<CacheAreas>().Length);
            foreach (var cacheArea in Enum.GetValues<CacheAreas>())
            {
                // The value "unknown" is not a valid cache area, so don't add it to the dictionary.
                if (cacheArea == CacheAreas.Unknown)
                {
                    continue;
                }

                CancellationTokenSources.TryAdd(cacheArea, new CancellationTokenSource());
            }
        }

        /// <inheritdoc />
        public CancellationTokenSource GetCacheAreaCancellationTokenSource(CacheAreas cacheArea)
        {
            if (cacheArea == CacheAreas.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(cacheArea), cacheArea.ToString("G"), $"The cache area '{cacheArea:G}' cannot be used for caching.");
            }

            CancellationTokenSources.TryAdd(cacheArea, new CancellationTokenSource());
            return CancellationTokenSources[cacheArea];
        }

        /// <inheritdoc />
        public void ClearCacheInArea(CacheAreas cacheArea)
        {
            if (!CancellationTokenSources.TryGetValue(cacheArea, out var cancellationTokenSource))
            {
                return;
            }

            // Cancel the token source, which triggers the cache items to expire.
            cancellationTokenSource.Cancel();

            logger.LogInformation($"Cleared '{cacheArea:G}' cache.");

            // Dispose the old CancellationTokenSource. It no longer serves a purpose.
            cancellationTokenSource.Dispose();

            // Now re-create the token source.
            CancellationTokenSources[cacheArea] = new CancellationTokenSource();
        }

        /// <inheritdoc />
        public void ClearMemoryCache()
        {
            foreach (var cacheArea in Enum.GetValues<CacheAreas>())
            {
                ClearCacheInArea(cacheArea);
            }
        }

        /// <inheritdoc />
        public void ClearOutputCache()
        {
            var outputCacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(outputCacheFolder))
            {
                return;
            }

            var directoryInfo = new DirectoryInfo(outputCacheFolder);
            if (!directoryInfo.Exists)
            {
                return;
            }

            var withIssues = false;
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"An error occurred trying to delete '{file.FullName}'.");
                    withIssues = true;
                }
            }

            logger.LogInformation(withIssues ? "Cleared output cache, but some files could not be deleted." : "Cleared output cache.");
        }

        /// <inheritdoc />
        public void ClearFilesCache()
        {
            var contentFilesFolder = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(contentFilesFolder))
            {
                return;
            }

            var directoryInfo = new DirectoryInfo(contentFilesFolder);
            if (!directoryInfo.Exists)
            {
                return;
            }

            var withIssues = false;
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"An error occurred trying to delete '{file.FullName}'.");
                    withIssues = true;
                }
            }

            logger.LogInformation(withIssues ? "Cleared files cache, but some files could not be deleted." : "Cleared files cache.");
        }

        /// <inheritdoc />
        public void ClearAllCache()
        {
            ClearMemoryCache();
            ClearOutputCache();
            ClearFilesCache();
        }

        /// <inheritdoc />
        public MemoryCacheEntryOptions CreateMemoryCacheEntryOptions(CacheAreas cacheArea)
        {
            var expireToken = new CancellationChangeToken(GetCacheAreaCancellationTokenSource(cacheArea).Token);
            return new MemoryCacheEntryOptions().AddExpirationToken(expireToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var cacheArea in Enum.GetValues<CacheAreas>())
            {
                if (CancellationTokenSources.TryGetValue(cacheArea, out var cancellationTokenSource))
                {
                    cancellationTokenSource?.Dispose();
                }
            }
        }
    }
}
