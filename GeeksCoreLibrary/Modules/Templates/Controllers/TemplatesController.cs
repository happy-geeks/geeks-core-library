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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
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

        public TemplatesController(ILogger<TemplatesController> logger, ITemplatesService templatesService, IPagesService pagesService, IDataSelectorsService dataSelectorsService, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.pagesService = pagesService;
            this.dataSelectorsService = dataSelectorsService;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
        }

        [Route("template.gcl")]
        [Route("template.jcl")]
        public async Task<IActionResult> Template()
        {
            var context = HttpContext;
            var templateName = HttpContextHelpers.GetRequestValue(context, "templatename");
            Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out var templateId);
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

            var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);
            switch (contentTemplate.Type)
            {
                case TemplateTypes.Js:
                    return Content(contentTemplate.Content, "application/javascript");
                case TemplateTypes.Scss:
                case TemplateTypes.Css:
                    return Content(contentTemplate.Content, "text/css");
                case TemplateTypes.Query:
                    var jsonResult = await templatesService.GetJsonResponseFromQueryAsync((QueryTemplate)contentTemplate);
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
                        var dataRow = (DataRow)HttpContext.Items[Constants.TemplatePreLoadQueryResultKey];
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
            var url = (string)context.Items[Constants.OriginalPathAndQueryStringKey];

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

            if (!ombouw)
            {
                return Content(newBodyHtml, "text/html");
            }

            var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml);

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

        [Route("json.gcl")]
        [Route("json.jcl")]
        public async Task<IActionResult> JsonAsync()
        {
            var context = HttpContext;
            var templateName = HttpContextHelpers.GetRequestValue(context, "templatename");
            Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateid"), out var templateId);
            logger.LogDebug($"JsonAsync content from query template, templateName: '{templateName}', templateId: '{templateId}'.");

            if (String.IsNullOrWhiteSpace(templateName) && templateId <= 0)
            {
                throw new ArgumentException("No template specified.");
            }

            var result = (QueryTemplate)await templatesService.GetTemplateAsync(templateId, templateName, TemplateTypes.Query);
            if (result.Id <= 0)
            {
                // If ID is 0 and LoginRequired is true, it means no user is logged in while the template requires a login.
                if (result.LoginRequired)
                {
                    return Unauthorized();
                }

                return NotFound();
            }

            var jsonResult = await templatesService.GetJsonResponseFromQueryAsync(result);

            return Content(JsonConvert.SerializeObject(jsonResult), "application/json");
        }

        [Route("GclComponent.gcl")]
        [Route("component.gcl")]
        [Route("jclcomponent.jcl")]
        [HttpPost, HttpGet]
        public async Task<IActionResult> Component(string type, int? componentMode = null, string callMethod = null)
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

        [Route("partial.gcl")]
        [Route("partial.jcl")]
        public async Task<IActionResult> Partial()
        {
            var context = HttpContext;
            Int32.TryParse(HttpContextHelpers.GetRequestValue(context, "templateId"), out var templateId);
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

        [HttpGet, Route("template/{templateId:int}/")]
        public async Task<TemplateDataModel> TemplateData(int templateId)
        {
            return await this.templatesService.GetTemplateDataAsync(templateId);
        }
    }
}
