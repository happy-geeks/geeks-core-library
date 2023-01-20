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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GeeksCoreLibrary.Modules.Templates.Controllers
{
    [Area("Templates")]
    public class TemplatesController : Controller
    {
        private readonly ILogger<TemplatesController> logger;
        private readonly ITemplatesService templatesService;
        private readonly IPagesService pagesService;
        private readonly IDataSelectorsService dataSelectorsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IObjectsService objectsService;

        public TemplatesController(ILogger<TemplatesController> logger, ITemplatesService templatesService, IPagesService pagesService, IDataSelectorsService dataSelectorsService, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService, IObjectsService objectsService)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.pagesService = pagesService;
            this.dataSelectorsService = dataSelectorsService;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.objectsService = objectsService;
        }

        [Route("template.gcl")]
        [Route("template.jcl")]
        public async Task<IActionResult> Template()
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
                Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out templateId);
                logger.LogDebug($"GetAsync content from HTML template, templateName: '{templateName}', templateId: '{templateId}'.");

                if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
                {
                    return NotFound();
                }

                var javascriptTemplates = new List<int>();
                var cssTemplates = new List<int>();
                var externalJavascript = new List<string>();
                var externalCss = new List<string>();
                var contentTemplate = await templatesService.GetTemplateAsync(templateId, templateName);

                templateId = contentTemplate.Id;
                templateVersion = contentTemplate.Version;
                logRenderingOfTemplate = await templatesService.TemplateRenderingShouldBeLoggedAsync(templateId);
                if (logRenderingOfTemplate)
                {
                    stopWatch.Start();
                }

                javascriptTemplates.AddRange(contentTemplate.JavascriptTemplates);
                cssTemplates.AddRange(contentTemplate.CssTemplates);

                if (contentTemplate.Id <= 0)
                {
                    // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login.
                    if (!contentTemplate.LoginRequired)
                    {
                        // Login not required; return 404 (Not Found).
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

                var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase) && !contentTemplate.IsPartial;
                switch (contentTemplate.Type)
                {
                    case TemplateTypes.Js:
                        return Content(contentTemplate.Content, "application/javascript");
                    case TemplateTypes.Scss:
                    case TemplateTypes.Css:
                        return Content(contentTemplate.Content, "text/css");
                    case TemplateTypes.Query:
                        var jsonResult = await templatesService.GetJsonResponseFromQueryAsync((QueryTemplate) contentTemplate);
                        return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
                    case TemplateTypes.Normal:
                    case TemplateTypes.Unknown:
                        return Content(contentTemplate.Content, "text/plain");
                    case TemplateTypes.Html:
                        // Execute the pre load query before any replacements are being done and before any dynamic components are handled.
                        var hasResults = await templatesService.ExecutePreLoadQueryAndRememberResultsAsync(contentTemplate);
                        if (contentTemplate.ReturnNotFoundWhenPreLoadQueryHasNoData && !hasResults)
                        {
                            return NotFound();
                        }

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
                                var noIndex = Convert.ToBoolean(dataRow.GetValueIfColumnExists("noindex"));
                                var noFollow = Convert.ToBoolean(dataRow.GetValueIfColumnExists("nofollow"));
                                var robots = dataRow.GetValueIfColumnExists<string>("SEOrobots");
                                pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));

                                // Add Open Graph data.
                                var openGraphValues = dataRow.Table.Columns.Cast<DataColumn>().Where(c => c.ColumnName.StartsWith("opengraph_", StringComparison.OrdinalIgnoreCase)).ToDictionary(c => c.ColumnName, c => Convert.ToString(dataRow[c]));
                                pagesService.SetOpenGraphData(openGraphValues);
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(contentTemplate.Type), contentTemplate.Type.ToString());
                }

                var contentToWrite = new StringBuilder();
                var url = (string) context.Items[Constants.OriginalPathAndQueryStringKey];

                // Header template.
                if (ombouw)
                {
                    contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates));
                }

                // Content template.
                contentToWrite.Append(contentTemplate.Content);

                // Footer template.
                if (ombouw)
                {
                    contentToWrite.Append(await pagesService.GetGlobalFooter(url, javascriptTemplates, cssTemplates));
                }

                var newBodyHtml = await templatesService.DoReplacesAsync(contentToWrite.ToString(), templateType: contentTemplate.Type);
                newBodyHtml = await dataSelectorsService.ReplaceAllDataSelectorsAsync(newBodyHtml);
                newBodyHtml = await wiserItemsService.ReplaceAllEntityBlocksAsync(newBodyHtml);

                // Make relative image URls absolute to allow the template to show images when the HTML is placed inside another website.
                var useAbsoluteImageUrls = String.Equals(HttpContextHelpers.GetRequestValue(context, "absoluteImageUrls"), "true", StringComparison.OrdinalIgnoreCase);
                if (useAbsoluteImageUrls)
                {
                    var imagesDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
                    newBodyHtml = await wiserItemsService.ReplaceRelativeImagesToAbsoluteAsync(newBodyHtml, imagesDomain);
                }

                // Remove the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website.
                // To use this functionality the content of the SVG needs to be placed in the HTML, xlink can only load URLs from same domain, protocol and port.
                var removeSvgUrlsFromIcons = String.Equals(HttpContextHelpers.GetRequestValue(context, "removeSvgUrlsFromIcons"), "true", StringComparison.OrdinalIgnoreCase);
                if (removeSvgUrlsFromIcons)
                {
                    var regex = new Regex(@"<svg(?:[^>]*)>(?:\s*)<use(?:[^>]*)xlink:href=""([^>""]*)#(?:[^>""]*)""(?:[^>]*)>");
                    foreach (Match match in regex.Matches(newBodyHtml))
                    {
                        newBodyHtml = newBodyHtml.Replace(match.Groups[1].Value, "");
                    }
                }

                if (!ombouw)
                {
                    return Content(newBodyHtml, "text/html");
                }

                var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml, templateId);

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

                return View(viewModel);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Content = $"<pre>{exception}</pre>",
                    ContentType = "text/html"
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
                Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out templateId);
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

                return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
            }
            catch (Exception exception)
            {
                error = exception.ToString();
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
                Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateId"), out templateId);
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

                return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
            }
            catch (Exception exception)
            {
                error = exception.ToString();
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
            try
            {
                if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, $"__{type}"), out var componentId) || componentId <= 0)
                {
                    if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "componentId"), out componentId) || componentId <= 0)
                    {
                        if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "contentId"), out componentId) || componentId <= 0)
                        {
                            return Content("<!-- No component ID found -->", "text/html");
                        }
                    }
                }

                var result = await templatesService.GenerateDynamicContentHtmlAsync(componentId, componentMode, callMethod);
                var resultObject = result as (object Data, ViewDataDictionary ViewData)?;

                return result switch
                {
                    null => Content("", "text/html"),
                    string resultString => Content(resultString, "text/html"),
                    _ => Content(JsonConvert.SerializeObject(!resultObject.HasValue ? result : resultObject.Value.Data), "application/json")
                };
            }
            catch (Exception exception)
            {
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Content = exception.ToString(),
                    ContentType = "text/html"
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
                Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateId"), out templateId);
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
                    ? Content("The specified partial template can't be found on the current page", "text/html")
                    : Content(partialTemplateContent, "text/html");
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                return new ContentResult
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Content = exception.ToString(),
                    ContentType = "text/html"
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
            return await this.templatesService.GetTemplateDataAsync(templateId);
        }
    }
}
