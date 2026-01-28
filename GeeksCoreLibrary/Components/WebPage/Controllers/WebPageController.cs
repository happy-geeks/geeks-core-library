using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
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
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Components.WebPage.Controllers;

[Area("Templates")]
public class WebPageController(
    ILogger<WebPageController> logger,
    ITemplatesService templatesService,
    IPagesService pagesService,
    IDataSelectorsService dataSelectorsService,
    IWiserItemsService wiserItemsService,
    IHttpContextAccessor httpContextAccessor = null,
    ITempDataProvider tempDataProvider = null,
    IViewComponentHelper viewComponentHelper = null)
    : Controller
{
    [Route("webpage.gcl")]
    [Route("cmspage.jcl")]
    public async Task<IActionResult> WebPageAsync()
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            throw new Exception("No httpContext found. Did you add the dependency in Program.cs or Startup.cs?");
        }

        var httpContext = httpContextAccessor.HttpContext;
        string cmsPagePath;
        string webPageIdString;
        var isErrorPage = false;
        if (httpContext.Request.Query.TryGetValue("errorCode", out var errorCodeString))
        {
            cmsPagePath = $"error_{errorCodeString}";
            webPageIdString = String.Empty;
            isErrorPage = true;
        }
        else
        {
            cmsPagePath = HttpContextHelpers.GetRequestValue(httpContext, "gclcmspagepath");
            webPageIdString = HttpContextHelpers.GetRequestValue(httpContext, "id");
            if (String.IsNullOrWhiteSpace(cmsPagePath))
            {
                cmsPagePath = HttpContextHelpers.GetRequestValue(httpContext, "jclcmspagepath");
            }

            if (String.IsNullOrWhiteSpace(webPageIdString))
            {
                webPageIdString = HttpContextHelpers.GetRequestValue(httpContext, "jclcmspageid");
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
        var externalJavascript = new List<PageResourceModel>();
        var externalCss = new List<PageResourceModel>();
        var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(httpContext, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);

        var contentToWrite = new StringBuilder();
        var url = (string) httpContext.Items[Constants.OriginalPathAndQueryStringKey];

        // Header template.
        if (ombouw)
        {
            contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates));
        }

        // In endpoint routing, the action info lives on the matched endpoint metadata.
        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor =
            endpoint?.Metadata.GetMetadata<ActionDescriptor>()
            ?? throw new Exception("No ActionDescriptor found on the current endpoint. Are you executing inside an MVC endpoint?");

        // Build RouteData (helps certain MVC/view features that expect it)
        var routeData = httpContext.GetRouteData() ?? new RouteData();
        foreach (var kvp in httpContext.Request.RouteValues)
        {
            routeData.Values[kvp.Key] = kvp.Value;
        }

        // Build ActionContext without IActionContextAccessor
        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

        // Create a ViewContext
        var viewContext = new ViewContext(
            actionContext,
            NullView.Instance,
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            new TempDataDictionary(httpContext, tempDataProvider),
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
        var component = await viewComponentHelper.InvokeAsync("WebPage", new {dynamicContent, callMethod = "", forcedComponentMode = (int?) WebPage.ComponentModes.Render});
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
            return Content(newBodyHtml, MediaTypeNames.Text.Html);
        }

        var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml);

        return View("Template", viewModel);
    }
}