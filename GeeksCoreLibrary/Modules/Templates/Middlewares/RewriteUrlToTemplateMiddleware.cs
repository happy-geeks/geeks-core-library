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
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.Templates.Middlewares;

public class RewriteUrlToTemplateMiddleware(RequestDelegate next, ILogger<RewriteUrlToTemplateMiddleware> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// Services are added here instead of the constructor, because the constructor of a middleware can only contain Singleton services.
    /// </summary>
    public async Task Invoke(HttpContext context, IObjectsService objectsService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IReplacementsMediator replacementsMediator)
    {
        logger.LogDebug("Invoked RewriteUrlToTemplateMiddleware");

        if (HttpContextHelpers.IsGclMiddleWarePage(context))
        {
            // If this happens, it means that another middleware has already found something and we don't need to do this again.
            await next.Invoke(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            // If this happens, it means that another controller would already handle this and we don't need to do this again.
            await next.Invoke(context);
            return;
        }

        var routeValues = context.Request.RouteValues;
        var currentController = routeValues["controller"]?.ToString();
        var currentAction = routeValues["action"]?.ToString();
        var actionDescriptor = actionDescriptorCollectionProvider.ActionDescriptors.Items.FirstOrDefault(ad =>
        {
            if (ad is not ControllerActionDescriptor controllerActionDescriptor)
            {
                return false;
            }

            if (controllerActionDescriptor.AttributeRouteInfo?.Template != null)
            {
                var matcher = new TemplateMatcher(TemplateParser.Parse(controllerActionDescriptor.AttributeRouteInfo.Template), routeValues);
                return matcher.TryMatch(context.Request.Path, routeValues);
            }

            return currentController != null && currentAction != null
                                             && controllerActionDescriptor.ControllerName == currentController
                                             && controllerActionDescriptor.ActionName == currentAction;
        });

        if (actionDescriptor != null)
        {
            // If this happens, it means that another controller would already handle this and we don't need to do this again.
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

        await HandleRewritesAsync(context, path, queryString, templatesService, objectsService, databaseConnection, replacementsMediator);

        // if a Redirect() has been called, don't invoke the next context
        if (context.Response.StatusCode != 302)
            await next.Invoke(context);
    }

    /// <summary>
    /// This method checks if the current URI corresponds with one of the rewrites in the database.
    /// If one is found, it rewrites the current path and query string to certain GCL pages, such as template.gcl.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="path">The path of the current URI.</param>
    /// <param name="queryStringFromUrl">The query string from the URI.</param>
    /// <param name="templatesService">The templates service.</param>
    /// <param name="objectsService">The objects service.</param>
    /// <param name="databaseConnection">The database connection.</param>
    private async Task HandleRewritesAsync(HttpContext context, string path, QueryString queryStringFromUrl, ITemplatesService templatesService, IObjectsService objectsService, IDatabaseConnection databaseConnection, IReplacementsMediator replacementsMediator)
    {
        logger.LogDebug($"Start HandleRewrites, path: {path}");

        // First check if there are templates with an URL regex that matches the current URL.
        var templatesWithUrls = await templatesService.GetTemplateUrlsAsync();
        foreach (var template in templatesWithUrls)
        {
            var regex = new Regex(template.UrlRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
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

            // Redirect to the correct controller.
            switch (template.Type)
            {
                case TemplateTypes.Html:
                    context.Request.Path = "/template.gcl";
                    break;
                case TemplateTypes.Query when template is QueryTemplate{ UsedForRedirect: true }:
                    var redirectResults = await RunRedirectQuery(template, queryString, databaseConnection, templatesService, replacementsMediator);

                    // if we need to redirect, do that (this sets StatusCode to 302)
                    if (redirectResults.ContainsKey("redirecturl"))
                    {
                        context.Response.Redirect(redirectResults["redirecturl"]);
                        return;
                    }
                    // if the redirect result is a number, it's a template id, so use that
                    if (redirectResults.ContainsKey("templateid") && Int32.TryParse(redirectResults["templateid"], out Int32 templateId))
                    {
                        context.Request.Path = "/template.gcl";
                        template.Id = templateId;
                        foreach (var kvp in redirectResults)
                        {
                            if (kvp.Key.Equals("templateid", StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            queryString.Add(kvp.Key, kvp.Value);
                        }
                    }
                    // If the query doesn't give a (correct) result, we go 404.
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    break;
                case TemplateTypes.Query:
                    context.Request.Path = "/json.gcl";
                    break;
                case TemplateTypes.Routine:
                    context.Request.Path = "/routine.gcl";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(template.Type), template.Type.ToString(), null);
            }

            // Extra query string in the template.
            queryString = CombineQueryString(queryString, $"?templateId={template.Id}");
            queryString = CombineQueryString(queryString, queryStringFromUrl.Value);

            context.Request.QueryString = queryString;

            // We found a template that matches the URL, so we can exit the function here.
            return;
        }

        // If we haven't found a matching URL yet, check the old legacy settings.
        var urlRewrites = (await GetValues("url_syntax_templates", objectsService)).ToList();
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

        var alsoMatchWithQueryString = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("url_syntax_alsomatchquerystring", "false", String.Empty), "true", StringComparison.OrdinalIgnoreCase);

        foreach (var urlRewrite in urlRewrites)
        {
            var lastIndex = urlRewrite.LastIndexOf("|", StringComparison.Ordinal);
            var urlMatchFirstPart = urlRewrite[..lastIndex];
            var urlMatchLastPart = urlRewrite[(lastIndex + 1)..];

            // Example: ^/informatie/(?<name>.*?)/(?<pname>.*?)/$
            var regex = new Regex(urlMatchFirstPart, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
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
                    var value = matchResult.Groups[groupName].Value;
                    if (String.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    webpagePath.Add(value);
                }

                var newQueryString = String.Join(String.Empty, new[]
                {
                    $"?gclcmspagepath={await objectsService.FindSystemObjectByDomainNameAsync("cmsprefix")}",
                    String.Join("/", webpagePath),
                    queryString.Value,
                    queryStringFromUrl.Value
                });

                context.Request.Path = "/webpage.gcl";
                context.Request.QueryString = QueryString.FromUriComponent(newQueryString);

                // We found a template that matches the URL, so we can exit the function here.
                return;
            }

            // Payment service provider handling.
            if (String.Equals(urlMatchLastPart, "M2", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Path = Components.OrderProcess.Models.Constants.PaymentOutPage;
                context.Request.QueryString = QueryString.FromUriComponent($"?{queryString.ToString().Substring(1)}{queryStringFromUrl.Value}");

                // We found a template that matches the URL, so we can exit the function here.
                return;
            }

            // PDF files.
            int number;
            if (urlMatchLastPart.StartsWith("PDF", StringComparison.OrdinalIgnoreCase))
            {
                // Extra query string in the template
                queryString = CombineQueryString(queryString, urlMatchLastPart);

                // This is a template or webpage that must be loaded because the value is url|templateid or url|-1*webpageid.
                context.Request.Path = "/pdf.gcl";
                if (Int32.TryParse(urlMatchLastPart.AsSpan(3), out number) && number > 0)
                {
                    // It is a template.
                    context.Request.QueryString = QueryString.FromUriComponent($"?templateid={urlMatchLastPart.Substring(3)}{urlMatchLastPart}{queryStringFromUrl.Value}");

                    // We found a template that matches the URL, so we can exit the function here.
                    return;
                }

                // It is a webpage.
                context.Request.QueryString = QueryString.FromUriComponent($"?id={number * -1}{queryString}{queryStringFromUrl.Value}");

                // We found a template that matches the URL, so we can exit the function here.
                return;
            }

            // This is a template or webpage that must be loaded because the value is url|templateid or url|-1*webpageid.
            if (!Int32.TryParse(urlMatchLastPart.Split('?', '&').First(), out number) || number <= 0)
            {
                continue;
            }

            var template = await templatesService.GetTemplateAsync(number);
            if (template is QueryTemplate { UsedForRedirect: true })
            {
                var redirectResults = await RunRedirectQuery(template, queryString, databaseConnection, templatesService, replacementsMediator);

                // if we need to redirect, do that (this sets StatusCode to 302)
                if (redirectResults.ContainsKey("redirecturl"))
                {
                    context.Response.Redirect(redirectResults["redirecturl"]);
                    return;
                }
                // if the redirect result is a number, it's a template id, so use that
                if (redirectResults.ContainsKey("templateid") && Int32.TryParse(redirectResults["templateid"], out Int32 templateId))
                {
                    queryString = CombineQueryString(queryString, $"?templateid={urlMatchLastPart.Replace($"{number}", $"{templateId}").Replace("?", "&")}");
                    foreach (var kvp in redirectResults)
                    {
                        if (kvp.Key.Equals("templateid", StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        queryString.Add(kvp.Key, kvp.Value);
                    }
                }
                // If the query doesn't give a (correct) result, we go 404.
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }
            else
            {
                queryString = CombineQueryString(queryString, $"?templateid={urlMatchLastPart.Replace("?", "&")}");
            }

            // Extra query string in the template.
            queryString = CombineQueryString(queryString, queryStringFromUrl.Value);

            // It is a template.
            context.Request.Path = "/template.gcl";
            context.Request.QueryString = queryString;

            // We found a template that matches the URL, so we can exit the function here.
            return;
        }
    }

    private async Task<Dictionary<string, string>> RunRedirectQuery(Template template, QueryString queryString, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IReplacementsMediator replacementsMediator)
    {
        var queryStringCollection = System.Web.HttpUtility.ParseQueryString(queryString.ToString());
        var query = template.Content;

        query = replacementsMediator.DoReplacements(query, queryStringCollection, forQuery: true);
        query = await templatesService.DoReplacesAsync(query, forQuery: true, templateType: TemplateTypes.Query);

        var dataTable = await databaseConnection.GetAsync(query);
        if (dataTable.Rows.Count != 1)
            throw new InvalidOperationException($"Redirect query (id {template.Id}) returned {dataTable.Rows.Count} results, expected one!");

        var dataRow = dataTable.Rows[0];
        var queryResults = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (DataColumn column in dataTable.Columns)
        {
            string value = dataRow[column] == DBNull.Value ? "" : dataRow[column].ToString();
            queryResults.Add(column.ColumnName, value);
        }

        return queryResults;
    }

    /// <summary>
    /// Gets a list of values from a system object.
    /// </summary>
    /// <param name="objectName">The name / key of the system object to retrieve.</param>
    /// <param name="objectsService">The <see cref="IObjectsService"/> to use to find the setting.</param>
    /// <returns>A list of string values.</returns>
    private static async Task<IEnumerable<string>> GetValues(string objectName, IObjectsService objectsService)
    {
        var value = await objectsService.FindSystemObjectByDomainNameAsync(objectName);
        var urlRewrites = value.Split(["\r", "\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries)
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