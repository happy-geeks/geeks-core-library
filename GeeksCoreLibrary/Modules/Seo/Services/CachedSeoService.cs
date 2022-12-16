using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
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

        public CachedSeoService(IAppCache cache, ISeoService seoService, IOptions<GclSettings> gclSettings, ICacheService cacheService)
        {
            this.cache = cache;
            this.seoService = seoService;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<PageMetaDataModel> GetSeoDataForPageAsync(Uri pageUri)
        {
            return await cache.GetOrAddAsync($"Seo_{pageUri.AbsoluteUri}",
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
            return await cache.GetOrAddAsync("Sitemap",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultSeoModuleCacheDuration;
                    return await seoService.GenerateSiteMap();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }

        /// <inheritdoc />
        public async Task<XDocument> GenerateImageSiteMap()
        {
            return await cache.GetOrAddAsync("ImageSitemap",
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultSeoModuleCacheDuration;
                    return await seoService.GenerateImageSiteMap();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }
    }
}
