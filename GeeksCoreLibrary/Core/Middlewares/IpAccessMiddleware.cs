using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Core.Middlewares;

public class IpAccessMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<IpAccessMiddleware> logger;

    public IpAccessMiddleware(ILogger<IpAccessMiddleware> logger, RequestDelegate next)
    {
        this.logger = logger;
        this.next = next;
    }

    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, IObjectsService objectsService)
    {
        logger.LogDebug("Invoked IpAccessMiddleware");
        if (HttpContextHelpers.IsLocalhost(context))
        {
            logger.LogDebug("Current request is from localhost, so skip any ip access check.");
            await next.Invoke(context);
            return;
        }

        var userIp = HttpContextHelpers.GetUserIpAddress(context);
        var blockedIps = await objectsService.FindSystemObjectByDomainNameAsync("blockips");
        if (!String.IsNullOrEmpty(blockedIps) && blockedIps.Split(';').Contains(userIp))
        {
            logger.LogDebug("Ip blocked: found in blacklist");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        var whiteListedIps = await objectsService.FindSystemObjectByDomainNameAsync("whitelistips");
        if (!String.IsNullOrEmpty(whiteListedIps) && !whiteListedIps.Split(';').Contains(userIp))
        {
            logger.LogDebug("Ip blocked: not found in whitelist");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        await next.Invoke(context);
    }
}