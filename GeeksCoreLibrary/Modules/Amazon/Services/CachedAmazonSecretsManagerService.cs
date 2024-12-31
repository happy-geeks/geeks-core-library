using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Amazon.Services;

public class CachedAmazonSecretsManagerService : IAmazonSecretsManagerService
{
    private readonly GclSettings gclSettings;
    private readonly IAppCache cache;
    private readonly ICacheService cacheService;
    private readonly IAmazonSecretsManagerService amazonSecretsManagerService;
    private readonly IBranchesService branchesService;

    public CachedAmazonSecretsManagerService(
        IOptions<GclSettings> gclSettings,
        IAppCache cache,
        ICacheService cacheService,
        IAmazonSecretsManagerService amazonSecretsManagerService,
        IBranchesService branchesService)
    {
        this.gclSettings = gclSettings.Value;
        this.cache = cache;
        this.cacheService = cacheService;
        this.amazonSecretsManagerService = amazonSecretsManagerService;
        this.branchesService = branchesService;
    }

    /// <inheritdoc />
    public async Task<string> GetSshPrivateKeyFromAwsSecretsManagerAsync()
    {
        var cacheName = $"CachedAmazonSecretsManagerService_SshPrivateKey_{gclSettings.AwsSecretsManagerSettings.BaseDirectory}_{branchesService.GetDatabaseNameFromCookie()}";

        return await cache.GetOrAddAsync(cacheName, async cacheEntry =>
        {
            // This is to prevent unnecessary API calls to AWS Secrets Manager.
            if (gclSettings.DefaultAwsSecretsCacheDuration.TotalMinutes < Constants.MinimumDefaultAwsSecretsCacheDurationInMinutes)
            {
                gclSettings.DefaultAwsSecretsCacheDuration = new TimeSpan(0, Constants.MinimumDefaultAwsSecretsCacheDurationInMinutes, 0);
            }

            cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultAwsSecretsCacheDuration;
            return await amazonSecretsManagerService.GetSshPrivateKeyFromAwsSecretsManagerAsync();
        }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.AwsSecrets));
    }
}