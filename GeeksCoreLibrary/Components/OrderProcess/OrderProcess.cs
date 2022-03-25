using System;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess
{
    [ViewComponent(Name = "OrderProcess")]
    public class OrderProcess : CmsComponent<OrderProcessCmsSettingsModel, OrderProcess.ComponentModes>
    {
        private readonly GclSettings gclSettings;
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IOrderProcessesService orderProcessesService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IWiserItemsService wiserItemsService;

        private int ActiveStep { get; set; }

        #region Enums

        public enum ComponentModes
        {
            Automatic = 1,
            PaymentMethods = 2
        }

        #endregion

        #region Constructor

        public OrderProcess(IOptions<GclSettings> gclSettings,
            ILogger<OrderProcess> logger,
            IStringReplacementsService stringReplacementsService,
            ILanguagesService languagesService,
            IDatabaseConnection databaseConnection,
            ITemplatesService templatesService,
            IAccountsService accountsService,
            IHttpContextAccessor httpContextAccessor,
            IOrderProcessesService orderProcessesService,
            IShoppingBasketsService shoppingBasketsService,
            IWiserItemsService wiserItemsService)
        {
            this.gclSettings = gclSettings.Value;
            this.languagesService = languagesService;
            this.httpContextAccessor = httpContextAccessor;
            this.orderProcessesService = orderProcessesService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.wiserItemsService = wiserItemsService;

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
            Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<OrderProcessCmsSettingsModel>(settingsJson) ?? new OrderProcessCmsSettingsModel();
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

            // Get the active step.
            var activeStepValue = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, Constants.ActiveStepRequestKey);
            Int32.TryParse(activeStepValue, out var parsedActiveStep);
            ActiveStep = parsedActiveStep > 0 ? parsedActiveStep : 1;

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

            // If we have an invalid active step, return a 404.
            if (ActiveStep <= 0 || ActiveStep > steps.Count)
            {
                HttpContextHelpers.Return404(httpContextAccessor.HttpContext);
                return "";
            }

            // Gather some data.
            var httpContext = HttpContext;
            var response = httpContext?.Response;
            var request = httpContext?.Request;
            var isPostBack = request is { HasFormContentType: true } && request.Form.Count > 0 && request.Form[Constants.ComponentIdFormKey].ToString() == ComponentId.ToString();
            var fieldErrorsOccurred = false;
            string resultHtml = null;

            try
            {
                // Get the logged in user, if any.
                var loggedInUser = await AccountsService.GetUserDataFromCookieAsync();
                var userData = loggedInUser.UserId == 0 ? new WiserItemModel() : await wiserItemsService.GetItemDetailsAsync(loggedInUser.UserId);

                // Get the active basket, if any.
                var shoppingBasketSettings = await shoppingBasketsService.GetSettingsAsync();
                var (shoppingBasket, shoppingBasketLines, _, _) = await shoppingBasketsService.LoadAsync(shoppingBasketSettings);

                // Get the active step. The active step number starts with 1, so we subtract one to get the correct index.
                var step = steps[ActiveStep - 1];

                // Build the steps HTML.
                var replaceData = new Dictionary<string, string>
                {
                    { "id", step.Id.ToString() },
                    { "title", await languagesService.GetTranslationAsync($"orderProcess_step_{step.Title}_title", defaultValue: step.Title ?? "") },
                    { "confirmButtonText", await languagesService.GetTranslationAsync($"orderProcess_step_{step.Title}_confirmButtonText", defaultValue: step.ConfirmButtonText) }
                };

                var stepHtml = StringReplacementsService.DoReplacements(Settings.TemplateStep, replaceData);
                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, step.Header).ReplaceCaseInsensitive(Constants.FooterReplacement, step.Footer);

                // Build the groups HTML.
                var groupsBuilder = new StringBuilder();
                foreach (var group in step.Groups)
                {
                    // First get fields that we can show in this group, based on the visibility settings of each field.
                    var fieldsToShow = group.Fields.Where(field =>
                    {
                        // Check if we need to skip this field.
                        return field.Visibility switch
                        {
                            OrderProcessFieldVisibilityTypes.Always => true,
                            OrderProcessFieldVisibilityTypes.WhenNotLoggedIn => loggedInUser.UserId == 0,
                            OrderProcessFieldVisibilityTypes.WhenLoggedIn => loggedInUser.UserId > 0,
                            _ => throw new ArgumentOutOfRangeException(nameof(field.Visibility), field.Visibility.ToString())
                        };
                    }).ToList();

                    // Skip this group if it has no fields that we can show.
                    if (!fieldsToShow.Any())
                    {
                        continue;
                    }

                    // Create dictionary for replacements.
                    replaceData = new Dictionary<string, string>
                    {
                        { "id", group.Id.ToString() },
                        { "title", await languagesService.GetTranslationAsync($"orderProcess_group_{group.Title}_title", defaultValue: group.Title ?? "") }
                    };

                    var groupHtml = StringReplacementsService.DoReplacements(Settings.TemplateGroup, replaceData);
                    groupHtml = groupHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, group.Header).ReplaceCaseInsensitive(Constants.FooterReplacement, group.Footer);

                    // Build the fields HTML.
                    var fieldsBuilder = new StringBuilder();
                    foreach (var field in fieldsToShow)
                    {
                        var fieldIsValid = true;
                        var fieldValue = "";
                        if (isPostBack)
                        {
                            // Get the posted field value.
                            fieldValue = request.Form[field.FieldId];

                            // Do field validation.
                            fieldIsValid = FieldValueIsValid(field, fieldValue);

                            // Get value to save to database.
                            var valueForDatabase = fieldValue;
                            if (field.InputFieldType == OrderProcessInputTypes.Password && !String.IsNullOrEmpty(valueForDatabase))
                            {
                                valueForDatabase = fieldValue.ToSha512ForPasswords();
                                fieldValue = "";
                            }

                            // Set the posted value in the basket. We do that here so that we can be sure that people can only save values that are configured in Wiser.
                            // This way they can't just add a random field to the HTML to save that value in our database.
                            // TODO: Some values should be saved in the account instead of basket.
                            shoppingBasket.SetDetail(field.FieldId, valueForDatabase);
                        }
                        else if (field.InputFieldType != OrderProcessInputTypes.Password)
                        {
                            fieldValue = shoppingBasket.GetDetailValue(field.FieldId);
                            if (String.IsNullOrWhiteSpace(fieldValue))
                            {
                                fieldValue = userData.GetDetailValue(field.FieldId);
                            }
                        }

                        // Create dictionary for replacements.
                        replaceData = new Dictionary<string, string>
                        {
                            { "id", field.Id.ToString() },
                            { "title", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_title", defaultValue: field.Title ?? "") },
                            { "placeholder", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_placeholder", defaultValue: field.Placeholder ?? "") },
                            { "fieldId", field.FieldId },
                            { "inputType", EnumHelpers.ToEnumString(field.InputFieldType) },
                            { "label", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_label", defaultValue: field.Label ?? "") },
                            { "pattern", String.IsNullOrWhiteSpace(field.Pattern) ? "" : $"pattern='{field.Pattern}'" },
                            { "required", field.Mandatory ? "required" : "" },
                            { "value", fieldValue },
                            { "checked", String.IsNullOrWhiteSpace(fieldValue) ? "" : "checked" }
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

                                // Create dictionary for replacements.
                                replaceData = new Dictionary<string, string>
                                {
                                    { "fieldId", field.FieldId },
                                    { "required", field.Mandatory ? "required" : "" },
                                    { "optionValue", option.Key },
                                    { "optionText", await languagesService.GetTranslationAsync($"orderProcess_fieldOption_{option.Value}_text", defaultValue: option.Value ?? "") },
                                    { "selected", option.Key == fieldValue ? "selected" : "" },
                                    { "checked", option.Key == fieldValue ? "checked" : "" }
                                };

                                optionHtml = StringReplacementsService.DoReplacements(optionHtml, replaceData);
                                optionsBuilder.AppendLine(optionHtml);
                            }

                            fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.FieldOptionsReplacement, optionsBuilder.ToString());
                        }

                        if (fieldIsValid)
                        {
                            fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.ErrorReplacement, "").ReplaceCaseInsensitive(Constants.ErrorClassReplacement, "");
                        }
                        else
                        {
                            fieldErrorsOccurred = true;
                            var errorMessage = await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_text", defaultValue: field.ErrorMessage ?? "");
                            var errorHtml = Settings.TemplateFieldError.ReplaceCaseInsensitive(Constants.ErrorMessageReplacement, errorMessage);
                            fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.ErrorReplacement, errorHtml).ReplaceCaseInsensitive(Constants.ErrorClassReplacement, "error");
                        }

                        fieldsBuilder.AppendLine(fieldHtml);
                    }

                    groupHtml = groupHtml.ReplaceCaseInsensitive(Constants.FieldsReplacement, fieldsBuilder.ToString());
                    groupsBuilder.AppendLine(groupHtml);
                }

                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.GroupsReplacement, groupsBuilder.ToString());

                resultHtml = Settings.Template.ReplaceCaseInsensitive(Constants.StepReplacement, stepHtml);

                // Build the HTML for the steps progress.
                if (resultHtml.Contains(Constants.ProgressReplacement, StringComparison.OrdinalIgnoreCase))
                {
                    var progressBuilder = new StringBuilder();
                    for (var index = 0; index < steps.Count; index++)
                    {
                        var stepNumber = index + 1;
                        var progressStep = steps[index];

                        // Create dictionary for replacements.
                        replaceData = new Dictionary<string, string>
                        {
                            { "id", progressStep.Id.ToString() },
                            { "title", await languagesService.GetTranslationAsync($"orderProcess_step_{progressStep.Title}_title", defaultValue: progressStep.Title ?? "") },
                            { "number", stepNumber.ToString() },
                            { "active", ActiveStep == stepNumber ? "active" : "" }
                        };

                        var progressStepHtml = StringReplacementsService.DoReplacements(Settings.TemplateProgressStep, replaceData);
                        progressBuilder.AppendLine(progressStepHtml);
                    }

                    var progressHtml = Settings.TemplateProgress.ReplaceCaseInsensitive(Constants.StepsReplacement, progressBuilder.ToString());
                    resultHtml = resultHtml.ReplaceCaseInsensitive(Constants.ProgressReplacement, progressHtml);
                }

                // Save values to database.
                if (isPostBack)
                {
                    await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
                }

                if (fieldErrorsOccurred)
                {
                    resultHtml = AddStepErrorToResult(resultHtml, "Client");
                }
            }
            catch (Exception exception)
            {
                resultHtml = AddStepErrorToResult(resultHtml, "Server");
                Logger.LogError(exception.ToString());
            }

            // Do all generic replacement last and then return the final HTML.
            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(resultHtml), Constants.ComponentIdFormKey);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Validates whether a value for a field is valid.
        /// This checks if the field is mandatory, if the regex pattern matches and if the value is valid for the type of field (eg if an email field contains a valid e-mail address).
        /// </summary>
        /// <param name="field">The settings for the field.</param>
        /// <param name="fieldValue">The value that the user entered.</param>
        /// <returns>A <see cref="bool"/> indicating whether the value is valid or not.</returns>
        private static bool FieldValueIsValid(OrderProcessFieldModel field, string fieldValue)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(field.Pattern))
                {
                    // If the field is not mandatory, then it can be empty but must still pass validation if it's not empty.
                    return (!field.Mandatory && String.IsNullOrEmpty(fieldValue)) || Regex.IsMatch(fieldValue, field.Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
                }

                switch (field.Mandatory)
                {
                    case true when String.IsNullOrWhiteSpace(fieldValue):
                        return false;
                    case false when String.IsNullOrWhiteSpace(fieldValue):
                        return true;
                    default:
                        return field.InputFieldType switch
                        {
                            OrderProcessInputTypes.Email => Regex.IsMatch(fieldValue, @"(@)(.+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
                            OrderProcessInputTypes.Number => Decimal.TryParse(fieldValue, NumberStyles.Any, new CultureInfo("en-US"), out _),
                            _ => true
                        };
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Method to add an error to the result.
        /// If a variable '{error}' exists in the result HTML, then that variable will be replaced with the error.
        /// It that variable doesn't exist and the errorType is "Server", the entire result HTML will be replaced by the error.
        /// </summary>
        /// <param name="resultHtml">The result HTML.</param>
        /// <param name="errorType">The type of error (Server or Client).</param>
        /// <returns>The result HTML with the error.</returns>
        private string AddStepErrorToResult(string resultHtml, string errorType)
        {
            if (String.IsNullOrEmpty(resultHtml) || (!resultHtml.Contains(Constants.ErrorReplacement) && errorType != "Server"))
            {
                resultHtml = Settings.TemplateStepError;
            }
            else
            {
                resultHtml = resultHtml.ReplaceCaseInsensitive(Constants.ErrorReplacement, Settings.TemplateStepError);
            }

            resultHtml = resultHtml.ReplaceCaseInsensitive(Constants.ErrorTypeReplacement, errorType);
            return resultHtml;
        }

        #endregion
    }
}
