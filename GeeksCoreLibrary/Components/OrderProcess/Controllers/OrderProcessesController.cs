using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Helpers;
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

        public OrderProcessesController(ILogger<OrderProcessesController> logger,
            ITemplatesService templatesService,
            IPagesService pagesService,
            IActionContextAccessor actionContextAccessor,
            IHttpContextAccessor httpContextAccessor,
            ITempDataProvider tempDataProvider,
            IViewComponentHelper viewComponentHelper,
            IDataSelectorsService dataSelectorsService,
            IOrderProcessesService orderProcessesService)
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
        }

        [Route(Constants.CheckoutPage)]
        public async Task<IActionResult> OrderProcessAsync()
        {
            return await HandleControllerAction(OrderProcess.ComponentModes.Checkout);
        }

        [Route(Constants.PaymentOutPage)]
        public async Task<IActionResult> PaymentOutAsync()
        {
            return await HandleControllerAction(OrderProcess.ComponentModes.PaymentOut);
        }

        [Route(Constants.PaymentInPage)]
        public async Task<IActionResult> PaymentInAsync()
        {
            return await HandleControllerAction(OrderProcess.ComponentModes.PaymentIn);
        }

        private async Task<IActionResult> HandleControllerAction(OrderProcess.ComponentModes componentMode)
        {
            var context = HttpContext;
            var orderProcessIdString = HttpContextHelpers.GetRequestValue(context, Constants.OrderProcessIdRequestKey);

            UInt64.TryParse(orderProcessIdString, out var orderProcessId);
            logger.LogDebug($"GetAsync content from order process, orderProcessIdString: '{orderProcessIdString}'.");

            if (orderProcessId == 0)
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
            var externalJavascript = new List<string>();
            var externalCss = new List<string>();
            var ombouw = !String.Equals(HttpContextHelpers.GetRequestValue(context, "ombouw"), "false", StringComparison.OrdinalIgnoreCase);

            var contentToWrite = new StringBuilder();
            var url = (string)context.Items[Modules.Templates.Models.Constants.OriginalPathAndQueryStringKey];

            // Header template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalHeader(url, javascriptTemplates, cssTemplates));

                // Load (S)CSS file with the same name as the order process.
                if (!String.IsNullOrWhiteSpace(orderProcessSettings.Title))
                {
                    var cssTemplateId = await templatesService.GetTemplateIdFromNameAsync(orderProcessSettings.Title, TemplateTypes.Scss);
                    if (cssTemplateId > 0)
                    {
                        cssTemplates.Add(cssTemplateId);
                    }
                }
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
            var component = await viewComponentHelper.InvokeAsync("OrderProcess", new { dynamicContent, callMethod = "", forcedComponentMode = (int?)componentMode });
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

            if (!ombouw)
            {
                return Content(newBodyHtml, "text/html");
            }

            var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, newBodyHtml);

            return View("Template", viewModel);
        }
    }
}
