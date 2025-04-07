using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.WebPage.Middlewares;

public class RewriteUrlToWebPageMiddleware(RequestDelegate next, ILogger<RewriteUrlToWebPageMiddleware> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, IObjectsService objectsService, IWebPagesService webPagesService)
    {
        logger.LogDebug("Invoked RewriteUrlToWebPageMiddleware");

        if (HttpContextHelpers.IsGclMiddleWarePage(context))
        {
            // If this happens, it means that another middleware has already found something and we don't need to do this again.
            await next.Invoke(context);
            return;
        }

        var path = context.Request.Path.ToUriComponent();
        var queryString = context.Request.QueryString;
        if (!context.Items.ContainsKey(Constants.OriginalPathKey))
        {
            context.Items.Add(Constants.OriginalPathKey, context.Request.Path);
        }

        if (!context.Items.ContainsKey(Constants.OriginalPathAndQueryStringKey))
        {
            context.Items.Add(Constants.OriginalQueryStringKey, queryString);
        }

        if (!context.Items.ContainsKey(Constants.OriginalPathAndQueryStringKey))
        {
            context.Items.Add(Constants.OriginalPathAndQueryStringKey, $"{path}{queryString.Value}");
        }

        await HandleRewritesAsync(context, path, objectsService, webPagesService);

        await next.Invoke(context);
    }

    /// <summary>
    /// This method checks if the current URI corresponds with one of the rewrites in the database.
    /// If one is found, it rewrites the current path and query string to certain GCL pages, such as template.gcl.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="path">The path of the current URI.</param>
    /// <param name="objectsService">The objectService used to get the system settings.</param>
    /// <param name="webPagesService">The webpagesService used to find the webpage based on the url.</param>
    private async Task HandleRewritesAsync(HttpContext context, string path, IObjectsService objectsService, IWebPagesService webPagesService)
    {
        // Only handle the redirecting to webpages on normal URLs, not on images, css, js, etc.
        var currentUrl = HttpContextHelpers.GetOriginalRequestUri(context);
        if (PrecompiledRegexes.UrlsToSkipForMiddlewaresRegex.IsMatch(currentUrl.ToString()))
        {
            return;
        }

        var fixedUrlActive = String.Equals("true", await objectsService.FindSystemObjectByDomainNameAsync("fixed_url_active", "true"), StringComparison.OrdinalIgnoreCase);
        if (!fixedUrlActive)
        {
            return;
        }

        var webPage = await webPagesService.GetWebPageViaFixedUrlAsync(path);
        if (!webPage.HasValue || webPage.Value.Id == 0)
        {
            return;
        }

        string rewriteTo;
        var fixedUrlParentIds = (await objectsService.FindSystemObjectByDomainNameAsync("cms_fixedurl_parentids", "0")).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(UInt64.Parse).ToList();
        var fixedUrlPageMethod = new Dictionary<ulong, string>();
        var fixedUrlPageParamName = new Dictionary<ulong, string>();

        foreach (var entry in (await objectsService.FindSystemObjectByDomainNameAsync("cms_fixedurl_page_method", "0")).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pipeIndex = entry.IndexOf('|');
            if (pipeIndex <= 0) // Must contain '|' and not start with it
            {
                continue;
            }
            
            // Try parse the first part of the entry as a number
            if (!UInt64.TryParse(entry.AsSpan(..pipeIndex), out var id))
            {
                continue;
            }

            var secondPipe = entry.AsSpan((pipeIndex+1)..).IndexOf('|');
            if (secondPipe == -1)
            {
                logger.LogWarning($"Found invalid value in setting 'cms_fixedurl_page_method': '{entry}'");
                continue;
            }
            
            fixedUrlPageMethod.Add(id, entry[..secondPipe]);
            fixedUrlPageParamName.Add(id, entry[(secondPipe + 1)..]);
        }

        var queryString = new QueryString();
        ulong temporaryParentId = 0;
        string temporaryParameterName;

        for (var i = 0; i < webPage.Value.Path.Count; i++)
        {
            if (fixedUrlParentIds.Contains(webPage.Value.Parents[i]))
            {
                temporaryParentId = webPage.Value.Parents[i];
                break;
            }

            queryString = queryString.Add($"{new string('p', i + 1)}[PARAM]", webPage.Value.Path[i]);
        }

        if (fixedUrlPageMethod.TryGetValue(temporaryParentId, out var temporaryTemplateString))
        {
            temporaryParameterName = fixedUrlPageParamName[temporaryParentId];
        }
        else if (fixedUrlPageMethod.TryGetValue(0, out temporaryTemplateString))
        {
            temporaryParameterName = fixedUrlPageParamName[0];
        }
        else
        {
            temporaryTemplateString = "M1";
            temporaryParameterName = "name";
        }

        if (temporaryTemplateString.Equals("M1", StringComparison.OrdinalIgnoreCase))
        {
            queryString = queryString.Add("name", webPage.Value.Title);
            rewriteTo = "/webpage.gcl";
            queryString = queryString.Add("id", webPage.Value.Id.ToString());
            queryString = new QueryString(queryString.Value?.Replace("[PARAM]", "name"));
        }
        else
        {
            queryString = queryString.Add(temporaryParameterName, webPage.Value.Title);
            rewriteTo = "/template.gcl";
            queryString = queryString.Add("templateid", temporaryTemplateString);
            queryString = new QueryString(queryString.Value?.Replace("[PARAM]", temporaryParameterName));
        }

        context.Request.Path = rewriteTo;
        context.Request.QueryString = queryString;
    }
}