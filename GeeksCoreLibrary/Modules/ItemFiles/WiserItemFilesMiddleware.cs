using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Middlewares;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.ItemFiles;

public class WiserItemFilesMiddleware
{
        private readonly RequestDelegate next;
        private readonly ILogger<RewriteUrlToOrderProcessMiddleware> logger;

        public WiserItemFilesMiddleware(RequestDelegate next, ILogger<RewriteUrlToOrderProcessMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// IObjectsService and IDatabaseConnection are here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            logger.LogDebug("Invoked WiserItemFilesMiddleware");
            
            if (HttpContextHelpers.IsGclMiddleWarePage(context))
            {
                // If this happens, it means that another middleware has already found something and we don't need to do this again.
                await this.next.Invoke(context);
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

            await HandleRewritesAsync(context, path, queryString);

            await this.next.Invoke(context);
        }

        /// <summary>
        /// This method checks if the current URI corresponds with one of the rewrites in the database.
        /// If one is found, it rewrites the current path and query string to certain GCL pages, such as template.gcl.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/>.</param>
        /// <param name="path">The path of the current URI.</param>
        /// <param name="queryStringFromUrl">The query string from the URI.</param>
        private async Task HandleRewritesAsync(HttpContext context, string path, QueryString queryStringFromUrl)
        {
            // Check if the current URL is that of an image or a file.
            var urlRegex = new Regex(@"(?:image\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<itemId>\d+)(?:\/(?<fileType>itemlink|direct))?\/(?<propertyName>[^\/]+)(?:\/(?<resizeMode>normal|stretch|crop|fill)(?:-(?<anchorPosition>center|top|bottom|left|right|topleft|topright|bottomright|bottomleft))?)?(?:\/(?<preferredWidth>\d+)\/(?<preferredHeight>\d+))?(?:\/(?<fileNumber>\d+))?\/(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            var matchResult = urlRegex.Match(path);
            if (!matchResult.Success)
            {
                urlRegex = new Regex(@"(?:file\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<itemId>\d+)(?:\/(?<fileType>itemlink|direct))?\/(?<propertyName>.+?)(?:\/(?<fileNumber>\d+))?(?:\/)(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
                matchResult = urlRegex.Match(path);
                if (!matchResult.Success)
                {
                    return;
                }
                
                context.Request.Path = "/wiser-file.gcl";
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