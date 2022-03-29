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
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Account.Models;
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
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess
{
    [ViewComponent(Name = "OrderProcess")]
    public class OrderProcess : CmsComponent<OrderProcessCmsSettingsModel, OrderProcess.ComponentModes>
    {
        private readonly ILanguagesService languagesService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IOrderProcessesService orderProcessesService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IWiserItemsService wiserItemsService;

        private int ActiveStep { get; set; }

        #region Enums

        public enum ComponentModes
        {
            /// <summary>
            /// Checkout process, with 1 or more steps where the order can place an order.
            /// </summary>
            Checkout,

            /// <summary>
            /// For sending the user to the correct PSP, based on the chosen payment method and the settings in Wiser.
            /// </summary>
            PaymentOut,

            /// <summary>
            /// For handling web hooks from PSPs and updating orders with the correct status based on what the PSP sends us.
            /// </summary>
            PaymentIn
        }

        #endregion

        #region Constructor

        public OrderProcess(ILogger<OrderProcess> logger,
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
                case ComponentModes.Checkout:
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
            // Gather some data.
            var httpContext = HttpContext;
            var response = httpContext?.Response;
            var request = httpContext?.Request;
            var isPostBack = request is { HasFormContentType: true } && request.Form.Count > 0 && request.Form[Constants.ComponentIdFormKey].ToString() == ComponentId.ToString();
            var fieldErrorsOccurred = false;
            string resultHtml = null;

            try
            {
                // A single step can contain groups and a single group can contain fields.
                var steps = await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(Settings.OrderProcessId);

                // If we have an invalid active step, return a 404.
                if (ActiveStep <= 0 || ActiveStep > steps.Count)
                {
                    HttpContextHelpers.Return404(httpContextAccessor.HttpContext);
                    return "";
                }

                // Get the logged in user, if any.
                var loggedInUser = await AccountsService.GetUserDataFromCookieAsync();
                var userData = loggedInUser.UserId == 0 ? new WiserItemModel() : await wiserItemsService.GetItemDetailsAsync(loggedInUser.UserId);
                
                // Get the available payment methods.
                var paymentMethods = await orderProcessesService.GetPaymentMethodsAsync(Settings.OrderProcessId, loggedInUser);

                // Get the active basket, if any.
                var shoppingBasketSettings = await shoppingBasketsService.GetSettingsAsync();
                var (shoppingBasket, shoppingBasketLines, _, _) = await shoppingBasketsService.LoadAsync(shoppingBasketSettings);

                // Get the active step. The active step number starts with 1, so we subtract one to get the correct index.
                var step = steps[ActiveStep - 1];

                // Do all validation and saving first, so that we don't have to render the entire HTML of this step, if we are going to to send someone to the next step anyway.
                if (isPostBack)
                {
                    fieldErrorsOccurred = ValidatePostBackAndSaveValues(step, loggedInUser, request, shoppingBasket, paymentMethods);

                    // Save values to database if all validation succeeded.
                    if (!fieldErrorsOccurred)
                    {
                        await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);

                        // Redirect to the next step.
                        var nextStep = ActiveStep + 1;
                        if (nextStep <= steps.Count)
                        {
                            var nextStepUri = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);
                            var nextStepQueryString = HttpUtility.ParseQueryString(nextStepUri.Query);
                            nextStepQueryString[Constants.ActiveStepRequestKey] = nextStep.ToString();
                            nextStepUri.Query = nextStepQueryString?.ToString() ?? "";
                            response.Redirect(nextStepUri.ToString());
                        }
                        else
                        {
                            response.Redirect($"/{Constants.PaymentOutPage}");
                        }

                        return null;
                    }
                }
                
                // Generate URI for previous step.
                var previousStepUri = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);
                var previousStep = ActiveStep - 1;
                var previousStepQueryString = HttpUtility.ParseQueryString(previousStepUri.Query);
                if (previousStep <= 1)
                {
                    previousStepQueryString.Remove(Constants.ActiveStepRequestKey);
                }
                else
                {
                    previousStepQueryString[Constants.ActiveStepRequestKey] = previousStep.ToString();
                }

                previousStepUri.Query = previousStepQueryString.ToString() ?? String.Empty;
                
                // Build the steps HTML.
                var replaceData = new Dictionary<string, object>
                {
                    { "id", step.Id },
                    { "title", await languagesService.GetTranslationAsync($"orderProcess_step_{step.Title}_title", defaultValue: step.Title ?? "") },
                    { "confirmButtonText", await languagesService.GetTranslationAsync($"orderProcess_step_{step.Title}_confirmButtonText", defaultValue: step.ConfirmButtonText) },
                    { "previousStepLinkText", await languagesService.GetTranslationAsync($"orderProcess_step_{step.Title}_previousStepLinkText", defaultValue: step.PreviousStepLinkText) },
                    { "previousStepUrl", previousStepUri.ToString() }
                };

                var stepHtml = StringReplacementsService.DoReplacements(Settings.TemplateStep, replaceData);
                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, step.Header).ReplaceCaseInsensitive(Constants.FooterReplacement, step.Footer);

                // Build the groups HTML.
                var groupsBuilder = new StringBuilder();
                foreach (var group in step.Groups)
                {
                    var groupHtml = await RenderGroupAsync(group, loggedInUser, shoppingBasket, userData, paymentMethods, fieldErrorsOccurred);
                    if (groupHtml == null)
                    {
                        continue;
                    }

                    groupsBuilder.AppendLine(groupHtml);
                }

                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.GroupsReplacement, groupsBuilder.ToString());

                resultHtml = Settings.Template.ReplaceCaseInsensitive(Constants.StepReplacement, stepHtml);

                // Build the HTML for the steps progress.
                if (resultHtml.Contains(Constants.ProgressReplacement, StringComparison.OrdinalIgnoreCase))
                {
                    var progressHtml = await RenderStepsProgressAsync(steps);
                    resultHtml = resultHtml.ReplaceCaseInsensitive(Constants.ProgressReplacement, progressHtml);
                }

                if (fieldErrorsOccurred)
                {
                    resultHtml = AddStepErrorToResult(resultHtml, "Client");
                }
            }
            catch (ThreadAbortException)
            {
                // Ignore ThreadAbortExceptions, these always occur when we do a redirect and we don't need to see them in the logs or anything.
            }
            catch (Exception exception)
            {
                resultHtml = AddStepErrorToResult(resultHtml, "Server");
                Logger.LogError(exception.ToString());
            }

            // Do all generic replacement last and then return the final HTML.
            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(resultHtml), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Renders the HTML for a group.
        /// </summary>
        /// <param name="group">The group to render.</param>
        /// <param name="loggedInUser">The data of the logged in user or an empty object if the user is not logged in.</param>
        /// <param name="shoppingBasket">The basket of the user.</param>
        /// <param name="userData">The data of the user.</param>
        /// <param name="paymentMethods">The list of available payment methods for the current order process.</param>
        /// <param name="fieldErrorsOccurred">Whether any errors occurred in this group.</param>
        /// <returns>The HTML for the group.</returns>
        private async Task<string> RenderGroupAsync(OrderProcessGroupModel group, UserCookieDataModel loggedInUser, WiserItemModel shoppingBasket, WiserItemModel userData, List<PaymentMethodSettingsModel> paymentMethods, bool fieldErrorsOccurred)
        {
            return group.Type switch
            {
                OrderProcessGroupTypes.Fields => await RenderGroupFieldsAsync(group, loggedInUser, shoppingBasket, userData),
                OrderProcessGroupTypes.PaymentMethods => await RenderGroupPaymentMethodsAsync(group, paymentMethods, fieldErrorsOccurred),
                _ => throw new ArgumentOutOfRangeException(nameof(group.Type), group.Type.ToString())
            };
        }

        /// <summary>
        /// Renders the HTML for a group of type "Fields".
        /// </summary>
        /// <param name="group">The group to render.</param>
        /// <param name="loggedInUser">The data of the logged in user or an empty object if the user is not logged in.</param>
        /// <param name="shoppingBasket">The basket of the user.</param>
        /// <param name="userData">The data of the user.</param>
        /// <returns>The HTML for the group.</returns>
        private async Task<string> RenderGroupFieldsAsync(OrderProcessGroupModel group, UserCookieDataModel loggedInUser, WiserItemModel shoppingBasket, WiserItemModel userData)
        {
            // Get fields that we can show in this group, based on the visibility settings of each field.
            var fieldsToShow = GetGroupFieldsToShow(group, loggedInUser);

            // Skip this group if it has no fields that we can show.
            if (!fieldsToShow.Any())
            {
                return null;
            }

            // Create dictionary for replacements.
            var replaceData = new Dictionary<string, object>
            {
                { "id", group.Id },
                { "title", await languagesService.GetTranslationAsync($"orderProcess_group_{group.Title}_title", defaultValue: group.Title ?? "") }
            };

            var groupHtml = StringReplacementsService.DoReplacements(Settings.TemplateFieldsGroup, replaceData);
            groupHtml = groupHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, group.Header).ReplaceCaseInsensitive(Constants.FooterReplacement, group.Footer);

            // Build the fields HTML.
            var fieldsBuilder = new StringBuilder();
            foreach (var field in fieldsToShow)
            {
                var fieldHtml = await RenderFieldAsync(field, shoppingBasket, userData);
                fieldsBuilder.AppendLine(fieldHtml);
            }

            return groupHtml.ReplaceCaseInsensitive(Constants.FieldsReplacement, fieldsBuilder.ToString());
        }

        /// <summary>
        /// Renders the HTML for a group of type "PaymentMethods".
        /// </summary>
        /// <param name="group">The group to render.</param>
        /// <param name="paymentMethods">The list of available payment methods for the current order process.</param>
        /// <param name="fieldErrorsOccurred">Whether any errors occurred in this group.</param>
        /// <returns>The HTML for the group.</returns>
        private async Task<string> RenderGroupPaymentMethodsAsync(OrderProcessGroupModel group, List<PaymentMethodSettingsModel> paymentMethods, bool fieldErrorsOccurred)
        {
            // Skip this group if it has no fields that we can show.
            if (!paymentMethods.Any())
            {
                return null;
            }

            // Create dictionary for replacements.
            var replaceData = new Dictionary<string, object>
            {
                { "id", group.Id },
                { "title", await languagesService.GetTranslationAsync($"orderProcess_group_{group.Title}_title", defaultValue: group.Title ?? "") }
            };

            var errorHtml = "";
            if (fieldErrorsOccurred)
            {
                var errorMessage = await languagesService.GetTranslationAsync("orderProcess_paymentMethod_errorMessage", defaultValue: "");
                errorHtml = Settings.TemplateFieldError.ReplaceCaseInsensitive(Constants.ErrorMessageReplacement, errorMessage);
            }

            var groupHtml = StringReplacementsService.DoReplacements(Settings.TemplatePaymentMethodsGroup, replaceData);
            groupHtml = groupHtml
                .ReplaceCaseInsensitive(Constants.HeaderReplacement, group.Header)
                .ReplaceCaseInsensitive(Constants.FooterReplacement, group.Footer)
                .ReplaceCaseInsensitive(Constants.ErrorReplacement, errorHtml);

            // Build the fields HTML.
            var paymentMethodsBuilder = new StringBuilder();
            foreach (var paymentMethod in paymentMethods)
            {
                var paymentMethodHtml = await RenderPaymentMethodAsync(paymentMethod);
                paymentMethodsBuilder.AppendLine(paymentMethodHtml);
            }

            return groupHtml.ReplaceCaseInsensitive(Constants.PaymentMethodsReplacement, paymentMethodsBuilder.ToString());
        }

        /// <summary>
        /// Renders the HTML for a single payment method.
        /// </summary>
        /// <param name="paymentMethod">The payment method to generate the HTML for.</param>
        /// <returns>The HTML for the payment method.</returns>
        private async Task<string> RenderPaymentMethodAsync(PaymentMethodSettingsModel paymentMethod)
        {
            // Create dictionary for replacements.
            var replaceData = new Dictionary<string, object>
            {
                { "id", paymentMethod.Id },
                { "title", await languagesService.GetTranslationAsync($"orderProcess_paymentMethod_{paymentMethod.Title}_title", defaultValue: paymentMethod.Title ?? "") },
                { "logoPropertyName", Constants.PaymentMethodLogoProperty },
                { "fee", paymentMethod.Fee },
                { "paymentMethodFieldName", Constants.PaymentMethodProperty }
            };

            var paymentMethodHtml = StringReplacementsService.DoReplacements(Settings.TemplatePaymentMethod, replaceData);
            return paymentMethodHtml;
        }

        /// <summary>
        /// Renders the HTML for a single field.
        /// </summary>
        /// <param name="field">The field to generate HTML for.</param>
        /// <param name="shoppingBasket">The basket of the user.</param>
        /// <param name="userData">The data of the user.</param>
        /// <returns>The HTML for the field.</returns>
        private async Task<string> RenderFieldAsync(OrderProcessFieldModel field, WiserItemModel shoppingBasket, WiserItemModel userData)
        {
            var fieldValue = field.Value;
            if (String.IsNullOrEmpty(fieldValue) && field.InputFieldType != OrderProcessInputTypes.Password)
            {
                fieldValue = shoppingBasket.GetDetailValue(field.FieldId);
                if (String.IsNullOrWhiteSpace(fieldValue))
                {
                    fieldValue = userData.GetDetailValue(field.FieldId);
                }
            }

            // Create dictionary for replacements.
            var replaceData = new Dictionary<string, object>
            {
                { "id", field.Id },
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
                var optionsHtml = await RenderFieldOptionsAsync(field, fieldValue);
                fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.FieldOptionsReplacement, optionsHtml);
            }

            if (field.IsValid)
            {
                fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.ErrorReplacement, "").ReplaceCaseInsensitive(Constants.ErrorClassReplacement, "");
            }
            else
            {
                var errorMessage = await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_errorMessage", defaultValue: field.ErrorMessage ?? "");
                var errorHtml = Settings.TemplateFieldError.ReplaceCaseInsensitive(Constants.ErrorMessageReplacement, errorMessage);
                fieldHtml = fieldHtml.ReplaceCaseInsensitive(Constants.ErrorReplacement, errorHtml).ReplaceCaseInsensitive(Constants.ErrorClassReplacement, "error");
            }

            return fieldHtml;
        }

        /// <summary>
        /// Renders the HTML for all options of a field.
        /// </summary>
        /// <param name="field">The field to render the options for.</param>
        /// <param name="fieldValue"></param>
        /// <returns>The HTML with all options for the given field.</returns>
        private async Task<string> RenderFieldOptionsAsync(OrderProcessFieldModel field, string fieldValue)
        {
            var optionsBuilder = new StringBuilder();
            foreach (var (key, value) in field.Values)
            {
                var optionHtml = field.Type switch
                {
                    OrderProcessFieldTypes.Radio => Settings.TemplateRadioButtonFieldOption,
                    OrderProcessFieldTypes.Select => Settings.TemplateSelectFieldOption,
                    _ => throw new ArgumentOutOfRangeException(nameof(field.Type), field.Type.ToString())
                };

                // Create dictionary for replacements.
                var replaceData = new Dictionary<string, object>
                {
                    { "fieldId", field.FieldId },
                    { "required", field.Mandatory ? "required" : "" },
                    { "optionValue", key },
                    { "optionText", await languagesService.GetTranslationAsync($"orderProcess_fieldOption_{value}_text", defaultValue: value ?? "") },
                    { "selected", key == fieldValue ? "selected" : "" },
                    { "checked", key == fieldValue ? "checked" : "" }
                };

                optionHtml = StringReplacementsService.DoReplacements(optionHtml, replaceData);
                optionsBuilder.AppendLine(optionHtml);
            }

            return optionsBuilder.ToString();
        }

        /// <summary>
        /// Renders the HTML for the progress of the order process.
        /// </summary>
        /// <param name="steps">The available steps.</param>
        /// <returns>The HTML for the progress steps.</returns>
        private async Task<string> RenderStepsProgressAsync(List<OrderProcessStepModel> steps)
        {
            var progressBuilder = new StringBuilder();
            for (var index = 0; index < steps.Count; index++)
            {
                var stepNumber = index + 1;
                var progressStep = steps[index];

                // Create dictionary for replacements.
                var replaceData = new Dictionary<string, object>
                {
                    { "id", progressStep.Id },
                    { "title", await languagesService.GetTranslationAsync($"orderProcess_step_{progressStep.Title}_title", defaultValue: progressStep.Title ?? "") },
                    { "number", stepNumber },
                    { "active", ActiveStep == stepNumber ? "active" : "" }
                };

                var progressStepHtml = StringReplacementsService.DoReplacements(Settings.TemplateProgressStep, replaceData);
                progressBuilder.AppendLine(progressStepHtml);
            }

            return Settings.TemplateProgress.ReplaceCaseInsensitive(Constants.StepsReplacement, progressBuilder.ToString());
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Validates data posted by the user and saves that data in their basket or account.
        /// This will also set the Value and IsValid properties on each field in the current step.
        /// </summary>
        /// <param name="step">The currently active step.</param>
        /// <param name="loggedInUser">The <see cref="UserCookieDataModel"/> of the logged in user, or empty object if they're not logged in.</param>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="shoppingBasket">The basket of the user.</param>
        /// <param name="paymentMethods">All available payment methods.</param>
        /// <returns>A <see cref="Boolean"/> indicating whether any there were any errors in the validation.</returns>
        private static bool ValidatePostBackAndSaveValues(OrderProcessStepModel step, UserCookieDataModel loggedInUser, HttpRequest request, WiserItemModel shoppingBasket, List<PaymentMethodSettingsModel> paymentMethods)
        {
            var fieldErrorsOccurred = false;
            foreach (var group in step.Groups)
            {
                // Get fields that we can show in this group, based on the visibility settings of each field.
                var fieldsToShow = GetGroupFieldsToShow(group, loggedInUser);

                switch (group.Type)
                {
                    case OrderProcessGroupTypes.Fields:
                    {
                        // Skip this group if it has no fields that we can show.
                        if (!fieldsToShow.Any())
                        {
                            continue;
                        }

                        foreach (var field in fieldsToShow)
                        {
                            // Get the posted field value.
                            field.Value = request.Form[field.FieldId].ToString();

                            // Do field validation.
                            field.IsValid = FieldValueIsValid(field, field.Value);

                            if (!field.IsValid)
                            {
                                fieldErrorsOccurred = true;
                                continue;
                            }

                            // Get value to save to database.
                            var valueForDatabase = field.Value;
                            if (field.InputFieldType == OrderProcessInputTypes.Password && !String.IsNullOrEmpty(valueForDatabase))
                            {
                                valueForDatabase = field.Value.ToSha512ForPasswords();
                                field.Value = "";
                            }

                            // Set the posted value in the basket. We do that here so that we can be sure that people can only save values that are configured in Wiser.
                            // This way they can't just add a random field to the HTML to save that value in our database.
                            // TODO: Some values should be saved in the account instead of basket.
                            shoppingBasket.SetDetail(field.FieldId, valueForDatabase);
                        }

                        break;
                    }
                    case OrderProcessGroupTypes.PaymentMethods:
                    {
                        // Make sure the user selected a payment method and that the selected payment method is one of the available payment methods that are set in Wiser.
                        var selectedPaymentMethod = request.Form[Constants.PaymentMethodProperty].ToString();
                        if (String.IsNullOrWhiteSpace(selectedPaymentMethod) || paymentMethods.All(p => p.Id.ToString() != selectedPaymentMethod))
                        {
                            fieldErrorsOccurred = true;
                            continue;
                        }

                        shoppingBasket.SetDetail(Constants.PaymentMethodProperty, selectedPaymentMethod);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(group.Type), group.Type.ToString());
                }
            }

            return fieldErrorsOccurred;
        }

        /// <summary>
        /// Gets the fields of a group that should be shown to the user.
        /// </summary>
        /// <param name="group">The group settings.</param>
        /// <param name="loggedInUser">The data of the user.</param>
        /// <returns>A list of fields that should be shown/handled on this step/group.</returns>
        private static List<OrderProcessFieldModel> GetGroupFieldsToShow(OrderProcessGroupModel group, UserCookieDataModel loggedInUser)
        {
            return group.Fields.Where(field =>
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
        }

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
