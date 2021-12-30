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
        public async Task<DataTable> GetConfiguratorDataAsync(string name, int componentId)
        {
          
            return await appCache.GetOrAdd($"Redirect_{name}_{componentId}",
                                        async cacheEntry =>
                                        {
                                            cacheEntry.SlidingExpiration = gclSettings.DefaultConfiguratorsCacheDuration;
                                            return await configuratorsService.GetConfiguratorDataAsync(name, componentId);
                                        });
        }
    }
}
