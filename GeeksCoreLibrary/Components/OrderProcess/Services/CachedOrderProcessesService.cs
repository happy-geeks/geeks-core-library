using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
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
        public async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl)
        {
            var key = $"OrderProcessWithFixedUrl_{fixedUrl}";
            return await cache.GetOrAdd(key,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetOrderProcessViaFixedUrlAsync(fixedUrl);
                });
        }

        /// <inheritdoc />
        public async Task<List<OrderProcessStepModel>> GetAllStepsGroupsAndFieldsAsync(ulong orderProcessId)
        {
            var key = $"OrderProcessGetAllStepsGroupsAndFields_{orderProcessId}";
            return await cache.GetOrAdd(key,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);
                });
        }

        /// <inheritdoc />
        public async Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId)
        {
            var key = $"OrderProcessGetPaymentMethods_{orderProcessId}";
            return await cache.GetOrAdd(key,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetPaymentMethodsAsync(orderProcessId);
                });
        }
    }
}
