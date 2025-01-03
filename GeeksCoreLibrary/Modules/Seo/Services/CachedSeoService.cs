using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Models;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Seo.Services
{
    public class CachedSeoService(IAppCache cache, ISeoService seoService, IOptions<GclSettings> gclSettings, ICacheService cacheService, IBranchesService branchesService)
        : ISeoService
    {
        private readonly GclSettings gclSettings = gclSettings.Value;

        /// <inheritdoc />
        public async Task<PageMetaDataModel> GetSeoDataForPageAsync(Uri pageUri)
        {
            var cacheName = $"Seo_{pageUri.AbsoluteUri}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultSeoModuleCacheDuration;
                    return await seoService.GetSeoDataForPageAsync(pageUri);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }

        /// <inheritdoc />
        public Task<bool> SeoModuleIsEnabledAsync()
        {
            return seoService.SeoModuleIsEnabledAsync();
        }

        /// <inheritdoc />
        public async Task<XDocument> GenerateSiteMap()
        {
            var cacheName = $"Sitemap_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultSeoModuleCacheDuration;
                    return await seoService.GenerateSiteMap();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }

        /// <inheritdoc />
        public async Task<XDocument> GenerateImageSiteMap()
        {
            var cacheName = $"ImageSitemap_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultSeoModuleCacheDuration;
                    return await seoService.GenerateImageSiteMap();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }
    }
}
