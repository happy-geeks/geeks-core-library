using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using LazyCache;

namespace GeeksCoreLibrary.Modules.Templates.Middlewares
{
    public class OutputCachingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<OutputCachingMiddleware> logger;
        private IObjectsService objectsService;
        private ILanguagesService languagesService;
        private GclSettings gclSettings;

        public OutputCachingMiddleware(RequestDelegate next, ILogger<OutputCachingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context, IObjectsService objectsService, ITemplatesService templatesService, ILanguagesService languagesService, IWebHostEnvironment webHostEnvironment, IOptions<GclSettings> gclSettings, IAppCache cache)
        {
            logger.LogDebug("Invoked OutputCachingMiddleware");
            
            // Don't even bother doing anything if it's not the correct route.
            if (!context.Request.Method.Equals("GET") || context.Request.Path != "/template.gcl")
            {
                await next.Invoke(context);
                return;
            }

            this.objectsService = objectsService;
            this.languagesService = languagesService;
            this.gclSettings = gclSettings.Value;

            // Check if 
            var templateName = HttpContextHelpers.GetRequestValue(context, "templatename");
            Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out var templateId);

            if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
            {
                await next.Invoke(context);
                return;
            }

            logger.LogDebug($"Start output cache for page '{HttpContextHelpers.GetOriginalRequestUri(context)}'");
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

            if (skipCache)
            {
                logger.LogDebug($"Content cache disabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', because it's disabled for {this.gclSettings.Environment}.");
                await next.Invoke(context);
                return;
            }

            // Entire page HTML will be saved to this string.
            string pageHtml = null;

            // Retrieve the template.
            var contentTemplate = await templatesService.GetTemplateCacheSettingsAsync(templateId, templateName);

            // Check if caching is enabled for this template.
            if (contentTemplate.CachingMode == TemplateCachingModes.NoCaching || contentTemplate.CachingMinutes <= 0)
            {
                logger.LogDebug($"Content cache disabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', because it's disabled in the template settings ({contentTemplate.Id}).");
                await next.Invoke(context);
                return;
            }

            // Check regular expression for caching.
            if (!String.IsNullOrWhiteSpace(contentTemplate.CachingRegex))
            {
                var requestUri = HttpContextHelpers.GetOriginalRequestUri(context);
                if (!Regex.IsMatch(requestUri.PathAndQuery, contentTemplate.CachingRegex, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                {
                    logger.LogDebug($"Content cache disabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', because the regular expression ({contentTemplate.CachingRegex}) from the template settings ({contentTemplate.Id}) does not match the current URL ({requestUri.AbsolutePath}).");
                    await next.Invoke(context);
                    return;
                }
            }

            // Get folder and file name.
            var cacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(cacheFolder))
            {
                logger.LogWarning("Content cache is enabled but the directory 'contentcache' does not exist. Please create it and give it modify permissions to the user that is running the website (on Windows / IIS, this is the user 'IIS_IUSRS' bij default).");
                await next.Invoke(context);
                return;
            }

            var cacheFileName = await templatesService.GetTemplateOutputCacheFileNameAsync(contentTemplate);
            var fullCachePath = Path.Combine(cacheFolder, Models.Constants.PageCacheRootDirectoryName, contentTemplate.Type.ToString(), $"{contentTemplate.Name.StripIllegalPathCharacters()} ({contentTemplate.Id})", cacheFileName);
            var cacheKey = Path.GetFileNameWithoutExtension(cacheFileName);

            switch (contentTemplate.CachingLocation)
            {
                case TemplateCachingLocations.InMemory:
                {
                    logger.LogDebug($"Content cache enabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', cache in memory with key: {cacheKey}.");
                    pageHtml = await cache.GetAsync<string>(cacheKey);
                    break;
                }
                case TemplateCachingLocations.OnDisk:
                {
                    logger.LogDebug($"Content cache enabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', cache file location: {fullCachePath}.");

                    // Check if a cache file already exists and if it hasn't expired yet.
                    var fileInfo = new FileInfo(fullCachePath);
                    if (fileInfo.Directory is { Exists: false })
                    {
                        fileInfo.Directory.Create();
                    } 
                    else if (fileInfo.Exists)
                    {
                        if (fileInfo.LastWriteTimeUtc.AddMinutes(contentTemplate.CachingMinutes) > DateTime.UtcNow)
                        {
                            using var fileReader = new StreamReader(fileInfo.OpenRead(), Encoding.UTF8);
                            pageHtml = $"{await fileReader.ReadToEndAsync()}<!-- TEMPLATE FROM CACHE ({contentTemplate.Id}) -->";
                        }
                        else
                        {
                            // Cleanup the old cache file if it has expired.
                            fileInfo.Delete();
                        }
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentTemplate.CachingLocation), contentTemplate.CachingLocation.ToString());
            }

            if (!String.IsNullOrWhiteSpace(pageHtml))
            {
                // A cache file exists; create a ContentResult and execute it immediately to skip everything else.
                var result = new ContentResult
                {
                    Content = pageHtml,
                    ContentType = "text/html"
                };

                var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<ContentResult>>();
                var routeData = context.GetRouteData();
                var actionContext = new ActionContext(context, routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

                await executor.ExecuteAsync(actionContext, result);

                logger.LogDebug($"Content cache file found for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', templateId: {contentTemplate.Id}, templateName: {contentTemplate.Name}.");

                return;
            }

            logger.LogDebug($"Content cache file NOT found for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', creating new one...");

            // Remember the original body.
            var originalBody = context.Response.Body;

            try
            {
                // Create a new body that will be used temporarily.
                await using var newBody = new MemoryStream();
                context.Response.Body = newBody;

                // Invoke next middleware.
                await next.Invoke(context);

                // The HTML should be generated at this point; read the entire response body as a string.
                newBody.Position = 0;
                pageHtml = await new StreamReader(newBody).ReadToEndAsync();

                // Turn the string back into a stream.
                await using var newStream = new MemoryStream();
                await using var writer = new StreamWriter(newStream);
                await writer.WriteAsync(pageHtml);
                await writer.FlushAsync();
                newStream.Position = 0;

                // Copy the new body to the original body.
                await newStream.CopyToAsync(originalBody);

                switch (contentTemplate.CachingLocation)
                {
                    case TemplateCachingLocations.InMemory:
                        cache.Add(cacheKey, pageHtml, DateTimeOffset.UtcNow.AddMinutes(contentTemplate.CachingMinutes));
                        break;
                    case TemplateCachingLocations.OnDisk:
                        // Write the HTML to the cache file.
                        await File.WriteAllTextAsync(fullCachePath, pageHtml);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(contentTemplate.CachingLocation), contentTemplate.CachingLocation.ToString());
                }
            }
            finally
            {
                // Put the original body back in the response.
                context.Response.Body = originalBody;
            }
        }
    }
}
