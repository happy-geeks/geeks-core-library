﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.ViewModels;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Constants = GeeksCoreLibrary.Modules.Templates.Models.Constants;

namespace GeeksCoreLibrary.Modules.Templates.Services;

public class CachedPagesService(
    ILogger<CachedPagesService> logger,
    IPagesService pagesService,
    IFileCacheService fileCacheService,
    ITemplatesService templatesService,
    IAppCache cache,
    IOptions<GclSettings> gclSettings,
    ICacheService cacheService,
    ILanguagesService languagesService,
    IWebHostEnvironment webHostEnvironment = null,
    IHttpContextAccessor httpContextAccessor = null)
    : IPagesService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<Template> GetRenderedTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool skipPermissions = false, string templateContent = null, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        if (id <= 0 && String.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
        }

        Template template;
        if (templateContent != null)
        {
            template = await templatesService.GetTemplateAsync(id, name, type, parentId, parentName, false, skipPermissions);
            template.Content = templateContent;
            return template;
        }

        // Output caching for single/partial templates (such as header and footer).
        string content = null;
        string fullCachePath = null;
        var cacheSettings = await templatesService.GetTemplateCacheSettingsAsync(id, name, parentId, parentName);
        string contentCacheKey = null;

        // Check how long this template should be cached:
        TimeSpan cacheMinutes;
        if (cacheSettings.CachingMinutes == -1)
        {
            cacheMinutes = TimeSpan.Zero;
        }
        else
        {
            cacheMinutes = cacheSettings.CachingMinutes == 0 ? gclSettings.DefaultTemplateCacheDuration : TimeSpan.FromMinutes(cacheSettings.CachingMinutes);
        }

        if (cacheMinutes.TotalMinutes > 0)
        {
            // Get folder and file name.
            var cacheFolder = FileSystemHelpers.GetOutputCacheDirectory(webHostEnvironment);
            var cacheFileName = await templatesService.GetTemplateOutputCacheFileNameAsync(cacheSettings, cacheSettings.Type.ToString(), useAbsoluteImageUrls, removeSvgUrlsFromIcons);

            switch (cacheSettings.CachingLocation)
            {
                case TemplateCachingLocations.InMemory:
                {
                    // Cache the template contents in memory.
                    contentCacheKey = Path.GetFileNameWithoutExtension(cacheFileName);
                    logger.LogDebug($"Content cache enabled for template '{cacheSettings.Id}', cache in memory with key: {contentCacheKey}.");
                    var cachedTemplate = await cache.GetAsync<Template>(contentCacheKey);

                    if (cachedTemplate is {Id: > 0})
                    {
                        return ObjectCloner.ObjectCloner.DeepClone(await templatesService.CheckTemplatePermissionsAsync(cachedTemplate));
                    }

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

                        var fileContent = await fileCacheService.GetTextAsync(fullCachePath, cacheMinutes);

                        if (!String.IsNullOrWhiteSpace(fileContent))
                        {
                            content = cacheSettings.Type != TemplateTypes.Html ? fileContent : $"<!-- START PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->{fileContent}<!-- END PARTIAL TEMPLATE FROM CACHE ({cacheSettings.Id}) -->";
                        }
                    }
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(cacheSettings.CachingLocation), cacheSettings.CachingLocation.ToString(), null);
                }
            }
        }

        // Make sure the language code has a value.
        if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
        {
            // This function fills the property "CurrentLanguageCode".
            await languagesService.GetLanguageCodeAsync();
        }

        template = await pagesService.GetRenderedTemplateAsync(id, name, type, parentId, parentName, skipPermissions, content, useAbsoluteImageUrls, removeSvgUrlsFromIcons);

        // If we already have the content, or if we forced a 404 status code, we don't need to cache it.
        if (content != null || HttpContextHelpers.NotFoundStatusWasForced(httpContextAccessor?.HttpContext))
        {
            return ObjectCloner.ObjectCloner.DeepClone(await templatesService.CheckTemplatePermissionsAsync(template));
        }

        switch (cacheSettings.CachingLocation)
        {
            case TemplateCachingLocations.InMemory:
            {
                if (!String.IsNullOrWhiteSpace(contentCacheKey))
                {
                    var memoryCacheEntryOptions = cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Templates);
                    memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = gclSettings.DefaultTemplateCacheDuration;
                    cache.Add(contentCacheKey, template, memoryCacheEntryOptions);
                }

                break;
            }
            case TemplateCachingLocations.OnDisk:
            {
                if (!String.IsNullOrEmpty(fullCachePath))
                {
                    await fileCacheService.WriteFileIfNotExistsOrExpiredAsync(fullCachePath, template.Content, cacheMinutes);
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(cacheSettings.CachingLocation), cacheSettings.CachingLocation.ToString(), null);
        }

        return ObjectCloner.ObjectCloner.DeepClone(await templatesService.CheckTemplatePermissionsAsync(template));
    }

    /// <inheritdoc />
    public async Task<string> GetGlobalHeader(string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await GetGlobalHeader(this, url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
    }

    /// <inheritdoc />
    public async Task<string> GetGlobalHeader(IPagesService service, string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await pagesService.GetGlobalHeader(service, url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
    }

    /// <inheritdoc />
    public async Task<string> GetGlobalFooter(string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await GetGlobalFooter(this, url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
    }

    /// <inheritdoc />
    public async Task<string> GetGlobalFooter(IPagesService service, string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false)
    {
        return await pagesService.GetGlobalFooter(service, url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls, removeSvgUrlsFromIcons);
    }

    /// <inheritdoc />
    public async Task<PageViewModel> CreatePageViewModelAsync(List<PageResourceModel> externalCss, List<int> cssTemplates, List<PageResourceModel> externalJavascript, List<int> javascriptTemplates, string bodyHtml, int templateId = 0, bool useGeneralLayout = true)
    {
        return await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, bodyHtml, templateId, useGeneralLayout);
    }

    /// <inheritdoc />
    public void SetPageSeoData(string seoTitle = null, string seoDescription = null, string seoKeyWords = null, string seoCanonical = null, bool noIndex = false, bool noFollow = false, IEnumerable<string> robots = null, string previousPageLink = null, string nextPageLink = null)
    {
        pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots, previousPageLink, nextPageLink);
    }

    /// <inheritdoc />
    public void SetOpenGraphData(IDictionary<string, string> openGraphValues)
    {
        pagesService.SetOpenGraphData(openGraphValues);
    }
}