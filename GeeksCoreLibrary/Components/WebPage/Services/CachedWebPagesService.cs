using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.WebPage.Services
{
    public class CachedWebPagesService : IWebPagesService
    {
        private readonly IAppCache cache;
        private readonly GclSettings gclSettings;
        private readonly IWebPagesService webPagesService;
        private readonly ILanguagesService languagesService;
        private readonly ICacheService cacheService;

        public CachedWebPagesService(IAppCache cache, IOptions<GclSettings> gclSettings, IWebPagesService webPagesService, ILanguagesService languagesService, ICacheService cacheService)
        {
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.webPagesService = webPagesService;
            this.languagesService = languagesService;
            this.cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrlAsync(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }

            var key = $"WebPagesWithFixedUrl_{fixedUrl}";
            return await cache.GetOrAddAsync(key,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWebPageCacheDuration;
                    return await webPagesService.GetWebPageViaFixedUrlAsync(fixedUrl);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WebPages));
        }

        /// <inheritdoc />
        public async Task<DataTable> GetWebPageResultAsync(WebPageCmsSettingsModel settings, Dictionary<string, string> extraData = null)
        {
            var key = $"WebPage_{languagesService.CurrentLanguageCode ?? ""}_{settings.PageId}_{settings.PageName ?? ""}_{settings.PathMustContainName ?? ""}_{settings.SearchNumberOfLevels}";
            if (extraData != null && extraData.Any())
            {
                key += $"_{String.Join("_", extraData.Select(x => $"{x.Key}={x.Value}"))}";
            }

            return await cache.GetOrAddAsync(key,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWebPageCacheDuration;
                    return await webPagesService.GetWebPageResultAsync(settings, extraData);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WebPages));
        }
    }
}
