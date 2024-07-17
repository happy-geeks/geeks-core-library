using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    public class LegacyCachedTemplatesService : ITemplatesService
    {
        private readonly ILogger<LegacyCachedTemplatesService> logger;
        private readonly ITemplatesService templatesService;
        private readonly IAppCache cache;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly ICacheService cacheService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IBranchesService branchesService;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;

        public LegacyCachedTemplatesService(ILogger<LegacyCachedTemplatesService> logger,
            ITemplatesService templatesService,
            IAppCache cache,
            IOptions<GclSettings> gclSettings,
            IDatabaseConnection databaseConnection,
            ICacheService cacheService,
            IBranchesService branchesService,
            IObjectsService objectsService,
            IHttpContextAccessor httpContextAccessor = null,
            IWebHostEnvironment webHostEnvironment = null)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.cacheService = cacheService;
            this.webHostEnvironment = webHostEnvironment;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool includeContent = true)
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

            // Output caching for single/partial templates (such as header and footer).
            var templateContent = "";
            var foundInOutputCache = false;
            string fullCachePath = null;
            var cacheSettings = !includeContent ? new Template { CachingMode = TemplateCachingModes.NoCaching } : await GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
            string contentCacheKey = null;
            if (includeContent && cacheSettings.CachingMode != TemplateCachingModes.NoCaching && cacheSettings.CachingMinutes > 0)
            {
                // Get folder and file name.
                var cacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
                var cacheFileName = await GetTemplateOutputCacheFileNameAsync(cacheSettings, cacheSettings.Type.ToString());

                switch (cacheSettings.CachingLocation)
                {
                    case TemplateCachingLocations.InMemory:
                    {
                        // Cache the template contents in memory.
                        contentCacheKey = Path.GetFileNameWithoutExtension(cacheFileName);
                        logger.LogDebug($"Content cache enabled for template '{cacheSettings.Id}', cache in memory with key: {contentCacheKey}.");
                        templateContent = await cache.GetAsync<string>(contentCacheKey);
                        foundInOutputCache = !String.IsNullOrEmpty(templateContent);
                        break;
                    }
                    case TemplateCachingLocations.OnDisk:
                    {
                        if (String.IsNullOrWhiteSpace(cacheFolder))
                        {
                            logger.LogWarning($"Content cache enabled for template '{cacheSettings.Id}' but the cache folder 'contentcache' does not exist. Please create the folder and give it modify rights to the user running the website.");
                        }
                        else
                        {
                            logger.LogDebug($"Content cache enabled for template '{cacheSettings.Id}', cache file location: {fullCachePath}.");

                            // Check if a cache file already exists and if it hasn't expired yet.
                            fullCachePath = Path.Combine(cacheFolder, cacheFileName);
                            var fileInfo = new FileInfo(fullCachePath);
                            if (fileInfo.Exists)
                            {
                                if (fileInfo.LastWriteTimeUtc.AddMinutes(cacheSettings.CachingMinutes) > DateTime.UtcNow)
                                {
                                    using var fileReader = new StreamReader(fileInfo.OpenRead(), Encoding.UTF8);
                                    var fileContents = await fileReader.ReadToEndAsync();
                                    templateContent = cacheSettings.Type != TemplateTypes.Html
                                        ? fileContents
                                        : $"<!-- START PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->{fileContents}<!-- END PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->";
                                    foundInOutputCache = true;
                                }
                                else
                                {
                                    // Cleanup the old cache file if it has expired.
                                    fileInfo.Delete();
                                }
                            }
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cacheSettings.CachingLocation), cacheSettings.CachingLocation.ToString());
                }
            }

            // Cache the template settings in memory.
            var cacheName = $"Template_{id}_{name}_{parentId}_{parentName}_{!foundInOutputCache}_{branchesService.GetDatabaseNameFromCookie()}{await GetContentCachingCookieDeviationSuffixAsync()}";

            var template = await cache.GetOrAddAsync(cacheName.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, !foundInOutputCache);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));

            // Check if a login is required (only for HTML and query templates.
            if (template.Type.InList(TemplateTypes.Html, TemplateTypes.Query) && template.LoginRequired && template.Id == 0)
            {
                // If the template ID is 0, but "LoginRequired" is true, it means no user is logged in.
                return template;
            }

            if (!includeContent)
            {
                return template;
            }

            if (foundInOutputCache)
            {
                template.Content = templateContent;
            }
            else
            {
                switch (cacheSettings.CachingLocation)
                {
                    case TemplateCachingLocations.InMemory:
                        if (!String.IsNullOrWhiteSpace(contentCacheKey))
                        {
                            cache.Add(contentCacheKey, template.Content, DateTimeOffset.UtcNow.AddMinutes(cacheSettings.CachingMinutes));
                        }

                        break;
                    case TemplateCachingLocations.OnDisk:
                    {
                        if (!String.IsNullOrEmpty(fullCachePath))
                        {
                            await File.WriteAllTextAsync(fullCachePath, template.Content);
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cacheSettings.CachingLocation), cacheSettings.CachingLocation.ToString());
                }
            }

            return template;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateContentAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "")
        {
            var cacheKey = $"GetTemplateContent_{id}_{name}_{type}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateContentAsync(id, name, type, parentId, parentName);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            var cacheName = $"TemplateCacheSettings_{id}_{name}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var cacheName = $"GeneralTemplateLastChangedDate_{templateType}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateLastChangedDateAsync(templateType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var cacheName = $"GeneralTemplateValue_{templateType}_{byInsertMode:G}_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateValueAsync(templateType, byInsertMode);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent)
        {
            var results = new List<Template>();
            foreach (var id in templateIds)
            {
                results.Add(await GetTemplateAsync(id, includeContent: includeContent));
            }

            return results;
        }

        /// <inheritdoc />
        public Task<TemplateResponse> GetCombinedTemplateValueAsync(ICollection<int> templateIds, TemplateTypes templateType)
        {
            return GetCombinedTemplateValueAsync(this, templateIds, templateType);
        }

        /// <inheritdoc />
        public Task<TemplateResponse> GetCombinedTemplateValueAsync(ITemplatesService service, ICollection<int> templateIds, TemplateTypes templateType)
        {
            return templatesService.GetCombinedTemplateValueAsync(service, templateIds, templateType);
        }

        /// <inheritdoc />
        public Task AddTemplateToResponseAsync(ICollection<int> idsLoaded, Template template, string currentUrl, StringBuilder resultBuilder, TemplateResponse templateResponse)
        {
            return templatesService.AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, templateResponse);
        }

        /// <inheritdoc />
        public Task<string> GetWiserCdnFilesAsync(ICollection<string> fileNames)
        {
            return templatesService.GetWiserCdnFilesAsync(fileNames);
        }

        /// <inheritdoc />
        public Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true)
        {
            return DoReplacesAsync(this, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery, templateType, handleVariableDefaults);
        }

        /// <inheritdoc />
        public Task<string> DoReplacesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true)
        {
            return templatesService.DoReplacesAsync(service, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery, templateType, handleVariableDefaults);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true)
        {
            return HandleIncludesAsync(this, input, handleStringReplacements, dataRow, handleRequest, forQuery, templateType, handleVariableDefaults);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true)
        {
            return templatesService.HandleIncludesAsync(service, input, handleStringReplacements, dataRow, handleRequest, forQuery, templateType, handleVariableDefaults);
        }

        /// <inheritdoc />
        public Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "", string fileType = "")
        {
            return templatesService.GenerateImageUrl(itemId, type, number, filename, width, height, resizeMode, fileType);
        }

        /// <inheritdoc />
        public Task<string> HandleImageTemplating(string input)
        {
            return templatesService.HandleImageTemplating(input);
        }

        /// <summary>
        /// Gets all dynamic content data so that they can be cached.
        /// </summary>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        private async Task<Dictionary<int, DynamicContent>> GetDynamicContentForCachingAsync(ICacheEntry cacheEntry)
        {
            var dynamicContent = new Dictionary<int, DynamicContent>();
            string query = null;
            var templateVersionPart = "";
            switch (gclSettings.Environment)
            {
                case Environments.Development:
                    // Always get the latest version on development.
                    query = @"SELECT
                                d.id,
                                d.filledvariables, 
                                d.freefield1,
                                d.type,
                                d.version
                            FROM easy_dynamiccontent d
                            JOIN (SELECT id, MAX(version) AS version FROM easy_dynamiccontent GROUP BY id) d2 ON d2.id = d.id AND d2.version = d.version
                            GROUP BY d.id";
                    break;
                case Environments.Test:
                    templateVersionPart = "AND t.istest = 1";
                    break;
                case Environments.Acceptance:
                    templateVersionPart = "AND t.isacceptance = 1";
                    break;
                case Environments.Live:
                    templateVersionPart = "AND t.islive = 1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            query ??= $@"SELECT
                            d.id,
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        JOIN easy_templates t ON t.itemid = d.itemid AND t.version = d.version {templateVersionPart}

                        UNION

                        SELECT
                            d.id,
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        WHERE d.version = 1 
                        AND d.itemid = 0

                        GROUP BY id";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return dynamicContent;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var contentId = dataRow.Field<int>("id");
                dynamicContent.Add(contentId, new DynamicContent
                {
                    Id = contentId,
                    Name = dataRow.Field<string>("freefield1"),
                    SettingsJson = dataRow.Field<string>("filledvariables"),
                    Version = dataRow.Field<int>("version")
                });
            }

            cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;

            return dynamicContent;
        }

        /// <summary>
        /// GetAsync templates from database and write them to the MemoryCache if they are not yet there.
        /// </summary>
        private async Task<Dictionary<int, DynamicContent>> CacheDynamicContentAsync()
        {
            var cacheName = $"DynamicContent_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName, GetDynamicContentForCachingAsync, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<DynamicContent> GetDynamicContentData(int contentId)
        {
            if (contentId <= 0)
            {
                throw new ArgumentNullException($"The parameter {nameof(contentId)} must contain a value");
            }

            var cachedDynamicContent = await CacheDynamicContentAsync();

            if (!cachedDynamicContent.ContainsKey(contentId))
            {
                return null;
            }

            return cachedDynamicContent[contentId];
        }

        /// <inheritdoc />
        public async Task<object> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            var dynamicContent = await GetDynamicContentData(componentId);
            return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public Task<object> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            return templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(string template, List<DynamicContent> componentOverrides = null)
        {
            return await ReplaceAllDynamicContentAsync(this, template, componentOverrides);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(ITemplatesService service, string template, List<DynamicContent> componentOverrides = null)
        {
            return await templatesService.ReplaceAllDynamicContentAsync(service, template, componentOverrides);
        }

        /// <inheritdoc />
        public async Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false, bool childItemsMustHaveId = false)
        {
            return await templatesService.GetJsonResponseFromQueryAsync(queryTemplate, encryptionKey, skipNullValues, allowValueDecryption, recursive, childItemsMustHaveId);
        }

        /// <inheritdoc />
        public async Task<JArray> GetJsonResponseFromRoutineAsync(RoutineTemplate routineTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false)
        {
            return await templatesService.GetJsonResponseFromRoutineAsync(routineTemplate, encryptionKey, skipNullValues, allowValueDecryption);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            return await GetTemplateDataAsync(this, id, name, parentId, parentName);
        }

        /// <inheritdoc />
        public async Task<TemplateDataModel> GetTemplateDataAsync(ITemplatesService service, int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            return await templatesService.GetTemplateDataAsync(service, id, name, parentId, parentName);
        }

        /// <inheritdoc />
        public async Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(Template template)
        {
            return await ExecutePreLoadQueryAndRememberResultsAsync(this, template);
        }

        /// <inheritdoc />
        public async Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(ITemplatesService service, Template template)
        {
            return await templatesService.ExecutePreLoadQueryAndRememberResultsAsync(service, template);
        }

        /// <inheritdoc />
        public async Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate, string extension = ".html")
        {
            return await templatesService.GetTemplateOutputCacheFileNameAsync(contentTemplate, extension);
        }

        /// <inheritdoc />
        public async Task<List<Template>> GetTemplateUrlsAsync()
        {
            return await templatesService.GetTemplateUrlsAsync();
        }

        /// <inheritdoc />
        public async Task<bool> ComponentRenderingShouldBeLoggedAsync(int componentId)
        {
            return await templatesService.ComponentRenderingShouldBeLoggedAsync(componentId);
        }

        /// <inheritdoc />
        public async Task<bool> TemplateRenderingShouldBeLoggedAsync(int templateId)
        {
            return await templatesService.TemplateRenderingShouldBeLoggedAsync(templateId);
        }

        /// <inheritdoc />
        public async Task AddTemplateOrComponentRenderingLogAsync(int componentId, int templateId, int version, DateTime startTime, DateTime endTime, long timeTaken, string error = "")
        {
            await templatesService.AddTemplateOrComponentRenderingLogAsync(componentId, templateId, version, startTime, endTime, timeTaken, error);
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetGlobalPageWidgetsAsync()
        {
            return await templatesService.GetGlobalPageWidgetsAsync();
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(int templateId, bool includeGlobalSnippets = true)
        {
            return await GetPageWidgetsAsync(this, templateId, includeGlobalSnippets);
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(ITemplatesService service, int templateId, bool includeGlobalSnippets = true)
        {
            return await templatesService.GetPageWidgetsAsync(service, templateId, includeGlobalSnippets);
        }

        /// <summary>
        /// Get cookie deviation suffix for content caching when configured in settings.
        /// </summary>
        /// <returns>The suffix or an empty string if none are set.</returns>
        private async Task<string> GetContentCachingCookieDeviationSuffixAsync()
        {
            var result = new StringBuilder();

            // If the caching should deviate based on certain cookies, then the names and values of those cookies should be added to the file name.
            var cookieCacheDeviation = (await objectsService.FindSystemObjectByDomainNameAsync("contentcaching_cookie_deviation")).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (cookieCacheDeviation.Length > 0)
            {
                var requestCookies = httpContextAccessor?.HttpContext?.Request.Cookies;
                foreach (var cookieName in cookieCacheDeviation)
                {
                    if (requestCookies == null || !requestCookies.TryGetValue(cookieName, out var cookieValue))
                    {
                        continue;
                    }

                    var combinedCookiePart = $"{cookieName}:{cookieValue}";
                    result.Append($"_{Uri.EscapeDataString(combinedCookiePart.ToSha512Simple())}");
                }
            }

            return result.ToString();
        }
    }
}