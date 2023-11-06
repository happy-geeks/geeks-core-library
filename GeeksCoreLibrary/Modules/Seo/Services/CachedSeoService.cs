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
    public class CachedSeoService : ISeoService
    {
        private readonly IAppCache cache;
        private readonly ISeoService seoService;
        private readonly ICacheService cacheService;
        private readonly GclSettings gclSettings;
        private readonly IBranchesService branchesService;

        public CachedSeoService(IAppCache cache, ISeoService seoService, IOptions<GclSettings> gclSettings, ICacheService cacheService, IBranchesService branchesService)
        {
            this.cache = cache;
            this.seoService = seoService;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
            this.branchesService = branchesService;
        }

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
