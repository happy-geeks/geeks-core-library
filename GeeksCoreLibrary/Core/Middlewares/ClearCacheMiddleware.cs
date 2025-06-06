﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Middlewares;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Core.Middlewares;

public class ClearCacheMiddleware(RequestDelegate next, ILogger<RewriteUrlToOrderProcessMiddleware> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, ICacheService cacheService)
    {
        logger.LogDebug("Invoked ClearCacheMiddleware");

        // Don't event start processing if there's no query string or if the method is not GET.
        if (!context.Request.QueryString.HasValue || context.Request.Method != "GET")
        {
            await next.Invoke(context);
            return;
        }

        // Try to get the parameter that is the clear cache parameter.
        var clearCacheParameter = context.Request.Query.Keys.FirstOrDefault(key => Regex.IsMatch(key, "gcl_clear.*?cache", RegexOptions.IgnoreCase));
        if (clearCacheParameter == null || !context.Request.Query[clearCacheParameter].ToString().InList("1", "true"))
        {
            await next.Invoke(context);
            return;
        }

        // Remove the "gcl_" part.
        var cachePart = clearCacheParameter[4..];

        // Check if there's a specific area that should be cleared.
        var cacheArea = CacheAreas.Unknown;
        var regex = Regex.Match(cachePart, "^clear(?<area>.+?)cache$");
        if (regex.Success)
        {
            var areaValue = regex.Groups["area"].Value;
            if (!areaValue.InList("memory", "content", "files", "all") && (!Enum.TryParse(regex.Groups["area"].Value, true, out cacheArea) || cacheArea == CacheAreas.Unknown))
            {
                await next.Invoke(context);
                return;
            }
        }

        switch (cachePart)
        {
            case "clearcache":
            case "clearmemorycache":
                cacheService.ClearMemoryCache();
                break;
            case "clearcontentcache":
                cacheService.ClearOutputCache();
                break;
            case "clearfilescache":
                cacheService.ClearFilesCache();
                break;
            case "clearallcache":
                cacheService.ClearAllCache();
                break;
            default:
                cacheService.ClearCacheInArea(cacheArea);
                break;
        }

        var queryString = QueryHelpers.ParseQuery(context.Request.QueryString.Value ?? String.Empty);
        queryString.Remove(clearCacheParameter);

        var originalUrlPath = HttpContextHelpers.GetOriginalRequestUri(context).GetLeftPart(UriPartial.Path);
        var newUrl = QueryHelpers.AddQueryString(originalUrlPath, queryString);

        context.Response.Redirect(newUrl);
    }
}