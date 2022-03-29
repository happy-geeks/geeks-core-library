using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
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
        private readonly GclSettings gclSettings;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;

        #region Enums

        public enum ComponentModes
        {
            Render = 1
        }

        #endregion

        #region Constructor

        public WebPage(IOptions<GclSettings> gclSettings, ILogger<WebPage> logger, IStringReplacementsService stringReplacementsService, ILanguagesService languagesService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IHttpContextAccessor httpContextAccessor)
        {
            this.gclSettings = gclSettings.Value;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;

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
                    throw new NotImplementedException($"Unknown or unsupported component mode '{Settings.ComponentMode}' in 'GenerateHtmlAsync'.");
            }

            return new HtmlString(resultHtml.ToString());
        }

        #endregion

        #region Handling different component modes

        public async Task<string> HandleRenderModeAsync()
        {
            DatabaseConnection.ClearParameters();
            DatabaseConnection.AddParameter("pageName", Settings.PageName);
            DatabaseConnection.AddParameter("pageItemId", Settings.PageId);
            DatabaseConnection.AddParameter("path", Settings.PathMustContainName);
            DatabaseConnection.AddParameter("languageCode", languagesService?.CurrentLanguageCode ?? "");
            DatabaseConnection.AddParameter("environment", ConvertEnvironmentToInt());

            var getWebPageResult = await DatabaseConnection.GetAsync(GetWebPageQuery());
            if (getWebPageResult.Rows.Count == 0)
            {
                if (Settings.ReturnNotFoundStatusCodeOnNoData)
                {
                    HttpContextHelpers.Return404(httpContextAccessor.HttpContext);
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
            SetPageSeoData(seoTitle, seoDescription, seoKeyWords, seoCanonical, noIndex, noFollow, robots?.Split(",", StringSplitOptions.RemoveEmptyEntries));

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

        private string GetWebPageQuery()
        {
            var query = new StringBuilder();

            var languageCode = languagesService?.CurrentLanguageCode ?? "";

            // SELECT part.
            query.AppendLine("SELECT webPage.id, webPage.title AS `name`, CONCAT_WS('', webPageHtml.`value`, webPageHtml.long_value) AS `html`, webPageTitle.`value` AS `title`, CONCAT_WS('', webPageDescription.`value`, webPageDescription.long_value) AS `metadescription`");

            // FROM part.
            query.AppendLine($"FROM `{WiserTableNames.WiserItem}` AS webPage");

            // Web page SEO name.
            query.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageSeoName ON webPageSeoName.item_id = webPage.id AND webPageSeoName.`key` = '{Core.Models.CoreConstants.SeoTitlePropertyName}'");

            // Web page HTML.
            query.Append($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageHtml ON webPageHtml.item_id = webPage.id AND webPageHtml.`key` = 'html'");
            if (languageCode != "") query.Append(" AND webPageHtml.language_code = ?languageCode");
            query.AppendLine();

            // Web page title.
            query.Append($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageTitle ON webPageTitle.item_id = webPage.id AND webPageTitle.`key` = 'title'");
            if (languageCode != "") query.Append(" AND webPageTitle.language_code = ?languageCode");
            query.AppendLine();

            // Web page description.
            query.Append($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS webPageDescription ON webPageDescription.item_id = webPage.id AND webPageDescription.`key` = 'description'");
            if (languageCode != "") query.Append(" AND webPageDescription.language_code = ?languageCode");
            query.AppendLine();

            var pathMustContain = Settings.PathMustContainName;
            if (!String.IsNullOrWhiteSpace(pathMustContain) && Settings.SearchNumberOfLevels > 0)
            {
                if (Settings.HandleRequest)
                {
                    pathMustContain = StringReplacementsService.DoHttpRequestReplacements(pathMustContain);
                }

                for (var i = 1; i <= Settings.SearchNumberOfLevels; i++)
                {
                    var itemLinkAlias = $"searchUpLink{i}";
                    var itemAlias = $"searchUpItem{i}";
                    var titleAlias = $"item{i}Title";
                    var seoTitleAlias = $"item{i}SeoName";
                    var previousLink = i == 1 ? "webPage.id" : $"searchUpLink{i - 1}.destination_item_id";

                    query.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItemLink}` AS `{itemLinkAlias}` ON `{itemLinkAlias}`.item_id = {previousLink}");
                    query.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItem}` AS `{itemAlias}` ON `{itemAlias}`.id = `{itemLinkAlias}`.destination_item_id");
                    query.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS `{titleAlias}` ON `{titleAlias}`.item_id = `{itemAlias}`.id AND `{titleAlias}`.`key` = 'title'");
                    query.AppendLine($"LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS `{seoTitleAlias}` ON `{seoTitleAlias}`.item_id = `{itemAlias}`.id AND `{seoTitleAlias}`.`key` = '{Core.Models.CoreConstants.SeoTitlePropertyName}'");
                }
            }

            // WHERE part.
            query.Append("WHERE webPage.entity_type = 'webpagina' AND webPage.published_environment >= ?environment");

            if (Settings.PageId > 0)
            {
                query.Append(" AND webPage.id = ?pageItemId");
            }
            else if (!String.IsNullOrWhiteSpace(Settings.PageName))
            {
                query.Append(" AND IFNULL(webPageSeoName.`value`, webPageTitle.`value`) = ?pageName");

                if (!String.IsNullOrWhiteSpace(pathMustContain) && Settings.SearchNumberOfLevels > 0)
                {
                    query.Append(" AND CONCAT_WS('/'");
                    for (var i = Settings.SearchNumberOfLevels; i > 0; i--)
                    {
                        query.Append($", IFNULL(`item{i}SeoName`.`value`, `item{i}SeoName`.`value`)");
                    }
                    query.Append(") LIKE CONCAT('%', ?path, '%')");
                }
            }
            query.AppendLine();

            // TODO: ORDER BY part.

            // LIMIT part.
            query.Append("LIMIT 1");

            return query.ToString();
        }

        private int ConvertEnvironmentToInt()
        {
            return gclSettings.Environment switch
            {
                Environments.Development => 1,
                Environments.Test => 2,
                Environments.Acceptance => 3,
                Environments.Live => 4,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
