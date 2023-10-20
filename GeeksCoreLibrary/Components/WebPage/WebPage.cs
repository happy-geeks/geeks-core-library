using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.WebPage.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace GeeksCoreLibrary.Components.WebPage
{
    [ViewComponent(Name = "WebPage")]
    public class WebPage : CmsComponent<WebPageCmsSettingsModel, WebPage.ComponentModes>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IPagesService pagesService;
        private readonly IWebPagesService webPagesService;

        #region Enums

        public enum ComponentModes
        {
            Render = 1
        }

        #endregion

        #region Constructor

        public WebPage(ILogger<WebPage> logger,
            IStringReplacementsService stringReplacementsService,
            IDatabaseConnection databaseConnection,
            ITemplatesService templatesService,
            IAccountsService accountsService,
            IPagesService pagesService,
            IWebPagesService webPagesService,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.pagesService = pagesService;
            this.webPagesService = webPagesService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new WebPageCmsSettingsModel();
        }

        #endregion

        #region Rendering

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            ExtraDataForReplacements = extraData;
            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
            else if (!String.IsNullOrWhiteSpace(dynamicContent.ComponentMode))
            {
                Settings.ComponentMode = Enum.Parse<ComponentModes>(dynamicContent.ComponentMode);
            }

            HandleDefaultSettingsFromComponentMode();

            // Check if we should actually render this component for the current user.
            var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
            if (!renderHtml)
            {
                ViewBag.Html = debugInformation;
                return new HtmlString(debugInformation);
            }

            // Check if we need to call a specific method and then do so. Skip everything else, because we don't want to render the entire component then.
            if (!String.IsNullOrWhiteSpace(callMethod))
            {
                TempData["InvokeMethodResult"] = await InvokeMethodAsync(callMethod);
                return new HtmlString("");
            }

            if (Settings.PageId == 0 && String.IsNullOrWhiteSpace(Settings.PageName))
            {
                throw new Exception("No page ID or page name set. Web page rendering cannot continue.");
            }

            var resultHtml = new StringBuilder();

            switch (Settings.ComponentMode)
            {
                case ComponentModes.Render:
                    {
                        var html = await HandleRenderModeAsync();
                        resultHtml.Append(html);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Settings.ComponentMode), Settings.ComponentMode.ToString("G"), $"Unknown or unsupported component mode '{Settings.ComponentMode}' in 'GenerateHtmlAsync'.");
            }

            return new HtmlString(resultHtml.ToString());
        }

        #endregion

        #region Handling different component modes

        public async Task<string> HandleRenderModeAsync()
        {
            var getWebPageResult = await webPagesService.GetWebPageResultAsync(Settings, ExtraDataForReplacements);
            if (getWebPageResult.Rows.Count == 0)
            {
                if (Settings.ReturnNotFoundStatusCodeOnNoData)
                {
                    HttpContextHelpers.Return404(httpContextAccessor?.HttpContext);
                }
                else
                {
                    return "";
                }
            }

            var html = getWebPageResult.Rows[0].Field<string>("html");
            html = await TemplatesService.DoReplacesAsync(html, handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);

            if (!Settings.SetSeoInfo)
            {
                return html;
            }

            // Add SEO data.
            var seoTitle = getWebPageResult.Rows[0].GetValueIfColumnExists<string>("title");
            var seoDescription = getWebPageResult.Rows[0].GetValueIfColumnExists<string>("metadescription");
            var seoKeyWords = getWebPageResult.Rows[0].GetValueIfColumnExists<string>("keywords");
            var seoCanonical = getWebPageResult.Rows[0].GetValueIfColumnExists<string>("canonicalurl");
            var noIndex = Convert.ToBoolean(getWebPageResult.Rows[0].GetValueIfColumnExists("noindex"));
            var noFollow = Convert.ToBoolean(getWebPageResult.Rows[0].GetValueIfColumnExists<string>("nofollow"));
            var robots = getWebPageResult.Rows[0].GetValueIfColumnExists<string>("robots");
            pagesService.SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));

            return html;
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<WebPageCmsSettingsModel>(settingsJson);
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }

            HandleDefaultSettingsFromComponentMode();
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        }

        #endregion
    }
}