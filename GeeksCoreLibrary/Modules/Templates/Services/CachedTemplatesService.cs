using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    public class CachedTemplatesService : ITemplatesService
    {
        private readonly ILogger<LegacyCachedTemplatesService> logger;
        private readonly ITemplatesService templatesService;
        private readonly IAppCache cache;
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICacheService cacheService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public CachedTemplatesService(ILogger<LegacyCachedTemplatesService> logger, ITemplatesService templatesService, IAppCache cache, IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, ICacheService cacheService, IWebHostEnvironment webHostEnvironment)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes type = TemplateTypes.Html, int parentId = 0, string parentName = "", bool includeContent = true)
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }

            // Output caching for single/partial templates (such as header and footer).
            var templateContent = "";
            var foundInOutputCache = false;
            string fullCachePath = null;
            if (type == TemplateTypes.Html && includeContent)
            {
                var cacheSettings = await GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
                if (cacheSettings.CachingMode != TemplateCachingModes.NoCaching && cacheSettings.CachingMinutes > 0)
                {
                    // Get folder and file name.
                    var cacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
                    var cacheFileName = await GetTemplateOutputCacheFileNameAsync(cacheSettings);
                    fullCachePath = Path.Combine(cacheFolder, cacheFileName);

                    logger.LogDebug($"Content cache enabled for template '{cacheSettings.Id}', cache file location: {fullCachePath}.");

                    // Check if a cache file already exists and if it hasn't expired yet.
                    var fileInfo = new FileInfo(fullCachePath);
                    if (fileInfo.Exists && fileInfo.LastWriteTimeUtc.AddMinutes(cacheSettings.CachingMinutes) > DateTime.UtcNow)
                    {
                        using var fileReader = new StreamReader(fileInfo.OpenRead(), Encoding.UTF8);
                        templateContent = $"<!-- START PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->{await fileReader.ReadToEndAsync()}<!-- END PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->";
                        foundInOutputCache = true;
                    }
                }
            }

            // Cache the template settings in memory.
            var cacheKey = $"Template_{id}_{name}_{parentId}_{parentName}_{!foundInOutputCache}";
            var template = await cache.GetOrAdd(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, !foundInOutputCache);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));

            if (type != TemplateTypes.Html || !includeContent)
            {
                return template;
            }

            if (foundInOutputCache)
            {
                template.Content = templateContent;
            }
            else if (!String.IsNullOrEmpty(fullCachePath))
            {
                // Write the HTML to the cache file.
                await File.WriteAllTextAsync(fullCachePath, template.Content);
            }

            return template;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            var cacheKey = $"TemplateCacheSettings_{id}_{name}_{parentId}_{parentName}";
            return await cache.GetOrAdd(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType)
        {
            var cacheKey = $"GeneralTemplateLastChangedDate_{templateType}";
            return await cache.GetOrAdd(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateLastChangedDateAsync(templateType);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType)
        {
            var cacheKey = $"GeneralTemplateValue_{templateType}";
            return await cache.GetOrAdd(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateValueAsync(templateType);
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
        public Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false)
        {
            return DoReplacesAsync(this, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery);
        }

        /// <inheritdoc />
        public Task<string> DoReplacesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false)
        {
            return templatesService.DoReplacesAsync(service, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false)
        {
            return HandleIncludesAsync(this, input, handleStringReplacements, dataRow, handleRequest, forQuery);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false)
        {
            return templatesService.HandleIncludesAsync(service, input, handleStringReplacements, dataRow, handleRequest, forQuery);
        }

        public Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "")
        {
            return templatesService.GenerateImageUrl(itemId, type, number, filename, width, height);
        }

        /// <inheritdoc />
        public async Task<string> HandleImageTemplating(string input)
        {
            var cacheKey = $"image_template_{input.ToSha512Simple()}";
            return await cache.GetOrAdd(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.HandleImageTemplating(input);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <summary>
        /// Gets all dynamic content data so that they can be cached.
        /// </summary>
        /// <param name="cacheEntry"></param>
        /// <returns></returns>
        private async Task<Dictionary<int, DynamicContent>> GetDynamicContentForCachingAsync(ICacheEntry cacheEntry)
        {
            var dynamicContent = new Dictionary<int, DynamicContent>();
            var query = gclSettings.Environment == Environments.Development
                ? @$"SELECT 
                    component.content_id,
                    component.settings,
                    component.component,
                    component.component_mode,
                    component.version
                FROM {WiserTableNames.WiserDynamicContent} AS component
                LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
                WHERE otherVersion.id IS NULL"
                : @$"SELECT 
                    component.content_id,
                    component.settings,
                    component.component,
                    component.component_mode,
                    component.version
                FROM {WiserTableNames.WiserDynamicContent} AS component
                WHERE (component.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}";

            databaseConnection.ClearParameters();
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return dynamicContent;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var contentId = dataRow.Field<int>("content_id");
                dynamicContent.Add(contentId, new DynamicContent
                {
                    Id = contentId,
                    Name = dataRow.Field<string>("component"),
                    SettingsJson = dataRow.Field<string>("settings"),
                    ComponentMode = dataRow.Field<string>("component_mode"),
                    Version = dataRow.Field<int>("version")
                });
            }

            cacheEntry.SlidingExpiration = gclSettings.DefaultTemplateCacheDuration;

            return dynamicContent;
        }

        /// <summary>
        /// GetAsync templates from database and write them to the MemoryCache if they are not yet there.
        /// </summary>
        private async Task<Dictionary<int, DynamicContent>> CacheDynamicContentAsync()
        {
            return await cache.GetOrAdd("DynamicContent", GetDynamicContentForCachingAsync, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
        public async Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            var dynamicContent = await GetDynamicContentData(componentId);
            return await GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            if (dynamicContent == null || dynamicContent.Id == 0 || String.IsNullOrWhiteSpace(dynamicContent.SettingsJson))
            {
                return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
            }

            var settings = JsonConvert.DeserializeObject<CmsSettings>(dynamicContent.SettingsJson);
            if (settings == null)
            {
                return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
            }

            var originalUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
            var cacheKey = new StringBuilder($"dynamicContent_{dynamicContent.Id}_");
            switch (settings.CachingMode)
            {
                case TemplateCachingModes.ServerSideCaching:
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrl:
                    cacheKey.Append(Uri.EscapeDataString(originalUri.AbsolutePath.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrlAndQueryString:
                    cacheKey.Append(Uri.EscapeDataString(originalUri.PathAndQuery.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerHostNameAndQueryString:
                    cacheKey.Append(Uri.EscapeDataString(originalUri.ToString().ToSha512Simple()));
                    break;
                case TemplateCachingModes.NoCaching:
                    return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.CachingMode), settings.CachingMode.ToString());
            }
            
            return await cache.GetOrAdd(cacheKey.ToString(),
                async cacheEntry =>
                {
                    cacheEntry.SlidingExpiration = settings.CacheMinutes <= 0 ? gclSettings.DefaultTemplateCacheDuration : TimeSpan.FromMinutes(settings.CacheMinutes);
                    return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
        public async Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false)
        {
            return await templatesService.GetJsonResponseFromQueryAsync(queryTemplate, encryptionKey, skipNullValues, allowValueDecryption, recursive);
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
        public async Task ExecutePreLoadQueryAndRememberResultsAsync(Template template)
        {
            await ExecutePreLoadQueryAndRememberResultsAsync(this, template);
        }

        /// <inheritdoc />
        public async Task ExecutePreLoadQueryAndRememberResultsAsync(ITemplatesService service, Template template)
        {
            await templatesService.ExecutePreLoadQueryAndRememberResultsAsync(service, template);
        }

        /// <inheritdoc />
        public async Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate)
        {
            return await templatesService.GetTemplateOutputCacheFileNameAsync(contentTemplate);
        }
    }
}
