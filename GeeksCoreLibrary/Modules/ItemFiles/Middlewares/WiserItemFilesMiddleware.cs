using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Middlewares;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PrecompiledRegexes = GeeksCoreLibrary.Modules.ItemFiles.Helpers.PrecompiledRegexes;

namespace GeeksCoreLibrary.Modules.ItemFiles.Middlewares;

public class WiserItemFilesMiddleware(RequestDelegate next, ILogger<RewriteUrlToOrderProcessMiddleware> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        logger.LogDebug("Invoked WiserItemFilesMiddleware");

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

        HandleRewrites(context, path, queryString);

        await next.Invoke(context);
    }

    /// <summary>
    /// This method checks if the current URI corresponds with one of the rewrites in the database.
    /// If one is found, it rewrites the current path and query string to certain GCL pages, such as template.gcl.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="path">The path of the current URI.</param>
    /// <param name="queryStringFromUrl">The query string from the URI.</param>
    private void HandleRewrites(HttpContext context, string path, QueryString queryStringFromUrl)
    {
        // Check if the current URL is that of an image or a file.
        // If it is, we need to rewrite the URL to the correct GCL page.
        var matchResult = PrecompiledRegexes.ImageUrlRegex.Match(path);
        if (!matchResult.Success)
        {
            matchResult = PrecompiledRegexes.EncryptedImageUrlRegex.Match(path);
            if (!matchResult.Success)
            {
                matchResult = PrecompiledRegexes.FileUrlRegex.Match(path);
                if (!matchResult.Success)
                {
                    matchResult = PrecompiledRegexes.EncryptedFileUrlRegex.Match(path);
                    if (!matchResult.Success)
                    {
                        return;
                    }
                }

                context.Request.Path = "/wiser-file.gcl";
            }
            else
            {
                context.Request.Path = "/wiser-image.gcl";
            }
        }
        else
        {
            context.Request.Path = "/wiser-image.gcl";
        }

        // Add all values from the regex to the query string for the controller.
        foreach (Group group in matchResult.Groups)
        {
            if (!group.Success || String.IsNullOrWhiteSpace(group.Name) || group.Name == "0")
            {
                continue;
            }

            queryStringFromUrl = queryStringFromUrl.Add(group.Name, group.Value);
        }

        context.Request.QueryString = queryStringFromUrl;
    }
}