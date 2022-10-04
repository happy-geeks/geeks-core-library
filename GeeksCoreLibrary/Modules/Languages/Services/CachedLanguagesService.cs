using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Models;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace GeeksCoreLibrary.Modules.Languages.Services
{
    /// <inheritdoc cref="ILanguagesService" />
    public class CachedLanguagesService : ILanguagesService
    {
        private readonly ILanguagesService languagesService;
        private readonly IObjectsService objectsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAppCache cache;
        private readonly ICacheService cacheService;
        private readonly GclSettings gclSettings;
        private readonly ILogger<CachedLanguagesService> logger;

        /// <summary>
        /// Creates a new instance of <see cref="CachedLanguagesService"/>.
        /// </summary>
        public CachedLanguagesService(ILogger<CachedLanguagesService> logger, ILanguagesService languagesService, IObjectsService objectsService, IAppCache cache, IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, ICacheService cacheService)
        {
            this.logger = logger;
            this.languagesService = languagesService;
            this.objectsService = objectsService;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
        }
        
        /// <inheritdoc />
        public string CurrentLanguageCode
        {
            get => languagesService.CurrentLanguageCode;
            set => languagesService.CurrentLanguageCode = value;
        }

        /// <inheritdoc />
        public async Task<string> GetTranslationAsync(string original, string languageItemCode = null, string defaultValue = null)
        {
            var cachedLanguages = await CacheLanguageAsync(!String.IsNullOrWhiteSpace(languageItemCode) ? languageItemCode : CurrentLanguageCode);
            return cachedLanguages.ContainsKey(original) && !String.IsNullOrEmpty(cachedLanguages[original]?.ToString()) ? cachedLanguages[original]?.ToString() : (defaultValue ?? original);
        }

        /// <inheritdoc />
        public async Task<string> GetLanguageCodeAsync()
        {
            // Check if it should be overriden through a query string.
            if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.Request.Query.ContainsKey(Constants.LanguageCodeQueryStringKey) && !String.IsNullOrWhiteSpace(httpContextAccessor.HttpContext.Request.Query[Constants.LanguageCodeQueryStringKey]))
            {
                CurrentLanguageCode = httpContextAccessor.HttpContext.Request.Query[Constants.LanguageCodeQueryStringKey];
                logger.LogDebug($"LanguageCode determined through query string: {CurrentLanguageCode}");
                return CurrentLanguageCode;
            }
            
            var cacheName = new StringBuilder(Constants.LanguageCodeCacheKey);
            
            // Add hostname to cache key, because websites often have a different hostname per language.
            cacheName.Append('_').Append(HttpContextHelpers.GetHostName(httpContextAccessor.HttpContext));

            if (gclSettings.MultiLanguageBasedOnUrlSegments)
            {
                cacheName.Append('_').Append(HttpContextHelpers.GetUrlPrefix(httpContextAccessor.HttpContext));
            }

            var currentLanguageCode = await cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    // Use the normal languages service to get the language code, which always looks in the database if necessary.
                    var languageCode = await languagesService.GetLanguageCodeAsync();
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultLanguagesCacheDuration;
                    logger.LogDebug($"Cached language code '{languageCode}' in cache key '{cacheName}'.");
                    return languageCode;
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Languages));

            CurrentLanguageCode = currentLanguageCode;

            return currentLanguageCode;
        }

        /// <inheritdoc />
        public async Task<List<LanguageModel>> GetAllLanguagesAsync()
        {
            return await cache.GetOrAddAsync("all_languages",
                 async cacheEntry =>
                 {
                     // Use the normal languages service to get the language code, which always looks in the database if necessary.
                     var allLanguages = await languagesService.GetAllLanguagesAsync();
                     cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultLanguagesCacheDuration;
                     return allLanguages;
                 },
                 cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Languages));
        }

        /// <summary>
        /// Gets and caches all translations of a language.
        /// </summary>
        /// <param name="languageCode">The language code of the language </param>
        /// <returns></returns>
        private async Task<Hashtable> CacheLanguageAsync(string languageCode)
        {
            return await cache.GetOrAddAsync($"GCLTranslations{languageCode}", GetLanguagesAndTranslations, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Languages));

            async Task<Hashtable> GetLanguagesAndTranslations(ICacheEntry cacheEntry)
            {
                logger.LogDebug("Caching of translations started");
                var translations = new Hashtable(StringComparer.OrdinalIgnoreCase);

                try
                {
                    databaseConnection.AddParameter("gcl_languageCode", languageCode);
                    databaseConnection.AddParameter("gcl_groupName", Constants.TranslationsGroupName);
                    databaseConnection.AddParameter("gcl_translationsItemId", await objectsService.FindSystemObjectByDomainNameAsync("W2LANGUAGES_TranslationsItemId"));
                    await using (var reader = await databaseConnection.GetReaderAsync(
                        @$"SELECT
	                            `key`,
	                            CONCAT_WS('', `value`, long_value) AS `value`
                            FROM {WiserTableNames.WiserItemDetail}
                            WHERE item_id = ?gcl_translationsItemId
                                AND groupname = ?gcl_groupName
                                AND language_code = ?gcl_languageCode"))
                    {
                        while (await reader.ReadAsync())
                        {
                            translations.Add(reader.GetStringHandleNull(0), reader.GetStringHandleNull(1));
                        }
                    }

                    logger.LogDebug($"{translations.Count} translations loaded from database for language ID {languageCode}");
                }
                catch (Exception exception)
                {
                    logger.LogError($"Error loading translations. Error message: {exception}");
                }
                                
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultLanguagesCacheDuration;
                logger.LogDebug("Caching of translations completed");

                return translations;
            }
        }
    }
}
