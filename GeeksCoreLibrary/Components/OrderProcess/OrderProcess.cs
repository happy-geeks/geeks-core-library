using System;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Extensions;
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
        private readonly IOrderProcessesService orderProcessesService;

        #region Enums

        public enum ComponentModes
        {
            Automatic = 1,
            PaymentMethods = 2
        }

        #endregion

        #region Constructor

        public OrderProcess(IOptions<GclSettings> gclSettings, ILogger<OrderProcess> logger, IStringReplacementsService stringReplacementsService, ILanguagesService languagesService, IDatabaseConnection databaseConnection, ITemplatesService templatesService, IAccountsService accountsService, IHttpContextAccessor httpContextAccessor, IOrderProcessesService orderProcessesService)
        {
            this.gclSettings = gclSettings.Value;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;
            this.orderProcessesService = orderProcessesService;

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
            // A single step can contain groups and a single group can contain fields.
            var steps = await orderProcessesService.GetAllStepsGroupsAndFields(Settings.OrderProcessId);

            // Build the steps HTML.
            var stepsBuilder = new StringBuilder();
            foreach (var step in steps)
            {
                var replaceData = new Dictionary<string, string>
                {
                    { "id", step.Id.ToString() },
                    { "title", step.Title }
                };

                var stepHtml = StringReplacementsService.DoReplacements(Settings.TemplateStep, replaceData);
                stepHtml = stepHtml.ReplaceCaseInsensitive("{header}", step.Header).ReplaceCaseInsensitive("{footer}", step.Footer);

                // Build the groups HTML.
                var groupsBuilder = new StringBuilder();
                foreach (var group in step.Groups)
                {
                    replaceData = new Dictionary<string, string>
                    {
                        { "id", group.Id.ToString() },
                        { "title", group.Title }
                    };

                    var groupHtml = StringReplacementsService.DoReplacements(Settings.TemplateGroup, replaceData);
                    groupHtml = groupHtml.ReplaceCaseInsensitive("{header}", group.Header).ReplaceCaseInsensitive("{footer}", group.Footer);

                    // Build the fields HTML.
                    var fieldsBuilder = new StringBuilder();
                    foreach (var field in group.Fields)
                    {
                        replaceData = new Dictionary<string, string>
                        {
                            { "id", field.Id.ToString() },
                            { "title", field.Title },
                            { "placeholder", field.Placeholder },
                            { "fieldId", field.FieldId },
                            { "inputType", field.InputFieldType },
                            { "label", field.Label },
                            { "pattern", field.Pattern },
                            { "required", field.Mandatory ? "required" : "" }
                        };

                        var fieldHtml = field.Type switch
                        {
                            OrderProcessFieldTypes.Input => Settings.TemplateInputField,
                            OrderProcessFieldTypes.Radio => Settings.TemplateRadioButtonField,
                            OrderProcessFieldTypes.Select => Settings.TemplateSelectField,
                            OrderProcessFieldTypes.Checkbox => Settings.TemplateCheckboxField,
                            _ => throw new ArgumentOutOfRangeException(nameof(field.Type), field.Type.ToString())
                        };

                        fieldHtml = StringReplacementsService.DoReplacements(fieldHtml, replaceData);

                        // Build the field options HTML, if applicable.
                        if (field.Values != null && field.Values.Any() && field.Type is OrderProcessFieldTypes.Radio or OrderProcessFieldTypes.Select)
                        {
                            var optionsBuilder = new StringBuilder();
                            foreach (var option in field.Values)
                            {
                                var optionHtml = field.Type switch
                                {
                                    OrderProcessFieldTypes.Radio => Settings.TemplateRadioButtonFieldOption,
                                    OrderProcessFieldTypes.Select => Settings.TemplateSelectFieldOption,
                                    _ => throw new ArgumentOutOfRangeException(nameof(field.Type), field.Type.ToString())
                                };
                                
                                replaceData = new Dictionary<string, string>
                                {
                                    { "fieldId", field.FieldId },
                                    { "required", field.Mandatory ? "required" : "" },
                                    { "optionValue", option.Key },
                                    { "optionText", String.IsNullOrEmpty(option.Value) ? option.Key : option.Value },
                                };

                                optionHtml = StringReplacementsService.DoReplacements(optionHtml, replaceData);
                                optionsBuilder.AppendLine(optionHtml);
                            }
                            
                            fieldHtml = fieldHtml.Replace("{options}", optionsBuilder.ToString());
                        }
                        fieldsBuilder.AppendLine(fieldHtml);
                    }

                    groupHtml = groupHtml.Replace("{fields}", fieldsBuilder.ToString());
                    groupsBuilder.AppendLine(groupHtml);
                }

                stepHtml = stepHtml.Replace("{groups}", groupsBuilder.ToString());
                stepsBuilder.AppendLine(stepHtml);
            }

            var html = Settings.Template.Replace("{steps}", stepsBuilder.ToString());

            // TODO: Progress HTML
            
            // Do all generic replacement last and then return the final HTML.
            return await TemplatesService.DoReplacesAsync(html);
        }

        #endregion
    }
}
