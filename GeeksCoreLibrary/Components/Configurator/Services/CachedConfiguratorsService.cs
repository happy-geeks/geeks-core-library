using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.Configurator.Services
{
    public class CachedConfiguratorsService : IConfiguratorsService
    {
        private readonly IConfiguratorsService configuratorsService;
        private readonly IAppCache appCache;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;
        private readonly ICacheService cacheService;

        public CachedConfiguratorsService(IConfiguratorsService configuratorsService,
            IAppCache appCache,
            IOptions<GclSettings> gclSettings,
            IObjectsService objectsService,
            ICacheService cacheService,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.configuratorsService = configuratorsService;
            this.appCache = appCache;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.gclSettings = gclSettings.Value;
            this.cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<DataTable> GetConfiguratorDataAsync(string name)
        {
            var addHostNameToCache = String.Equals(await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_CacheDataByDomain"), "true", StringComparison.OrdinalIgnoreCase);

            var cacheKeyName = new StringBuilder();
            cacheKeyName.Append("GetConfiguratorDataAsync_");
            if (addHostNameToCache)
            {
                var httpContext = httpContextAccessor?.HttpContext;
                var hostName = httpContext != null ? HttpContextHelpers.GetHostName(httpContext) : "";
                cacheKeyName.Append($"{hostName}_");
            }
            cacheKeyName.Append(name);
            
            return await appCache.GetOrAddAsync(cacheKeyName.ToString(), async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultConfiguratorsCacheDuration;
                return await configuratorsService.GetConfiguratorDataAsync(name);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Configurators));
        }

        /// <inheritdoc />
        public async Task<VueConfiguratorDataModel> GetVueConfiguratorDataAsync(string name)
        {
            var addHostNameToCache = String.Equals(await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_CacheDataByDomain"), "true", StringComparison.OrdinalIgnoreCase);

            var cacheKeyName = new StringBuilder();
            cacheKeyName.Append("GetVueConfiguratorDataAsync_");
            if (addHostNameToCache)
            {
                var httpContext = httpContextAccessor?.HttpContext;
                var hostName = httpContext != null ? HttpContextHelpers.GetHostName(httpContext) : "";
                cacheKeyName.Append($"{hostName}_");
            }
            cacheKeyName.Append(name);
            
            return await appCache.GetOrAddAsync(cacheKeyName.ToString(), async cacheEntry =>
            {
               cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultConfiguratorsCacheDuration;
               return await configuratorsService.GetVueConfiguratorDataAsync(name);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Configurators));
        }

        /// <inheritdoc />
        public async Task<ulong> SaveConfigurationAsync(ConfigurationsModel input, ulong? parentId = null)
        {
            return await configuratorsService.SaveConfigurationAsync(input, parentId);
        }

        /// <inheritdoc />
        public async Task<(string deliveryTime, string deliveryExtra)> GetDeliveryTimeAsync(ConfigurationsModel configuration)
        {
            return await configuratorsService.GetDeliveryTimeAsync(configuration);
        }

        /// <inheritdoc />
        public Task<string> ReplaceConfiguratorItemsAsync(string templateOrQuery, ConfigurationsModel configuration, bool isQuery)
        {
            return configuratorsService.ReplaceConfiguratorItemsAsync(templateOrQuery, configuration, isQuery);
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input)
        {
            return await configuratorsService.CalculatePriceAsync(input);
        }
    }
}
