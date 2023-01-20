using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Templates.Middlewares
{
    public class RewriteUrlToTemplateMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RewriteUrlToTemplateMiddleware> logger;
        private IObjectsService objectsService;
        private IDatabaseConnection databaseConnection;
        private ITemplatesService templatesService;

        public RewriteUrlToTemplateMiddleware(RequestDelegate next, ILogger<RewriteUrlToTemplateMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the middleware.
        /// IObjectsService, IDatabaseConnection and templatesService are here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
        /// </summary>
        public async Task Invoke(HttpContext context, IObjectsService objectsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService)
        {
            logger.LogDebug("Invoked RewriteUrlToTemplateMiddleware");
            
            this.objectsService = objectsService;
            this.databaseConnection = databaseConnection;
            this.templatesService = templatesService;
            
            if (HttpContextHelpers.IsGclMiddleWarePage(context))
            {
                // If this happens, it means that another middleware has already found something and we don't need to do this again.
                await this.next.Invoke(context);
                return;
            }

            var path = context.Request.Path.ToUriComponent();
            if (path.StartsWith("/api/", StringComparison.InvariantCultureIgnoreCase))
            {
                // An API URL is called, no need to find a template.
                await this.next.Invoke(context);
                return;
            }
            
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
            logger.LogDebug($"Start HandleRewrites, path: {path}");

            // First check if there are templates with an URL regex that matches the current URL.
            var templatesWithUrls = await templatesService.GetTemplateUrlsAsync();
            foreach (var template in templatesWithUrls)
            {
                var regex = new Regex(template.UrlRegex);
                var matchResult = regex.Match(path);
                if (!matchResult.Success)
                {
                    continue;
                }

                // Add all matched groups to the query string, so that they can be used as variables in templates.
                var queryString = new QueryString();
                foreach (Group matchGroup in matchResult.Groups)
                {
                    queryString = queryString.Add(matchGroup.Name, matchGroup.Value);
                }
                
                // Extra query string in the template.
                queryString = CombineQueryString(queryString, $"?templateId={template.Id}");
                queryString = CombineQueryString(queryString, queryStringFromUrl.Value);

                // Redirect to the correct controller.
                switch (template.Type)
                {
                    case TemplateTypes.Html:
                        context.Request.Path = "/template.gcl";
                        break;
                    case TemplateTypes.Query:
                        context.Request.Path = "/json.gcl";
                        break;
                    case TemplateTypes.Routine:
                        context.Request.Path = "/routine.gcl";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(template.Type), template.Type.ToString());
                }

                context.Request.QueryString = queryString;
                break;
            }

            // If we haven't found a matching URL yet, check the old legacy settings.
            var urlRewrites = (await GetValues("url_syntax_templates")).ToList();
            var fixedUrlQuery = await objectsService.FindSystemObjectByDomainNameAsync("fixedurl_query");

            // If there is a query for fixed URLs, add the results to the urlRewrites list.
            if (!String.IsNullOrWhiteSpace(fixedUrlQuery))
            {
                var dataSet = await databaseConnection.GetAsync(fixedUrlQuery);
                foreach (DataRow row in dataSet.Rows)
                {
                    urlRewrites.Insert(0, row.Field<string>(0));
                }
            }

            var alsoMatchWithQueryString = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("url_syntax_alsomatchquerystring", "false", String.Empty, true), "true", StringComparison.OrdinalIgnoreCase);

            foreach (var urlRewrite in urlRewrites)
            {
                var lastIndex = urlRewrite.LastIndexOf("|", StringComparison.Ordinal);
                var urlMatchFirstPart = urlRewrite[..lastIndex];
                var urlMatchLastPart = urlRewrite[(lastIndex + 1)..];

                // Example: ^/informatie/(?<name>.*?)/(?<pname>.*?)/$
                var regex = new Regex(urlMatchFirstPart);
                var matchResult = regex.Match(path);

                if (alsoMatchWithQueryString && !matchResult.Success)
                {
                    matchResult = regex.Match(context.Request.GetEncodedPathAndQuery());
                }

                if (!matchResult.Success)
                {
                    continue;
                }

                // Add all matched groups to the query string, so that they can be used as variables in templates.
                var queryString = new QueryString();
                foreach (Group matchGroup in matchResult.Groups)
                {
                    queryString = queryString.Add(matchGroup.Name, matchGroup.Value);
                }

                // Webpages module.
                if (String.Equals(urlMatchLastPart, "M1", StringComparison.OrdinalIgnoreCase))
                {
                    var webpagePath = new List<string>();
                    for (var i = 0; i <= 5; i++)
                    {
                        var groupName = $"{new string('p', 5 - i)}name";
                        var value = matchResult.Groups[groupName]?.Value;
                        if (String.IsNullOrWhiteSpace(value))
                        {
                            continue;
                        }

                        webpagePath.Add(value);
                    }

                    var newQueryString = String.Join(String.Empty, new[]
                    {
                        $"?gclcmspagepath={await this.objectsService.FindSystemObjectByDomainNameAsync("cmsprefix")}",
                        String.Join("/", webpagePath),
                        queryString.Value,
                        queryStringFromUrl.Value
                    });

                    context.Request.Path = "/webpage.gcl";
                    context.Request.QueryString = QueryString.FromUriComponent(newQueryString);
                    break;
                }

                // Payment service provider handling.
                if (String.Equals(urlMatchLastPart, "M2", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Path = Components.OrderProcess.Models.Constants.PaymentOutPage;
                    context.Request.QueryString = QueryString.FromUriComponent($"?{queryString.ToString().Substring(1)}{queryStringFromUrl.Value}");
                    break;
                }

                // PDF files.
                int number;
                if (urlMatchLastPart.StartsWith("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    // Extra query string in the template
                    queryString = CombineQueryString(queryString, urlMatchLastPart);

                    // This is a template or webpage that must be loaded because the value is url|templateid or url|-1*webpageid.
                    context.Request.Path = "/pdf.gcl";
                    if (Int32.TryParse(urlMatchLastPart.Substring(3), out number) && number > 0)
                    {
                        // It is a template.
                        context.Request.QueryString = QueryString.FromUriComponent($"?templateid={urlMatchLastPart.Substring(3)}{urlMatchLastPart}{queryStringFromUrl.Value}");
                        break;
                    }

                    // It is a webpage.
                    context.Request.QueryString = QueryString.FromUriComponent($"?id={number * -1}{queryString}{queryStringFromUrl.Value}");
                    break;
                }

                //This is a template or webpage that must be loaded because the value is url|templateid or url|-1*webpageid
                if (Int32.TryParse(urlMatchLastPart.Split('?', '&').First(), out number) && number > 0)
                {
                    // Extra query string in the template.
                    queryString = CombineQueryString(queryString, $"?templateid={urlMatchLastPart.Replace("?", "&")}");
                    queryString = CombineQueryString(queryString, queryStringFromUrl.Value);

                    // It is a template.
                    context.Request.Path = "/template.gcl";
                    context.Request.QueryString = queryString;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets a list of values from a system object.
        /// </summary>
        /// <param name="objectName">The name / key of the system object to retreive.</param>
        /// <returns>A list of string values.</returns>
        private async Task<IEnumerable<string>> GetValues(string objectName)
        {
            var value = await objectsService.FindSystemObjectByDomainNameAsync(objectName);
            var urlRewrites = value.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !String.IsNullOrWhiteSpace(s));

            return urlRewrites;
        }

        /// <summary>
        /// Combines two query strings.
        /// </summary>
        /// <param name="extraQueryString"></param>
        /// <param name="queryString"></param>
        private static QueryString CombineQueryString(QueryString queryString, string extraQueryString)
        {
            if (String.IsNullOrWhiteSpace(extraQueryString))
            {
                return queryString;
            }

            var parsedQuery = QueryHelpers.ParseQuery(extraQueryString);
            foreach (var (key, value) in parsedQuery)
            {
                queryString = queryString.Add(key, value);
            }

            return queryString;
        }
    }
}
