using System;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.DataSelectorParser.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.DataSelectorParser.Services
{
    public class CachedDataSelectorParsersService : IDataSelectorParsersService
    {
        private readonly GclSettings gclSettings;
        private readonly IDataSelectorParsersService dataSelectorParsersService;
        private readonly IAppCache cache;
        private readonly ICacheService cacheService;
        private readonly IBranchesService branchesService;

        public CachedDataSelectorParsersService(IOptions<GclSettings> gclSettings, IDataSelectorParsersService dataSelectorParsersService, IAppCache cache, ICacheService cacheService, IBranchesService branchesService)
        {
            this.gclSettings = gclSettings.Value;
            this.dataSelectorParsersService = dataSelectorParsersService;
            this.cache = cache;
            this.cacheService = cacheService;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<JToken> GetDataSelectorResponseAsync(string dataSelectorId = null, string dataSelectorJson = null)
        {
            if (String.IsNullOrWhiteSpace(dataSelectorId) && String.IsNullOrWhiteSpace(dataSelectorJson))
            {
                return null;
            }

            var cacheName = new StringBuilder();
            cacheName.Append("GCLDataSelectorParser_");
            cacheName.Append(!String.IsNullOrWhiteSpace(dataSelectorId) ? dataSelectorId : dataSelectorJson.ToSha512Simple());
            cacheName.Append('_').Append(branchesService.GetDatabaseNameFromCookie());
            return await cache.GetOrAddAsync(
                cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultDataSelectorParsersCacheDuration;
                    return await dataSelectorParsersService.GetDataSelectorResponseAsync(dataSelectorId, dataSelectorJson);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.DataSelectors));
        }
    }
}
