﻿using System;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using GeeksCoreLibrary.Modules.Payments.Enums;
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
            PaymentIn,
            
            /// <summary>
            /// For PSPs that always send the user to the same page, no matter the result of their payment.
            /// From this page, we can then send the user to the correct page based on the result of the payment.
            /// </summary>
            PaymentReturn
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

            var html = Settings.ComponentMode switch
            {
                ComponentModes.Checkout => await HandleCheckoutModeAsync(),
                ComponentModes.PaymentOut => await HandlePaymentOutModeAsync(),
                ComponentModes.PaymentIn => await HandlePaymentInModeAsync(),
                ComponentModes.PaymentReturn => await HandlePaymentReturnModeAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(Settings.ComponentMode), Settings.ComponentMode.ToString())
            };
            
            return new HtmlString(html);
        }

        #endregion

        #region Handling different component modes
        
        /// <summary>
        /// Handles the checkout component mode and outputs the HTML for this mode.
        /// </summary>
        /// <returns>The output HTML of the component.</returns>
        private async Task<string> HandleCheckoutModeAsync()
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
                var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(Settings.OrderProcessId);

                // If we have an invalid active step, return a 404.
                if (ActiveStep <= 0 || ActiveStep > steps.Count)
                {
                    HttpContextHelpers.Return404(httpContextAccessor.HttpContext);
                    return "";
                }
                
                // If we have no confirmation page and/or no payment methods, then the order process has not been fully configured and we want to throw an error.
                if (steps.All(step => step.Type != OrderProcessStepTypes.OrderConfirmation))
                {
                    throw new Exception($"There is no step with type '{OrderProcessStepTypes.OrderConfirmation.ToString()}' configured yet, therefor we cannot proceed with the order process.");
                }

                if (steps.All(step => step.Groups.All(group => group.Type != OrderProcessGroupTypes.PaymentMethods)))
                {
                    throw new Exception($"There is no group with type '{OrderProcessGroupTypes.PaymentMethods.ToString()}' configured yet, therefor we cannot proceed with the order process.");
                }

                // Get the active basket, if any.
                var shoppingBasketSettings = await shoppingBasketsService.GetSettingsAsync();
                var (shoppingBasket, shoppingBasketLines, _, _) = await shoppingBasketsService.LoadAsync(shoppingBasketSettings);

                // Get the logged in user, if any.
                var loggedInUser = await AccountsService.GetUserDataFromCookieAsync();
                WiserItemModel userData;
                if (loggedInUser.UserId > 0)
                {
                    userData = await wiserItemsService.GetItemDetailsAsync(loggedInUser.UserId, entityType: Account.Models.Constants.DefaultEntityType);
                }
                else
                {
                    var basketUser = (await wiserItemsService.GetLinkedItemDetailsAsync(shoppingBasket.Id, ShoppingBasket.Models.Constants.BasketToUserLinkType, Account.Models.Constants.DefaultEntityType, reverse: true)).FirstOrDefault();
                    userData = basketUser ?? new WiserItemModel { EntityType = Account.Models.Constants.DefaultEntityType } ;
                    loggedInUser.UserId = userData.Id;
                }

                // Get list of all items that are used in the order process, except basket.
                var currentItems = new List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> { (new LinkSettingsModel(), userData) };
                var allLinkTypeSettings = await wiserItemsService.GetAllLinkTypeSettingsAsync();
                foreach (var linkTypeSettings in allLinkTypeSettings)
                {
                    if (shoppingBasket.Id > 0 && String.Equals(linkTypeSettings.DestinationEntityType, ShoppingBasket.Models.Constants.BasketEntityType, StringComparison.OrdinalIgnoreCase))
                    {
                        currentItems.AddRange((await wiserItemsService.GetLinkedItemDetailsAsync(shoppingBasket.Id, linkTypeSettings.Type, linkTypeSettings.SourceEntityType, userId: userData.Id))
                            .Where(item => !String.Equals(item.EntityType, ShoppingBasket.Models.Constants.BasketLineEntityType, StringComparison.OrdinalIgnoreCase) && !String.Equals(item.EntityType, Constants.OrderLineEntityType, StringComparison.OrdinalIgnoreCase))
                            .Select(item => (linkTypeSettings, item)));
                    }

                    if (userData.Id > 0 && String.Equals(linkTypeSettings.DestinationEntityType, Account.Models.Constants.DefaultEntityType, StringComparison.OrdinalIgnoreCase))
                    {
                        currentItems.AddRange((await wiserItemsService.GetLinkedItemDetailsAsync(userData.Id, linkTypeSettings.Type, linkTypeSettings.SourceEntityType, userId: userData.Id)).Select(item => (linkTypeSettings, item)));
                    }
                }

                // Get the available payment methods.
                var paymentMethods = await orderProcessesService.GetPaymentMethodsAsync(Settings.OrderProcessId, loggedInUser);

                // If there are no payment methods, throw an error.
                if (paymentMethods == null || !paymentMethods.Any())
                {
                    throw new Exception("No payment methods have been configured, therefor we cannot proceed with the order process.");
                }
                
                // Generate the URL for the next step, we'll need this for a few things.
                var nextStep = ActiveStep + 1;
                UriBuilder nextStepUri;
                if (nextStep <= steps.Count && steps[ActiveStep].Type != OrderProcessStepTypes.OrderConfirmation && steps[ActiveStep].Type != OrderProcessStepTypes.OrderPending)
                {
                    // If we still have a next step and that step is not for the order confirmation, then go to the next step.
                    nextStepUri = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);
                    var nextStepQueryString = HttpUtility.ParseQueryString(nextStepUri.Query);
                    nextStepQueryString[Constants.ActiveStepRequestKey] = nextStep.ToString();
                    nextStepQueryString.Remove(Constants.ErrorFromPaymentOutRequestKey);
                    nextStepUri.Query = nextStepQueryString?.ToString() ?? "";
                }
                else
                {
                    // If the next step is for the order confirmation, it means the user needs to do their payment first and will be redirected to that step after.
                    nextStepUri = new UriBuilder(HttpContextHelpers.GetOriginalRequestUri(httpContext))
                    {
                        Path = $"/{Constants.PaymentOutPage}",
                        Query = $"?{Constants.OrderProcessIdRequestKey}={Settings.OrderProcessId}"
                    };
                }

                // Get the active step. The active step number starts with 1, so we subtract one to get the correct index.
                var step = steps[ActiveStep - 1];

                // Do all validation and saving first, so that we don't have to render the entire HTML of this step, if we are going to to send someone to the next step anyway.
                if (isPostBack)
                {
                    await DatabaseConnection.BeginTransactionAsync();

                    // If we have no user ID yet, create it here because the ValidatePostBackAndSaveValuesAsync method will need a user ID to link items that might be created there. 
                    if (userData.Id == 0)
                    {
                        userData = await wiserItemsService.CreateAsync(userData, createNewTransaction: false);
                    }

                    fieldErrorsOccurred = await ValidatePostBackAndSaveValuesAsync(step, loggedInUser, request, shoppingBasket, paymentMethods, currentItems);

                    // Save values to database if all validation succeeded.
                    if (!fieldErrorsOccurred)
                    {
                        // Save basket to database.
                        shoppingBasket = await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings, createNewTransaction: false);
                        
                        // Save all other items to database.
                        foreach (var item in currentItems)
                        {
                            await wiserItemsService.SaveAsync(item.Item, userId: userData.Id, createNewTransaction: false);
                        }
                        
                        // Link basket to active user.
                        await wiserItemsService.AddItemLinkAsync(shoppingBasket.Id, userData.Id, ShoppingBasket.Models.Constants.BasketToUserLinkType);

                        await DatabaseConnection.CommitTransactionAsync();

                        response.Redirect(nextStepUri.ToString());

                        return null;
                    }
                }
                
                // Redirect the user if needed.
                if (!String.IsNullOrWhiteSpace(step.StepRedirectUrl) && response != null)
                {
                    var uriBuilder = new UriBuilder(await StringReplacementsService.DoAllReplacementsAsync(step.StepRedirectUrl));
                    var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
                    queryString[Constants.OrderProcessIdRequestKey] = Settings.OrderProcessId.ToString();
                    queryString[Constants.OrderIdRequestKey] = shoppingBasket.Id.ToString();
                    uriBuilder.Query = queryString.ToString()!;
                    response.Redirect(uriBuilder.ToString());
                    return null;
                }

                // Generate URI for previous step.
                var previousStepUri = HttpContextHelpers.GetOriginalRequestUriBuilder(httpContextAccessor.HttpContext);
                var previousStep = ActiveStep - 1;
                var previousStepQueryString = HttpUtility.ParseQueryString(previousStepUri.Query);
                previousStepQueryString.Remove(Constants.ErrorFromPaymentOutRequestKey);
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
                    { "previousStepUrl", previousStepUri.ToString() },
                    { "type", step.Type.ToString() }
                };

                var stepHtml = StringReplacementsService.DoReplacements(Settings.TemplateStep, replaceData);
                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, orderProcessSettings.Header + step.Header)
                    .ReplaceCaseInsensitive(Constants.FooterReplacement, step.Footer + orderProcessSettings.Footer);

                // Build the groups HTML.
                var groupsBuilder = new StringBuilder();
                switch (step.Type)
                {
                    case OrderProcessStepTypes.GroupsAndFields:
                        // If this step contains only one group, that group is of type PaymentMethods and there exists only one payment method,
                        // then save that payment method in the basket and redirect to the next step.
                        if (response != null && step.Groups.Count == 1 && step.Groups.Single().Type == OrderProcessGroupTypes.PaymentMethods && paymentMethods.Count == 1)
                        {
                            shoppingBasket.SetDetail(Constants.PaymentMethodProperty, paymentMethods.Single().Id);
                            await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
                            response.Redirect(nextStepUri.ToString());
                            return null;
                        }

                        // Otherwise render the group(s) for this step.
                        foreach (var group in step.Groups)
                        {
                            // If we only have a single payment method, set that as the selected payment in the basket and don't render the group.
                            if (group.Type == OrderProcessGroupTypes.PaymentMethods && paymentMethods.Count == 1)
                            {
                                shoppingBasket.SetDetail(Constants.PaymentMethodProperty, paymentMethods.Single().Id);
                                await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
                                continue;
                            }
                            
                            var groupHtml = await RenderGroupAsync(group, loggedInUser, shoppingBasket, currentItems, paymentMethods, fieldErrorsOccurred);
                            if (groupHtml == null)
                            {
                                continue;
                            }

                            groupsBuilder.AppendLine(groupHtml);
                        }
                        break;
                    case OrderProcessStepTypes.Summary:
                        var summaryHtml = ReplaceEntityDataInTemplate(shoppingBasket, currentItems, step, steps, paymentMethods);
                        groupsBuilder.AppendLine(summaryHtml);
                        break;
                    case OrderProcessStepTypes.OrderConfirmation:
                    case OrderProcessStepTypes.OrderPending:
                        var confirmationHtml = ReplaceEntityDataInTemplate(shoppingBasket, currentItems, step, steps, paymentMethods);
                        groupsBuilder.AppendLine(confirmationHtml);

                        // Empty the shopping basket.
                        if (orderProcessSettings.ClearBasketOnConfirmationPage)
                        {
                            var id = shoppingBasket.Id;
                            shoppingBasket = new WiserItemModel();
                            shoppingBasketLines = new List<WiserItemModel>();
                            shoppingBasket.Id = id;

                            await shoppingBasketsService.SaveAsync(shoppingBasket, shoppingBasketLines, shoppingBasketSettings);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(step.Type), step.Type.ToString());
                }

                stepHtml = stepHtml.ReplaceCaseInsensitive(Constants.GroupsReplacement, groupsBuilder.ToString());

                resultHtml = orderProcessSettings.Template;
                if (String.IsNullOrEmpty(resultHtml))
                {
                    resultHtml = Settings.Template;
                }

                resultHtml = resultHtml.ReplaceCaseInsensitive(Constants.StepReplacement, stepHtml);

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

                if (String.Equals(HttpContextHelpers.GetRequestValue(httpContext, Constants.ErrorFromPaymentOutRequestKey), "true", StringComparison.OrdinalIgnoreCase))
                {
                    resultHtml = AddStepErrorToResult(resultHtml, "Payment");
                }

                resultHtml = resultHtml.ReplaceCaseInsensitive("{activeStep}", ActiveStep.ToString());
            }
            catch (ThreadAbortException)
            {
                // Ignore ThreadAbortExceptions, these always occur when we do a redirect and we don't need to see them in the logs or anything.
            }
            catch (Exception exception)
            {
                await DatabaseConnection.RollbackTransactionAsync(false);
                resultHtml = AddStepErrorToResult(resultHtml, "Server");
                Logger.LogError(exception.ToString());
            }

            // Do all generic replacement last and then return the final HTML.
            return AddComponentIdToForms(await TemplatesService.DoReplacesAsync(resultHtml), Constants.ComponentIdFormKey);
        }

        /// <summary>
        /// Replaces data from the user and their shopping basket in a template. This uses the prefixes "basket", "order" and "account".
        /// Examples: {order.Id}, {account.firstName}
        /// </summary>
        /// <param name="shoppingBasket">The active basket of the user.</param>
        /// <param name="currentItems">The data of the user and other entities.</param>
        /// <param name="step">The data/settings of the current step.</param>
        /// <param name="allSteps">The date/settings of all steps.</param>
        /// <param name="paymentMethods">All available payment methods.</param>
        private string ReplaceEntityDataInTemplate(WiserItemModel shoppingBasket, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems, OrderProcessStepModel step, List<OrderProcessStepModel> allSteps, List<PaymentMethodSettingsModel> paymentMethods)
        {
            // Replace basket data.
            var replaceData = new Dictionary<string, object>
            {
                { $"{ShoppingBasket.Models.Constants.BasketEntityType}.id", shoppingBasket.Id },
                { $"{Constants.OrderEntityType}.id", shoppingBasket.Id }
            };

            foreach (var basketDetail in shoppingBasket.Details)
            {
                if (basketDetail?.Value == null)
                {
                    continue;
                }

                var value = basketDetail.Value.ToString();

                if (String.Equals(basketDetail.Key, Constants.PaymentMethodProperty, StringComparison.OrdinalIgnoreCase))
                {
                    var paymentMethod = paymentMethods.SingleOrDefault(p => p.Id.ToString() == value);
                    if (paymentMethod == null)
                    {
                        continue;
                    }

                    var logoUrl = $"/image/wiser2/{paymentMethod.Id}/{Constants.PaymentMethodLogoProperty}/crop/32/32/{paymentMethod.Title.ConvertToSeo()}.png";
                    replaceData.Add($"{ShoppingBasket.Models.Constants.BasketEntityType}.{Constants.PaymentMethodProperty}Logo", logoUrl);
                    replaceData.Add($"{Constants.OrderEntityType}.{Constants.PaymentMethodProperty}Logo", logoUrl);
                    value = paymentMethod.Title;
                }

                var field = allSteps.SelectMany(s => s.Groups.SelectMany(group => group.Fields.Where(field =>
                    field.SaveTo.Any(x => String.Equals($"{x.EntityType}.{x.PropertyName}", $"{ShoppingBasket.Models.Constants.BasketEntityType}.{basketDetail.Key}", StringComparison.OrdinalIgnoreCase)
                        || String.Equals($"{x.EntityType}.{x.PropertyName}", $"{Constants.OrderEntityType}.{basketDetail.Key}", StringComparison.OrdinalIgnoreCase))
                    || String.Equals(field.FieldId, basketDetail.Key, StringComparison.OrdinalIgnoreCase)))).FirstOrDefault();
                if (field?.Values != null && field.Values.ContainsKey(value))
                {
                    value = field.Values[value];
                }

                replaceData.Add($"{ShoppingBasket.Models.Constants.BasketEntityType}.{basketDetail.Key}", value);
                replaceData.Add($"{Constants.OrderEntityType}.{basketDetail.Key}", value);
            }

            // Replace data of all other items.
            foreach (var (linkSettings, item) in currentItems)
            {
                var idKey = $"{item.EntityType}.id";
                if (replaceData.ContainsKey(idKey))
                {
                    continue;
                }

                replaceData.Add($"{item.EntityType}.id", item.Id);

                foreach (var itemDetail in item.Details)
                {
                    if (itemDetail?.Value == null)
                    {
                        continue;
                    }

                    var value = itemDetail.Value.ToString();
                    var field = allSteps.SelectMany(s => s.Groups.SelectMany(group => group.Fields.Where(field =>
                        field.SaveTo.Any(x => (String.Equals($"{x.EntityType}.{x.PropertyName}", $"{item.EntityType}.{itemDetail.Key}", StringComparison.OrdinalIgnoreCase) || String.Equals(field.FieldId, itemDetail.Key, StringComparison.OrdinalIgnoreCase))
                        && x.LinkType == linkSettings.Type)))).FirstOrDefault();
                    if (field?.Values != null && field.Values.ContainsKey(value))
                    {
                        value = field.Values[value];
                    }
                
                    replaceData.Add($"{item.EntityType}.{itemDetail.Key}", value);
                }
            }

            return StringReplacementsService.DoReplacements(step.Template, replaceData);
        }

        /// <summary>
        /// Handles the payment out component mode and outputs the HTML for this mode.
        /// </summary>
        /// <returns>The output HTML of the component.</returns>
        private async Task<string> HandlePaymentOutModeAsync()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "HttpContext not available.";
            }
            
            var paymentRequestResult = await orderProcessesService.HandlePaymentRequestAsync(Settings.OrderProcessId);

            switch (paymentRequestResult.Action)
            {
                case PaymentRequestActions.Redirect:
                    httpContextAccessor.HttpContext.Response.Redirect(paymentRequestResult.ActionData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paymentRequestResult.Action), paymentRequestResult.Action.ToString());
            }

            return "";
        }

        /// <summary>
        /// Handles the payment out component mode and outputs the HTML for this mode.
        /// </summary>
        /// <returns>The output HTML of the component.</returns>
        private async Task<string> HandlePaymentInModeAsync()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "HttpContext not available.";
            }

            var paymentMethodFromRequest = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, Constants.SelectedPaymentMethodRequestKey);
            if (!UInt64.TryParse(paymentMethodFromRequest, out var paymentMethodId) || paymentMethodId == 0)
            {
                throw new Exception($"Invalid payment method ID: {paymentMethodFromRequest}");
            }

            var success = await orderProcessesService.HandlePaymentServiceProviderWebhookAsync(Settings.OrderProcessId, paymentMethodId);
            if (!success)
            {
                throw new Exception("Payment update webhook failed.");
            }

            return "";
        }

        /// <summary>
        /// Handles the payment out component mode and outputs the HTML for this mode.
        /// </summary>
        /// <returns>The output HTML of the component.</returns>
        private async Task<string> HandlePaymentReturnModeAsync()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "HttpContext not available.";
            }
            
            var paymentMethodFromRequest = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, Constants.SelectedPaymentMethodRequestKey);
            if (!UInt64.TryParse(paymentMethodFromRequest, out var paymentMethodId) || paymentMethodId == 0)
            {
                throw new Exception($"Invalid payment method ID: {paymentMethodFromRequest}");
            }
            
            var result = await orderProcessesService.HandlePaymentReturnAsync(Settings.OrderProcessId, paymentMethodId);

            switch (result.Action)
            {
                case PaymentResultActions.Redirect:
                    httpContextAccessor.HttpContext.Response.Redirect(result.ActionData);
                    break;
                case PaymentResultActions.None:
                    // Do nothing.
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Action), result.Action.ToString());
            }

            return "";
        }

        /// <summary>
        /// Renders the HTML for a group.
        /// </summary>
        /// <param name="group">The group to render.</param>
        /// <param name="loggedInUser">The data of the logged in user or an empty object if the user is not logged in.</param>
        /// <param name="shoppingBasket">The basket of the user.</param>
        /// <param name="currentItems">The data of the user and other items associated with the user or basket (such as address).</param>
        /// <param name="paymentMethods">The list of available payment methods for the current order process.</param>
        /// <param name="fieldErrorsOccurred">Whether any errors occurred in this group.</param>
        /// <returns>The HTML for the group.</returns>
        private async Task<string> RenderGroupAsync(OrderProcessGroupModel group, UserCookieDataModel loggedInUser, WiserItemModel shoppingBasket, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems, List<PaymentMethodSettingsModel> paymentMethods, bool fieldErrorsOccurred)
        {
            return group.Type switch
            {
                OrderProcessGroupTypes.Fields => await RenderGroupFieldsAsync(group, loggedInUser, shoppingBasket, currentItems),
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
        /// <param name="currentItems">The data of the user and other items associated with the user or basket (such as address).</param>
        /// <returns>The HTML for the group.</returns>
        private async Task<string> RenderGroupFieldsAsync(OrderProcessGroupModel group, UserCookieDataModel loggedInUser, WiserItemModel shoppingBasket, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems)
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
                { "title", await languagesService.GetTranslationAsync($"orderProcess_group_{group.Title}_title", defaultValue: group.Title ?? "") },
                { "groupClass", group.CssClass }
            };

            var groupHtml = StringReplacementsService.DoReplacements(Settings.TemplateFieldsGroup, replaceData);
            groupHtml = groupHtml.ReplaceCaseInsensitive(Constants.HeaderReplacement, group.Header).ReplaceCaseInsensitive(Constants.FooterReplacement, group.Footer);

            // Build the fields HTML.
            var fieldsBuilder = new StringBuilder();
            foreach (var field in fieldsToShow)
            {
                var fieldHtml = await RenderFieldAsync(field, shoppingBasket, currentItems);
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
                { "title", await languagesService.GetTranslationAsync($"orderProcess_group_{group.Title}_title", defaultValue: group.Title ?? "") },
                { "groupClass", group.CssClass }
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
        /// <param name="currentItems">The data of the user and other items associated with the user or basket (such as address).</param>
        /// <returns>The HTML for the field.</returns>
        private async Task<string> RenderFieldAsync(OrderProcessFieldModel field, WiserItemModel shoppingBasket, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems)
        {
            var fieldValue = field.Value;
            if (String.IsNullOrEmpty(fieldValue) && field.InputFieldType != OrderProcessInputTypes.Password)
            {
                if (!field.SaveTo.Any())
                {
                    fieldValue = shoppingBasket.GetDetailValue(field.FieldId);
                }
                else
                {
                    foreach (var saveLocation in field.SaveTo)
                    {
                        if (String.Equals(saveLocation.EntityType, ShoppingBasket.Models.Constants.BasketEntityType, StringComparison.OrdinalIgnoreCase))
                        {
                            fieldValue = shoppingBasket.GetDetailValue(saveLocation.PropertyName);
                        }
                        else
                        {
                            var itemsOfEntityType = currentItems.Where(item => String.Equals(item.Item.EntityType, saveLocation.EntityType, StringComparison.CurrentCultureIgnoreCase) && item.LinkSettings.Type == saveLocation.LinkType).ToList();
                            foreach (var item in itemsOfEntityType)
                            {
                                fieldValue = item.Item.GetDetailValue(saveLocation.PropertyName);
                                if (!String.IsNullOrWhiteSpace(fieldValue))
                                {
                                    break;
                                }
                            }
                        }
                        
                        if (!String.IsNullOrWhiteSpace(fieldValue))
                        {
                            break;
                        }
                    }
                }
            }

            // Create dictionary for replacements.
            var replaceData = new Dictionary<string, object>
            {
                { "id", field.Id },
                { "title", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_title", defaultValue: field.Title ?? "") },
                { "placeholder", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_placeholder", defaultValue: field.Placeholder ?? "") },
                { "fieldId", field.FieldId },
                { "inputType", EnumHelpers.ToEnumString(field.InputFieldType).ToLowerInvariant() },
                { "label", await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_label", defaultValue: field.Label ?? "") },
                { "pattern", String.IsNullOrWhiteSpace(field.Pattern) ? "" : $"pattern='{field.Pattern}'" },
                { "required", field.Mandatory ? "required" : "" },
                { "value", fieldValue },
                { "checked", String.IsNullOrWhiteSpace(fieldValue) ? "" : "checked" },
                { "fieldClass", field.CssClass }
            };

            var fieldHtml = field.Type switch
            {
                OrderProcessFieldTypes.Input => Settings.TemplateInputField,
                OrderProcessFieldTypes.Textarea => Settings.TemplateTextareaField,
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
                if (progressStep.HideInProgress)
                {
                    continue;
                }

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
        /// <param name="currentItems">The data of the user and other items associated with the user or basket (such as address).</param>
        /// <returns>A <see cref="Boolean"/> indicating whether any there were any errors in the validation.</returns>
        private async Task<bool> ValidatePostBackAndSaveValuesAsync(OrderProcessStepModel step, UserCookieDataModel loggedInUser, HttpRequest request, WiserItemModel shoppingBasket, List<PaymentMethodSettingsModel> paymentMethods, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems)
        {
            if (currentItems == null)
            {
                throw new ArgumentNullException(nameof(currentItems));
            }

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
                            field.IsValid = await orderProcessesService.ValidateFieldValueAsync(field, currentItems);

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

                            // Set the posted value in the basket or user. We do that here so that we can be sure that people can only save values that are configured in Wiser.
                            // This way they can't just add a random field to the HTML to save that value in our database.
                            if (!field.SaveTo.Any())
                            {
                                shoppingBasket.SetDetail(field.FieldId, valueForDatabase);
                            }
                            else
                            {
                                foreach (var saveLocation in field.SaveTo)
                                {
                                    if (String.Equals(saveLocation.EntityType, ShoppingBasket.Models.Constants.BasketEntityType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        shoppingBasket.SetDetail(saveLocation.PropertyName, valueForDatabase);
                                    }
                                    else
                                    {
                                        var itemsOfEntityType = currentItems.Where(item => String.Equals(item.Item.EntityType, saveLocation.EntityType, StringComparison.CurrentCultureIgnoreCase) && item.LinkSettings.Type == saveLocation.LinkType).ToList();
                                        if (itemsOfEntityType.Any())
                                        {
                                            // If we already have item(s) of the given entity type, save the value there.
                                            itemsOfEntityType.ForEach(item => item.Item.SetDetail(saveLocation.PropertyName, valueForDatabase));
                                        }
                                        else
                                        {
                                            // If we don't have an item yet, see if we can create one.
                                            var userData = currentItems.Single(item => item.Item.EntityType == Account.Models.Constants.DefaultEntityType).Item;
                                            var userId = userData.Id;
                                            var parentId = userId;
                                            var linkSettings = await wiserItemsService.GetLinkTypeSettingsAsync(0, saveLocation.EntityType, Account.Models.Constants.DefaultEntityType);
                                            if (linkSettings == null || linkSettings.Id == 0)
                                            {
                                                parentId = shoppingBasket.Id;
                                                linkSettings = await wiserItemsService.GetLinkTypeSettingsAsync(0, saveLocation.EntityType, ShoppingBasket.Models.Constants.BasketEntityType);
                                            }

                                            if (linkSettings == null || linkSettings.Id == 0)
                                            {
                                                throw new NotImplementedException($"Unknown entity type '{saveLocation.EntityType}' for field '{field.Id}' set for saving.");
                                            }

                                            var newItem = await wiserItemsService.CreateAsync(new WiserItemModel { EntityType = saveLocation.EntityType }, parentId > 0 ? parentId : null, linkSettings.Type, userId, createNewTransaction: false);
                                            newItem.SetDetail(saveLocation.PropertyName, valueForDatabase);
                                            currentItems.Add((new LinkSettingsModel { Type = saveLocation.LinkType, SourceEntityType = newItem.EntityType, DestinationEntityType = userData.EntityType }, newItem));
                                        }
                                    }
                                }
                            }
                        }

                        // If there are exactly 2 fields of type "password", we assume that this is for creating a new account and we'll check if both values are the same.
                        var passwordFields = group.Fields.Where(field => field.InputFieldType == OrderProcessInputTypes.Password).ToList();
                        if (passwordFields.Count == 2)
                        {
                            if (request.Form[passwordFields.First().FieldId].ToString() != request.Form[passwordFields.Last().FieldId].ToString())
                            {
                                passwordFields.First().IsValid = false;
                                passwordFields.Last().IsValid = false;
                                fieldErrorsOccurred = true;
                                
                                var errorMessage = await languagesService.GetTranslationAsync("orderProcess_passwords_not_the_same_errorMessage", defaultValue: "");
                                if (!String.IsNullOrEmpty(errorMessage))
                                {
                                    passwordFields.First().ErrorMessage = errorMessage;
                                    passwordFields.Last().ErrorMessage = errorMessage;
                                }
                            }
                        }

                        break;
                    }
                    case OrderProcessGroupTypes.PaymentMethods:
                    {
                        // Make sure the user selected a payment method and that the selected payment method is one of the available payment methods that are set in Wiser.
                        if (paymentMethods.Count == 1 && !String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(Constants.PaymentMethodProperty)))
                        {
                            // If there is only one payment method, we will have already set it.
                            continue;
                        }

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
        /// Method to add an error to the result.
        /// If a variable '{error}' exists in the result HTML, then that variable will be replaced with the error.
        /// It that variable doesn't exist and the errorType is "Server", the entire result HTML will be replaced by the error.
        /// </summary>
        /// <param name="resultHtml">The result HTML.</param>
        /// <param name="errorType">The type of error (Server or Client).</param>
        /// <returns>The result HTML with the error.</returns>
        private string AddStepErrorToResult(string resultHtml, string errorType)
        {
            if (String.IsNullOrEmpty(resultHtml) || !resultHtml.Contains(Constants.ErrorReplacement))
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
