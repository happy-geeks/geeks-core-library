using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Interfaces;
using GeeksCoreLibrary.Modules.Redirect.Models;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Redirect.Services;

public class CachedRedirectService(IAppCache cache, IRedirectService redirectService, IOptions<GclSettings> gclSettings, ICacheService cacheService, IBranchesService branchesService)
    : IRedirectService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<RedirectModel> GetRedirectAsync(Uri uri)
    {
        var cacheName = $"Redirect_{uri}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultRedirectModuleCacheDuration;
                return await redirectService.GetRedirectAsync(uri);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Redirects));
    }

    /// <inheritdoc />
    public Task<bool> RedirectModuleIsEnabledAsync()
    {
        return redirectService.RedirectModuleIsEnabledAsync();
    }

    /// <inheritdoc />
    public Task<string> GetMainDomainForRedirectAsync()
    {
        return redirectService.GetMainDomainForRedirectAsync();
    }

    /// <inheritdoc />
    public Task<bool> ShouldRedirectToUrlWithTrailingSlashAsync()
    {
        return redirectService.ShouldRedirectToUrlWithTrailingSlashAsync();
    }

    /// <inheritdoc />
    public Task<bool> ShouldRedirectToLowerCaseUrlAsync()
    {
        return redirectService.ShouldRedirectToLowerCaseUrlAsync();
    }

    /// <inheritdoc />
    public Task<bool> ShouldRedirectToHttpsAsync()
    {
        return redirectService.ShouldRedirectToHttpsAsync();
    }
}