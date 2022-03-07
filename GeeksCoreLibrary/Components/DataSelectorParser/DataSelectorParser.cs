using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.DataSelectorParser.Interfaces;
using GeeksCoreLibrary.Components.DataSelectorParser.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.DataSelectorParser
{
    [CmsObject(
        PrettyName = "Data Selector Parser",
        Description = "Read and parse a data selector's response",
        DeveloperRemarks = ""
    )]
    public class DataSelectorParser : CmsComponent<DataSelectorParserCmsSettingsModel, DataSelectorParser.ComponentModes>
    {
        private readonly IDataSelectorParsersService dataSelectorParsersService;

        #region Enums

        public enum ComponentModes
        {
            Render = 1
        }

        #endregion

        #region Constructor

        public DataSelectorParser(ILogger<DataSelectorParser> logger, ITemplatesService templatesService, IStringReplacementsService stringReplacementsService, IDataSelectorParsersService dataSelectorParsersService)
        {
            this.dataSelectorParsersService = dataSelectorParsersService;

            Logger = logger;
            TemplatesService = templatesService;
            StringReplacementsService = stringReplacementsService;

            Settings = new DataSelectorParserCmsSettingsModel();
        }

        #endregion

        #region Rendering

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

            if (String.IsNullOrWhiteSpace(Settings.DataSelectorId) && String.IsNullOrWhiteSpace(Settings.DataSelectorJson) && String.IsNullOrWhiteSpace(Settings.DataSelectorDemoJson))
            {
                throw new Exception("No data selector ID, JSON, or demo JSON set. Data Selector Parser rendering cannot continue.");
            }

            var resultHtml = new StringBuilder();

            switch (Settings.ComponentMode)
            {
                case ComponentModes.Render:
                    {
                        resultHtml.Append(await HandleRenderModeAsync());
                        break;
                    }
                default:
                    throw new NotImplementedException($"Unknown or unsupported component mode '{Settings.ComponentMode}' in 'InvokeAsync'.");
            }

            return new HtmlString(resultHtml.ToString());
        }

        #endregion

        #region Handling different component modes

        public async Task<string> HandleRenderModeAsync()
        {
            JToken dataSelectorResult;

            if (!String.IsNullOrWhiteSpace(Settings.DataSelectorId) || !String.IsNullOrWhiteSpace(Settings.DataSelectorJson))
            {
                dataSelectorResult = await dataSelectorParsersService.GetDataSelectorResponseAsync(Settings.DataSelectorId, Settings.DataSelectorJson);
            }
            else if (!String.IsNullOrWhiteSpace(Settings.DataSelectorDemoJson))
            {
                dataSelectorResult = JToken.Parse(Settings.DataSelectorDemoJson);
            }
            else
            {
                return String.Empty;
            }

            try
            {
                if (Settings.SetSeoInfoFromFirstItem)
                {
                    if (dataSelectorResult.Any())
                    {
                        var seoTitle = dataSelectorResult.First?.Value<string>(Settings.SeoTitleEntityPropertyName);
                        var seoDescription = dataSelectorResult.First?.Value<string>(Settings.SeoDescriptionEntityPropertyName);
                        var seoCanonicalUrl = dataSelectorResult.First?.Value<string>(Settings.SeoCanoicalUrlEntityPropertyName);
                        var seoNoIndex = dataSelectorResult.First?.Value<bool>(Settings.SeoNoIndexEntityPropertyName);
                        var seoNoFollow = dataSelectorResult.First?.Value<bool>(Settings.SeoNoFollowEntityPropertyName);

                        SetPageSeoData(seoTitle, seoDescription, null, seoCanonicalUrl, seoNoIndex ?? false, seoNoFollow ?? false);
                    }
                }

                var resultHtml = new StringBuilder();
                var parsedTemplate = Settings.Template;
                parsedTemplate = StringReplacementsService.FillStringByClassList(dataSelectorResult, parsedTemplate);
                parsedTemplate = await TemplatesService.DoReplacesAsync(parsedTemplate);

                resultHtml.Append(parsedTemplate);
                if (!String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
                {
                    resultHtml.Append($"<script>{Settings.TemplateJavaScript}</script>");
                }

                return resultHtml.ToString();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "DataSelectorParser encountered an error.");
                return String.Empty;
            }
        }

        #endregion

        #region Handling settings

        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<DataSelectorParserCmsSettingsModel>(settingsJson);
            if (Settings != null && forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
        }

        public override string GetSettingsJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        }

        #endregion
    }
}
