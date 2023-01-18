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

namespace GeeksCoreLibrary.Components.WebPage.Middlewares
{
    public class RewriteUrlToWebPageMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RewriteUrlToWebPageMiddleware> logger;
        private IObjectsService objectsService;
        private IWebPagesService webPagesService;

        public RewriteUrlToWebPageMiddleware(RequestDelegate next, ILogger<RewriteUrlToWebPageMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// IObjectsService and IDatabaseConnection are here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="objectsService"></param>
        /// <param name="webPagesService"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, IObjectsService objectsService, IWebPagesService webPagesService)
        {
            logger.LogDebug("Invoked RewriteUrlToWebPageMiddleware");
            
            this.objectsService = objectsService;
            this.webPagesService = webPagesService;
            
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
            // Only handle the redirecting to webpages on normal URLs, not on images, css, js, etc.
            var regEx = new Regex(Core.Models.CoreConstants.UrlsToSkipForMiddlewaresRegex);
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(context);
            if (regEx.IsMatch(currentUrl.ToString()))
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
            
            var rewriteTo = "";
            var fixedUrlParentIds = (await objectsService.FindSystemObjectByDomainNameAsync("cms_fixedurl_parentids", "0")).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(UInt64.Parse).ToList();
            var fixedUrlPageMethod = new Dictionary<ulong, string>();
            var fixedUrlPageParamName = new Dictionary<ulong, string>();

            foreach (var entry in (await objectsService.FindSystemObjectByDomainNameAsync("cms_fixedurl_page_method", "0")).Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var regex = new Regex(@"^\d+\|");
                if (!entry.Contains("|", StringComparison.Ordinal) || !regex.IsMatch(entry))
                {
                    continue;
                }

                var splitValues = entry.Split('|');
                if (splitValues.Length < 3)
                {
                    logger.LogWarning($"Found invalid value in setting 'cms_fixedurl_page_method': '{entry}'");
                    continue;
                }

                var id = UInt64.Parse(splitValues[0]);
                fixedUrlPageMethod.Add(id, splitValues[1]);
                fixedUrlPageParamName.Add(id, splitValues[2]);
            }

            var queryString = new QueryString();
            var webPagePath = new List<string>();
            ulong temporaryParentId = 0;
            string temporaryTemplateString;
            string temporaryParameterName;

            for (var i = 0; i < webPage.Value.Path.Count; i++)
            {
                if (fixedUrlParentIds.Contains(webPage.Value.Parents[i]))
                {
                    temporaryParentId = webPage.Value.Parents[i];
                    break;
                }

                webPagePath.Add(webPage.Value.Path[i]);
                queryString = queryString.Add($"{new string('p', i + 1)}[PARAM]", webPage.Value.Path[i]);
            }

            if (fixedUrlPageMethod.ContainsKey(temporaryParentId))
            {
                temporaryTemplateString = fixedUrlPageMethod[temporaryParentId];
                temporaryParameterName = fixedUrlPageParamName[temporaryParentId];
            }
            else if (fixedUrlPageMethod.ContainsKey(0))
            {
                temporaryTemplateString = fixedUrlPageMethod[0];
                temporaryParameterName = fixedUrlPageParamName[0];
            }
            else
            {
                temporaryTemplateString = "M1";
                temporaryParameterName = "name";
            }

            if (temporaryTemplateString.Equals("M1", StringComparison.OrdinalIgnoreCase))
            {
                // The path parts are added in a reverse order, so invert the list here.
                webPagePath.Reverse();
                webPagePath.Add(webPage.Value.Title);
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
}
