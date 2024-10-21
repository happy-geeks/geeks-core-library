using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Extensions;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Models;
using GeeksCoreLibrary.Modules.Templates.Services;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Objects.Services
{
    public class CachedObjectsService : IObjectsService
    {
        private readonly IObjectsService objectsService;
        private readonly IAppCache cache;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;
        private readonly GclSettings gclSettings;
        private readonly IBranchesService branchesService;

        private readonly string hostName;
        private readonly string hostNameIncludingTestWww;
        private readonly string urlPrefix;

        public CachedObjectsService(IObjectsService objectsService,
            IAppCache cache,
            IOptions<GclSettings> gclSettings,
            IDatabaseConnection databaseConnection,
            ICacheService cacheService,
            IBranchesService branchesService,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.objectsService = objectsService;
            this.cache = cache;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.gclSettings = gclSettings.Value;
            this.branchesService = branchesService;

            hostName = HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false);
            hostNameIncludingTestWww = HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includingTestWww: true, includePort: false);
            urlPrefix = HttpContextHelpers.GetUrlPrefix(httpContextAccessor?.HttpContext, gclSettings.Value.IndexOfLanguagePartInUrl);
        }

        /// <summary>
        /// Get all objects from the database and load them into the caching object
        /// </summary>
        private async Task<Dictionary<string, SettingObject>> CacheObjectsAsync()
        {
            var cacheName = $"SettingObjects_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName, GetAllObjects, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Objects));

            async Task<Dictionary<string, SettingObject>> GetAllObjects(ICacheEntry cacheEntry)
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultObjectsCacheDuration;

                var objects = new Dictionary<string, SettingObject>(StringComparer.OrdinalIgnoreCase);

                await using var reader = await databaseConnection.GetReaderAsync(@"SELECT `key`, `value`, `description`, `typenr` FROM easy_objects WHERE active = 1");
                while (await reader.ReadAsync())
                {
                    objects.Add($"{reader.GetString(reader.GetOrdinal("key"))}{reader.GetInt32(reader.GetOrdinal("typenr"))}", reader.ToObjectModel());
                }

                return objects;
            }
        }

        /// <inheritdoc />
        public async Task<string> FindSystemObjectByDomainNameAsync(string objectKey, string defaultResult = "", string overrideDomain = "", bool searchFromSpecificToGeneral = true, bool stripAllLowerLevelDomains = false, bool throwErrorIfEmpty = false)
        {
            // TODO: This is the exact same code as ObjectsService.FindSystemObjectByDomainNameAsync, how can we prevent that?
            // TODO: The problem is that the function "GetSystemObjectValueAsync" is different in both implementations, so it needs to call the correct one.
            string finalResult;
            var domain = overrideDomain;
            if (String.IsNullOrEmpty(domain))
            {
                domain = hostName;
            }

            if (searchFromSpecificToGeneral)
            {
                // By passing an empty list to the testDomains parameters, no test domains will be checked.
                finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, new List<string>(), true)}");

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostNameIncludingTestWww}");
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");

                    if (stripAllLowerLevelDomains && String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        while (domain.Contains(".") && String.IsNullOrEmpty(finalResult))
                        {
                            domain = domain[(domain.IndexOf(".", StringComparison.Ordinal) + 1)..];
                            finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");
                        }
                    }
                    else if (!String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostName}");
                    }
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostName.Split('.')[0]}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_url_{urlPrefix}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync(objectKey);
                }
            }
            else
            {
                finalResult = await GetSystemObjectValueAsync(objectKey);
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_url_{urlPrefix}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostName.Split('.')[0]}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");

                    if (stripAllLowerLevelDomains && String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        while (domain.Contains(".") && String.IsNullOrEmpty(finalResult))
                        {
                            domain = domain[(domain.IndexOf(".", StringComparison.Ordinal) + 1)..];
                            finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");
                        }
                    }
                    else if (!String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostName}");
                    }
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{hostNameIncludingTestWww}");
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    // By passing an empty list to the testDomains parameters, no test domains will be checked.
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, new List<string>(), true)}");
                }
            }

            if (finalResult != null && finalResult.Contains("{"))
            {
                finalResult = LegacyTemplatesService.DoHttpContextReplacements(finalResult, httpContextAccessor?.HttpContext);
            }

            if (!String.IsNullOrEmpty(finalResult))
            {
                return finalResult;
            }

            if (throwErrorIfEmpty)
            {
                throw new Exception($"System object {objectKey} not found");
            }

            finalResult = defaultResult;

            return finalResult;
        }

        /// <inheritdoc />
        public async Task<string> GetObjectValueAsync(string key, int typeNumber = -100)
        {
            var cachedObjects = await CacheObjectsAsync();

            if (typeNumber == -100)
            {
                foreach (var currentKey in cachedObjects.Keys)
                {
                    if (!currentKey.StartsWith(key))
                    {
                        continue;
                    }

                    return cachedObjects[currentKey].Value;
                }
            }
            else if (cachedObjects.TryGetValue(key + typeNumber, out var item))
            {
                return item.Value;
            }

            return "";
        }

        /// <inheritdoc />
        public Task<string> GetSystemObjectValueAsync(string key)
        {
            return GetObjectValueAsync(key, -1);
        }

        /// <inheritdoc />
        public async Task SetObjectValueAsync(string key, string value, int typeNumber, bool saveHistory = true)
        {
            await objectsService.SetObjectValueAsync(key, value, typeNumber, saveHistory);
        }

        /// <inheritdoc />
        public async Task SetSystemObjectValueAsync(string key, string value, bool saveHistory = true)
        {
            await SetObjectValueAsync(key, value, -1, saveHistory);
        }
    }
}