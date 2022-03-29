using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    public class OrderProcessesService : IOrderProcessesService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IAccountsService accountsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ILogger<OrderProcessesService> logger;
        private readonly ILanguagesService languagesService;
        private readonly ITemplatesService templatesService;
        private readonly IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory;
        private readonly GclSettings gclSettings;

        public OrderProcessesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, IShoppingBasketsService shoppingBasketsService, IAccountsService accountsService, IWiserItemsService wiserItemsService, ILogger<OrderProcessesService> logger, ILanguagesService languagesService, ITemplatesService templatesService, IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory)
        {
            this.databaseConnection = databaseConnection;
            this.shoppingBasketsService = shoppingBasketsService;
            this.accountsService = accountsService;
            this.wiserItemsService = wiserItemsService;
            this.logger = logger;
            this.languagesService = languagesService;
            this.templatesService = templatesService;
            this.paymentServiceProviderServiceFactory = paymentServiceProviderServiceFactory;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<OrderProcessSettingsModel> GetOrderProcessSettingsAsync(ulong orderProcessId)
        {
            if (orderProcessId == 0)
            {
                throw new ArgumentNullException(nameof(orderProcessId));
            }
            
            var query = @$"SELECT 
                                orderProcess.id,
	                            IFNULL(titleSeo.value, orderProcess.title) AS name,
                                IFNULL(fixedUrl.value, '/payment.html') AS fixedUrl,
                                COUNT(step.id) AS amountOfSteps
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            JOIN {WiserTableNames.WiserItemLink} AS linkToStep ON linkToStep.destination_item_id = orderProcess.id AND linkToStep.type = {Constants.StepToProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS step ON step.id = linkToStep.item_id AND step.entity_type = '{Constants.StepEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = orderProcess.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
                            WHERE orderProcess.id = ?id
                            AND orderProcess.entity_type = '{Constants.OrderProcessEntityType}'
                            AND orderProcess.published_environment >= ?publishedEnvironment
                            GROUP BY orderProcess.id
                            LIMIT 1";
            
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("id", orderProcessId);
            databaseConnection.AddParameter("publishedEnvironment", (int)gclSettings.Environment);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var firstRow = dataTable.Rows[0];
            return new OrderProcessSettingsModel
            {
                Id = firstRow.Field<ulong>("id"),
                Title = firstRow.Field<string>("name"),
                FixedUrl = firstRow.Field<string>("fixedUrl")
            };
        }

        /// <inheritdoc />
        public async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl)
        {
            return await GetOrderProcessViaFixedUrlAsync(this, fixedUrl);
        }

        /// <inheritdoc />
        public async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(IOrderProcessesService orderProcessesService, string fixedUrl)
        {
            var query = @$"SELECT orderProcess.id
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}'
                            WHERE orderProcess.entity_type = '{Constants.OrderProcessEntityType}'
                            AND orderProcess.published_environment >= ?publishedEnvironment
                            AND IFNULL(fixedUrl.value, '/payment.html') = ?fixedUrl
                            LIMIT 1";
            
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("fixedUrl", fixedUrl);
            databaseConnection.AddParameter("publishedEnvironment", (int)gclSettings.Environment);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return await orderProcessesService.GetOrderProcessSettingsAsync(dataTable.Rows[0].Field<ulong>("id"));
        }

        /// <inheritdoc />
        public async Task<List<OrderProcessStepModel>> GetAllStepsGroupsAndFieldsAsync(ulong orderProcessId)
        {
            if (orderProcessId == 0)
            {
                throw new ArgumentNullException(nameof(orderProcessId));
            }

            var results = new List<OrderProcessStepModel>();

            var query = $@"SELECT
	                        # Step
	                        step.id AS stepId,
	                        step.title AS stepTitle,
	                        CONCAT_WS('', stepHeader.value, stepHeader.long_value) AS stepHeader,
	                        CONCAT_WS('', stepFooter.value, stepFooter.long_value) AS stepFooter,
                            stepConfirmButtonText.value AS stepConfirmButtonText,
                            previousStepLinkText.value AS previousStepLinkText,
	                        
	                        # Group
	                        fieldGroup.id AS groupId,
	                        fieldGroup.title AS groupTitle,
	                        groupType.value AS groupType,
	                        CONCAT_WS('', groupHeader.value, groupHeader.long_value) AS groupHeader,
	                        CONCAT_WS('', groupFooter.value, groupFooter.long_value) AS groupFooter,
	                        
	                        # Field
	                        field.id AS fieldId,
	                        field.title AS fieldTitle,
	                        fieldFormId.value AS fieldFormId,
	                        fieldLabel.value AS fieldLabel,
	                        fieldPlaceholder.value AS fieldPlaceholder,
	                        fieldType.value AS fieldType,
	                        fieldInputType.value AS fieldInputType,
	                        fieldMandatory.value AS fieldMandatory,
	                        fieldPattern.value AS fieldPattern,
	                        fieldVisible.value AS fieldVisible,
	                        fieldErrorMessage.value AS fieldErrorMessage,

                            # Field values
	                        IF(NULLIF(fieldValues.`key`, '') IS NULL AND NULLIF(fieldValues.value, '') IS NULL, NULL, JSON_OBJECTAGG(IFNULL(fieldValues.`key`, ''), IFNULL(fieldValues.value, ''))) AS fieldValues
                        FROM {WiserTableNames.WiserItem} AS orderProcess

                        # Step
                        JOIN {WiserTableNames.WiserItemLink} AS linkToStep ON linkToStep.destination_item_id = orderProcess.id AND linkToStep.type = {Constants.StepToProcessLinkType}
                        JOIN {WiserTableNames.WiserItem} AS step ON step.id = linkToStep.item_id AND step.entity_type = '{Constants.StepEntityType}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepHeader ON stepHeader.item_id = step.id AND stepHeader.`key` = '{Constants.StepHeaderProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepFooter ON stepFooter.item_id = step.id AND stepFooter.`key` = '{Constants.StepFooterProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepConfirmButtonText ON stepConfirmButtonText.item_id = step.id AND stepConfirmButtonText.`key` = '{Constants.StepConfirmButtonTextProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS previousStepLinkText ON previousStepLinkText.item_id = step.id AND previousStepLinkText.`key` = '{Constants.StepPreviousStepLinkTextProperty}'

                        # Group
                        JOIN {WiserTableNames.WiserItemLink} AS linkToGroup ON linkToGroup.destination_item_id = step.id AND linkToGroup.type = {Constants.GroupToStepLinkType}
                        JOIN {WiserTableNames.WiserItem} AS fieldGroup ON fieldGroup.id = linkToGroup.item_id AND fieldGroup.entity_type = '{Constants.GroupEntityType}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS groupType ON groupType.item_id = fieldGroup.id AND groupType.`key` = '{Constants.GroupTypeProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS groupHeader ON groupHeader.item_id = fieldGroup.id AND groupHeader.`key` = '{Constants.GroupHeaderProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS groupFooter ON groupFooter.item_id = fieldGroup.id AND groupFooter.`key` = '{Constants.GroupFooterProperty}'

                        # Fields
                        LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToField ON linkToField.destination_item_id = fieldGroup.id AND linkToField.type = {Constants.FieldToGroupLinkType}
                        LEFT JOIN {WiserTableNames.WiserItem} AS field ON field.id = linkToField.item_id AND field.entity_type = '{Constants.FormFieldEntityType}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldFormId ON fieldFormId.item_id = field.id AND fieldFormId.`key` = '{Constants.FieldIdProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldLabel ON fieldLabel.item_id = field.id AND fieldLabel.`key` = '{Constants.FieldLabelProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldPlaceholder ON fieldPlaceholder.item_id = field.id AND fieldPlaceholder.`key` = '{Constants.FieldPlaceholderProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldType ON fieldType.item_id = field.id AND fieldType.`key` = '{Constants.FieldTypeProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldInputType ON fieldInputType.item_id = field.id AND fieldInputType.`key` = '{Constants.FieldInputTypeProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldMandatory ON fieldMandatory.item_id = field.id AND fieldMandatory.`key` = '{Constants.FieldMandatoryProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldPattern ON fieldPattern.item_id = field.id AND fieldPattern.`key` = '{Constants.FieldValidationPatternProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldVisible ON fieldVisible.item_id = field.id AND fieldVisible.`key` = '{Constants.FieldVisibilityProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldErrorMessage ON fieldErrorMessage.item_id = field.id AND fieldErrorMessage.`key` = '{Constants.FieldErrorMessageProperty}'
                        
                        # Field values
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldValues ON fieldValues.item_id = field.id AND fieldValues.groupname = '{Constants.FieldValuesGroupName}'

                        WHERE orderProcess.id = ?id
                        AND orderProcess.entity_type = '{Constants.OrderProcessEntityType}'

                        GROUP BY step.id, fieldGroup.id, field.id
                        ORDER BY linkToStep.ordering ASC, linkToGroup.ordering ASC, linkToField.ordering ASC";

            databaseConnection.AddParameter("id", orderProcessId);
            var dataTable = await databaseConnection.GetAsync(query);
            
            foreach (DataRow dataRow in dataTable.Rows)
            {
                // Get the step if it already exists in the results, or create a new one if it doesn't.
                var stepId = dataRow.Field<ulong>("stepId");
                var step = results.SingleOrDefault(s => s.Id == stepId);
                if (step == null)
                {
                    step = new OrderProcessStepModel
                    {
                        Id = stepId,
                        Title = dataRow.Field<string>("stepTitle"),
                        Header = dataRow.Field<string>("stepHeader"),
                        Footer = dataRow.Field<string>("stepFooter"),
                        ConfirmButtonText = dataRow.Field<string>("stepConfirmButtonText"),
                        PreviousStepLinkText = dataRow.Field<string>("previousStepLinkText"),
                        Groups = new List<OrderProcessGroupModel>()
                    };

                    results.Add(step);
                }

                // Get the group if it already exists in the current step, or create a new one if it doesn't.
                var groupId = dataRow.Field<ulong>("groupId");
                var group = step.Groups.SingleOrDefault(g => g.Id == groupId);
                if (group == null)
                {
                    group = new OrderProcessGroupModel
                    {
                        Id = groupId,
                        Title = dataRow.Field<string>("groupTitle"),
                        Type = EnumHelpers.ToEnum<OrderProcessGroupTypes>(dataRow.Field<string>("groupType") ?? "Fields"),
                        Header = dataRow.Field<string>("groupHeader"),
                        Footer = dataRow.Field<string>("groupFooter"),
                        Fields = new List<OrderProcessFieldModel>()
                    };

                    step.Groups.Add(group);
                }

                var fieldId = dataRow.Field<ulong?>("fieldId");
                if (fieldId is null or 0)
                {
                    // If we have no (more) fields, we stop here.
                    continue;
                }

                // Get the field values, for fields that have multiple choises.
                var fieldValues = dataRow.Field<string>("fieldValues");

                // The query will generate a JSON object with all keys and values, we can deserialize that to a dictionary.
                var fieldValuesDictionary = String.IsNullOrWhiteSpace(fieldValues) ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(fieldValues) ?? new Dictionary<string, string>();

                // The user can leave either the key or the value empty, so make sure we always have a key and value, even if they're the same.
                fieldValuesDictionary = fieldValuesDictionary.ToDictionary(k => String.IsNullOrWhiteSpace(k.Key) ? k.Value : k.Key, k => String.IsNullOrWhiteSpace(k.Value) ? k.Key : k.Value);

                // Order the dictionary.
                fieldValuesDictionary = fieldValuesDictionary.OrderBy(k =>
                {
                    var split = k.Value.Split('|');
                    if (split.Length < 2 || !Int32.TryParse(split[1], out var ordering))
                    {
                        return k.Value;
                    }
                    
                    return ordering.ToString().PadLeft(11, '0');
                }).ToDictionary(k => k.Key, k => k.Value);

                // Strip the ordering numbers from the values.
                fieldValuesDictionary = fieldValuesDictionary.ToDictionary(k => k.Key.Contains("|") ? k.Key[..k.Key.LastIndexOf("|", StringComparison.Ordinal)] : k.Key, k => k.Value.Contains("|") ? k.Value[..k.Value.LastIndexOf("|", StringComparison.Ordinal)] : k.Value);
                
                // Create a new field model and add it to the current group.
                var field = new OrderProcessFieldModel
                {
                    Id = fieldId.Value,
                    Title = dataRow.Field<string>("fieldTitle"),
                    FieldId = dataRow.Field<string>("fieldFormId"),
                    Label = dataRow.Field<string>("fieldLabel"),
                    Placeholder = dataRow.Field<string>("fieldPlaceholder"),
                    Type = EnumHelpers.ToEnum<OrderProcessFieldTypes>(dataRow.Field<string>("fieldType") ?? "Input"),
                    Values = fieldValuesDictionary,
                    Mandatory = dataRow.Field<string>("fieldMandatory") == "1",
                    Pattern = dataRow.Field<string>("fieldPattern"),
                    Visibility = EnumHelpers.ToEnum<OrderProcessFieldVisibilityTypes>(dataRow.Field<string>("fieldVisible") ?? "Always"),
                    InputFieldType = EnumHelpers.ToEnum<OrderProcessInputTypes>(dataRow.Field<string>("fieldInputType") ?? "text"),
                    ErrorMessage = dataRow.Field<string>("fieldErrorMessage")
                };

                group.Fields.Add(field);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId, UserCookieDataModel loggedInUser = null)
        {
            if (orderProcessId == 0)
            {
                throw new ArgumentNullException(nameof(orderProcessId));
            }

            var query = $@"SELECT 
	                            paymentMethod.id AS paymentMethodId,
	                            paymentMethod.title AS paymentMethodTitle,
	                            paymentServiceProvider.id AS paymentServiceProviderId,
	                            paymentServiceProvider.title AS paymentServiceProviderTitle,
	                            paymentMethodFee.value AS paymentMethodFee,
	                            paymentMethodVisibility.value AS paymentMethodVisibility
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            JOIN {WiserTableNames.WiserItemLink} AS paymentMethodLink ON paymentMethodLink.destination_item_id = orderProcess.id AND paymentMethodLink.type = {Constants.PaymentMethodToOrderProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS paymentMethod ON paymentMethod.id = paymentMethodLink.item_id AND paymentMethod.entity_type = '{Constants.PaymentMethodEntityType}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'

                            # Other payment method properties
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            WHERE orderProcess.id = ?id
                            AND orderProcess.entity_type = '{Constants.OrderProcessEntityType}'";

            databaseConnection.AddParameter("id", orderProcessId);
            var dataTable = await databaseConnection.GetAsync(query);
            var results = dataTable.Rows.Cast<DataRow>().Select(DataRowToPaymentMethodSettingsModel).ToList();

            if (loggedInUser == null)
            {
                return results;
            }

            // Only get the payment methods that the user can see.
            results = results.Where(paymentMethod =>
            {
                // Check if we need to skip this field.
                return paymentMethod.Visibility switch
                {
                    OrderProcessFieldVisibilityTypes.Always => true,
                    OrderProcessFieldVisibilityTypes.WhenNotLoggedIn => loggedInUser.UserId == 0,
                    OrderProcessFieldVisibilityTypes.WhenLoggedIn => loggedInUser.UserId > 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod.Visibility), paymentMethod.Visibility.ToString())
                };
            }).ToList();

            return results;
        }

        /// <inheritdoc />
        public async Task<PaymentMethodSettingsModel> GetPaymentMethodAsync(ulong paymentMethodId)
        {
            if (paymentMethodId == 0)
            {
                throw new ArgumentNullException(nameof(paymentMethodId));
            }
            
            var query = $@"SELECT 
	                            paymentMethod.id AS paymentMethodId,
	                            paymentMethod.title AS paymentMethodTitle,
	                            paymentServiceProvider.id AS paymentServiceProviderId,
	                            paymentServiceProvider.title AS paymentServiceProviderTitle,
	                            paymentMethodFee.value AS paymentMethodFee,
	                            paymentMethodVisibility.value AS paymentMethodVisibility
                            FROM {WiserTableNames.WiserItem} AS paymentMethod

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'

                            # Other payment method properties
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            WHERE paymentMethod.id = ?id
                            AND paymentMethod.entity_type = '{Constants.PaymentMethodEntityType}'";

            databaseConnection.AddParameter("id", paymentMethodId);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var dataRow = dataTable.Rows[0];
            var result = DataRowToPaymentMethodSettingsModel(dataRow);

            return result;
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(ulong orderProcessId)
        {
            return await HandlePaymentRequestAsync(this, orderProcessId);
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId)
        {
            // Retrieve baskets.
            var shoppingBaskets = await shoppingBasketsService.GetShoppingBasketsAsync();
            var selectedPaymentMethodId = shoppingBaskets.First().Main.GetDetailValue<ulong>(Constants.PaymentMethodProperty);
            var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);

            // Build the fail URL.
            var failUrl = new UriBuilder(orderProcessSettings.FixedUrl);
            var queryString = HttpUtility.ParseQueryString(failUrl.Query);
            queryString[Constants.ErrorFromPaymentOutRequestKey] = "true";

            if (orderProcessSettings.AmountOfSteps > 1)
            {
                queryString[Constants.ActiveStepRequestKey] = orderProcessSettings.AmountOfSteps.ToString();
            }

            failUrl.Query = queryString.ToString();
            
            // Check if we have a valid payment method.
            if (selectedPaymentMethodId == 0)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = failUrl.ToString(),
                    ErrorMessage = $"Invalid payment method '{selectedPaymentMethodId}'"
                };
            }

            var paymentMethodSettings = await orderProcessesService.GetPaymentMethodAsync(selectedPaymentMethodId);
            if (paymentMethodSettings == null)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = failUrl.ToString(),
                    ErrorMessage = $"Invalid payment method '{selectedPaymentMethodId}'"
                };
            }
            
            // Get current user.
            var user = await accountsService.GetUserDataFromCookieAsync();
            var userDetails = user.UserId > 0 ? await wiserItemsService.GetItemDetailsAsync(user.UserId) : new WiserItemModel();
            
            // Double check that we received a valid payment method.
            var availablePaymentMethods = await orderProcessesService.GetPaymentMethodsAsync(orderProcessId, user);
            
            if (availablePaymentMethods == null || availablePaymentMethods.All(p => p.Id != selectedPaymentMethodId))
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = failUrl.ToString(),
                    ErrorMessage = "This user is not allowed to pay"
                };
            }
            
            // Convert baskets to concept orders.
            var orderId = 0UL;
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var conceptOrders = new List<(WiserItemModel Main, List<WiserItemModel> Lines)>();
            foreach (var (main, lines) in shoppingBaskets)
            {
                var (conceptOrderId, conceptOrder, conceptOrderLines) = await shoppingBasketsService.MakeConceptOrderFromBasketAsync(main, lines, basketSettings);

                conceptOrders.Add((conceptOrder, conceptOrderLines));

                orderId = conceptOrderId;
            }
            
            // Generate invoice number.
            var invoiceNumber = "";
            var invoiceNumberQuery = (await templatesService.GetTemplateAsync(name: Constants.InvoiceNumberQueryTemplate, type: TemplateTypes.Query))?.Content;
            if (!String.IsNullOrWhiteSpace(invoiceNumberQuery))
            {
                invoiceNumberQuery = invoiceNumberQuery.Replace("{oid}", orderId.ToString());
                var getInvoiceNumberResult = await databaseConnection.GetAsync(invoiceNumberQuery);
                if (getInvoiceNumberResult.Rows.Count > 0)
                {
                    invoiceNumber = Convert.ToString(getInvoiceNumberResult.Rows[0][0]);
                }
            }

            if (String.IsNullOrWhiteSpace(invoiceNumber))
            {
                invoiceNumber = orderId.ToString();
            }
            
            var uniquePaymentNumber = $"{invoiceNumber}-{DateTime.Now:yyyyMMddHHmmss}";

            // Check if the order is a test order.
            var isTestOrder = gclSettings.Environment.InList(Environments.Test, Environments.Development);
            
            // Save data to the concept order(s).
            foreach (var (main, lines) in conceptOrders)
            {
                main.SetDetail(Constants.PaymentMethodProperty, paymentMethodSettings.Id);
                main.SetDetail(Constants.PaymentMethodNameProperty, paymentMethodSettings.Title);
                main.SetDetail(Constants.PaymentProviderProperty, paymentMethodSettings.PaymentServiceProvider.Id);
                main.SetDetail(Constants.PaymentProviderNameProperty, paymentMethodSettings.PaymentServiceProvider.Title);
                main.SetDetail(Constants.UniquePaymentNumberProperty, uniquePaymentNumber);
                main.SetDetail(Constants.InvoiceNumberProperty, invoiceNumber);
                main.SetDetail(Constants.LanguageCodeProperty, languagesService?.CurrentLanguageCode ?? "");
                main.SetDetail(Constants.IsTestOrderProperty, isTestOrder.ToString());
                await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
            }
            
            var convertConceptOrderToOrder = paymentMethodSettings.PaymentServiceProvider.OrdersCanBeSetDirectoryToFinished;
            
            // Increment use count of redeemed coupons.
            foreach (var (main, lines) in conceptOrders)
            {
                foreach (var basketLine in shoppingBasketsService.GetLines(lines, "coupon"))
                {
                    var couponItemId = basketLine.GetDetailValue<ulong>(ShoppingBasket.Models.Constants.ConnectedItemIdProperty);
                    if (couponItemId == 0)
                    {
                        continue;
                    }

                    var couponItem = await wiserItemsService.GetItemDetailsAsync(couponItemId);
                    if (couponItem is not { Id: > 0 })
                    {
                        continue;
                    }

                    var totalBasketPrice = await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, lineType: "product");
                    await shoppingBasketsService.UseCouponAsync(couponItem, totalBasketPrice);
                }
            }

            // TODO: Call "TransactionBeforeOut" site function.
            
            if (!convertConceptOrderToOrder && paymentMethodSettings.PaymentServiceProvider.SkipPaymentWhenOrderAmountEqualsZero)
            {
                var totalPrice = 0M;
                foreach (var (main, lines) in conceptOrders)
                {
                    totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings);
                }

                if (totalPrice == 0M)
                {
                    convertConceptOrderToOrder = true;
                }
            }
            
            if (convertConceptOrderToOrder)
            {
                foreach (var (main, _) in conceptOrders)
                {
                    await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                    // TODO: Call "TransactionFinished" site function.
                }

                return new PaymentRequestResult
                {
                    Successful = true,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.SuccessUrl
                };
            }
            
            // Get the correct service based on name.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentMethodSettings.PaymentServiceProvider.Title);
            paymentServiceProviderService.LogPaymentActions = true;

            if (convertConceptOrderToOrder)
            {
                // TODO
                //await ProcessStatusUpdateAsync(conceptOrders, "Success", true, convertConceptOrderToOrder);
            }

            // TODO
            //return await paymentServiceProviderService.HandlePaymentRequestAsync(conceptOrders, userDetails, paymentMethod, invoiceNumber);
            return new PaymentRequestResult();
        }

        private static PaymentMethodSettingsModel DataRowToPaymentMethodSettingsModel(DataRow dataRow)
        {
            Decimal.TryParse(dataRow.Field<string>("paymentMethodFee")?.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out var fee);
            var result = new PaymentMethodSettingsModel
            {
                Id = dataRow.Field<ulong>("paymentMethodId"),
                Title = dataRow.Field<string>("paymentMethodTitle"),
                Fee = fee,
                Visibility = EnumHelpers.ToEnum<OrderProcessFieldVisibilityTypes>(dataRow.Field<string>("paymentMethodVisibility") ?? "Always"),
                PaymentServiceProvider = new PaymentServiceProviderSettingsModel
                {
                    Id = dataRow.Field<ulong>("paymentServiceProviderId"),
                    Title = dataRow.Field<string>("paymentServiceProviderTitle")
                }
            };
            return result;
        }
    }
}
