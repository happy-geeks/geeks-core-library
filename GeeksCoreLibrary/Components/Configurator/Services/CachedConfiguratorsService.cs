using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Configurator.Interfaces;
using GeeksCoreLibrary.Components.Configurator.Models;
using GeeksCoreLibrary.Core.Models;
using LazyCache;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.Configurator.Services
{
    public class CachedConfiguratorsService : IConfiguratorsService
    {
        private readonly IConfiguratorsService configuratorsService;
        private readonly IAppCache appCache;
        private readonly GclSettings gclSettings;

        public CachedConfiguratorsService(IConfiguratorsService configuratorsService, IAppCache appCache, IOptions<GclSettings> gclSettings)
        {
            this.configuratorsService = configuratorsService;
            this.appCache = appCache;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<DataTable> GetConfiguratorDataAsync(string name)
        {
          
            return await appCache.GetOrAdd($"GetConfiguratorDataAsync_{name}",
                                        async cacheEntry =>
                                        {
                                            cacheEntry.SlidingExpiration = gclSettings.DefaultConfiguratorsCacheDuration;
                                            return await configuratorsService.GetConfiguratorDataAsync(name);
                                        });
        }

        /// <inheritdoc />
        public async Task<ulong> SaveConfigurationAsync(ConfigurationsModel input)
        {
            return await configuratorsService.SaveConfigurationAsync(input);
        }

        /// <inheritdoc />
        public async Task<(string deliveryTime, string deliveryExtra)> GetDeliveryTimeAsync(ConfigurationsModel configuration)
        {
            return await configuratorsService.GetDeliveryTimeAsync(configuration);
        }

        /// <inheritdoc />
        public Task<string> ReplaceConfiguratorItemsAsync(string templateOrQuery, ConfigurationsModel configuration)
        {
            return configuratorsService.ReplaceConfiguratorItemsAsync(templateOrQuery, configuration);
        }

        /// <inheritdoc />
        public async Task<(decimal purchasePrice, decimal customerPrice, decimal fromPrice)> CalculatePriceAsync(ConfigurationsModel input)
        {
            return await configuratorsService.CalculatePriceAsync(input);
        }
    }
}
