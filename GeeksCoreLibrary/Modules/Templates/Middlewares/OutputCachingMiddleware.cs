using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;

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

        public async Task Invoke(HttpContext context, IObjectsService objectsService, ITemplatesService templatesService, ILanguagesService languagesService, IWebHostEnvironment webHostEnvironment, IOptions<GclSettings> gclSettings)
        {
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
            var contentTemplate = await templatesService.GetTemplateAsync(templateId, templateName);

            // Check if caching is enabled for this template.
            if (contentTemplate.CachingMode == TemplateCachingModes.NoCaching || contentTemplate.CachingMinutes <= 0)
            {
                logger.LogDebug($"Content cache disabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', because it's disabled in the template settings ({contentTemplate.Id}).");
                await next.Invoke(context);
                return;
            }

            // Get folder and file name.
            var cacheFolder = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
            var cacheFileName = await GetTemplateOutputCacheFileNameAsync(context, contentTemplate);
            var fullCachePath = Path.Combine(cacheFolder, cacheFileName);
            
            logger.LogDebug($"Content cache enabled for page '{HttpContextHelpers.GetOriginalRequestUri(context)}', cache file location: {fullCachePath}.");

            // Check if a cache file already exists and if it hasn't expired yet.
            var fileInfo = new FileInfo(fullCachePath);
            if (fileInfo.Exists && fileInfo.LastWriteTimeUtc.AddMinutes(contentTemplate.CachingMinutes) > DateTime.UtcNow)
            {
                using var fileReader = new StreamReader(fileInfo.OpenRead(), Encoding.UTF8);
                pageHtml = $"{await fileReader.ReadToEndAsync()}<!-- TEMPLATE FROM CACHE ({contentTemplate.Id}) -->";
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

                // Write the HTML to the cache file.
                await File.WriteAllTextAsync(Path.Combine(cacheFolder, cacheFileName), pageHtml);
            }
            finally
            {
                // Put the original body back in the response.
                context.Response.Body = originalBody;
            }
        }

        /// <summary>
        /// Creates the file name the cached HTML will be saved to and loaded from.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/>.</param>
        /// <param name="contentTemplate">The <see cref="Template"/>.</param>
        /// <returns></returns>
        private async Task<string> GetTemplateOutputCacheFileNameAsync(HttpContext context, Template contentTemplate)
        {
            var originalUri = HttpContextHelpers.GetOriginalRequestUri(context);
            var cacheFileName = new StringBuilder($"template_{contentTemplate.Id}_");
            switch (contentTemplate.CachingMode)
            {
                case TemplateCachingModes.ServerSideCaching:
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrl:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.AbsolutePath.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerUrlAndQueryString:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.PathAndQuery.ToSha512Simple()));
                    break;
                case TemplateCachingModes.ServerSideCachingPerHostNameAndQueryString:
                    cacheFileName.Append(Uri.EscapeDataString(originalUri.ToString().ToSha512Simple()));
                    break;
                case TemplateCachingModes.NoCaching:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentTemplate.CachingMode), contentTemplate.CachingMode.ToString());
            }

            // If the caching should deviate based on certain cookies, then the names and values of those cookies should be added to the file name.
            var cookieCacheDeviation = (await objectsService.FindSystemObjectByDomainNameAsync("contentcaching_cookie_deviation")).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (cookieCacheDeviation.Length > 0)
            {
                var requestCookies = context?.Request.Cookies;
                foreach (var cookieName in cookieCacheDeviation)
                {
                    if (requestCookies == null || !requestCookies.TryGetValue(cookieName, out var cookieValue))
                    {
                        continue;
                    }

                    var combinedCookiePart = $"{cookieName}:{cookieValue}";
                    cacheFileName.Append($"_{Uri.EscapeDataString(combinedCookiePart.ToSha512Simple())}");
                }
            }

            // And finally add the language code to the file name.
            if (!String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                cacheFileName.Append($"_{languagesService.CurrentLanguageCode}");
            }

            cacheFileName.Append(".html");

            return cacheFileName.ToString();
        }
    }
}
