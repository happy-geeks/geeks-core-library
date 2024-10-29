using System;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Core.Middlewares;

public class AddCacheHeaderValueMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<AddCacheHeaderValueMiddleware> logger;
    private readonly GclSettings gclSettings;

    public AddCacheHeaderValueMiddleware(RequestDelegate next, ILogger<AddCacheHeaderValueMiddleware> logger, IOptions<GclSettings> gclSettings)
    {
        this.next = next;
        this.logger = logger;
        this.gclSettings = gclSettings.Value;
    }

    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        logger.LogDebug("Invoked AddCacheHeaderValueMiddleware");

        // Get the path of the current HTTP request.
        var requestPath = context.Request.Path.Value;

        // Retrieve the list of cache-control rules from the app settings.
        var cacheControlRules = gclSettings.CacheControlRules;

        // Find the first rule that matches the current request path (case-insensitive comparison).
        var matchingRule = cacheControlRules?.FirstOrDefault(rule => rule.FilePath.Equals(requestPath, StringComparison.OrdinalIgnoreCase));

        // If a matching rule is found, set the Cache-Control header in the response.
        if (matchingRule != null)
        {
            context.Response.Headers["Cache-Control"] = matchingRule.HeaderValue;
            logger.LogDebug("Cache-Control header set to '{HeaderValue}' for path '{RequestPath}'.", matchingRule.HeaderValue, requestPath);
        }

        await next.Invoke(context);
    }
}