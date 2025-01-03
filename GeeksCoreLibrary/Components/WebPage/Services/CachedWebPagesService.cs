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
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.WebPage.Services
{
    public class CachedWebPagesService(IAppCache cache, IOptions<GclSettings> gclSettings, IWebPagesService webPagesService, ILanguagesService languagesService, ICacheService cacheService, IStringReplacementsService stringReplacementsService, IBranchesService branchesService)
        : IWebPagesService
    {
        private readonly GclSettings gclSettings = gclSettings.Value;

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrlAsync(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }

            var cacheName = $"WebPagesWithFixedUrl_{fixedUrl}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWebPageCacheDuration;
                    return await webPagesService.GetWebPageViaFixedUrlAsync(fixedUrl);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WebPages));
        }

        /// <inheritdoc />
        public async Task<DataTable> GetWebPageResultAsync(WebPageCmsSettingsModel settings, Dictionary<string, string> extraData = null)
        {
            var pageName = String.Empty;
            var pathMustContainName = String.Empty;
            var languageCode = String.Empty;

            if (!String.IsNullOrWhiteSpace(settings.PageName))
            {
                pageName = await stringReplacementsService.DoAllReplacementsAsync(stringReplacementsService.DoReplacements(settings.PageName, extraData));
            }

            if (!String.IsNullOrWhiteSpace(settings.PathMustContainName))
            {
                pathMustContainName = await stringReplacementsService.DoAllReplacementsAsync(stringReplacementsService.DoReplacements(settings.PathMustContainName, extraData));
            }

            if (!String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                languageCode = await stringReplacementsService.DoAllReplacementsAsync(languagesService.CurrentLanguageCode);
            }

            var cacheName = $"WebPage_{languageCode}_{settings.PageId}_{pageName}_{pathMustContainName}_{settings.SearchNumberOfLevels}";
            if (extraData != null && extraData.Any())
            {
                cacheName += $"_{String.Join("_", extraData.Select(x => $"{x.Key}={x.Value}"))}";
            }

            cacheName += $"_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultWebPageCacheDuration;
                    return await webPagesService.GetWebPageResultAsync(settings, extraData);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.WebPages));
        }
    }
}
