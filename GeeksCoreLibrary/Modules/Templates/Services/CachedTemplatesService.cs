using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IObjectsService objectsService;
        private readonly ILanguagesService languagesService;

        public CachedTemplatesService(ILogger<LegacyCachedTemplatesService> logger, ITemplatesService templatesService, IAppCache cache, IOptions<GclSettings> gclSettings, IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, ICacheService cacheService, IWebHostEnvironment webHostEnvironment, IObjectsService objectsService, ILanguagesService languagesService)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.cache = cache;
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.cacheService = cacheService;
            this.webHostEnvironment = webHostEnvironment;
            this.objectsService = objectsService;
            this.languagesService = languagesService;
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

            // Check if cache should be skipped:
            // - It can be skipped if on the development environment, but dev caching is not enabled.
            // - It can be skipped if on the test environment, but test caching is not enabled.
            var skipCache = false;

            switch (this.gclSettings.Environment)
            {
                case Environments.Development when !(await this.objectsService.FindSystemObjectByDomainNameAsync("contentcaching_dev_enabled")).Equals("true"):
                case Environments.Test when !(await this.objectsService.FindSystemObjectByDomainNameAsync("contentcaching_test_enabled")).Equals("true"):
                    skipCache = true;
                    break;
            }

            if (!skipCache && includeContent && cacheSettings.CachingMode != TemplateCachingModes.NoCaching && cacheSettings.CachingMinutes > 0)
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
                            logger.LogWarning($"Content cache enabled for template '{cacheSettings.Id}' but the cache folder 'contentcache' does not exist. Please create the folder and give it modify rights to the user running the website (on Windows / IIS, this is the user 'IIS_IUSRS' bij default).");
                        }
                        else
                        {
                            // Build the cache directory, based on template type and name.
                            fullCachePath = Path.Combine(cacheFolder, Constants.TemplateCacheRootDirectoryName, cacheSettings.Type.ToString(), $"{cacheSettings.Name.StripIllegalPathCharacters()} ({cacheSettings.Id})", cacheFileName);
                            logger.LogDebug($"Content cache enabled for template '{cacheSettings.Id}', cache file location: {fullCachePath}.");

                            // Check if a cache file already exists and if it hasn't expired yet.
                            var fileInfo = new FileInfo(fullCachePath);
                            if (fileInfo.Directory is { Exists: false })
                            {
                                fileInfo.Directory.Create();
                            }
                            else if (fileInfo.Exists)
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
            var cacheKey = $"Template_{languagesService.CurrentLanguageCode ?? ""}_{id}_{name}_{parentId}_{parentName}_{!foundInOutputCache}";
            var template = await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, !foundInOutputCache);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));

            // Check if a login is required (only for HTML and query templates.
            if (template.Type.InList(TemplateTypes.Html, TemplateTypes.Query) && template.LoginRequired && template.Id == 0)
            {
                // If the template ID is 0, but "LoginRequired" is true, it means no user is logged in, or that the user doesn't have any of the required roles.
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
        public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
        {
            var cacheKey = $"TemplateCacheSettings_{id}_{name}_{parentId}_{parentName}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type)
        {
            var cacheKey = $"GetTemplateIdFromName_{name}_{type}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateIdFromNameAsync(name, type);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var cacheKey = $"GeneralTemplateLastChangedDate_{languagesService.CurrentLanguageCode ?? ""}_{templateType}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateLastChangedDateAsync(templateType);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard)
        {
            var cacheKey = $"GeneralTemplateValue_{languagesService.CurrentLanguageCode ?? ""}_{templateType}_{byInsertMode:G}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGeneralTemplateValueAsync(templateType, byInsertMode);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
        public Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return DoReplacesAsync(this, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery, templateType);
        }

        /// <inheritdoc />
        public Task<string> DoReplacesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return templatesService.DoReplacesAsync(service, input, handleStringReplacements, handleDynamicContent, evaluateLogicSnippets, dataRow, handleRequest, removeUnknownVariables, forQuery, templateType);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return HandleIncludesAsync(this, input, handleStringReplacements, dataRow, handleRequest, forQuery, templateType);
        }

        /// <inheritdoc />
        public Task<string> HandleIncludesAsync(ITemplatesService service, string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null)
        {
            return templatesService.HandleIncludesAsync(service, input, handleStringReplacements, dataRow, handleRequest, forQuery, templateType);
        }
        
        /// <inheritdoc />
        public Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "")
        {
            return templatesService.GenerateImageUrl(itemId, type, number, filename, width, height);
        }

        /// <inheritdoc />
        public async Task<string> HandleImageTemplating(string input)
        {
            var cacheKey = $"image_template_{languagesService.CurrentLanguageCode ?? ""}_{input.ToSha512Simple()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.HandleImageTemplating(input);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
                    component.version,
                    component.title
                FROM {WiserTableNames.WiserDynamicContent} AS component
                LEFT JOIN {WiserTableNames.WiserDynamicContent} AS otherVersion ON otherVersion.content_id = component.content_id AND otherVersion.version > component.version
                WHERE otherVersion.id IS NULL"
                : @$"SELECT 
                    component.content_id,
                    component.settings,
                    component.component,
                    component.component_mode,
                    component.version,
                    component.title
                FROM {WiserTableNames.WiserDynamicContent} AS component
                WHERE (component.published_environment & {(int)gclSettings.Environment}) = {(int)gclSettings.Environment}";

            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return dynamicContent;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var contentId = dataRow.Field<int>("content_id");
                dynamicContent.Add(contentId,
                    new DynamicContent
                    {
                        Id = contentId,
                        Name = dataRow.Field<string>("component"),
                        SettingsJson = dataRow.Field<string>("settings"),
                        ComponentMode = dataRow.Field<string>("component_mode"),
                        Version = dataRow.Field<int>("version"),
                        Title = dataRow.Field<string>("title")
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
            return await cache.GetOrAddAsync("DynamicContent", GetDynamicContentForCachingAsync, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
            return await GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<object> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
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

            switch (this.gclSettings.Environment)
            {
                case Environments.Development when !(await this.objectsService.FindSystemObjectByDomainNameAsync("contentcaching_dev_enabled")).Equals("true"):
                case Environments.Test when !(await this.objectsService.FindSystemObjectByDomainNameAsync("contentcaching_test_enabled")).Equals("true"):
                    return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
            }

            var originalUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext);
            var cacheKey = new StringBuilder($"dynamicContent_{languagesService.CurrentLanguageCode ?? ""}_{dynamicContent.Id}_");
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
                case TemplateCachingModes.ServerSideCachingBasedOnUrlRegex:
                    if (String.IsNullOrWhiteSpace(settings.CacheRegex))
                    {
                        throw new Exception($"Caching for component {dynamicContent.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but no regex has been entered.");
                    }

                    try
                    {
                        var regex = new Regex(settings.CacheRegex, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
                        var match = regex.Match(originalUri.PathAndQuery);
                        if (!match.Success)
                        {
                            return "";
                        }

                        // Add all values of named groups to the cache key.
                        foreach (Group group in match.Groups)
                        {
                            if (String.IsNullOrWhiteSpace(group.Name) || Int32.TryParse(group.Name, out _))
                            {
                                // Ignore groups without a name (when you have no name given in the regex, the group name will be a number).
                                continue;
                            }

                            // Strip invalid characters that can't be in a file name.
                            var value = Path.GetInvalidFileNameChars().Aggregate(group.Value, (current, character) => current.Replace(character, '-'));
                            
                            // Add the group value to the file name.
                            cacheKey.Append($"{Uri.EscapeDataString(value)}_");
                        }
                    }
                    catch (ArgumentException argumentException)
                    {
                        // ArgumentException will be thrown if the regex is not valid.
                        logger.LogWarning(argumentException, $"Caching for template {dynamicContent.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but an invalid regex has been entered.");
                        throw new Exception($"Caching for template {dynamicContent.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but an invalid regex has been entered. The exact error was: {argumentException.Message}");
                    }

                    break;
                case TemplateCachingModes.NoCaching:
                    return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.CachingMode), settings.CachingMode.ToString());
            }

            if (extraData != null && extraData.Any())
            {
                foreach (var key in extraData.Keys)
                {
                    cacheKey.Append($"_{key}={extraData[key]}");
                }
            }

            string html;
            var addedToCache = false;
            switch (settings.CachingLocation)
            {
                case TemplateCachingLocations.InMemory:
                case TemplateCachingLocations.OnDisk when !String.IsNullOrWhiteSpace(callMethod):
                    html = (string)await cache.GetOrAddAsync(cacheKey.ToString(),
                        async cacheEntry =>
                        {
                            addedToCache = true;
                            cacheEntry.AbsoluteExpirationRelativeToNow = settings.CacheMinutes <= 0 ? gclSettings.DefaultTemplateCacheDuration : TimeSpan.FromMinutes(settings.CacheMinutes);
                            return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                        },
                        cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
                    break;
                case TemplateCachingLocations.OnDisk:
                {
                    var fileName = $"{cacheKey}.html";
                    var cacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
                    var fullCachePath = Path.Combine(cacheFolder, Constants.ComponentsCacheRootDirectoryName, dynamicContent.Name.StripIllegalPathCharacters(), $"{dynamicContent.Title.StripIllegalPathCharacters()} ({dynamicContent.Id})", fileName);

                    // Check if a cache file already exists and if it hasn't expired yet.
                    var fileInfo = new FileInfo(fullCachePath);
                    if (fileInfo.Directory is { Exists: false })
                    {
                        fileInfo.Directory.Create();
                    }
                    else if (fileInfo.Exists)
                    {
                        if (fileInfo.LastWriteTimeUtc.AddMinutes(settings.CacheMinutes) > DateTime.UtcNow)
                        {
                            using var fileReader = new StreamReader(fileInfo.OpenRead(), Encoding.UTF8);
                            var fileContents = await fileReader.ReadToEndAsync();
                            html = $"<!-- START DYNAMIC CONTENT FROM CACHE ({dynamicContent.Id}) -->{fileContents}<!-- END DYNAMIC CONTENT FROM CACHE ({dynamicContent.Id}) -->";
                            return html;
                        }

                        // Cleanup the old cache file if it has expired.
                        fileInfo.Delete();
                    }

                    html = (string)await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                    addedToCache = true;
                    await File.WriteAllTextAsync(fullCachePath, html);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.CachingLocation), settings.CachingLocation.ToString());
            }

            // Cache page SEO data,
            if (httpContextAccessor.HttpContext == null)
            {
                return html;
            }

            cacheKey.Append($"_{Constants.PageMetaDataFromComponentKey}");

            if (addedToCache && httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] is PageMetaDataModel componentSeoData)
            {
                cache.Add(cacheKey.ToString(), componentSeoData);
            }
            else if (!addedToCache && httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] == null)
            {
                componentSeoData = cache.Get<PageMetaDataModel>(cacheKey.ToString());
                if (componentSeoData != null)
                {
                    httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] = componentSeoData;
                }
            }

            return html;
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
            var cacheKey = $"template_urls_{databaseConnection.GetDatabaseNameForCaching()}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetTemplateUrlsAsync();
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
            var cacheKey = $"page_widgets_{databaseConnection.GetDatabaseNameForCaching()}_global";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetGlobalPageWidgetsAsync();
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(int templateId, bool includeGlobalSnippets = true)
        {
            return await GetPageWidgetsAsync(this, templateId, includeGlobalSnippets);
        }

        /// <inheritdoc />
        public async Task<List<PageWidgetModel>> GetPageWidgetsAsync(ITemplatesService service, int templateId, bool includeGlobalSnippets = true)
        {
            var cacheKey = $"page_widgets_{databaseConnection.GetDatabaseNameForCaching()}_{templateId}_{includeGlobalSnippets}";
            return await cache.GetOrAddAsync(cacheKey,
                async cacheEntry =>
                {                    
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    return await templatesService.GetPageWidgetsAsync(service, templateId, includeGlobalSnippets);
                },
                cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
        }
    }
}