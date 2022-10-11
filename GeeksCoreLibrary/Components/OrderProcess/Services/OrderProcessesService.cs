using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<OrderProcessesService> logger;
        private readonly IObjectsService objectsService;
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly GclSettings gclSettings;
        private readonly IMeasurementProtocolService measurementProtocolService;

        /// <summary>
        /// Creates a new instance of <see cref="OrderProcessesService"/>.
        /// </summary>
        public OrderProcessesService(IDatabaseConnection databaseConnection,
            IOptions<GclSettings> gclSettings,
            IShoppingBasketsService shoppingBasketsService,
            IAccountsService accountsService,
            IWiserItemsService wiserItemsService,
            ILanguagesService languagesService,
            ITemplatesService templatesService,
            IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory,
            ICommunicationsService communicationsService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<OrderProcessesService> logger,
            IObjectsService objectsService,
            IMeasurementProtocolService measurementProtocolService,
            IHtmlToPdfConverterService htmlToPdfConverterService)
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
            this.logger = logger;
            this.objectsService = objectsService;
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.gclSettings = gclSettings.Value;
            this.measurementProtocolService = measurementProtocolService;
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
                                merchantEmailAddressField.value AS merchantEmailAddressField,
                                IF(statusUpdateTemplate.value IS NULL OR statusUpdateTemplate.value = '', '0', statusUpdateTemplate.value) AS statusUpdateTemplate,
                                IF(statusUpdateWebShopTemplate.value IS NULL OR statusUpdateWebShopTemplate.value = '', '0', statusUpdateWebShopTemplate.value) AS statusUpdateWebShopTemplate,
                                IF(statusUpdateAttachmentTemplate.value IS NULL OR statusUpdateAttachmentTemplate.value = '', '0', statusUpdateAttachmentTemplate.value) AS statusUpdateAttachmentTemplate,
                                IF(clearBasketOnConfirmationPage.value IS NULL OR clearBasketOnConfirmationPage.value = '', '1', clearBasketOnConfirmationPage.value) AS clearBasketOnConfirmationPage,
	                            CONCAT_WS('', header.value, header.long_value) AS header,
	                            CONCAT_WS('', footer.value, footer.long_value) AS footer,
                                CONCAT_WS('', template.value, template.long_value) AS template,
                                IF(measurementProtocolActive.`value` = 1, TRUE, FALSE) AS measurementProtocolActive,
                                CONCAT_WS('', measurementProtocolItemJson.`value`, measurementProtocolItemJson.long_value) AS measurementProtocolItemJson,
                                CONCAT_WS('', measurementProtocolBeginCheckoutJson.`value`, measurementProtocolBeginCheckoutJson.long_value) AS measurementProtocolBeginCheckoutJson,
                                CONCAT_WS('', measurementProtocolAddPaymentInfoJson.`value`, measurementProtocolAddPaymentInfoJson.long_value) AS measurementProtocolAddPaymentInfoJson,
                                CONCAT_WS('', measurementProtocolPurchaseJson.`value`, measurementProtocolPurchaseJson.long_value) AS measurementProtocolPurchaseJson,
                                measurementProtocolMeasurementId.`value` AS measurementProtocolMeasurementId,
                                measurementProtocolApiSecret.`value` AS measurementProtocolApiSecret
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            JOIN {WiserTableNames.WiserItemLink} AS linkToStep ON linkToStep.destination_item_id = orderProcess.id AND linkToStep.type = {Constants.StepToProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS step ON step.id = linkToStep.item_id AND step.entity_type = '{Constants.StepEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = orderProcess.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS emailAddressField ON emailAddressField.item_id = orderProcess.id AND emailAddressField.`key` = '{Constants.OrderProcessEmailAddressFieldProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS merchantEmailAddressField ON merchantEmailAddressField.item_id = orderProcess.id AND merchantEmailAddressField.`key` = '{Constants.OrderProcessMerchantEmailAddressFieldProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateTemplate ON statusUpdateTemplate.item_id = orderProcess.id AND statusUpdateTemplate.`key` = '{Constants.OrderProcessStatusUpdateTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateWebShopTemplate ON statusUpdateWebShopTemplate.item_id = orderProcess.id AND statusUpdateWebShopTemplate.`key` = '{Constants.OrderProcessStatusUpdateWebShopTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS statusUpdateAttachmentTemplate ON statusUpdateAttachmentTemplate.item_id = orderProcess.id AND statusUpdateAttachmentTemplate.`key` = '{Constants.OrderProcessStatusUpdateAttachmentTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS clearBasketOnConfirmationPage ON clearBasketOnConfirmationPage.item_id = orderProcess.id AND clearBasketOnConfirmationPage.`key` = '{Constants.OrderProcessClearBasketOnConfirmationPageProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS header ON header.item_id = orderProcess.id AND header.`key` = '{Constants.OrderProcessHeaderProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS footer ON footer.item_id = orderProcess.id AND footer.`key` = '{Constants.OrderProcessFooterProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS template ON template.item_id = orderProcess.id AND template.`key` = '{Constants.OrderProcessTemplateProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolActive ON measurementProtocolActive.item_id = orderProcess.id AND measurementProtocolActive.`key` = '{Constants.MeasurementProtocolActiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolItemJson ON measurementProtocolItemJson.item_id = orderProcess.id AND measurementProtocolItemJson.`key` = '{Constants.MeasurementProtocolItemJsonProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolBeginCheckoutJson ON measurementProtocolBeginCheckoutJson.item_id = orderProcess.id AND measurementProtocolBeginCheckoutJson.`key` = '{Constants.MeasurementProtocolBeginCheckoutJsonProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolAddPaymentInfoJson ON measurementProtocolAddPaymentInfoJson.item_id = orderProcess.id AND measurementProtocolAddPaymentInfoJson.`key` = '{Constants.MeasurementProtocolAddPaymentInfoJsonProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolPurchaseJson ON measurementProtocolPurchaseJson.item_id = orderProcess.id AND measurementProtocolPurchaseJson.`key` = '{Constants.MeasurementProtocolPurchaseJsonProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolMeasurementId ON measurementProtocolMeasurementId.item_id = orderProcess.id AND measurementProtocolMeasurementId.`key` = '{Constants.MeasurementProtocolMeasurementIdProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS measurementProtocolApiSecret ON measurementProtocolApiSecret.item_id = orderProcess.id AND measurementProtocolApiSecret.`key` = '{Constants.MeasurementProtocolApiSecretProperty}'
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
                MerchantEmailAddressProperty = firstRow.Field<string>("merchantEmailAddressField"),
                StatusUpdateMailTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateTemplate")),
                StatusUpdateMailWebShopTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateWebShopTemplate")),
                StatusUpdateInvoiceTemplateId = Convert.ToUInt64(firstRow.Field<string>("statusUpdateAttachmentTemplate")),
                ClearBasketOnConfirmationPage = firstRow.Field<string>("clearBasketOnConfirmationPage") == "1",
                Header = firstRow.Field<string>("header"),
                Footer = firstRow.Field<string>("footer"),
                Template = firstRow.Field<string>("template"),
                MeasurementProtocolActive = Convert.ToBoolean(firstRow["measurementProtocolActive"]),
                MeasurementProtocolItemJson = firstRow.Field<string>("measurementProtocolItemJson"),
                MeasurementProtocolBeginCheckoutJson = firstRow.Field<string>("measurementProtocolBeginCheckoutJson"),
                MeasurementProtocolAddPaymentInfoJson = firstRow.Field<string>("measurementProtocolAddPaymentInfoJson"),
                MeasurementProtocolPurchaseJson = firstRow.Field<string>("measurementProtocolPurchaseJson"),
                MeasurementProtocolMeasurementId = firstRow.Field<string>("measurementProtocolMeasurementId"),
                MeasurementProtocolApiSecret = firstRow.Field<string>("measurementProtocolApiSecret")
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
                            stepType.value AS stepType,
	                        CONCAT_WS('', stepTemplate.value, stepTemplate.long_value) AS stepTemplate,
	                        CONCAT_WS('', stepHeader.value, stepHeader.long_value) AS stepHeader,
	                        CONCAT_WS('', stepFooter.value, stepFooter.long_value) AS stepFooter,
                            stepConfirmButtonText.value AS stepConfirmButtonText,
                            previousStepLinkText.value AS previousStepLinkText,
                            stepRedirectUrl.value AS stepRedirectUrl,
                            IF(stepHideInProgress.value = '1', TRUE, FALSE) AS stepHideInProgress,
	                        
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
                            fieldRequiresUniqueValue.value AS fieldRequiresUniqueValue,
                            fieldTabIndex.value AS fieldTabIndex,

                            # Field values
	                        IF(NULLIF(fieldValues.`key`, '') IS NULL AND NULLIF(fieldValues.value, '') IS NULL, NULL, JSON_OBJECTAGG(IFNULL(fieldValues.`key`, ''), IFNULL(fieldValues.value, ''))) AS fieldValues
                        FROM {WiserTableNames.WiserItem} AS orderProcess

                        # Step
                        JOIN {WiserTableNames.WiserItemLink} AS linkToStep ON linkToStep.destination_item_id = orderProcess.id AND linkToStep.type = {Constants.StepToProcessLinkType}
                        JOIN {WiserTableNames.WiserItem} AS step ON step.id = linkToStep.item_id AND step.entity_type = '{Constants.StepEntityType}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepType ON stepType.item_id = step.id AND stepType.`key` = '{Constants.StepTypeProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepTemplate ON stepTemplate.item_id = step.id AND stepTemplate.`key` = '{Constants.StepTemplateProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepHeader ON stepHeader.item_id = step.id AND stepHeader.`key` = '{Constants.StepHeaderProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepFooter ON stepFooter.item_id = step.id AND stepFooter.`key` = '{Constants.StepFooterProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepConfirmButtonText ON stepConfirmButtonText.item_id = step.id AND stepConfirmButtonText.`key` = '{Constants.StepConfirmButtonTextProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS previousStepLinkText ON previousStepLinkText.item_id = step.id AND previousStepLinkText.`key` = '{Constants.StepPreviousStepLinkTextProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepRedirectUrl ON stepRedirectUrl.item_id = step.id AND stepRedirectUrl.`key` = '{Constants.StepRedirectUrlProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS stepHideInProgress ON stepHideInProgress.item_id = step.id AND stepHideInProgress.`key` = '{Constants.StepHideInProgressProperty}'

                        # Group
                        LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToGroup ON linkToGroup.destination_item_id = step.id AND linkToGroup.type = {Constants.GroupToStepLinkType}
                        LEFT JOIN {WiserTableNames.WiserItem} AS fieldGroup ON fieldGroup.id = linkToGroup.item_id AND fieldGroup.entity_type = '{Constants.GroupEntityType}'
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
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldRequiresUniqueValue ON fieldRequiresUniqueValue.item_id = field.id AND fieldRequiresUniqueValue.`key` = '{Constants.FieldRequiresUniqueValueProperty}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS fieldTabIndex ON fieldTabIndex.item_id = field.id AND fieldTabIndex.`key` = '{Constants.FieldTabIndexProperty}'
                        
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
                        Type = EnumHelpers.ToEnum<OrderProcessStepTypes>(dataRow.Field<string>("stepType") ?? "GroupsAndFields"),
                        Template = dataRow.Field<string>("stepTemplate"),
                        Header = dataRow.Field<string>("stepHeader"),
                        Footer = dataRow.Field<string>("stepFooter"),
                        ConfirmButtonText = dataRow.Field<string>("stepConfirmButtonText"),
                        PreviousStepLinkText = dataRow.Field<string>("previousStepLinkText"),
                        StepRedirectUrl = dataRow.Field<string>("stepRedirectUrl"),
                        HideInProgress = Convert.ToBoolean(dataRow["stepHideInProgress"]),
                        Groups = new List<OrderProcessGroupModel>()
                    };

                    results.Add(step);
                }

                // Get the group if it already exists in the current step, or create a new one if it doesn't.
                var groupId = dataRow.Field<ulong?>("groupId");
                if (!groupId.HasValue || groupId.Value == 0)
                {
                    continue;
                }

                var group = step.Groups.SingleOrDefault(g => g.Id == groupId.Value);
                if (group == null)
                {
                    group = new OrderProcessGroupModel
                    {
                        Id = groupId.Value,
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
                    RequireUniqueValue = dataRow.Field<string>("fieldRequiresUniqueValue") == "1"
                };

                if (String.IsNullOrWhiteSpace(field.FieldId))
                {
                    field.FieldId = field.Title;
                }

                var tabIndexStringValue = dataRow.Field<string>("fieldTabIndex");
                if (Int32.TryParse(tabIndexStringValue, out var tabIndex))
                {
                    field.TabIndex = tabIndex;
                }

                var saveTo = dataRow.Field<string>("fieldSaveTo");
                if (!String.IsNullOrWhiteSpace(saveTo))
                {
                    foreach (var saveLocation in saveTo.Split(','))
                    {
                        var split = saveLocation.Split('.');
                        if (split.Length != 2)
                        {
                            throw new Exception($"Invalid save location found for field {field.Id}: {saveLocation}");
                        }

                        var saveToSettings = new OrderProcessFieldSaveToSettingsModel
                        {
                            EntityType = split[0],
                            PropertyName = split[1]
                        };

                        if (String.IsNullOrWhiteSpace(saveToSettings.EntityType) || String.IsNullOrWhiteSpace(saveToSettings.PropertyName))
                        {
                            throw new Exception($"Invalid save location found for field {field.Id}: {saveLocation}");
                        }
                        
                        field.SaveTo.Add(saveToSettings);
                        
                        if (!saveToSettings.PropertyName.Contains("[") || !saveToSettings.PropertyName.EndsWith("]"))
                        {
                            continue;
                        }

                        // We can indicate what type number to use with adding the suffix "[X]", but we don't need the type number here, so just strip that. 
                        split = saveToSettings.PropertyName.Split('[');
                        saveToSettings.PropertyName = split.First();
                        var linkTypeString = split.Last().TrimEnd(']');
                        if (!Int32.TryParse(linkTypeString, out var linkType) || linkType <= 0)
                        {
                            throw new Exception($"Invalid link type found ({linkTypeString}) in save location for field {field.Id}: {saveLocation}");
                        }

                        saveToSettings.LinkType = linkType;
                    }
                }

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
                                paymentServiceProviderType.value AS paymentServiceProviderType,
	                            paymentMethodFee.value AS paymentMethodFee,
	                            paymentMethodVisibility.value AS paymentMethodVisibility,
	                            paymentMethodExternalName.value AS paymentMethodExternalName,

                                paymentServiceProviderLogAllRequests.value AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.value AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.value AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,

                                mollieApiKeyLive.value AS mollieApiKeyLive,
                                mollieApiKeyTest.value AS mollieApiKeyTest,
                                buckarooWebsiteKeyLive.value AS buckarooWebsiteKeyLive,
                                buckarooWebsiteKeyTest.value AS buckarooWebsiteKeyTest,
                                buckarooSecretKeyLive.value AS buckarooSecretKeyLive,
                                buckarooSecretKeyTest.value AS buckarooSecretKeyTest,
                                multiSafepayApiKeyLive.value AS multiSafepayApiKeyLive,
                                multiSafepayApiKeyTest.value AS multiSafepayApiKeyTest,
                                raboOmniKassaRefreshTokenLive.value AS raboOmniKassaRefreshTokenLive,
                                raboOmniKassaRefreshTokenTest.value AS raboOmniKassaRefreshTokenTest,
                                raboOmniKassaSigningKeyLive.value AS raboOmniKassaSigningKeyLive,
                                raboOmniKassaSigningKeyTest.value AS raboOmniKassaSigningKeyTest
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
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderType ON paymentServiceProviderType.item_id = paymentServiceProvider.id AND paymentServiceProviderType.`key` = '{Constants.PaymentServiceProviderTypeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            
                            # Mollie
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS mollieApiKeyLive ON mollieApiKeyLive.item_id = paymentServiceProvider.id AND mollieApiKeyLive.`key` = '{Constants.MollieApiKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS mollieApiKeyTest ON mollieApiKeyTest.item_id = paymentServiceProvider.id AND mollieApiKeyTest.`key` = '{Constants.MollieApiKeyTestProperty}'

                            # Buckaroo
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooWebsiteKeyLive ON buckarooWebsiteKeyLive.item_id = paymentServiceProvider.id AND buckarooWebsiteKeyLive.`key` = '{Constants.BuckarooWebsiteKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooWebsiteKeyTest ON buckarooWebsiteKeyTest.item_id = paymentServiceProvider.id AND buckarooWebsiteKeyTest.`key` = '{Constants.BuckarooWebsiteKeyTestProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooSecretKeyLive ON buckarooSecretKeyLive.item_id = paymentServiceProvider.id AND buckarooSecretKeyLive.`key` = '{Constants.BuckarooSecretKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooSecretKeyTest ON buckarooSecretKeyTest.item_id = paymentServiceProvider.id AND buckarooSecretKeyTest.`key` = '{Constants.BuckarooSecretKeyTestProperty}'
                            
                            # MultiSafepay
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS multiSafepayApiKeyLive ON multiSafepayApiKeyLive.item_id = paymentServiceProvider.id AND multiSafepayApiKeyLive.`key` = '{Constants.MultiSafepayApiKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS multiSafepayApiKeyTest ON multiSafepayApiKeyTest.item_id = paymentServiceProvider.id AND multiSafepayApiKeyTest.`key` = '{Constants.MultiSafepayApiKeyTestProperty}'

                            # RaboOmniKassa
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaRefreshTokenLive ON raboOmniKassaRefreshTokenLive.item_id = paymentServiceProvider.id AND raboOmniKassaRefreshTokenLive.`key` = '{Constants.RaboOmniKassaRefreshTokenLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaRefreshTokenTest ON raboOmniKassaRefreshTokenTest.item_id = paymentServiceProvider.id AND raboOmniKassaRefreshTokenTest.`key` = '{Constants.RaboOmniKassaRefreshTokenTestProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaSigningKeyLive ON raboOmniKassaSigningKeyLive.item_id = paymentServiceProvider.id AND raboOmniKassaSigningKeyLive.`key` = '{Constants.RaboOmniKassaSigningKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaSigningKeyTest ON raboOmniKassaSigningKeyTest.item_id = paymentServiceProvider.id AND raboOmniKassaSigningKeyTest.`key` = '{Constants.RaboOmniKassaSigningKeyTestProperty}'

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
        public async Task<bool> ValidateFieldValueAsync(OrderProcessFieldModel field, List<(LinkSettingsModel LinkSettings, WiserItemModel Item)> currentItems)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(field.Pattern))
                {
                    // If the field is not mandatory, then it can be empty but must still pass validation if it's not empty.
                    return (!field.Mandatory && String.IsNullOrEmpty(field.Value)) || Regex.IsMatch(field.Value, field.Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
                }

                var isValid = field.Mandatory switch
                {
                    true when String.IsNullOrWhiteSpace(field.Value) => false,
                    false when String.IsNullOrWhiteSpace(field.Value) => true,
                    _ => field.InputFieldType switch
                    {
                        OrderProcessInputTypes.Email => Regex.IsMatch(field.Value, @"(@)(.+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
                        OrderProcessInputTypes.Number => Decimal.TryParse(field.Value, NumberStyles.Any, new CultureInfo("en-US"), out _),
                        _ => true
                    }
                };

                if (!isValid || !field.RequireUniqueValue)
                {
                    return isValid;
                }
                
                // Check if the entered value is unique.
                foreach (var saveLocation in field.SaveTo)
                {
                    // Don't check baskets, because a user can have multiple baskets and should always be able to have the same values in all baskets.
                    if (String.Equals(saveLocation.EntityType, ShoppingBasket.Models.Constants.BasketEntityType))
                    {
                        continue;
                    }

                    var itemsOfEntityType = currentItems?.Where(item => String.Equals(item.Item.EntityType, saveLocation.EntityType, StringComparison.CurrentCultureIgnoreCase) && item.LinkSettings.Type == saveLocation.LinkType).ToList();
                    var idsClause = itemsOfEntityType == null || !itemsOfEntityType.Any() ? "" : $"AND item.id NOT IN ({String.Join(",", itemsOfEntityType.Select(item => item.Item.Id))})";
                    var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(saveLocation.EntityType);
                    
                    databaseConnection.AddParameter("entityType", saveLocation.EntityType);
                    databaseConnection.AddParameter("propertyName", saveLocation.PropertyName);
                    databaseConnection.AddParameter("value", field.Value);
                    var query = $@"SELECT NULL
                                FROM {tablePrefix}{WiserTableNames.WiserItem} AS item
                                JOIN {tablePrefix}{WiserTableNames.WiserItemDetail} AS detail ON detail.item_id = item.id AND detail.`key` = ?propertyName AND (detail.value = ?value OR detail.long_value = ?value)
                                WHERE item.entity_type = ?entityType
                                {idsClause}
                                LIMIT 1";

                    var dataTable = await databaseConnection.GetAsync(query);
                    isValid = dataTable.Rows.Count == 0;
                    if (isValid)
                    {
                        continue;
                    }

                    field.ErrorMessage = await languagesService.GetTranslationAsync($"orderProcess_field_{field.Title}_notUniqueErrorMessage", defaultValue: field.ErrorMessage);
                    break;
                }

                return isValid;
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
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
                                paymentServiceProviderType.value AS paymentServiceProviderType,
	                            paymentMethodFee.value AS paymentMethodFee,
	                            paymentMethodVisibility.value AS paymentMethodVisibility,
	                            paymentMethodExternalName.value AS paymentMethodExternalName,

                                paymentServiceProviderLogAllRequests.value AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.value AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.value AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,

                                mollieApiKeyLive.value AS mollieApiKeyLive,
                                mollieApiKeyTest.value AS mollieApiKeyTest,
                                buckarooWebsiteKeyLive.value AS buckarooWebsiteKeyLive,
                                buckarooWebsiteKeyTest.value AS buckarooWebsiteKeyTest,
                                buckarooSecretKeyLive.value AS buckarooSecretKeyLive,
                                buckarooSecretKeyTest.value AS buckarooSecretKeyTest,
                                multiSafepayApiKeyLive.value AS multiSafepayApiKeyLive,
                                multiSafepayApiKeyTest.value AS multiSafepayApiKeyTest,
                                raboOmniKassaRefreshTokenLive.value AS raboOmniKassaRefreshTokenLive,
                                raboOmniKassaRefreshTokenTest.value AS raboOmniKassaRefreshTokenTest,
                                raboOmniKassaSigningKeyLive.value AS raboOmniKassaSigningKeyLive,
                                raboOmniKassaSigningKeyTest.value AS raboOmniKassaSigningKeyTest
                            FROM {WiserTableNames.WiserItem} AS paymentMethod
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodExternalName ON paymentMethodExternalName.item_id = paymentMethod.id AND paymentMethodExternalName.`key` = '{Constants.PaymentMethodExternalNameProperty}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderType ON paymentServiceProviderType.item_id = paymentServiceProvider.id AND paymentServiceProviderType.`key` = '{Constants.PaymentServiceProviderTypeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            
                            # Mollie
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS mollieApiKeyLive ON mollieApiKeyLive.item_id = paymentServiceProvider.id AND mollieApiKeyLive.`key` = '{Constants.MollieApiKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS mollieApiKeyTest ON mollieApiKeyTest.item_id = paymentServiceProvider.id AND mollieApiKeyTest.`key` = '{Constants.MollieApiKeyTestProperty}'

                            # Buckaroo
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooWebsiteKeyLive ON buckarooWebsiteKeyLive.item_id = paymentServiceProvider.id AND buckarooWebsiteKeyLive.`key` = '{Constants.BuckarooWebsiteKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooWebsiteKeyTest ON buckarooWebsiteKeyTest.item_id = paymentServiceProvider.id AND buckarooWebsiteKeyTest.`key` = '{Constants.BuckarooWebsiteKeyTestProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooSecretKeyLive ON buckarooSecretKeyLive.item_id = paymentServiceProvider.id AND buckarooSecretKeyLive.`key` = '{Constants.BuckarooSecretKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS buckarooSecretKeyTest ON buckarooSecretKeyTest.item_id = paymentServiceProvider.id AND buckarooSecretKeyTest.`key` = '{Constants.BuckarooSecretKeyTestProperty}'
                            
                            # MultiSafepay
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS multiSafepayApiKeyLive ON multiSafepayApiKeyLive.item_id = paymentServiceProvider.id AND multiSafepayApiKeyLive.`key` = '{Constants.MultiSafepayApiKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS multiSafepayApiKeyTest ON multiSafepayApiKeyTest.item_id = paymentServiceProvider.id AND multiSafepayApiKeyTest.`key` = '{Constants.MultiSafepayApiKeyTestProperty}'

                            # RaboOmniKassa
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaRefreshTokenLive ON raboOmniKassaRefreshTokenLive.item_id = paymentServiceProvider.id AND raboOmniKassaRefreshTokenLive.`key` = '{Constants.RaboOmniKassaRefreshTokenLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaRefreshTokenTest ON raboOmniKassaRefreshTokenTest.item_id = paymentServiceProvider.id AND raboOmniKassaRefreshTokenTest.`key` = '{Constants.RaboOmniKassaRefreshTokenTestProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaSigningKeyLive ON raboOmniKassaSigningKeyLive.item_id = paymentServiceProvider.id AND raboOmniKassaSigningKeyLive.`key` = '{Constants.RaboOmniKassaSigningKeyLiveProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS raboOmniKassaSigningKeyTest ON raboOmniKassaSigningKeyTest.item_id = paymentServiceProvider.id AND raboOmniKassaSigningKeyTest.`key` = '{Constants.RaboOmniKassaSigningKeyTestProperty}'

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

            // Build the fail, success and pending URLs.
            var (failUrl, successUrl, pendingUrl) = BuildUrls(orderProcessSettings, steps, shoppingBaskets.First().Main);

            // Check if we have a valid payment method.
            if (selectedPaymentMethodId == 0)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = failUrl,
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
                    ActionData = failUrl,
                    ErrorMessage = $"Invalid payment method '{selectedPaymentMethodId}'"
                };
            }

            paymentMethodSettings.PaymentServiceProvider.FailUrl = failUrl;
            paymentMethodSettings.PaymentServiceProvider.SuccessUrl = successUrl;
            paymentMethodSettings.PaymentServiceProvider.PendingUrl = pendingUrl;

            try
            {
                // Build the webhook URL.
                UriBuilder webhookUrl;
                // The PSP can't reach our development and test environments, so use the main domain in those cases.
                if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
                {
                    var mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
                    if (String.IsNullOrWhiteSpace(mainDomain))
                    {
                        throw new Exception("Please set the maindomain in easy_objects, otherwise we don't know what to use as webhook URL for the PSP.");
                    }

                    if (!mainDomain.StartsWith("http", StringComparison.Ordinal) && !mainDomain.StartsWith("//", StringComparison.Ordinal))
                    {
                        mainDomain = $"https://{mainDomain}";
                    }

                    webhookUrl = new UriBuilder(mainDomain);
                }
                else
                {
                    webhookUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext));
                }

                webhookUrl.Path = Constants.PaymentInPage;

                var queryString = HttpUtility.ParseQueryString(webhookUrl.Query);
                queryString[Constants.OrderProcessIdRequestKey] = orderProcessId.ToString();
                queryString[Constants.SelectedPaymentMethodRequestKey] = paymentMethodSettings.Id.ToString();
                webhookUrl.Query = queryString.ToString()!;
                paymentMethodSettings.PaymentServiceProvider.WebhookUrl = webhookUrl.ToString();

                // Build the return URL.
                var returnUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext))
                {
                    Path = Constants.PaymentReturnPage
                };
                queryString = HttpUtility.ParseQueryString(returnUrl.Query);
                queryString[Constants.OrderProcessIdRequestKey] = orderProcessId.ToString();
                queryString[Constants.SelectedPaymentMethodRequestKey] = paymentMethodSettings.Id.ToString();
                returnUrl.Query = queryString.ToString()!;
                paymentMethodSettings.PaymentServiceProvider.ReturnUrl = returnUrl.ToString();

                // Get current user.
                var loggedInUser = await accountsService.GetUserDataFromCookieAsync();
                WiserItemModel userDetails;
                if (loggedInUser.UserId > 0)
                {
                    userDetails = await wiserItemsService.GetItemDetailsAsync(loggedInUser.UserId, skipPermissionsCheck: true);
                }
                else
                {
                    var basketUser = (await wiserItemsService.GetLinkedItemDetailsAsync(shoppingBaskets.First().Main.Id, ShoppingBasket.Models.Constants.BasketToUserLinkType, Account.Models.Constants.DefaultEntityType, reverse: true, skipPermissionsCheck: true)).FirstOrDefault();
                    userDetails = basketUser ?? new WiserItemModel { EntityType = Account.Models.Constants.DefaultEntityType };
                    loggedInUser.UserId = userDetails.Id;
                }

                // Double check that we received a valid payment method.
                var availablePaymentMethods = await orderProcessesService.GetPaymentMethodsAsync(orderProcessId, loggedInUser);

                if (availablePaymentMethods == null || availablePaymentMethods.All(p => p.Id != selectedPaymentMethodId))
                {
                    return new PaymentRequestResult
                    {
                        Successful = false,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = failUrl,
                        ErrorMessage = "Invalid payment method selected"
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
                    main.SetDetail(Constants.IsTestOrderProperty, isTestOrder ? 1 : 0);
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

                        var couponItem = await wiserItemsService.GetItemDetailsAsync(couponItemId, skipPermissionsCheck: true);
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
                    
                    await HandlePaymentStatusUpdateAsync(orderProcessesService, orderProcessSettings, conceptOrders, "Success", true, convertConceptOrderToOrder);

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
                
                return await paymentServiceProviderService.HandlePaymentRequestAsync(conceptOrders, userDetails, paymentMethodSettings, uniquePaymentNumber);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, $"An exception occurred in {Constants.PaymentOutPage}");
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = failUrl,
                    ErrorMessage = exception.Message
                };
            }
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
            var userEmailAddress = "";
            var merchantEmailAddress = "";
            var bcc = "";
            var senderAddress = "";
            var senderName = "";
            var replyToAddress = "";
            var replyToName = "";

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

                // Generate an invoice for this order and save the HTML with the order.
                var fileId = 0UL;
                if (orderProcessSettings.StatusUpdateInvoiceTemplateId > 0)
                {
                    // Get PDF settings.
                    var pdfSettings = await htmlToPdfConverterService.GetHtmlToPdfSettingsAsync(orderProcessSettings.StatusUpdateInvoiceTemplateId, languagesService.CurrentLanguageCode);
                    if (!String.IsNullOrWhiteSpace(pdfSettings.Html)) 
                    {
                        pdfSettings.Html = await shoppingBasketsService.ReplaceBasketInTemplateAsync(main, lines, basketSettings, pdfSettings.Html, isForConfirmationEmail: true);
                        pdfSettings.Header = await shoppingBasketsService.ReplaceBasketInTemplateAsync(main, lines, basketSettings, pdfSettings.Header, isForConfirmationEmail: true);
                        pdfSettings.Footer = await shoppingBasketsService.ReplaceBasketInTemplateAsync(main, lines, basketSettings, pdfSettings.Footer, isForConfirmationEmail: true);
                        pdfSettings.FileName = await shoppingBasketsService.ReplaceBasketInTemplateAsync(main, lines, basketSettings, pdfSettings.FileName, isForConfirmationEmail: true);

                        // Save invoice HTML in order details.
                        main.SetDetail(Constants.InvoiceHtmlProperty, pdfSettings.Html);

                        // Convert HTML to PDF and save the PDF in wiser_itemfile, linked to the order.
                        var file = await htmlToPdfConverterService.ConvertHtmlStringToPdfAsync(pdfSettings);
                        var wiserItemFile = new WiserItemFileModel
                        {
                            Content = file.FileContents,
                            FileName = file.FileDownloadName,
                            Extension = Path.GetExtension(file.FileDownloadName),
                            ContentType = "application/pdf",
                            ItemId = main.Id,
                            PropertyName = Constants.InvoicePdfProperty
                        };

                        fileId = await wiserItemsService.AddItemFileAsync(wiserItemFile, skipPermissionsCheck: true);
                    }
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
                        Bcc = !String.IsNullOrWhiteSpace(bcc) ? new List<string> { bcc } : null,
                        ReplyTo = replyToAddress,
                        ReplyToName = replyToName,
                        Sender = senderAddress,
                        SenderName = senderName,
                        WiserItemFiles = fileId > 0 ? new List<ulong> { fileId } : null
                    });
                }

                if (!String.IsNullOrWhiteSpace(merchantEmailAddress) && !String.IsNullOrWhiteSpace(merchantEmailContent))
                {
                    mailsToSendToMerchant.Add(new SingleCommunicationModel
                    {
                        Content = merchantEmailContent,
                        Subject = merchantEmailSubject,
                        Receivers = new List<CommunicationReceiverModel> { new() { Address = merchantEmailAddress } },
                        Bcc = !String.IsNullOrWhiteSpace(merchantBcc) ? new List<string> { merchantBcc } : null,
                        ReplyTo = merchantReplyToAddress,
                        ReplyToName = merchantReplyToName,
                        Sender = merchantSenderAddress,
                        SenderName = merchantSenderName,
                        WiserItemFiles = fileId > 0 ? new List<ulong> { fileId } : null
                    });
                }

                if (isSuccessfulStatus && orderProcessSettings.MeasurementProtocolActive)
                {
                    await measurementProtocolService.PurchaseEventAsync(orderProcessSettings, main, lines, basketSettings, main.GetDetailValue(Constants.UniquePaymentNumberProperty));
                }
            }

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

        /// <inheritdoc />
        public async Task<bool> HandlePaymentServiceProviderWebhookAsync(ulong orderProcessId, ulong paymentMethodId)
        {
            return await HandlePaymentServiceProviderWebhookAsync(this, orderProcessId, paymentMethodId);
        }

        /// <inheritdoc />
        public async Task<bool> HandlePaymentServiceProviderWebhookAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, ulong paymentMethodId)
        {
            var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
            var paymentMethodSettings = await orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
            if (orderProcessSettings == null || orderProcessSettings.Id == 0 || paymentMethodSettings == null || paymentMethodSettings.Id == 0)
            {
                logger.LogError($"Called HandlePaymentServiceProviderWebhookAsync with invalid orderProcessId ({orderProcessId}) and/or invalid paymentMethodId ({paymentMethodId}). Full URL: {HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)}");
                return false;
            }

            var invoiceNumber = GetInvoiceNumberFromRequest(paymentMethodSettings.PaymentServiceProvider.Type);
            var conceptOrders = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(invoiceNumber);
            
            // Create the correct service for the payment service provider using the factory.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentMethodSettings.PaymentServiceProvider.Type);
            paymentServiceProviderService.LogPaymentActions = paymentMethodSettings.PaymentServiceProvider.LogAllRequests;
            
            // Let the payment service provider service handle the status update.
            var pspUpdateResult = await paymentServiceProviderService.ProcessStatusUpdateAsync(orderProcessSettings, paymentMethodSettings);

            var result = await orderProcessesService.HandlePaymentStatusUpdateAsync(orderProcessSettings, conceptOrders, pspUpdateResult.Status, pspUpdateResult.Successful);

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            foreach (var (main, lines) in conceptOrders)
            {
                // Set payment completed to true if the PSP indicated that the payment was successful.
                // This should not be done in orderProcessesService.HandlePaymentStatusUpdateAsync, because that method is also called for NOPSP.
                main.SetDetail(Constants.PaymentCompleteProperty, result);
                await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync(ulong orderProcessId, ulong paymentMethodId)
        {
            return await HandlePaymentReturnAsync(this, orderProcessId, paymentMethodId);
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, ulong paymentMethodId)
        {
            var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
            var paymentMethodSettings = await orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
            var steps = await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);

            // Build the fail, success and pending URLs.
            var (failUrl, successUrl, pendingUrl) = BuildUrls(orderProcessSettings, steps);
            
            if (orderProcessSettings == null || orderProcessSettings.Id == 0 || paymentMethodSettings == null || paymentMethodSettings.Id == 0)
            {
                logger.LogError($"Called HandlePaymentReturnAsync with invalid orderProcessId ({orderProcessId}) and/or invalid paymentMethodId ({paymentMethodId}). Full URL: {HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)}");
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = failUrl
                };
            }
            
            paymentMethodSettings.PaymentServiceProvider.FailUrl = failUrl;
            paymentMethodSettings.PaymentServiceProvider.SuccessUrl = successUrl;
            paymentMethodSettings.PaymentServiceProvider.PendingUrl = pendingUrl;
            
            if (paymentMethodSettings.PaymentServiceProvider.Type == PaymentServiceProviders.Unknown)
            {
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = paymentMethodSettings.PaymentServiceProvider.FailUrl
                };
            }

            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentMethodSettings.PaymentServiceProvider.Type);
            paymentServiceProviderService.LogPaymentActions = paymentMethodSettings.PaymentServiceProvider.LogAllRequests;

            return await paymentServiceProviderService.HandlePaymentReturnAsync(orderProcessSettings, paymentMethodSettings);
        }

        /// <inheritdoc />
        public async Task<WiserItemFileModel> GetInvoicePdfAsync(ulong orderId)
        {
            if (orderId == 0)
            {
                return null;
            }

            var userData = await accountsService.GetUserDataFromCookieAsync();
            
            var linkTypeOrderToUser = await wiserItemsService.GetLinkTypeAsync(Account.Models.Constants.DefaultEntityType, Constants.OrderEntityType);
            if (linkTypeOrderToUser == 0)
            {
                linkTypeOrderToUser = ShoppingBasket.Models.Constants.BasketToUserLinkType;
            }

            var linkedOrders = await wiserItemsService.GetLinkedItemIdsAsync(userData.MainUserId, linkTypeOrderToUser, Constants.OrderEntityType, skipPermissionsCheck: true);
            if (!linkedOrders.Contains(orderId))
            {
                return null;
            }

            var files = await wiserItemsService.GetItemFilesAsync(new[] { orderId }, "item_id", Constants.InvoicePdfProperty);
            return files.OrderBy(file => file.Id).LastOrDefault();
        }

        private PaymentMethodSettingsModel DataRowToPaymentMethodSettingsModel(DataRow dataRow)
        {
            // Build the payment settings model.
            Decimal.TryParse(dataRow.Field<string>("paymentMethodFee")?.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out var fee);
            var result = new PaymentMethodSettingsModel
            {
                Id = dataRow.Field<ulong>("paymentMethodId"),
                Title = dataRow.Field<string>("paymentMethodTitle"),
                Fee = fee,
                Visibility = EnumHelpers.ToEnum<OrderProcessFieldVisibilityTypes>(dataRow.Field<string>("paymentMethodVisibility") ?? "Always"),
                ExternalName = dataRow.Field<string>("paymentMethodExternalName")
            };

            // Build the PSP settings model based on the type of PSP.
            var paymentServiceProvider = EnumHelpers.ToEnum<PaymentServiceProviders>(dataRow.Field<string>("paymentServiceProviderType") ?? "0");
            result.PaymentServiceProvider = paymentServiceProvider switch
            {
                PaymentServiceProviders.Unknown => new PaymentServiceProviderSettingsModel(),
                PaymentServiceProviders.NoPsp => new PaymentServiceProviderSettingsModel(),
                PaymentServiceProviders.Buckaroo => new BuckarooSettingsModel
                {
                    WebsiteKey = GetSecretKeyValue(dataRow, "buckarooWebsiteKey"),
                    SecretKey = GetSecretKeyValue(dataRow, "buckarooSecretKey")
                },
                PaymentServiceProviders.MultiSafepay => new MultiSafepaySettingsModel
                {
                    ApiKey = GetSecretKeyValue(dataRow, "multiSafepayApiKey")
                },
                PaymentServiceProviders.RaboOmniKassa => new RaboOmniKassaSettingsModel
                {
                    RefreshToken = GetSecretKeyValue(dataRow, "raboOmniKassaRefreshToken"),
                    SigningKey = GetSecretKeyValue(dataRow, "raboOmniKassaSigningKey")
                },
                PaymentServiceProviders.Mollie => new MollieSettingsModel
                {
                    ApiKey = GetSecretKeyValue(dataRow, "mollieApiKey")
                },
                _ => throw new ArgumentOutOfRangeException(nameof(paymentServiceProvider), paymentServiceProvider.ToString())
            };

            // Settings that are the same for all PSPs.
            result.PaymentServiceProvider.Id = dataRow.Field<ulong>("paymentServiceProviderId");
            result.PaymentServiceProvider.Title = dataRow.Field<string>("paymentServiceProviderTitle");
            result.PaymentServiceProvider.Type = paymentServiceProvider;
            result.PaymentServiceProvider.LogAllRequests = dataRow.Field<string>("paymentServiceProviderLogAllRequests") == "1";
            result.PaymentServiceProvider.OrdersCanBeSetDirectlyToFinished = dataRow.Field<string>("paymentServiceProviderSetOrdersDirectlyToFinished") == "1";
            result.PaymentServiceProvider.SkipPaymentWhenOrderAmountEqualsZero = dataRow.Field<string>("paymentServiceProviderSkipWhenOrderAmountEqualsZero") == "1";

            if (String.IsNullOrEmpty(result.ExternalName))
            {
                result.ExternalName = result.Title;
            }

            return result;
        }

        private string GetSecretKeyValue(DataRow dataRow, string itemDetailKey)
        {
            var suffix = gclSettings.Environment.InList(Environments.Development, Environments.Test) ? "Test" : "Live";
            var result = dataRow.Field<string>($"{itemDetailKey}{suffix}");
            if (String.IsNullOrWhiteSpace(result))
            {
                return result;
            }

            return result.DecryptWithAesWithSalt();
        }

        private async Task<EmailValues> GetMailValuesAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel conceptOrder, List<WiserItemModel> conceptOrderLines, bool forMerchantMail = false, bool forAttachment = false)
        {
            var userEmailAddress = "";

            var linkedUsers = await wiserItemsService.GetLinkedItemDetailsAsync(conceptOrder.Id, reverse: true, skipPermissionsCheck: true);

            ulong templateItemId;
            string templatePropertyName;
            if (forAttachment)
            {
                templatePropertyName = Constants.StatusUpdateMailAttachmentProperty;
                templateItemId = orderProcessSettings.StatusUpdateInvoiceTemplateId;
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
            var templateItem = await wiserItemsService.GetItemDetailsAsync(templateItemId, languageCode: languageCode, userId: user.UserId, skipPermissionsCheck: true) ?? await wiserItemsService.GetItemDetailsAsync(templateItemId, userId: user.UserId, skipPermissionsCheck: true);

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

            //get merchant basket email instead of the potentially linked merchant email address
            if (!String.IsNullOrWhiteSpace(orderProcessSettings.MerchantEmailAddressProperty) && !String.IsNullOrWhiteSpace(conceptOrder.GetDetailValue(orderProcessSettings.MerchantEmailAddressProperty)))
            {
                merchantEmailAddress = conceptOrder.GetDetailValue(orderProcessSettings.MerchantEmailAddressProperty);
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
        
        /// <summary>
        /// Gets the invoice number from the request.
        /// Each PSP sends this number in their own way, this method will get the number from the request based on which PSP is being used.  
        /// </summary>
        /// <param name="paymentServiceProvider">The PSP that is being used.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">If an unknown PSP has been used.</exception>
        private string GetInvoiceNumberFromRequest(PaymentServiceProviders paymentServiceProvider)
        {
            if (paymentServiceProvider == PaymentServiceProviders.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(paymentServiceProvider), "Unknown payment service provider.");
            }

            return paymentServiceProvider switch
            {
                PaymentServiceProviders.Buckaroo => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "brq_invoicenumber"),
                PaymentServiceProviders.MultiSafepay => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "transactionid"),
                PaymentServiceProviders.RaboOmniKassa => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "order_id"),
                PaymentServiceProviders.Mollie => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "invoice_number"),
                _ => throw new ArgumentOutOfRangeException(nameof(paymentServiceProvider), $"Payment service provider '{paymentServiceProvider:G}' is not yet supported.")
            };
        }

        private (string FailUrl, string SuccessUrl, string PendingUrl) BuildUrls(OrderProcessSettingsModel orderProcessSettings, List<OrderProcessStepModel> steps, WiserItemModel shoppingBasket = null)
        {
            var failUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)) { Path = orderProcessSettings.FixedUrl };
            var successUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)) { Path = orderProcessSettings.FixedUrl };
            var pendingUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext)) { Path = orderProcessSettings.FixedUrl };

            var failUrlQueryString = HttpUtility.ParseQueryString(failUrl.Query);
            failUrlQueryString[Constants.ErrorFromPaymentOutRequestKey] = "true";

            var successUrlQueryString = HttpUtility.ParseQueryString(successUrl.Query);
            var pendingUrlQueryString = HttpUtility.ParseQueryString(pendingUrl.Query);

            if (orderProcessSettings.AmountOfSteps > 1)
            {
                // Error page.
                var stepForPaymentErrors = orderProcessSettings.AmountOfSteps;
                var stepWithPaymentMethods = steps.LastOrDefault(step => step.Groups.Any(group => @group.Type == OrderProcessGroupTypes.PaymentMethods));
                if (stepWithPaymentMethods != null)
                {
                    stepForPaymentErrors = steps.IndexOf(stepWithPaymentMethods) + 1;
                }

                failUrlQueryString[Constants.ActiveStepRequestKey] = stepForPaymentErrors.ToString();

                // Success page.
                var stepForSuccessPage = orderProcessSettings.AmountOfSteps;
                var stepWithConfirmation = steps.LastOrDefault(step => step.Type == OrderProcessStepTypes.OrderConfirmation);
                if (stepWithConfirmation != null)
                {
                    stepForSuccessPage = steps.IndexOf(stepWithConfirmation) + 1;
                }

                successUrlQueryString[Constants.ActiveStepRequestKey] = stepForSuccessPage.ToString();

                // Pending page.
                var stepForPendingPage = stepForSuccessPage;
                var stepWithPending = steps.LastOrDefault(step => step.Type == OrderProcessStepTypes.OrderPending);
                if (stepWithPending != null)
                {
                    stepForPendingPage = steps.IndexOf(stepWithPending) + 1;
                }

                pendingUrlQueryString[Constants.ActiveStepRequestKey] = stepForPendingPage.ToString();
            }

            failUrl.Query = failUrlQueryString.ToString()!;
            successUrl.Query = successUrlQueryString.ToString()!;
            pendingUrl.Query = pendingUrlQueryString.ToString()!;
            return (failUrl.ToString(), successUrl.ToString(), pendingUrl.ToString());
        }
    }
}
