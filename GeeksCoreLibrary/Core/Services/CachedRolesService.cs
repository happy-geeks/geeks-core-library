using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Services;

public class CachedRolesService(IRolesService rolesService, IAppCache cache, ICacheService cacheService, IOptions<GclSettings> gclSettings, ILogger<CachedRolesService> logger, IBranchesService branchesService)
    : IRolesService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<List<RoleModel>> GetRolesAsync(bool includePermissions = false)
    {
        var cacheName = new StringBuilder(CoreConstants.RolesDataCachingKey);

        // Base the cache name on whether permissions are included.
        cacheName.Append(includePermissions ? "WithPermissions" : "WithoutPermissions");
        cacheName.Append('_').Append(branchesService.GetDatabaseNameFromCookie());
        var roles = await cache.GetOrAddAsync(cacheName.ToString(),
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