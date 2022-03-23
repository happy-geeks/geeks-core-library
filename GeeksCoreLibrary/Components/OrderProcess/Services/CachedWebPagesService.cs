using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Core.Models;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    public class CachedOrderProcessesService: IOrderProcessesService
    {
        private readonly IAppCache cache;
        private readonly GclSettings gclSettings;
        private readonly IOrderProcessesService orderProcessesService;

        public CachedOrderProcessesService(IAppCache cache, IOptions<GclSettings> gclSettings, IOrderProcessesService orderProcessesService)
        {
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.orderProcessesService = orderProcessesService;
        }

        /// <inheritdoc />
        public async Task<(ulong Id, string Title, string FixedUrl)?> GetOrderProcessViaFixedUrl(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }

            var key = $"OrderProcessWithFixedUrl_{fixedUrl}";
            return await cache.GetOrAdd(key,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultWebPageCacheDuration;
                    return orderProcessesService.GetOrderProcessViaFixedUrl(fixedUrl);
                });
        }
    }
}
