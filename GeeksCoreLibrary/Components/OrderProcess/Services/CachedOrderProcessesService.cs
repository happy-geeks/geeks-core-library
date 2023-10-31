using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    public class CachedOrderProcessesService: DecoratorOrderProcessesService
    {
        private readonly IAppCache cache;
        private readonly GclSettings gclSettings;
        private readonly IOrderProcessesService orderProcessesService;
        private readonly IBranchesService branchesService;

        public CachedOrderProcessesService(IAppCache cache, IOptions<GclSettings> gclSettings, IOrderProcessesService orderProcessesService, IBranchesService branchesService) : base(orderProcessesService)
        {
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.orderProcessesService = orderProcessesService;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public override async Task<OrderProcessSettingsModel> GetOrderProcessSettingsAsync(ulong orderProcessId)
        {
            var cacheName = $"OrderProcessSettings_{orderProcessId}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                delegate(ICacheEntry cacheEntry)
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
                });
        }

        /// <inheritdoc />
        public override async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl)
        {
            var cacheName = $"OrderProcessWithFixedUrl_{fixedUrl}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                delegate(ICacheEntry cacheEntry)
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetOrderProcessViaFixedUrlAsync(this, fixedUrl);
                });
        }

        /// <inheritdoc />
        public override async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(IOrderProcessesService service, string fixedUrl)
        {
            return await orderProcessesService.GetOrderProcessViaFixedUrlAsync(service, fixedUrl);
        }

        /// <inheritdoc />
        public override async Task<List<OrderProcessStepModel>> GetAllStepsGroupsAndFieldsAsync(ulong orderProcessId)
        {
            // Always return a new instance. The values of the fields will be set by the user, if a cached element is used each user will have a reference to the same object and thus sharing information.
            return await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);
        }

        /// <inheritdoc />
        public override async Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId, UserCookieDataModel loggedInUser = null)
        {
            var cacheName = $"OrderProcessGetPaymentMethods_{orderProcessId}_{(loggedInUser == null ? "all" : loggedInUser.UserId.ToString())}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                delegate(ICacheEntry cacheEntry)
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetPaymentMethodsAsync(orderProcessId, loggedInUser);
                });
        }

        /// <inheritdoc />
        public override async Task<PaymentMethodSettingsModel> GetPaymentMethodAsync(ulong paymentMethodId)
        {
            var cacheName = $"OrderProcessGetPaymentMethodAsync_{paymentMethodId}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                delegate(ICacheEntry cacheEntry)
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultOrderProcessCacheDuration;
                    return orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
                });
        }

        /// <inheritdoc />
        public override async Task<PaymentRequestResult> HandlePaymentRequestAsync(ulong orderProcessId)
        {
            return await orderProcessesService.HandlePaymentRequestAsync(this, orderProcessId);
        }

        /// <inheritdoc />
        public override async Task<bool> HandlePaymentStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
        {
            return await orderProcessesService.HandlePaymentStatusUpdateAsync(this, orderProcessSettings, conceptOrders, newStatus, isSuccessfulStatus, convertConceptOrderToOrder);
        }

        /// <inheritdoc />
        public override async Task<bool> HandlePaymentServiceProviderWebhookAsync(ulong orderProcessId, ulong paymentMethodId)
        {
            return await orderProcessesService.HandlePaymentServiceProviderWebhookAsync(this, orderProcessId, paymentMethodId);
        }

        /// <inheritdoc />
        public override async Task<PaymentReturnResult> HandlePaymentReturnAsync(ulong orderProcessId, ulong paymentMethodId)
        {
            return await orderProcessesService.HandlePaymentReturnAsync(this, orderProcessId, paymentMethodId);
        }
    }
}
