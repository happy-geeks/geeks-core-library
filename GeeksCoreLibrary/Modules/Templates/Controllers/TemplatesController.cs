using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Constants = GeeksCoreLibrary.Modules.Templates.Models.Constants;

namespace GeeksCoreLibrary.Modules.Templates.Controllers;

[Area("Templates")]
public class TemplatesController(
    ILogger<TemplatesController> logger,
    ITemplatesService templatesService,
    IPagesService pagesService,
    IDataSelectorsService dataSelectorsService,
    IWiserItemsService wiserItemsService,
    IStringReplacementsService stringReplacementsService,
    IBranchesService branchesService,
    IOptions<GclSettings> gclSettings)
    : Controller
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    [Route("template.gcl")]
    [Route("template.jcl")]
    public async Task<IActionResult> Template()
    {
        var templateId = 0;

        try
        {
            var context = HttpContext;
            var templateName = HttpContextHelpers.GetRequestValue(context, "templatename");
            _ = Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out templateId);
            if (templateId <= 0)
            {
                _ = Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "id"), out templateId);
            }

            logger.LogDebug($"GetAsync content from HTML template, templateName: '{templateName}', templateId: '{templateId}'.");

            if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
            {
                return NotFound();
            }

            var useAbsoluteImageUrls = String.Equals(HttpContextHelpers.GetRequestValue(HttpContext, "absoluteImageUrls"), "true", StringComparison.OrdinalIgnoreCase);
            var removeSvgUrlsFromIcons = String.Equals(HttpContextHelpers.GetRequestValue(HttpContext, "removeSvgUrlsFromIcons"), "true", StringComparison.OrdinalIgnoreCase);

            var javascriptTemplates = new List<int>();
            var cssTemplates = new List<int>();
            var externalJavascript = new List<PageResourceModel>();
            var externalCss = new List<PageResourceModel>();
            var contentTemplate = await pagesService.GetRenderedTemplateAsync(templateId, templateName, useAbsoluteImageUrls: useAbsoluteImageUrls, removeSvgUrlsFromIcons: removeSvgUrlsFromIcons);

            templateId = contentTemplate.Id;

            javascriptTemplates.AddRange(contentTemplate.JavascriptTemplates);
            cssTemplates.AddRange(contentTemplate.CssTemplates);

            if (contentTemplate.Id <= 0)
            {
                // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login.
                if (!contentTemplate.LoginRequired)
                {
                    // Login isn't required; return 404 (Not Found).
                    return NotFound();
                }

                if (contentTemplate.Type == TemplateTypes.Html && !String.IsNullOrWhiteSpace(contentTemplate.LoginRedirectUrl))
                {
                    // Login required and a redirect URL is set; return redirect.
                    var redirectUrl = await stringReplacementsService.DoAllReplacementsAsync(contentTemplate.LoginRedirectUrl);
                    return Redirect(redirectUrl);
                }

                // Return unauthorized.
                return Unauthorized();
            }

            if (contentTemplate.Content == null && contentTemplate.ReturnNotFoundWhenPreLoadQueryHasNoData)
            {
                return NotFound();
            }

            var useGeneralLayout = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase) && !contentTemplate.IsPartial;
            switch (contentTemplate.Type)
            {
                case TemplateTypes.Js:
                    return Content(contentTemplate.Content ?? "", MediaTypeNames.Text.JavaScript);
                case TemplateTypes.Scss:
                case TemplateTypes.Css:
                    return Content(contentTemplate.Content ?? "", MediaTypeNames.Text.Css);
                case TemplateTypes.Query:
                    var jsonResult = await templatesService.GetJsonResponseFromQueryAsync((QueryTemplate) contentTemplate);
                    return Content(JsonConvert.SerializeObject(jsonResult), MediaTypeNames.Application.Json);
                case TemplateTypes.Normal:
                case TemplateTypes.Unknown:
                    return Content(contentTemplate.Content ?? "", MediaTypeNames.Text.Plain);
                case TemplateTypes.Html:
                    var noIndex = contentTemplate.RobotsNoIndex;
                    var noFollow = contentTemplate.RobotsNoFollow;

                    // Set SEO and Open Graph information.
                    if (HttpContext.Items.ContainsKey(Constants.TemplatePreLoadQueryResultKey))
                    {
                        var dataRow = (DataRow) HttpContext.Items[Constants.TemplatePreLoadQueryResultKey];

                        if (dataRow != null)
                        {
                            var seoTitle = dataRow.GetValueIfColumnExists<string>("SEOtitle");
                            var seoDescription = dataRow.GetValueIfColumnExists<string>("SEOdescription");
                            var seoKeyWords = dataRow.GetValueIfColumnExists<string>("SEOkeywords");
                            var seoCanonical = dataRow.GetValueIfColumnExists<string>("SEOcanonical");
                            var robots = dataRow.GetValueIfColumnExists<string>("SEOrobots");

                            if (dataRow.Table.Columns.Contains("noindex"))
                            {
                                noIndex = Convert.ToBoolean(dataRow.GetValueIfColumnExists("noindex"));
                            }

                            if (dataRow.Table.Columns.Contains("nofollow"))
                            {
                                noFollow = Convert.ToBoolean(dataRow.GetValueIfColumnExists("nofollow"));
                            }

                            pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));

                            // Add Open Graph data.
                            var openGraphValues = dataRow.Table.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("opengraph_", StringComparison.OrdinalIgnoreCase)).ToDictionary(c => c.ColumnName, c => Convert.ToString(dataRow[c]));
                            pagesService.SetOpenGraphData(openGraphValues);
                        }
                    }
                    else
                    {
                        pagesService.SetPageSeoData("", "", "", "", noIndex, noFollow);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentTemplate.Type), contentTemplate.Type.ToString(), null);
            }

            var contentToWrite = new StringBuilder();
            var url = (string) context.Items[Constants.OriginalPathAndQueryStringKey];

            // Header template.
            if (useGeneralLayout)
            {
                contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls: useAbsoluteImageUrls, removeSvgUrlsFromIcons: removeSvgUrlsFromIcons));
            }

            // Content template.
            contentToWrite.Append(contentTemplate.Content);

            // Footer template.
            if (useGeneralLayout)
            {
                contentToWrite.Append(await pagesService.GetGlobalFooter(url, javascriptTemplates, cssTemplates, useAbsoluteImageUrls: useAbsoluteImageUrls, removeSvgUrlsFromIcons: removeSvgUrlsFromIcons));
            }

            var newBodyHtml = contentToWrite.ToString();

            var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml, templateId, useGeneralLayout);

            if (!useGeneralLayout)
            {
                // Check if the page has any CSS or JS to load. If so, return the view model so the view can load the CSS and JS.
                if (externalCss.Count > 0 || cssTemplates.Count > 0 || externalJavascript.Count > 0 || javascriptTemplates.Count > 0)
                {
                    return View(viewModel);
                }

                return Content(newBodyHtml, MediaTypeNames.Text.Html);
            }

            // If a component set the status code to a 4xx status code, then return that actual status code
            // here too, so the StatusCodePagesWithReExecute middleware can handle the showing of custom error pages.
            if (Response.StatusCode is >= 400 and <= 499)
            {
                return StatusCode(Response.StatusCode);
            }

            // If we still have no page title, just use the name of the template.
            if (String.IsNullOrWhiteSpace(viewModel.MetaData.PageTitle))
            {
                viewModel.MetaData.PageTitle = contentTemplate.Name;
            }

            var branchDatabase = branchesService.GetDatabaseNameFromCookie();
            if (!String.IsNullOrWhiteSpace(branchDatabase))
            {
                viewModel.MetaData.PageTitle = $"BRANCH {branchDatabase} - {viewModel.MetaData.PageTitle}";
            }

            return View(viewModel);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{Constants.TemplateRenderingError} '{templateId}'");

            if (gclSettings.Environment == Environments.Live)
            {
                // When in production, don't show the exception to the user, but show a generic error message.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = $"<pre>{exception}</pre>",
                ContentType = MediaTypeNames.Text.Html
            };
        }
    }

    [Route("json.gcl")]
    [Route("json.jcl")]
    public async Task<IActionResult> JsonAsync()
    {
        var error = "";
        var startTime = DateTime.Now;
        var stopWatch = new Stopwatch();
        var logRenderingOfTemplate = false;
        var templateId = 0;
        var templateVersion = 0;

        try
        {
            var context = HttpContext;
            var templateName = HttpContextHelpers.GetRequestValue(context, "templatename");
            _ = Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out templateId);
            logger.LogDebug($"JsonAsync content from query template, templateName: '{templateName}', templateId: '{templateId}'.");

            if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
            {
                throw new ArgumentException("No template specified.");
            }

            var result = (QueryTemplate) await templatesService.GetTemplateAsync(templateId, templateName, TemplateTypes.Query);
            if (result.Id <= 0)
            {
                // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login, or that the templates requires a role the user doesn't have.
                if (result.LoginRequired)
                {
                    return Unauthorized();
                }

                return NotFound();
            }

            templateId = result.Id;
            templateVersion = result.Version;
            logRenderingOfTemplate = await templatesService.TemplateRenderingShouldBeLoggedAsync(templateId);
            if (logRenderingOfTemplate)
            {
                stopWatch.Start();
            }

            var jsonResult = await templatesService.GetJsonResponseFromQueryAsync(result);

            return Content(JsonConvert.SerializeObject(jsonResult), MediaTypeNames.Application.Json);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{Constants.TemplateRenderingError} '{templateId}'");
            error = exception.ToString();

            if (gclSettings.Environment == Environments.Live)
            {
                // When in production, don't show the exception to the user, but show a generic error message.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, exception);
        }
        finally
        {
            if (logRenderingOfTemplate)
            {
                stopWatch.Stop();
                var endTime = DateTime.Now;
                await templatesService.AddTemplateOrComponentRenderingLogAsync(0, templateId, templateVersion, startTime, endTime, stopWatch.ElapsedMilliseconds, error);
            }
        }
    }

    [Route("routine.gcl")]
    public async Task<IActionResult> RoutineAsync()
    {
        var error = "";
        var startTime = DateTime.Now;
        var stopWatch = new Stopwatch();
        var logRenderingOfTemplate = false;
        var templateId = 0;
        var templateVersion = 0;

        try
        {
            var context = HttpContext;
            var templateName = HttpContextHelpers.GetRequestValue(context, "templateName");
            _ = Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateId"), out templateId);
            logger.LogDebug($"JsonAsync content from query template, templateName: '{templateName}', templateId: '{templateId}'.");

            if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
            {
                throw new ArgumentException("No template specified.");
            }

            var result = (RoutineTemplate) await templatesService.GetTemplateAsync(templateId, templateName, TemplateTypes.Routine);
            if (result.Id <= 0)
            {
                // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login.
                if (result.LoginRequired)
                {
                    return Unauthorized();
                }

                return NotFound();
            }

            templateId = result.Id;
            templateVersion = result.Version;
            logRenderingOfTemplate = await templatesService.TemplateRenderingShouldBeLoggedAsync(templateId);
            if (logRenderingOfTemplate)
            {
                stopWatch.Start();
            }

            var jsonResult = await templatesService.GetJsonResponseFromRoutineAsync(result);

            return Content(JsonConvert.SerializeObject(jsonResult), MediaTypeNames.Application.Json);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{Constants.TemplateRenderingError} '{templateId}'");
            error = exception.ToString();

            if (gclSettings.Environment == Environments.Live)
            {
                // When in production, don't show the exception to the user, but show a generic error message.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return StatusCode(StatusCodes.Status500InternalServerError, exception);
        }
        finally
        {
            if (logRenderingOfTemplate)
            {
                stopWatch.Stop();
                var endTime = DateTime.Now;
                await templatesService.AddTemplateOrComponentRenderingLogAsync(0, templateId, templateVersion, startTime, endTime, stopWatch.ElapsedMilliseconds, error);
            }
        }
    }

    [Route("GclComponent.gcl")]
    [Route("component.gcl")]
    [Route("jclcomponent.jcl")]
    [HttpPost, HttpGet]
    public async Task<IActionResult> Component(string type, int? componentMode = null, string callMethod = null)
    {
        var componentId = 0;
        try
        {
            if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, $"__{type}"), out componentId) || componentId <= 0)
            {
                if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "componentId"), out componentId) || componentId <= 0)
                {
                    if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "contentId"), out componentId) || componentId <= 0)
                    {
                        return Content("<!-- No component ID found -->", MediaTypeNames.Text.Html);
                    }
                }
            }

            var result = await templatesService.GenerateDynamicContentHtmlAsync(componentId, componentMode, callMethod);
            var resultObject = result as (object Data, ViewDataDictionary ViewData)?;

            return result switch
            {
                null => Content("", MediaTypeNames.Text.Html),
                string resultString => Content(resultString, MediaTypeNames.Text.Html),
                _ => Content(JsonConvert.SerializeObject(!resultObject.HasValue ? result : resultObject.Value.Data), MediaTypeNames.Application.Json)
            };
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{Constants.DynamicComponentRenderingError} '{componentId}'");

            if (gclSettings.Environment == Environments.Live)
            {
                // When in production, don't show the exception to the user, but show a generic error message.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = exception.ToString(),
                ContentType = MediaTypeNames.Text.Html
            };
        }
    }

    [Route("partial.gcl")]
    [Route("partial.jcl")]
    public async Task<IActionResult> Partial()
    {
        var error = "";
        var startTime = DateTime.Now;
        var stopWatch = new Stopwatch();
        var logRenderingOfTemplate = false;
        var templateId = 0;
        var templateVersion = 0;

        try
        {
            var context = HttpContext;
            _ = Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateId"), out templateId);
            var partialTemplateName = HttpContextHelpers.GetRequestValue(context, "partialName");

            if (String.IsNullOrWhiteSpace(partialTemplateName) && templateId <= 0)
            {
                return NotFound();
            }

            // Get the template and replace the dynamic content.
            var template = await templatesService.GetTemplateAsync(templateId);

            // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login.
            if (template.Id <= 0 && template.LoginRequired)
            {
                return Unauthorized();
            }

            templateId = template.Id;
            templateVersion = template.Version;
            logRenderingOfTemplate = await templatesService.TemplateRenderingShouldBeLoggedAsync(templateId);
            if (logRenderingOfTemplate)
            {
                stopWatch.Start();
            }

            var templateContent = template.Content;
            templateContent = await templatesService.HandleIncludesAsync(templateContent, templateType: TemplateTypes.Html);
            templateContent = await templatesService.ReplaceAllDynamicContentAsync(templateContent);
            templateContent = await dataSelectorsService.ReplaceAllDataSelectorsAsync(templateContent);
            templateContent = await wiserItemsService.ReplaceAllEntityBlocksAsync(templateContent);

            // Parse the html to get the partial template part.
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(templateContent);
            var partialTemplateContent = htmlDocument.DocumentNode.SelectSingleNode($"//div[@data-type='partial-template'][@data-name='{partialTemplateName}']")?.InnerHtml;

            return String.IsNullOrWhiteSpace(partialTemplateContent)
                ? Content("The specified partial template can't be found on the current page", MediaTypeNames.Text.Html)
                : Content(partialTemplateContent, MediaTypeNames.Text.Html);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, $"{Constants.TemplateRenderingError} '{templateId}'");
            error = exception.ToString();

            if (gclSettings.Environment == Environments.Live)
            {
                // When in production, don't show the exception to the user, but show a generic error message.
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = exception.ToString(),
                ContentType = MediaTypeNames.Text.Html
            };
        }
        finally
        {
            if (logRenderingOfTemplate)
            {
                stopWatch.Stop();
                var endTime = DateTime.Now;
                await templatesService.AddTemplateOrComponentRenderingLogAsync(0, templateId, templateVersion, startTime, endTime, stopWatch.ElapsedMilliseconds, error);
            }
        }
    }

    [HttpGet, Route("template/{templateId:int}/")]
    public async Task<TemplateDataModel> TemplateData(int templateId)
    {
        return await templatesService.GetTemplateDataAsync(templateId);
    }
}