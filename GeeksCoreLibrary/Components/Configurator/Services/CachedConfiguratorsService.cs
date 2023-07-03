using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
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
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;
        private readonly ICacheService cacheService;

        public CachedConfiguratorsService(IConfiguratorsService configuratorsService, IAppCache appCache, IOptions<GclSettings> gclSettings, IObjectsService objectsService, ILanguagesService languagesService, ICacheService cacheService, IHttpContextAccessor httpContextAccessor = null)
        {
            this.configuratorsService = configuratorsService;
            this.appCache = appCache;
            this.objectsService = objectsService;
            this.languagesService = languagesService;
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
        public async Task<VueConfiguratorDataModel> GetVueConfiguratorDataAsync(string name, bool includeStepsData = true)
        {
            var addHostNameToCache = String.Equals(await objectsService.GetSystemObjectValueAsync("CONFIGURATOR_CacheDataByDomain"), "true", StringComparison.OrdinalIgnoreCase);

            // Make sure the language code has a value.
            if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                // This function fills the property "CurrentLanguageCode".
                await languagesService.GetLanguageCodeAsync();
            }

            var cacheKeyName = new StringBuilder();
            cacheKeyName.Append("GetVueConfiguratorDataAsync_");
            cacheKeyName.Append($"{languagesService.CurrentLanguageCode}_");

            if (includeStepsData)
            {
                cacheKeyName.Append("includeStepsData_");
            }
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
               return await configuratorsService.GetVueConfiguratorDataAsync(name, includeStepsData);
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
        public Task<string> ReplaceConfiguratorItemsAsync(string template, VueConfigurationsModel configuration, bool isDataQuery)
        {
            return configuratorsService.ReplaceConfiguratorItemsAsync(template, configuration, isDataQuery);
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input)
        {
            return await configuratorsService.CalculatePriceAsync(input);
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(VueConfigurationsModel input)
        {
            return await configuratorsService.CalculatePriceAsync(input);
        }
    }
}
