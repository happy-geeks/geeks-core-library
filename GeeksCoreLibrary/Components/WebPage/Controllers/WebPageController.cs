using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.WebPage.Controllers
{
    [Area("Templates")]
    public class WebPageController : Controller
    {
        private readonly ILogger<WebPageController> logger;
        private readonly ITemplatesService templatesService;
        private readonly IPagesService pagesService;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IViewComponentHelper viewComponentHelper;
        private readonly IDataSelectorsService dataSelectorsService;
        private readonly IWiserItemsService wiserItemsService;

        public WebPageController(ILogger<WebPageController> logger,
                                 ITemplatesService templatesService,
                                 IPagesService pagesService,
                                 IDataSelectorsService dataSelectorsService,
                                 IWiserItemsService wiserItemsService,
                                 IActionContextAccessor actionContextAccessor = null,
                                 IHttpContextAccessor httpContextAccessor = null,
                                 ITempDataProvider tempDataProvider = null,
                                 IViewComponentHelper viewComponentHelper = null)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.pagesService = pagesService;
            this.actionContextAccessor = actionContextAccessor;
            this.httpContextAccessor = httpContextAccessor;
            this.tempDataProvider = tempDataProvider;
            this.viewComponentHelper = viewComponentHelper;
            this.dataSelectorsService = dataSelectorsService;
            this.wiserItemsService = wiserItemsService;
        }

        [Route("webpage.gcl")]
        [Route("cmspage.jcl")]
        public async Task<IActionResult> WebPageAsync()
        {
            if (httpContextAccessor?.HttpContext == null || actionContextAccessor?.ActionContext == null)
            {
                throw new Exception("No httpContext found. Did you add the dependency in Program.cs or Startup.cs?");
            }

            var context = HttpContext;
            string cmsPagePath;
            string webPageIdString;
            bool isErrorPage = false;
            if (context.Request.Query.TryGetValue("errorCode", out var errorCodeString))
            {
                cmsPagePath = $"error_{errorCodeString}";
                webPageIdString = String.Empty;
                isErrorPage = true;
            }
            else
            {
                cmsPagePath = HttpContextHelpers.GetRequestValue(context, "gclcmspagepath");
                webPageIdString = HttpContextHelpers.GetRequestValue(context, "id");
                if (String.IsNullOrWhiteSpace(cmsPagePath))
                {
                    cmsPagePath = HttpContextHelpers.GetRequestValue(context, "jclcmspagepath");
                }

                if (String.IsNullOrWhiteSpace(webPageIdString))
                {
                    webPageIdString = HttpContextHelpers.GetRequestValue(context, "jclcmspageid");
                }
            }

            UInt64.TryParse(webPageIdString, out var webPageId);
            logger.LogDebug($"GetAsync content from web page, cmsPagePath: '{cmsPagePath}', cmsPageIdString: '{webPageIdString}'.");

            if (String.IsNullOrWhiteSpace(cmsPagePath) && webPageId == 0)
            {
                return NotFound();
            }

            var javascriptTemplates = new List<int>();
            var cssTemplates = new List<int>();
            var externalJavascript = new List<PageResource>();
            var externalCss = new List<PageResource>();
            var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);

            var contentToWrite = new StringBuilder();
            var url = (string)context.Items[Constants.OriginalPathAndQueryStringKey];

            // Header template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates));
            }

            // Create a fake ViewContext (but with a real ActionContext and a real HttpContext).
            var viewContext = new ViewContext(
                actionContextAccessor.ActionContext,
                NullView.Instance,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                new TempDataDictionary(httpContextAccessor.HttpContext, tempDataProvider),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Set the context in the ViewComponentHelper, so that the ViewComponents that we use actually have the proper context.
            (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);

            // Dynamically invoke the correct ViewComponent.
            var dynamicContent = new DynamicContent();
            var webPageSettings = new WebPageCmsSettingsModel
            {
                HandleRequest = false,
                ComponentMode = WebPage.ComponentModes.Render,
                EvaluateIfElseInTemplates = true,
                UserNeedsToBeLoggedIn = false
            };

            if (!isErrorPage)
            {
                webPageSettings.SetSeoInfo = true;
            }

            if (webPageId > 0)
            {
                webPageSettings.PageId = webPageId;
            }
            else if (!String.IsNullOrWhiteSpace(cmsPagePath))
            {
                var lastSeparatorIndex = cmsPagePath.LastIndexOf('/');
                string pageName;
                if (lastSeparatorIndex >= 0)
                {
                    pageName = cmsPagePath[(lastSeparatorIndex + 1)..];
                    cmsPagePath = cmsPagePath[..lastSeparatorIndex];
                }
                else
                {
                    pageName = cmsPagePath;
                    cmsPagePath = String.Empty;
                }

                webPageSettings.PathMustContainName = cmsPagePath;
                webPageSettings.PageName = pageName;
            }

            dynamicContent.SettingsJson = JsonConvert.SerializeObject(webPageSettings);
            var component = await viewComponentHelper.InvokeAsync("WebPage", new { dynamicContent, callMethod = "", forcedComponentMode = (int?)WebPage.ComponentModes.Render });
            await using (var stringWriter = new StringWriter())
            {
                component.WriteTo(stringWriter, HtmlEncoder.Default);
                var html = stringWriter.ToString();

                // Content template.
                contentToWrite.Append(html);
            }

            // Footer template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalFooter(url, javascriptTemplates, cssTemplates));
            }

            var newBodyHtml = await templatesService.DoReplacesAsync(contentToWrite.ToString());
            newBodyHtml = await dataSelectorsService.ReplaceAllDataSelectorsAsync(newBodyHtml);
            newBodyHtml = await wiserItemsService.ReplaceAllEntityBlocksAsync(newBodyHtml);

            if (!ombouw)
            {
                return Content(newBodyHtml, "text/html");
            }

            var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml);

            return View("Template", viewModel);
        }
    }
}