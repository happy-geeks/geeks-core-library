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
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Seo.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Constants = GeeksCoreLibrary.Modules.Templates.Models.Constants;

namespace GeeksCoreLibrary.Modules.Templates.Services;

public class CachedTemplatesService(
    ILogger<CachedTemplatesService> logger,
    ITemplatesService templatesService,
    IFileCacheService fileCacheService,
    IAppCache cache,
    IOptions<GclSettings> gclSettings,
    IDatabaseConnection databaseConnection,
    ICacheService cacheService,
    ILanguagesService languagesService,
    IBranchesService branchesService,
    IHttpContextAccessor httpContextAccessor = null,
    IWebHostEnvironment webHostEnvironment = null)
    : ITemplatesService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool includeContent = true, bool skipPermissions = false)
    {
        if (id <= 0 && String.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
        }

        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        // Cache the template settings in memory.
        var cacheName = $"Template_{languagesService.CurrentLanguageCode ?? ""}_{id}_{name}_{parentId}_{parentName}_{includeContent}_{branchesService.GetDatabaseNameFromCookie()}";
        var template = await cache.GetAsync<Template>(cacheName);
        if (template != null)
        {
            return ObjectCloner.ObjectCloner.DeepClone(template);
        }

        template = await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, includeContent, skipPermissions: true);
        if (HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
        {
            // Don't cache the content if a 404 was forced.
            return ObjectCloner.ObjectCloner.DeepClone(template);
        }

        var memoryCacheEntryOptions = cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates);
        memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
        cache.Add(cacheName, template, memoryCacheEntryOptions);

        return ObjectCloner.ObjectCloner.DeepClone(template);
    }

    /// <inheritdoc />
    public async Task<Template> GetTemplateContentAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "")
    {
        var cacheName = $"GetTemplateContent_{id}_{name}_{type}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
        var template = await cache.GetAsync<Template>(cacheName);
        if (template != null)
        {
            return ObjectCloner.ObjectCloner.DeepClone(template);
        }

        template = await templatesService.GetTemplateContentAsync(id, name, type, parentId, parentName);
        if (HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
        {
            // Don't cache the content if a 404 was forced.
            return ObjectCloner.ObjectCloner.DeepClone(template);
        }

        var memoryCacheEntryOptions = cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates);
        memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
        cache.Add(cacheName, template, memoryCacheEntryOptions);

        return ObjectCloner.ObjectCloner.DeepClone(template);
    }

    /// <inheritdoc />
    public async Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
    {
        var cacheName = $"TemplateCacheSettings_{id}_{name}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
        var template = await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
            },
            cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));

        return ObjectCloner.ObjectCloner.DeepClone(template);
    }

    /// <inheritdoc />
    public async Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type)
    {
        var cacheName = $"GetTemplateIdFromName_{name}_{type}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
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
        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        var cacheName = $"GeneralTemplateLastChangedDate_{languagesService.CurrentLanguageCode ?? ""}_{templateType}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
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
        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        var cacheName = $"GeneralTemplateValue_{languagesService.CurrentLanguageCode}_{templateType}_{byInsertMode:G}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
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
        var cacheKey = $"GetMultipleTemplates_{includeContent}_{String.Join("_", templateIds.Order())}";
        return await cache.GetOrAddAsync(cacheKey,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetTemplatesAsync(templateIds, includeContent);
            },
            cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
    public async Task<string> HandleImageTemplating(string input)
    {
        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        var cacheName = $"image_template_{languagesService.CurrentLanguageCode ?? ""}_{input.ToSha512Simple()}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.HandleImageTemplating(input);
            },
            cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
    }

    /// <inheritdoc />
    public async Task<DynamicContent> GetDynamicContentData(int contentId)
    {
        if (contentId <= 0)
        {
            throw new ArgumentNullException($"The parameter {nameof(contentId)} must contain a value");
        }

        var cacheName = $"DynamicContentData_{contentId}_{branchesService.GetDatabaseNameFromCookie()}";
        var dynamicContent = await cache.GetAsync<DynamicContent>(cacheName);
        if (dynamicContent != null)
        {
            return ObjectCloner.ObjectCloner.DeepClone(dynamicContent);
        }

        dynamicContent = await templatesService.GetDynamicContentData(contentId);
        if (HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
        {
            // Don't cache the content if a 404 was forced.
            return ObjectCloner.ObjectCloner.DeepClone(dynamicContent);
        }

        var memoryCacheEntryOptions = cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates);
        memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
        cache.Add(cacheName, dynamicContent, memoryCacheEntryOptions);

        return ObjectCloner.ObjectCloner.DeepClone(dynamicContent);
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
        if (settings == null || settings.CacheMinutes < 0)
        {
            return await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        var originalUri = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);
        var cacheName = new StringBuilder($"dynamicContent_{languagesService.CurrentLanguageCode ?? ""}_{dynamicContent.Id}_");
        switch (settings.CachingMode)
        {
            case TemplateCachingModes.ServerSideCaching:
                break;
            case TemplateCachingModes.ServerSideCachingPerUrl:
                cacheName.Append(Uri.EscapeDataString(originalUri.AbsolutePath.ToSha512Simple()));
                break;
            case TemplateCachingModes.ServerSideCachingPerUrlAndQueryString:
                cacheName.Append(Uri.EscapeDataString(originalUri.PathAndQuery.ToSha512Simple()));
                break;
            case TemplateCachingModes.ServerSideCachingPerHostNameAndQueryString:
                cacheName.Append(Uri.EscapeDataString(originalUri.ToString().ToSha512Simple()));
                break;
            case TemplateCachingModes.ServerSideCachingBasedOnUrlRegex:
                if (String.IsNullOrWhiteSpace(settings.CacheRegex))
                {
                    throw new InvalidOperationException($"Caching for component {dynamicContent.Id} is set to {nameof(TemplateCachingModes.ServerSideCachingBasedOnUrlRegex)}, but no regex has been entered.");
                }

                try
                {
                    var regex = new Regex(settings.CacheRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
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
                        cacheName.Append($"{Uri.EscapeDataString(value)}_");
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
                throw new ArgumentOutOfRangeException(nameof(settings.CachingMode), settings.CachingMode.ToString(), null);
        }

        if (extraData != null && extraData.Count != 0)
        {
            foreach (var key in extraData.Keys)
            {
                cacheName.Append($"_{key}={extraData[key]}");
            }
        }

        cacheName.Append($"_{branchesService.GetDatabaseNameFromCookie()}");
        cacheName.Append($"_{callMethod}");

        object content = null;
        var addedToCache = false;

        // Check how long this template should be cached:
        TimeSpan cacheTimeSpan;
        if (settings.CacheMinutes == -1)
        {
            cacheTimeSpan = TimeSpan.Zero;
        }
        else
        {
            cacheTimeSpan = settings.CacheMinutes == 0 ? gclSettings.DefaultTemplateCacheDuration : TimeSpan.FromMinutes(settings.CacheMinutes);
        }

        var memoryCacheEntryOptions = cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates);
        memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = cacheTimeSpan;

        switch (settings.CachingLocation)
        {
            case TemplateCachingLocations.InMemory:
            {
                content = await cache.GetAsync<object>(cacheName.ToString());
                if (content != null)
                {
                    return ObjectCloner.ObjectCloner.DeepClone(content);
                }

                content = await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                if (HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
                {
                    // Don't cache the content if a 404 was forced.
                    break;
                }

                addedToCache = true;
                cache.Add(cacheName.ToString(), content, memoryCacheEntryOptions);

                break;
            }
            case TemplateCachingLocations.OnDisk:
            {
                var cacheFolder = FileSystemHelpers.GetOutputCacheDirectory(webHostEnvironment);
                if (String.IsNullOrWhiteSpace(cacheFolder))
                {
                    logger.LogWarning($"Content cache enabled for component '{dynamicContent.Id}' but the cache folder 'contentcache' does not exist. Please create the folder and give it modify rights to the user running the website (on Windows / IIS, this is the user 'IIS_IUSRS' bij default).");
                }
                else
                {
                    var fileName = $"{cacheName}.html";
                    var fullCachePath = Path.Combine(cacheFolder, Constants.ComponentsCacheRootDirectoryName, dynamicContent.Name.StripIllegalPathCharacters(), $"{dynamicContent.Title.StripIllegalPathCharacters()} ({dynamicContent.Id})", fileName);

                    content = await fileCacheService.GetTextAsync(fullCachePath, cacheTimeSpan);

                    if (content is null)
                    {
                        content = await templatesService.GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
                        if (HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
                        {
                            // Don't cache the content if a 404 was forced.
                            break;
                        }

                        if (content is string contentString)
                        {
                            addedToCache = true;
                            await fileCacheService.WriteFileIfNotExistsOrExpiredAsync(fullCachePath, contentString, cacheTimeSpan);
                        }
                        else
                        {
                            // If the content is not a string, we can't save it to disk.
                            logger.LogWarning($"The generated content for component '{dynamicContent.Id}' is not a string. It cannot be saved to disk.");
                        }
                    }
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(settings.CachingLocation), settings.CachingLocation.ToString(), null);
        }

        // Cache page SEO data,
        if (httpContextAccessor?.HttpContext == null)
        {
            return ObjectCloner.ObjectCloner.DeepClone(content);
        }

        cacheName.Append($"_{Constants.PageMetaDataFromComponentKey}");

        if (addedToCache && httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] is PageMetaDataModel componentSeoData)
        {
            cache.GetOrAdd(cacheName.ToString(),
                cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = settings.CacheMinutes == 0 ? gclSettings.DefaultSeoModuleCacheDuration : TimeSpan.FromMinutes(settings.CacheMinutes);
                    return componentSeoData;
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Seo));
        }
        else if (!addedToCache && httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] == null)
        {
            componentSeoData = cache.Get<PageMetaDataModel>(cacheName.ToString());
            if (componentSeoData != null)
            {
                httpContextAccessor.HttpContext.Items[Constants.PageMetaDataFromComponentKey] = componentSeoData;
            }
        }

        return ObjectCloner.ObjectCloner.DeepClone(content);
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
    public async Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate, string extension = ".html", bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await templatesService.GetTemplateOutputCacheFileNameAsync(contentTemplate, extension, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
    }

    /// <inheritdoc />
    public async Task<List<Template>> GetTemplateUrlsAsync()
    {
        var cacheName = $"template_urls_{databaseConnection.GetDatabaseNameForCaching()}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
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
        var cacheName = $"page_widgets_{databaseConnection.GetDatabaseNameForCaching()}_global_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
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
        var cacheName = $"page_widgets_{databaseConnection.GetDatabaseNameForCaching()}_{templateId}_{includeGlobalSnippets}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetPageWidgetsAsync(service, templateId, includeGlobalSnippets);
            },
            cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
    }

    /// <inheritdoc />
    public async Task<Template> CheckTemplatePermissionsAsync(Template template)
    {
        return await templatesService.CheckTemplatePermissionsAsync(template);
    }

    /// <inheritdoc />
    public async Task<Template> GetTemplatePermissionSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
    {
        var cacheName = $"TemplatePermissionSettings_{id}_{name}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetTemplatePermissionSettingsAsync(id, name, parentId, parentName);
            });
    }
}