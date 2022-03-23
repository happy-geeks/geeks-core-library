using System;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.OrderProcess
{
    [ViewComponent(Name = "OrderProcess")]
    public class OrderProcess : CmsComponent<OrderProcessCmsSettingsModel, OrderProcess.ComponentModes>
    {
        private readonly GclSettings gclSettings;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;

        #region Enums

        public enum ComponentModes
        {
            Automatic = 1,
            PaymentMethods = 2
        }

        #endregion

        #region Constructor

        public OrderProcess(IOptions<GclSettings> gclSettings, ILogger<OrderProcess> logger, IStringReplacementsService stringReplacementsService, ILanguagesService languagesService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IHttpContextAccessor httpContextAccessor)
        {
            this.gclSettings = gclSettings.Value;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new OrderProcessCmsSettingsModel();
        }

        #endregion
        
        #region Handling settings
        
        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<OrderProcessCmsSettingsModel>(settingsJson);
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

            if (Settings.OrderProcessId == 0)
            {
                throw new Exception("No order process ID set. Order process rendering cannot continue.");
            }

            var resultHtml = new StringBuilder();

            switch (Settings.ComponentMode)
            {
                case ComponentModes.Automatic:
                {
                    var html = await HandleAutomaticModeAsync();
                    resultHtml.Append(html);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Settings.ComponentMode), Settings.ComponentMode.ToString());
            }
            
            return new HtmlString(resultHtml.ToString());
        }

        #endregion

        #region Handling different component modes
        
        /// <summary>
        /// Handles the automatic component mode and outputs the HTML for this mode.
        /// </summary>
        /// <returns></returns>
        private async Task<string> HandleAutomaticModeAsync()
        {
            return $"<h1>Test order process {Settings.OrderProcessId}</h1>";
        }

        #endregion
    }
}
