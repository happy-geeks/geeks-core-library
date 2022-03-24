using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.OrderProcess.Middlewares
{
    public class RewriteUrlToOrderProcessMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RewriteUrlToOrderProcessMiddleware> logger;
        private IOrderProcessesService orderProcessesService;

        public RewriteUrlToOrderProcessMiddleware(RequestDelegate next, ILogger<RewriteUrlToOrderProcessMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// IObjectsService and IDatabaseConnection are here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
        /// </summary>
        public async Task Invoke(HttpContext context, IOrderProcessesService orderProcessesService)
        {
            this.orderProcessesService = orderProcessesService;

            if (context.Request.Path == "/webpage.gcl" || context.Request.Path == "/template.gcl" || context.Request.Path == "/webpage.jcl" || context.Request.Path == "/template.jcl" || context.Request.Path == "/orderProcess.gcl")
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
                context.Items.Add(Constants.OriginalPathAndQueryStringKey, path + queryString.Value);
            }

            await HandleRewrites(context, path, queryString);

            await this.next.Invoke(context);
        }

        /// <summary>
        /// This method checks if the current URI corresponds with one of the rewrites in the database.
        /// If one if found, it rewrites the current path and query string to certain GCL pages, such as template.gcl.
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/>.</param>
        /// <param name="path">The path of the current URI.</param>
        /// <param name="queryStringFromUrl">The query string from the URI.</param>
        private async Task HandleRewrites(HttpContext context, string path, QueryString queryStringFromUrl)
        {
            // Only handle the redirecting to webpages on normal URLs, not on images, css, js, etc.
            var regEx = new Regex(Core.Models.CoreConstants.UrlsToSkipForMiddlewaresRegex);
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(context);
            if (regEx.IsMatch(currentUrl.ToString()))
            {
                return;
            }
            
            var orderProcess = await orderProcessesService.GetOrderProcessViaFixedUrl(path);
            if (orderProcess == null || orderProcess.Id == 0)
            {
                return;
            }
            
            logger.LogInformation($"Found order process with id '{orderProcess.Id}' and name '{orderProcess.Title}' for current URL '{currentUrl}'.");
            queryStringFromUrl = queryStringFromUrl.Add("id", orderProcess.Id.ToString());
            queryStringFromUrl = queryStringFromUrl.Add("title", orderProcess.Title);

            context.Request.Path = "/orderProcess.gcl";
            context.Request.QueryString = queryStringFromUrl;
        }
    }
}
