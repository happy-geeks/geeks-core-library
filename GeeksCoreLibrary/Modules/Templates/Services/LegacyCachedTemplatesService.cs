using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LazyCache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

#pragma warning disable CS0618 // Type or member is obsolete
namespace GeeksCoreLibrary.Modules.Templates.Services;

public class LegacyCachedTemplatesService(
    ITemplatesService templatesService,
    IAppCache cache,
    IOptions<GclSettings> gclSettings,
    ICacheService cacheService,
    IBranchesService branchesService,
    IObjectsService objectsService,
    IHttpContextAccessor httpContextAccessor = null)
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

        // Cache the template settings in memory.
        var cacheName = $"Template_{id}_{name}_{parentId}_{parentName}_{includeContent}_{branchesService.GetDatabaseNameFromCookie()}{await GetContentCachingCookieDeviationSuffixAsync()}";

        var template = await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, includeContent);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));

        return ObjectCloner.ObjectCloner.DeepClone(template);
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

    /// <inheritdoc />
    public async Task<DynamicContent> GetDynamicContentData(int contentId)
    {
        if (contentId <= 0)
        {
            throw new ArgumentNullException($"The parameter {nameof(contentId)} must contain a value");
        }

        var cacheName = $"DynamicContentData_{contentId}_{branchesService.GetDatabaseNameFromCookie()}";

        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetDynamicContentData(contentId);
            },
            cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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
    public async Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate, string extension = ".html", bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await templatesService.GetTemplateOutputCacheFileNameAsync(contentTemplate, extension, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
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

    public async Task<Template> CheckTemplatePermissionsAsync(Template template)
    {
        return await templatesService.CheckTemplatePermissionsAsync(template);
    }

    public async Task<Template> GetTemplatePermissionSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "")
    {
        var cacheName = $"TemplatePermissionSettings_{id}_{name}_{parentId}_{parentName}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                return await templatesService.GetTemplatePermissionSettingsAsync(id, name, parentId, parentName);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates));
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