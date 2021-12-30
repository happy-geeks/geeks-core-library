using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.WebPage.Services
{
    public class CachedWebPagesService : IWebPagesService
    {
        private readonly IAppCache cache;
        private readonly GclSettings gclSettings;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IWebPagesService webPagesService;

        public CachedWebPagesService(IAppCache cache, IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IWebPagesService webPagesService)
        {
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.webPagesService = webPagesService;
        }

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl, List<string> Path, List<ulong> Parents)?> GetWebPageViaFixedUrl(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }

            var key = $"WebPagesWithFixedUrl_{fixedUrl}";
            return await cache.GetOrAdd(key,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultWebPageCacheDuration;
                    return webPagesService.GetWebPageViaFixedUrl(fixedUrl);
                });
        }
    }
}
