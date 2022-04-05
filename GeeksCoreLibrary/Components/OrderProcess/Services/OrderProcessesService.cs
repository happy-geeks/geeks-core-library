using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
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
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    /// <inheritdoc cref="IOrderProcessesService" />
    public class OrderProcessesService : IOrderProcessesService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IAccountsService accountsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ILanguagesService languagesService;
        private readonly ITemplatesService templatesService;
        private readonly IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory;
        private readonly ICommunicationsService communicationsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of <see cref="OrderProcessesService"/>.
        /// </summary>
        public OrderProcessesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, IShoppingBasketsService shoppingBasketsService, IAccountsService accountsService, IWiserItemsService wiserItemsService, ILanguagesService languagesService, ITemplatesService templatesService, IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory, ICommunicationsService communicationsService, IHttpContextAccessor httpContextAccessor)
        {
            this.databaseConnection = databaseConnection;
            this.shoppingBasketsService = shoppingBasketsService;
            this.accountsService = accountsService;
            this.wiserItemsService = wiserItemsService;
            this.languagesService = languagesService;
            this.templatesService = templatesService;
            this.paymentServiceProviderServiceFactory = paymentServiceProviderServiceFactory;
            this.communicationsService = communicationsService;
            this.httpContextAccessor = httpContextAccessor;
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
                                COUNT(step.id) AS amountOfSteps,
                                emailAddressField.value AS emailAddressField,
                                IF(statusUpdateTemplate.value IS NULL OR statusUpdateTemplate.value = '', '0', statusUpdateTemplate.value) AS statusUpdateTemplate,
                                IF(statusUpdateWebShopTemplate.value IS NULL OR statusUpdateWebShopTemplate.value = '', '0', statusUpdateWebShopTemplate.value) AS statusUpdateWebShopTemplate,
                                IF(statusUpdateAttachmentTemplate.value IS NULL OR statusUpdateAttachmentTemplate.value = '', '0', statusUpdateAttachmentTemplate.value) AS statusUpdateAttachmentTemplate
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            JOIN {WiserTableNames.WiserItemLink} AS linkToStep ON linkToStep.destination_item_id = orderProcess.id AND linkToStep.type = {Constants.StepToProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS step ON step.id = linkToStep.item_id AND step.entity_type = '{Constants.StepEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = orderProcess.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS emailAddressField ON emailAddressField.item_id = orderProcess.id AND emailAddressField.`key` = '{Constants.OrderProcessEmailAddressFieldProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateTemplate ON statusUpdateTemplate.item_id = orderProcess.id AND statusUpdateTemplate.`key` = '{Constants.OrderProcessStatusUpdateTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateWebShopTemplate ON statusUpdateWebShopTemplate.item_id = orderProcess.id AND statusUpdateWebShopTemplate.`key` = '{Constants.OrderProcessStatusUpdateWebShopTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateAttachmentTemplate ON statusUpdateAttachmentTemplate.item_id = orderProcess.id AND statusUpdateAttachmentTemplate.`key` = '{Constants.StatusUpdateMailAttachmentProperty}'
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
                return new OrderProcessSettingsModel();
            }

            var firstRow = dataTable.Rows[0];
            return new OrderProcessSettingsModel
            {
                Id = firstRow.Field<ulong>("id"),
                Title = firstRow.Field<string>("name"),
                FixedUrl = firstRow.Field<string>("fixedUrl"),
                AmountOfSteps = Convert.ToInt32(firstRow["amountOfSteps"]),
                EmailAddressProperty = firstRow.Field<string>("emailAddressField"),
                StatusUpdateMailTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateTemplate")),
                StatusUpdateMailWebShopTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateWebShopTemplate")),
                StatusUpdateMailAttachmentTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateAttachmentTemplate"))
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
                return new OrderProcessSettingsModel();
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
	                        groupCssClass.value AS groupCssClass,
	                        
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
                            fieldCssClass.value AS fieldCssClass,
                            fieldSaveTo.value AS fieldSaveTo,

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
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS groupCssClass ON groupCssClass.item_id = fieldGroup.id AND groupCssClass.`key` = '{Constants.GroupCssClassProperty}'

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
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldCssClass ON fieldCssClass.item_id = field.id AND fieldCssClass.`key` = '{Constants.FieldCssClassProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldSaveTo ON fieldSaveTo.item_id = field.id AND fieldSaveTo.`key` = '{Constants.FieldSaveToProperty}'
                        
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
                        CssClass = dataRow.Field<string>("groupCssClass"),
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
                    ErrorMessage = dataRow.Field<string>("fieldErrorMessage"),
                    CssClass = dataRow.Field<string>("fieldCssClass"),
                    SaveTo = dataRow.Field<string>("fieldSaveTo")
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
	                            paymentMethodVisibility.value AS paymentMethodVisibility,
	                            paymentMethodExternalName.value AS paymentMethodExternalName,

                                paymentServiceProviderLogAllRequests.value AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.value AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.value AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,
                                paymentServiceProviderSuccessUrl.value AS paymentServiceProviderSuccessUrl
                            FROM {WiserTableNames.WiserItem} AS orderProcess

                            # Payment method
                            JOIN {WiserTableNames.WiserItemLink} AS paymentMethodLink ON paymentMethodLink.destination_item_id = orderProcess.id AND paymentMethodLink.type = {Constants.PaymentMethodToOrderProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS paymentMethod ON paymentMethod.id = paymentMethodLink.item_id AND paymentMethod.entity_type = '{Constants.PaymentMethodEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodExternalName ON paymentMethodExternalName.item_id = paymentMethod.id AND paymentMethodExternalName.`key` = '{Constants.PaymentMethodExternalNameProperty}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSuccessUrl ON paymentServiceProviderSuccessUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderSuccessUrl.`key` = '{Constants.PaymentServiceProviderSuccessUrlProperty}'

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
	                            paymentMethodVisibility.value AS paymentMethodVisibility,
	                            paymentMethodExternalName.value AS paymentMethodExternalName,

                                paymentServiceProviderLogAllRequests.value AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.value AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.value AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,
                                paymentServiceProviderSuccessUrl.value AS paymentServiceProviderSuccessUrl
                            FROM {WiserTableNames.WiserItem} AS paymentMethod
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodExternalName ON paymentMethodExternalName.item_id = paymentMethod.id AND paymentMethodExternalName.`key` = '{Constants.PaymentMethodExternalNameProperty}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSuccessUrl ON paymentServiceProviderSuccessUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderSuccessUrl.`key` = '{Constants.PaymentServiceProviderSuccessUrlProperty}'

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
            var steps = await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);

            // Build the fail URL.
            var failUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)) 
            {
                Path = orderProcessSettings.FixedUrl
            };
            var queryString = HttpUtility.ParseQueryString(failUrl.Query);
            queryString[Constants.ErrorFromPaymentOutRequestKey] = "true";

            if (orderProcessSettings.AmountOfSteps > 1)
            {
                var stepForPaymentErrors = orderProcessSettings.AmountOfSteps;
                var stepWithPaymentMethods = steps.LastOrDefault(step => step.Groups.Any(group => group.Type == OrderProcessGroupTypes.PaymentMethods));
                if (stepWithPaymentMethods != null)
                {
                    stepForPaymentErrors = steps.IndexOf(stepWithPaymentMethods) + 1;
                }

                queryString[Constants.ActiveStepRequestKey] = stepForPaymentErrors.ToString();
            }

            failUrl.Query = queryString.ToString()!;
            
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

            paymentMethodSettings.PaymentServiceProvider.FailUrl = failUrl.ToString();
            
            // Build the webhook URL.
            var webhookUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext))
            {
                Path = Constants.PaymentInPage
            };
            queryString = HttpUtility.ParseQueryString(failUrl.Query);
            queryString[Constants.OrderProcessIdRequestKey] = orderProcessId.ToString();
            queryString[Constants.SelectedPaymentMethodRequestKey] = paymentMethodSettings.Id.ToString();
            webhookUrl.Query = queryString.ToString()!;
            paymentMethodSettings.PaymentServiceProvider.WebhookUrl = webhookUrl.ToString();
            
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
                invoiceNumberQuery = invoiceNumberQuery.ReplaceCaseInsensitive("{oid}", orderId.ToString()).ReplaceCaseInsensitive("{orderId}", orderId.ToString());
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
            
            var convertConceptOrderToOrder = paymentMethodSettings.PaymentServiceProvider.OrdersCanBeSetDirectlyToFinished;
            
            // Increment use count of redeemed coupons.
            foreach (var (main, lines) in conceptOrders)
            {
                foreach (var basketLine in shoppingBasketsService.GetLines(lines, Constants.OrderLineCouponType))
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

                    var totalBasketPrice = await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, lineType: Constants.OrderLineProductType);
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
            paymentServiceProviderService.LogPaymentActions = paymentMethodSettings.PaymentServiceProvider.LogAllRequests;

            if (convertConceptOrderToOrder)
            {
                await HandlePaymentStatusUpdateAsync(orderProcessSettings, conceptOrders, "Success", true, convertConceptOrderToOrder);
            }

            return await paymentServiceProviderService.HandlePaymentRequestAsync(conceptOrders, userDetails, paymentMethodSettings, uniquePaymentNumber);
        }

        /// <inheritdoc />
        public async Task<bool> HandlePaymentStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
        {
            return await HandlePaymentStatusUpdateAsync(this, orderProcessSettings, conceptOrders, newStatus, isSuccessfulStatus, convertConceptOrderToOrder);
        }

        /// <inheritdoc />
        public async Task<bool> HandlePaymentStatusUpdateAsync(IOrderProcessesService orderProcessesService, OrderProcessSettingsModel orderProcessSettings, ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
        {
            var mailsToSendToUser = new List<SingleCommunicationModel>();
            var mailsToSendToMerchant = new List<SingleCommunicationModel>();
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var emailContent = "";
            var emailSubject = "";
            var attachmentTemplate = "";
            var userEmailAddress = "";
            var merchantEmailAddress = "";
            var bcc = "";
            var senderAddress = "";
            var senderName = "";
            var replyToAddress = "";
            var replyToName = "";

            var attachments = new List<string>();

            var orderIsFinished = false;

            foreach (var (main, lines) in conceptOrders)
            {
                orderIsFinished = main.EntityType == Constants.OrderEntityType;

                // Get email content and addresses.
                var mailValues = await GetMailValuesAsync(orderProcessSettings, main, lines);
                if (mailValues != null)
                {
                    emailContent = mailValues.Content;
                    emailSubject = mailValues.Subject;
                    userEmailAddress = mailValues.User?.Address ?? "";
                    merchantEmailAddress = mailValues.Merchant?.Address ?? "";
                    bcc = mailValues.Bcc;
                    senderAddress = mailValues.Sender?.Address ?? "";
                    senderName = mailValues.Sender?.DisplayName ?? "";
                    replyToAddress = mailValues.ReplyTo?.Address ?? "";
                    replyToName = mailValues.ReplyTo?.DisplayName ?? "";
                }

                // Get email content specifically for the merchant.
                mailValues = await GetMailValuesAsync(orderProcessSettings, main, lines, true);
                string merchantEmailContent;
                string merchantEmailSubject;
                string merchantBcc;
                string merchantSenderAddress;
                string merchantSenderName;
                string merchantReplyToAddress;
                string merchantReplyToName;
                if (mailValues != null)
                {
                    merchantEmailContent = mailValues.Content;
                    merchantEmailSubject = mailValues.Subject;
                    merchantEmailAddress = mailValues.Merchant.Address;
                    merchantBcc = mailValues.Bcc;
                    merchantSenderAddress = mailValues.Sender?.Address ?? "";
                    merchantSenderName = mailValues.Sender?.DisplayName ?? "";
                    merchantReplyToAddress = mailValues.ReplyTo?.Address ?? "";
                    merchantReplyToName = mailValues.ReplyTo?.DisplayName ?? "";
                }
                else
                {
                    merchantEmailContent = emailContent;
                    merchantEmailSubject = emailSubject;
                    merchantBcc = bcc;
                    merchantSenderAddress = senderAddress;
                    merchantSenderName = senderName;
                    merchantReplyToAddress = replyToAddress;
                    merchantReplyToName = replyToName;
                }

                // Get email content specifically for the attachment.
                mailValues = await GetMailValuesAsync(orderProcessSettings, main, lines, false, true);
                if (mailValues != null)
                {
                    attachmentTemplate = mailValues.Content;
                }

                main.SetDetail(Constants.PaymentHistoryProperty, $"{DateTime.Now:yyyyMMddHHmmss} - {newStatus}", true);

                // If order is not finished yet and the payment was successful.
                if (!orderIsFinished && isSuccessfulStatus && convertConceptOrderToOrder)
                {
                    await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                }

                if (!String.IsNullOrWhiteSpace(userEmailAddress) && !String.IsNullOrWhiteSpace(emailContent))
                {
                    mailsToSendToUser.Add(new SingleCommunicationModel
                    {
                        Content = emailContent,
                        Subject = emailSubject,
                        Receivers = new List<CommunicationReceiverModel> { new() { Address = userEmailAddress } },
                        Bcc = new List<string> { bcc },
                        ReplyTo = replyToAddress,
                        ReplyToName = replyToName,
                        Sender = senderAddress,
                        SenderName = senderName
                    });
                }

                if (!String.IsNullOrWhiteSpace(merchantEmailAddress) && !String.IsNullOrWhiteSpace(merchantEmailContent))
                {
                    mailsToSendToMerchant.Add(new SingleCommunicationModel
                    {
                        Content = merchantEmailContent,
                        Subject = merchantEmailSubject,
                        Receivers = new List<CommunicationReceiverModel> { new() { Address = merchantEmailAddress } },
                        Bcc = new List<string> { merchantBcc },
                        ReplyTo = merchantReplyToAddress,
                        ReplyToName = merchantReplyToName,
                        Sender = merchantSenderAddress,
                        SenderName = merchantSenderName
                    });
                }
            }

            // TODO: Add customer mail attachments.

            if (!orderIsFinished)
            {
                foreach (var mailToSend in mailsToSendToUser)
                {
                    if (isSuccessfulStatus && mailToSend.Receivers.Any() && !String.IsNullOrWhiteSpace(mailToSend.Content))
                    {
                        await communicationsService.SendEmailAsync(mailToSend);
                    }
                }
                
                foreach (var mailToSend in mailsToSendToMerchant)
                {
                    if (!mailToSend.Receivers.Any() || String.IsNullOrWhiteSpace(mailToSend.Content))
                    {
                        continue;
                    }

                    await communicationsService.SendEmailAsync(mailToSend);
                }
            }

            return true;
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
                ExternalName = dataRow.Field<string>("paymentMethodExternalName"),
                PaymentServiceProvider = new PaymentServiceProviderSettingsModel
                {
                    Id = dataRow.Field<ulong>("paymentServiceProviderId"),
                    Title = dataRow.Field<string>("paymentServiceProviderTitle"),
                    SuccessUrl = dataRow.Field<string>("paymentServiceProviderSuccessUrl"),
                    LogAllRequests = dataRow.Field<string>("paymentServiceProviderLogAllRequests") == "1",
                    OrdersCanBeSetDirectlyToFinished = dataRow.Field<string>("paymentServiceProviderSetOrdersDirectlyToFinished") == "1",
                    SkipPaymentWhenOrderAmountEqualsZero = dataRow.Field<string>("paymentServiceProviderSkipWhenOrderAmountEqualsZero") == "1",
                }
            };

            if (String.IsNullOrEmpty(result.ExternalName))
            {
                result.ExternalName = result.Title;
            }

            return result;
        }

        private async Task<EmailValues> GetMailValuesAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel conceptOrder, List<WiserItemModel> conceptOrderLines, bool forMerchantMail = false, bool forAttachment = false)
        {
            var userEmailAddress = "";

            var linkedUsers = await wiserItemsService.GetLinkedItemDetailsAsync(conceptOrder.Id, reverse: true);

            ulong templateItemId;
            string templatePropertyName;
            if (forAttachment)
            {
                templatePropertyName = Constants.StatusUpdateMailAttachmentProperty;
                templateItemId = orderProcessSettings.StatusUpdateMailAttachmentTemplateId;
            }
            else if (forMerchantMail)
            {
                templatePropertyName = Constants.StatusUpdateMailWebShopProperty;
                templateItemId = orderProcessSettings.StatusUpdateMailWebShopTemplateId;
            }
            else
            {
                templatePropertyName = Constants.StatusUpdateMailToConsumerProperty;
                templateItemId = orderProcessSettings.StatusUpdateMailTemplateId;
            }
            
            if (!String.IsNullOrWhiteSpace(conceptOrder.GetDetailValue(templatePropertyName)) && UInt64.TryParse(conceptOrder.GetDetailValue(templatePropertyName), out var idFromOrder) && idFromOrder > 0)
            {
                templateItemId = idFromOrder;
            }

            if (templateItemId == 0)
            {
                return null;
            }

            var languageCode = conceptOrder.GetDetailValue(Constants.LanguageCodeProperty);
            var user = await accountsService.GetUserDataFromCookieAsync();
            var templateItem = await wiserItemsService.GetItemDetailsAsync(templateItemId, languageCode: languageCode, userId: user.UserId) ?? await wiserItemsService.GetItemDetailsAsync(templateItemId, userId: user.UserId);

            var templateContent = templateItem.GetDetailValue(Constants.MailTemplateBodyProperty) ?? "";
            var templateSubject = templateItem.GetDetailValue(Constants.MailTemplateSubjectProperty) ?? "";
            var merchantEmailAddress = templateItem.GetDetailValue(Constants.MailTemplateToAddressProperty) ?? "";
            var bcc = templateItem.GetDetailValue(Constants.MailTemplateBccProperty);
            var replyToAddress = templateItem.GetDetailValue(Constants.MailTemplateReplyToAddressProperty);
            var replyToName = templateItem.GetDetailValue(Constants.MailTemplateReplyToNameProperty);
            var senderAddress = templateItem.GetDetailValue(Constants.MailTemplateSenderEmailProperty);
            var senderName = templateItem.GetDetailValue(Constants.MailTemplateSenderNameProperty);

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            // Do subject replacements.
            templateSubject = await shoppingBasketsService.ReplaceBasketInTemplateAsync(conceptOrder, conceptOrderLines, basketSettings, templateSubject, isForConfirmationEmail: true);

            // Do basket replacements.
            if (linkedUsers.Count > 0)
            {
                templateContent = await shoppingBasketsService.ReplaceBasketInTemplateAsync(conceptOrder, conceptOrderLines, basketSettings, templateContent, userDetails: linkedUsers.Last().GetSortedList(), isForConfirmationEmail: true);
                if (linkedUsers.Last().ContainsDetail(orderProcessSettings.EmailAddressProperty))
                {
                    userEmailAddress = linkedUsers.Last().GetDetailValue(orderProcessSettings.EmailAddressProperty);
                }
            }
            else
            {
                templateContent = await shoppingBasketsService.ReplaceBasketInTemplateAsync(conceptOrder, conceptOrderLines, basketSettings, templateContent, isForConfirmationEmail: true);
            }

            //get customer basket email instead of the potentially linked user email address
            if (!String.IsNullOrWhiteSpace(orderProcessSettings.EmailAddressProperty) && !String.IsNullOrWhiteSpace(conceptOrder.GetDetailValue(orderProcessSettings.EmailAddressProperty)))
            {
                userEmailAddress = conceptOrder.GetDetailValue(orderProcessSettings.EmailAddressProperty);
            }

            return new EmailValues
            {
                Content = templateContent,
                Subject = templateSubject,
                User = new CommunicationReceiverModel
                {
                    Address = userEmailAddress
                },
                Merchant = new CommunicationReceiverModel
                {
                    Address = merchantEmailAddress
                },
                Bcc = bcc,
                ReplyTo = new CommunicationReceiverModel
                {
                    Address = replyToAddress,
                    DisplayName = replyToName
                },
                Sender = new CommunicationReceiverModel
                {
                    Address = senderAddress,
                    DisplayName = senderName
                }
            };
        }
    }
}
