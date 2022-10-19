using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services;

public class CachedRolesService : IRolesService
{
    private readonly IRolesService rolesService;
    private readonly IAppCache cache;
    private readonly ICacheService cacheService;
    private readonly GclSettings gclSettings;
    private readonly ILogger<CachedRolesService> logger;

    public CachedRolesService(IRolesService rolesService, IAppCache cache, ICacheService cacheService, IOptions<GclSettings> gclSettings, ILogger<CachedRolesService> logger)
    {
        this.rolesService = rolesService;
        this.cache = cache;
        this.cacheService = cacheService;
        this.gclSettings = gclSettings.Value;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RoleModel>> GetRolesAsync(bool includePermissions = false)
    {
        var cacheName = new StringBuilder(CoreConstants.RolesDataCachingKey);

        // Base the cache name on whether permissions are included.
        cacheName.Append(includePermissions ? "WithPermissions" : "WithoutPermissions");

        var roles = await cache.GetOrAddAsync(
            cacheName.ToString(),
            async cacheEntry =>
            {
                // Use the normal roles service to get the roles.
                var roles = await rolesService.GetRolesAsync(includePermissions);
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultQueryCacheDuration;
                logger.LogDebug($"Cached roles {(includePermissions ? "with" : "without")} permissions in cache key '{cacheName}'.");
                return roles;
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Database));

        return roles;
    }
}