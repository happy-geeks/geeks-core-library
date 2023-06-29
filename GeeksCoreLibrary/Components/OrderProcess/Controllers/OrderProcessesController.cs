using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
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
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess.Controllers
{
    [Area("Templates")]
    public class OrderProcessesController : Controller
    {
        private readonly ILogger<OrderProcessesController> logger;
        private readonly ITemplatesService templatesService;
        private readonly IPagesService pagesService;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IViewComponentHelper viewComponentHelper;
        private readonly IDataSelectorsService dataSelectorsService;
        private readonly IOrderProcessesService orderProcessesService;
        private readonly IWiserItemsService wiserItemsService;

        public OrderProcessesController(ILogger<OrderProcessesController> logger,
                                        ITemplatesService templatesService,
                                        IPagesService pagesService,
                                        IDataSelectorsService dataSelectorsService,
                                        IOrderProcessesService orderProcessesService,
                                        IWiserItemsService wiserItemsService,
                                        IActionContextAccessor actionContextAccessor = null,
                                        IHttpContextAccessor httpContextAccessor = null,
                                        IViewComponentHelper viewComponentHelper = null,
                                        ITempDataProvider tempDataProvider = null)
        {
            this.logger = logger;
            this.templatesService = templatesService;
            this.pagesService = pagesService;
            this.actionContextAccessor = actionContextAccessor;
            this.httpContextAccessor = httpContextAccessor;
            this.tempDataProvider = tempDataProvider;
            this.viewComponentHelper = viewComponentHelper;
            this.dataSelectorsService = dataSelectorsService;
            this.orderProcessesService = orderProcessesService;
            this.wiserItemsService = wiserItemsService;
        }

        [Route(Constants.CheckoutPage)]
        public async Task<IActionResult> OrderProcessAsync()
        {
            return await HandleControllerAction(OrderProcess.ComponentModes.Checkout);
        }

        [Route(Constants.PaymentOutPage)]
        public async Task<IActionResult> PaymentOutAsync()
        {
            var context = HttpContext;
            var orderProcessIdString = HttpContextHelpers.GetRequestValue(context, Constants.OrderProcessIdRequestKey);

            if (!UInt64.TryParse(orderProcessIdString, out var orderProcessId) || orderProcessId == 0)
            {
                return NotFound();
            }

            var html = await RenderAndExecuteComponentAsync(OrderProcess.ComponentModes.PaymentOut, orderProcessId);
            return Content(html, "text/html");
        }

        [Route(Constants.PaymentInPage)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentInAsync()
        {
            var context = HttpContext;
            var orderProcessIdString = HttpContextHelpers.GetRequestValue(context, Constants.OrderProcessIdRequestKey);

            if (!UInt64.TryParse(orderProcessIdString, out var orderProcessId) || orderProcessId == 0)
            {
                return NotFound();
            }

            var html = await RenderAndExecuteComponentAsync(OrderProcess.ComponentModes.PaymentIn, orderProcessId);
            return Content(html, "text/html");
        }

        [Route(Constants.PaymentReturnPage)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentReturnAsync()
        {
            var context = HttpContext;
            var orderProcessIdString = HttpContextHelpers.GetRequestValue(context, Constants.OrderProcessIdRequestKey);

            if (!UInt64.TryParse(orderProcessIdString, out var orderProcessId) || orderProcessId == 0)
            {
                return NotFound();
            }

            var html = await RenderAndExecuteComponentAsync(OrderProcess.ComponentModes.PaymentReturn, orderProcessId);
            return Content(html, "text/html");
        }

        [Route(Constants.DownloadInvoicePage + "{orderId:int}")]
        public async Task<IActionResult> DownloadInvoiceAsync(ulong orderId)
        {
            var invoicePdf = await orderProcessesService.GetInvoicePdfAsync(orderId);
            if (invoicePdf == null)
            {
                return NotFound();
            }

            return File(invoicePdf.Content, invoicePdf.ContentType, invoicePdf.FileName);
        }

        private async Task<IActionResult> HandleControllerAction(OrderProcess.ComponentModes componentMode)
        {
            var context = HttpContext;
            var orderProcessIdString = HttpContextHelpers.GetRequestValue(context, Constants.OrderProcessIdRequestKey);

            if (!UInt64.TryParse(orderProcessIdString, out var orderProcessId) || orderProcessId == 0)
            {
                return NotFound();
            }

            var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
            if (orderProcessSettings == null || orderProcessSettings.Id == 0)
            {
                return NotFound();
            }

            var javascriptTemplates = new List<int>();
            var cssTemplates = new List<int>();
            var externalJavascript = new List<PageResource>();
            var externalCss = new List<PageResource>();
            var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);

            var contentToWrite = new StringBuilder();
            var url = (string)context.Items[Modules.Templates.Models.Constants.OriginalPathAndQueryStringKey];

            // Header template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates));

                // Load (S)CSS and/or JavaScript files with the same name as the order process.
                if (!String.IsNullOrWhiteSpace(orderProcessSettings.Title))
                {
                    var cssTemplateId = await templatesService.GetTemplateIdFromNameAsync(orderProcessSettings.Title, TemplateTypes.Scss);
                    if (cssTemplateId > 0)
                    {
                        cssTemplates.Add(cssTemplateId);
                    }

                    var javascriptTemplateId = await templatesService.GetTemplateIdFromNameAsync(orderProcessSettings.Title, TemplateTypes.Js);
                    if (javascriptTemplateId > 0)
                    {
                        javascriptTemplates.Add(javascriptTemplateId);
                    }
                }
            }

            // Content template.
            var html = await RenderAndExecuteComponentAsync(componentMode, orderProcessId);
            contentToWrite.Append(html);

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

            // We never want the checkout process to be indexed.
            viewModel.MetaData.MetaTags["robots"] = "noindex,nofollow";

            return View("Template", viewModel);
        }

        private async Task<string> RenderAndExecuteComponentAsync(OrderProcess.ComponentModes componentMode, ulong orderProcessId)
        {
            if (httpContextAccessor?.HttpContext == null || actionContextAccessor?.ActionContext == null)
            {
                throw new Exception("No httpContext found. Did you add the dependency in Program.cs or Startup.cs?");
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
            var orderProcessCmsSettings = new OrderProcessCmsSettingsModel
            {
                HandleRequest = true,
                ComponentMode = componentMode,
                EvaluateIfElseInTemplates = true,
                UserNeedsToBeLoggedIn = false,
                OrderProcessId = orderProcessId
            };

            var dynamicContent = new DynamicContent
            {
                Id = 1,
                SettingsJson = JsonConvert.SerializeObject(orderProcessCmsSettings)
            };
            var htmlContent = await viewComponentHelper.InvokeAsync("OrderProcess", new { dynamicContent, callMethod = "", forcedComponentMode = (int?)componentMode });

            await using var stringWriter = new StringWriter();
            htmlContent.WriteTo(stringWriter, HtmlEncoder.Default);
            return stringWriter.ToString();
        }
    }
}