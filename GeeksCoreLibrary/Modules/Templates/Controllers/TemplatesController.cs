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
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;

namespace GeeksCoreLibrary.Modules.Templates.Controllers
{
    [Area("Templates")]
    public class TemplatesController : Controller
    {
        private readonly ILogger<TemplatesController> logger;
        private readonly ITemplatesService templatesService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IPagesService pagesService;
        private readonly IDataSelectorsService dataSelectorsService;

        public TemplatesController(ILogger<TemplatesController> logger, ITemplatesService templatesService, IDatabaseConnection databaseConnection, IPagesService pagesService, IDataSelectorsService dataSelectorsService)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.databaseConnection = databaseConnection;
            this.pagesService = pagesService;
            this.dataSelectorsService = dataSelectorsService;
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
                return NotFound();
            }

            var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);
            switch (contentTemplate.Type)
            {
                case TemplateTypes.Js:
                    context.Response.ContentType = "application/javascript";
                    ombouw = false;
                    break;
                case TemplateTypes.Scss:
                case TemplateTypes.Css:
                    context.Response.ContentType = "text/css";
                    ombouw = false;
                    break;
                case TemplateTypes.Html:
                    // Execute the pre load query before any replacements are being done and before any dynamic components are handled.
                    await templatesService.ExecutePreLoadQueryAndRememberResultsAsync(contentTemplate);

                    // Set SEO information.
                    if (HttpContext.Items.ContainsKey(Constants.TemplatePreLoadQueryResultKey))
                    {
                        var dataRow = (DataRow)HttpContext.Items[Constants.TemplatePreLoadQueryResultKey];
                        var seoTitle = dataRow.GetValueIfColumnExists<string>("SEOtitle");
                        var seoDescription = dataRow.GetValueIfColumnExists<string>("SEOdescription");
                        var seoKeyWords = dataRow.GetValueIfColumnExists<string>("SEOkeywords");
                        var seoCanonical = dataRow.GetValueIfColumnExists<string>("SEOcanonical");
                        var noIndex = Convert.ToBoolean(dataRow.GetValueIfColumnExists("noindex"));
                        var noFollow = Convert.ToBoolean(dataRow.GetValueIfColumnExists("nofollow"));
                        var robots = dataRow.GetValueIfColumnExists<string>("SEOrobots");
                        pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));
                    }

                    break;
                case TemplateTypes.Query:
                    context.Response.ContentType = "application/json";
                    ombouw = false;
                    break;
                case TemplateTypes.Normal:
                    break;
                case TemplateTypes.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

            var newBodyHtml = await templatesService.DoReplacesAsync(contentToWrite.ToString());
            newBodyHtml = await dataSelectorsService.ReplaceAllDataSelectorsAsync(newBodyHtml);

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

            return result switch
            {
                null => Content("", "text/html"),
                string resultString => Content(resultString, "text/html"),
                _ => Content(JsonConvert.SerializeObject(result), "application/json")
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
            var template = (await templatesService.GetTemplateAsync(templateId)).Content;
            template = await templatesService.HandleIncludesAsync(template);
            template = await templatesService.ReplaceAllDynamicContentAsync(template);
            template = await dataSelectorsService.ReplaceAllDataSelectorsAsync(template);

            // Parse the html to get the partial template part.
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(template);
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
