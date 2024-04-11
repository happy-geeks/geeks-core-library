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
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Http;
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
            ILogger<OrderProcessesService> logger,
            IObjectsService objectsService,
            IMeasurementProtocolService measurementProtocolService,
            IHtmlToPdfConverterService htmlToPdfConverterService,
            IHttpContextAccessor httpContextAccessor = null)
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
                                IFNULL(basketToConceptOrderMethod.`value`, 0) AS basketToConceptOrderMethod,
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
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS basketToConceptOrderMethod ON basketToConceptOrderMethod.item_id = orderProcess.id AND basketToConceptOrderMethod.`key` = '{Constants.OrderProcessBasketToConceptOrderMethodProperty}'
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
                BasketToConceptOrderMethod = (OrderProcessBasketToConceptOrderMethods) Convert.ToInt32(firstRow["basketToConceptOrderMethod"]),
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
                                paymentServiceProviderType.`value` AS paymentServiceProviderType,
                                CAST(paymentMethodFee.value AS DECIMAL(65,30)) AS paymentMethodFee,
                                paymentMethodVisibility.`value` AS paymentMethodVisibility,
                                paymentMethodExternalName.`value` AS paymentMethodExternalName,
                                CAST(paymentMethodMinimalAmount.value AS DECIMAL(65,30)) AS paymentMethodMinimalAmount,
                                CAST(paymentMethodMaximumAmount.value AS DECIMAL(65,30)) AS paymentMethodMaximumAmount,
                                IF(paymentMethodUseMinimalAmount.`value` = 1, TRUE, FALSE) AS paymentMethodUseMinimalAmount,
                                IF(paymentMethodUseMaximumAmount.`value` = 1, TRUE, FALSE) AS paymentMethodUseMaximumAmount,

                                paymentServiceProviderLogAllRequests.`value` AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.`value` AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.`value` AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,
                                IFNULL(paymentServiceProviderSuccessUrl.`value`,'') AS paymentServiceProviderSuccessUrl,
                                IFNULL(paymentServiceProviderFailUrl.`value`,'') AS paymentServiceProviderFailUrl,
                                IFNULL(paymentServiceProviderPendingUrl.`value`,'') AS paymentServiceProviderPendingUrl
                            FROM {WiserTableNames.WiserItem} AS orderProcess

                            # Payment method
                            JOIN {WiserTableNames.WiserItemLink} AS paymentMethodLink ON paymentMethodLink.destination_item_id = orderProcess.id AND paymentMethodLink.type = {Constants.PaymentMethodToOrderProcessLinkType}
                            JOIN {WiserTableNames.WiserItem} AS paymentMethod ON paymentMethod.id = paymentMethodLink.item_id AND paymentMethod.entity_type = '{Constants.PaymentMethodEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodExternalName ON paymentMethodExternalName.item_id = paymentMethod.id AND paymentMethodExternalName.`key` = '{Constants.PaymentMethodExternalNameProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodMinimalAmount ON paymentMethodMinimalAmount.item_id = paymentMethod.id AND paymentMethodMinimalAmount.`key` = '{Constants.PaymentMethodMinimalAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodMaximumAmount ON paymentMethodMaximumAmount.item_id = paymentMethod.id AND paymentMethodMaximumAmount.`key` = '{Constants.PaymentMethodMaximumAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodUseMinimalAmount ON paymentMethodUseMinimalAmount.item_id = paymentMethod.id AND paymentMethodUseMinimalAmount.`key` = '{Constants.PaymentMethodUseMinimalAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodUseMaximumAmount ON paymentMethodUseMaximumAmount.item_id = paymentMethod.id AND paymentMethodUseMaximumAmount.`key` = '{Constants.PaymentMethodUseMaximumAmountProperty}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderType ON paymentServiceProviderType.item_id = paymentServiceProvider.id AND paymentServiceProviderType.`key` = '{Constants.PaymentServiceProviderTypeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSuccessUrl ON paymentServiceProviderSuccessUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderSuccessUrl.`key` = '{Constants.PaymentServiceProviderSuccessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderFailUrl ON paymentServiceProviderFailUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderFailUrl.`key` = '{Constants.PaymentServiceProviderFailUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderPendingUrl ON paymentServiceProviderPendingUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderPendingUrl.`key` = '{Constants.PaymentServiceProviderPendingUrlProperty}'

                            WHERE orderProcess.id = ?id
                            AND orderProcess.entity_type = '{Constants.OrderProcessEntityType}'";

            databaseConnection.AddParameter("id", orderProcessId);
            var dataTable = await databaseConnection.GetAsync(query);
            var results = new List<PaymentMethodSettingsModel>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(await DataRowToPaymentMethodSettingsModelAsync(dataRow));
            }

            // get total amount of order
            var shoppingBaskets = await shoppingBasketsService.GetShoppingBasketsAsync();
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var totalPrice = 0M;
            foreach (var (main, lines) in shoppingBaskets)
            {
                totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings);
            }

            // check if paymentmethods are allowed for the price that the costumer should pay
            results = results.Where(paymentMethod =>
            {
                // check if total price is below the minimal
                if (paymentMethod.UseMinimalAmountCheck && totalPrice < paymentMethod.MinimalAmountCheck)
                {
                    return false;
                }

                // check if total price is above the maximum
                if (paymentMethod.UseMaximumAmountCheck && totalPrice > paymentMethod.MaximumAmountCheck)
                {
                    return false;
                }

                // amount is within range or checks are disabled
                return true;
            }).ToList();

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
                    return (!field.Mandatory && String.IsNullOrEmpty(field.Value)) || Regex.IsMatch(field.Value, field.Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000));
                }

                var isValid = field.Mandatory switch
                {
                    true when String.IsNullOrWhiteSpace(field.Value) => false,
                    false when String.IsNullOrWhiteSpace(field.Value) => true,
                    _ => field.InputFieldType switch
                    {
                        OrderProcessInputTypes.Email => Regex.IsMatch(field.Value, @"(@)(.+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000)),
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
                                paymentServiceProviderType.`value` AS paymentServiceProviderType,
                                CAST(paymentMethodFee.value AS decimal(65,30)) AS paymentMethodFee,
                                paymentMethodVisibility.`value` AS paymentMethodVisibility,
                                paymentMethodExternalName.`value` AS paymentMethodExternalName,
                                CAST(paymentMethodMinimalAmount.value AS decimal(65,30)) AS paymentMethodMinimalAmount,
                                CAST(paymentMethodMaximumAmount.value AS decimal(65,30)) AS paymentMethodMaximumAmount,
                                CAST(IFNULL(paymentMethodUseMinimalAmount.`value`, 0) AS SIGNED) AS paymentMethodUseMinimalAmount,
                                CAST(IFNULL(paymentMethodUseMaximumAmount.`value`, 0) AS SIGNED) AS paymentMethodUseMaximumAmount,

                                paymentServiceProviderLogAllRequests.`value` AS paymentServiceProviderLogAllRequests,
                                paymentServiceProviderSetOrdersDirectlyToFinished.`value` AS paymentServiceProviderSetOrdersDirectlyToFinished,
                                paymentServiceProviderSkipWhenOrderAmountEqualsZero.`value` AS paymentServiceProviderSkipWhenOrderAmountEqualsZero,
                                IFNULL(paymentServiceProviderSuccessUrl.`value`,'') AS paymentServiceProviderSuccessUrl,
                                IFNULL(paymentServiceProviderFailUrl.`value`,'') AS paymentServiceProviderFailUrl,
                                IFNULL(paymentServiceProviderPendingUrl.`value`,'') AS paymentServiceProviderPendingUrl
                            FROM {WiserTableNames.WiserItem} AS paymentMethod
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodFee ON paymentMethodFee.item_id = paymentMethod.id AND paymentMethodFee.`key` = '{Constants.PaymentMethodFeeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodVisibility ON paymentMethodVisibility.item_id = paymentMethod.id AND paymentMethodVisibility.`key` = '{Constants.PaymentMethodVisibilityProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodExternalName ON paymentMethodExternalName.item_id = paymentMethod.id AND paymentMethodExternalName.`key` = '{Constants.PaymentMethodExternalNameProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodMinimalAmount ON paymentMethodMinimalAmount.item_id = paymentMethod.id AND paymentMethodMinimalAmount.`key` = '{Constants.PaymentMethodMinimalAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodMaximumAmount ON paymentMethodMaximumAmount.item_id = paymentMethod.id AND paymentMethodMaximumAmount.`key` = '{Constants.PaymentMethodMaximumAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodUseMinimalAmount ON paymentMethodUseMinimalAmount.item_id = paymentMethod.id AND paymentMethodUseMinimalAmount.`key` = '{Constants.PaymentMethodUseMinimalAmountProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentMethodUseMaximumAmount ON paymentMethodUseMaximumAmount.item_id = paymentMethod.id AND paymentMethodUseMaximumAmount.`key` = '{Constants.PaymentMethodUseMaximumAmountProperty}'

                            # PSP
                            JOIN {WiserTableNames.WiserItemDetail} AS linkedProvider ON linkedProvider.item_id = paymentMethod.id AND linkedProvider.`key` = '{Constants.PaymentMethodServiceProviderProperty}'
                            JOIN {WiserTableNames.WiserItem} AS paymentServiceProvider ON paymentServiceProvider.id = linkedProvider.`value` AND paymentServiceProvider.entity_type = '{Constants.PaymentServiceProviderEntityType}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderType ON paymentServiceProviderType.item_id = paymentServiceProvider.id AND paymentServiceProviderType.`key` = '{Constants.PaymentServiceProviderTypeProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderLogAllRequests ON paymentServiceProviderLogAllRequests.item_id = paymentServiceProvider.id AND paymentServiceProviderLogAllRequests.`key` = '{Constants.PaymentServiceProviderLogAllRequestsProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSetOrdersDirectlyToFinished ON paymentServiceProviderSetOrdersDirectlyToFinished.item_id = paymentServiceProvider.id AND paymentServiceProviderSetOrdersDirectlyToFinished.`key` = '{Constants.PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSkipWhenOrderAmountEqualsZero ON paymentServiceProviderSkipWhenOrderAmountEqualsZero.item_id = paymentServiceProvider.id AND paymentServiceProviderSkipWhenOrderAmountEqualsZero.`key` = '{Constants.PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderSuccessUrl ON paymentServiceProviderSuccessUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderSuccessUrl.`key` = '{Constants.PaymentServiceProviderSuccessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderFailUrl ON paymentServiceProviderFailUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderFailUrl.`key` = '{Constants.PaymentServiceProviderFailUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS paymentServiceProviderPendingUrl ON paymentServiceProviderPendingUrl.item_id = paymentServiceProvider.id AND paymentServiceProviderPendingUrl.`key` = '{Constants.PaymentServiceProviderPendingUrlProperty}'

                            WHERE paymentMethod.id = ?id
                            AND paymentMethod.entity_type = '{Constants.PaymentMethodEntityType}'";

            databaseConnection.AddParameter("id", paymentMethodId);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var dataRow = dataTable.Rows[0];
            var result = await DataRowToPaymentMethodSettingsModelAsync(dataRow);

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
            var orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
            var steps = await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);
            
            // Build the fail, success and pending URLs.
            var (failUrl, successUrl, pendingUrl) = BuildUrls(orderProcessSettings, steps);
            var basketToConceptOrderMethod = orderProcessSettings.BasketToConceptOrderMethod;
            
            return await HandlePaymentRequestAsync(orderProcessesService, orderProcessId, failUrl, successUrl, pendingUrl, basketToConceptOrderMethod, orderProcessSettings);
        }

        /// <inheritdoc />
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, string failUrl, string successUrl, string pendingUrl, OrderProcessBasketToConceptOrderMethods basketToConceptOrderMethod, OrderProcessSettingsModel orderProcessSettings)
        {
            // Retrieve baskets.
            var shoppingBaskets = await shoppingBasketsService.GetShoppingBasketsAsync();
            var selectedPaymentMethodId = shoppingBaskets.First().Main.GetDetailValue<ulong>(Constants.PaymentMethodProperty);
            var conceptOrders = new List<(WiserItemModel Main, List<WiserItemModel> Lines)>();

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

            if (!String.IsNullOrEmpty(failUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.FailUrl = failUrl;    
            }

            if (!String.IsNullOrEmpty(successUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.SuccessUrl = successUrl;    
            }
            if (!String.IsNullOrEmpty(pendingUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.PendingUrl = pendingUrl;    
            }

            try
            {
                // Build the webhook URL.
                UriBuilder webhookUrl;

                var pspWebhookDomain = await objectsService.GetSystemObjectValueAsync("psp_webhook_domain");

                // If a specific webhook domain is set for the PSP always use it.
                if (!String.IsNullOrWhiteSpace(pspWebhookDomain))
                {
                    if (!pspWebhookDomain.StartsWith("http", StringComparison.Ordinal) && !pspWebhookDomain.StartsWith("//", StringComparison.Ordinal))
                    {
                        pspWebhookDomain = $"https://{pspWebhookDomain}";
                    }

                    webhookUrl = new UriBuilder(pspWebhookDomain);
                }
                // The PSP can't reach our development and test environments, so use the main domain in those cases.
                else if (gclSettings.Environment.InList(Environments.Development, Environments.Test))
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
                    webhookUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext));
                }

                webhookUrl.Path = (orderProcessId == 0 ? Constants.DirectPaymentInPage : Constants.PaymentInPage);

                var queryString = HttpUtility.ParseQueryString(webhookUrl.Query);
                if (orderProcessId > 0)
                {
                    queryString[Constants.OrderProcessIdRequestKey] = orderProcessId.ToString();
                }
                queryString[Constants.SelectedPaymentMethodRequestKey] = paymentMethodSettings.Id.ToString();
                webhookUrl.Query = queryString.ToString()!;
                paymentMethodSettings.PaymentServiceProvider.WebhookUrl = webhookUrl.ToString();

                // Build the return URL.
                var returnUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext))
                {
                    Path = (orderProcessId == 0 ? Constants.DirectPaymentReturnPage : Constants.PaymentReturnPage)
                };
                queryString = HttpUtility.ParseQueryString(returnUrl.Query);
                if (orderProcessId > 0)
                {
                    queryString[Constants.OrderProcessIdRequestKey] = orderProcessId.ToString();
                }
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
                if (orderProcessId > 0)
                {
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
                }

                // Convert baskets to concept orders.
                var orderId = 0UL;
                var basketSettings = await shoppingBasketsService.GetSettingsAsync();
                foreach (var (main, lines) in shoppingBaskets)
                {
                    var copyBasketToConceptOrderOnPspPayment  = (await objectsService.FindSystemObjectByDomainNameAsync("psp_copy_basket_to_concept_order","1")) == "1";
                    if (paymentMethodSettings.PaymentServiceProvider.Type != PaymentServiceProviders.NoPsp && basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert && copyBasketToConceptOrderOnPspPayment)
                    {
                        // Converting a basket to a concept order is only allowed for payment methods that don't go via an external PSP.
                        // Otherwise users can create an order with only one product, start a payment, go back to the website and add several more products,
                        // then finish their original payment and they will have several free products. This is not possible when there is no external PSP, that's why we allow it there.
                        basketToConceptOrderMethod = OrderProcessBasketToConceptOrderMethods.CreateCopy;
                    }

                    var (conceptOrderId, conceptOrder, conceptOrderLines) = await shoppingBasketsService.MakeConceptOrderFromBasketAsync(main, lines, basketSettings, basketToConceptOrderMethod);

                    conceptOrders.Add((conceptOrder, conceptOrderLines));

                    orderId = conceptOrderId;
                }

                // Add order ID to the URLs for later reference.
                var queryParameters = new Dictionary<string, string> {{"order", orderId.ToString().Encrypt()}};
                paymentMethodSettings.PaymentServiceProvider.SuccessUrl = UriHelpers.AddToQueryString(String.IsNullOrEmpty(paymentMethodSettings.PaymentServiceProvider.SuccessUrl) ? $"{webhookUrl.Scheme}://{webhookUrl.Host}/" : paymentMethodSettings.PaymentServiceProvider.SuccessUrl, queryParameters);
                paymentMethodSettings.PaymentServiceProvider.PendingUrl = UriHelpers.AddToQueryString(String.IsNullOrEmpty(paymentMethodSettings.PaymentServiceProvider.PendingUrl) ? $"{webhookUrl.Scheme}://{webhookUrl.Host}/" : paymentMethodSettings.PaymentServiceProvider.PendingUrl, queryParameters);
                // Generate invoice number.
                var invoiceNumber = "";
                var invoiceNumberQuery = (await templatesService.GetTemplateAsync(name: Constants.InvoiceNumberQueryTemplate, type: TemplateTypes.Query))?.Content;
                if (!String.IsNullOrWhiteSpace(invoiceNumberQuery))
                {
                    invoiceNumberQuery = invoiceNumberQuery.Replace("{oid}", orderId.ToString(), StringComparison.OrdinalIgnoreCase).Replace("{orderId}", orderId.ToString(), StringComparison.OrdinalIgnoreCase);
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

                // Make sure the language code has a value.
                if (String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
                {
                    // This function fills the property "CurrentLanguageCode".
                    await languagesService.GetLanguageCodeAsync();
                }

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

                // Allow custom code to be executed before we send the user to the PSP and cancel the payment if the code returned false.
                PaymentRequestResult beforeOutResult = null;
                if (orderProcessSettings == null)
                {
                    beforeOutResult = await orderProcessesService.PaymentRequestBeforeOutAsync(conceptOrders, paymentMethodSettings);
                }
                else
                {
                    beforeOutResult = await orderProcessesService.PaymentRequestBeforeOutAsync(conceptOrders, orderProcessSettings, paymentMethodSettings);
                }
                
                if (beforeOutResult is {Successful: false})
                {
                    if (String.IsNullOrWhiteSpace(beforeOutResult.ActionData) && beforeOutResult.Action == PaymentRequestActions.Redirect)
                    {
                        beforeOutResult.ActionData = failUrl;
                    }
                    if (String.IsNullOrWhiteSpace(beforeOutResult.ErrorMessage))
                    {
                        beforeOutResult.ErrorMessage = "Custom code in PaymentRequestBeforeOutAsync returned an unsuccessful result.";
                    }

                    // Delete the concept order(s) if this failed.
                    await DeleteConceptOrdersAsync(conceptOrders);

                    return beforeOutResult;
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
                    await HandlePaymentStatusUpdateAsync(orderProcessesService, orderProcessSettings, conceptOrders, "Success", true, convertConceptOrderToOrder);

                    return new PaymentRequestResult
                    {
                        Successful = true,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = paymentMethodSettings.PaymentServiceProvider.SuccessUrl
                    };
                }

                // Get the correct service based on type.
                var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentMethodSettings.PaymentServiceProvider.Type);
                paymentServiceProviderService.LogPaymentActions = paymentMethodSettings.PaymentServiceProvider.LogAllRequests;

                return await paymentServiceProviderService.HandlePaymentRequestAsync(conceptOrders, userDetails, paymentMethodSettings, uniquePaymentNumber);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, $"An exception occurred in {Constants.PaymentOutPage}");

                try
                {
                    // Delete the concept order(s) if this failed.
                    if (basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert)
                    {
                        // Convert concept order back to basket
                        foreach (var (main, lines) in conceptOrders)
                        {
                            await shoppingBasketsService.RevertConceptOrderToBasketAsync(main, lines);
                        }
                    }
                    else
                    {
                        // Delete concept order (is copy of basket)
                        await DeleteConceptOrdersAsync(conceptOrders);    
                    }
                }
                catch (Exception deleteException)
                {
                    logger.LogError(deleteException, "Tried to delete concept orders after exception, but failed.");
                }

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
            var hasAlreadyBeenConvertedToOrderBefore = false;

            // Make sure the language code has a value.
            if ((orderProcessSettings != null) && String.IsNullOrWhiteSpace(languagesService.CurrentLanguageCode))
            {
                // This function fills the property "CurrentLanguageCode".
                await languagesService.GetLanguageCodeAsync();
            }

            foreach (var (main, lines) in conceptOrders)
            {
                hasAlreadyBeenConvertedToOrderBefore = main.EntityType == Constants.OrderEntityType;

                if (orderProcessSettings == null)
                {
                    main.SetDetail(Constants.PaymentHistoryProperty, $"{DateTime.Now:yyyyMMddHHmmss} - {newStatus}", true);
                    await shoppingBasketsService.SaveAsync(main, lines, basketSettings);

                    // If order is not finished yet and the payment was successful.
                    if (!hasAlreadyBeenConvertedToOrderBefore && isSuccessfulStatus && convertConceptOrderToOrder)
                    {
                        await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                    }

                    // Allow custom code to be executed before we send the user to the PSP and cancel the payment if the code returned false.
                    var success = await orderProcessesService.PaymentStatusUpdateBeforeCommunicationAsync(main, lines, hasAlreadyBeenConvertedToOrderBefore, isSuccessfulStatus);    
                
                    if (!success)
                    {
                        return false;
                    }
                }
                else
                {
                    var emailContent = "";
                    var emailSubject = "";
                    var userEmailAddress = "";
                    var merchantEmailAddress = "";
                    var bcc = "";
                    var senderAddress = "";
                    var senderName = "";
                    var replyToAddress = "";
                    var replyToName = "";
                    
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

                    main.SetDetail("user_mail_body", emailContent);
                    main.SetDetail("user_mail_subject", emailSubject);
                    main.SetDetail(Constants.PaymentHistoryProperty, $"{DateTime.Now:yyyyMMddHHmmss} - {newStatus}", true);
                    await shoppingBasketsService.SaveAsync(main, lines, basketSettings);

                    // If order is not finished yet and the payment was successful.
                    if (!hasAlreadyBeenConvertedToOrderBefore && isSuccessfulStatus && convertConceptOrderToOrder)
                    {
                        await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                    }

                    // Allow custom code to be executed before we send the user to the PSP and cancel the payment if the code returned false.
                    var success = await orderProcessesService.PaymentStatusUpdateBeforeCommunicationAsync(main, lines, orderProcessSettings, hasAlreadyBeenConvertedToOrderBefore, isSuccessfulStatus);

                    if (!success)
                    {
                        return false;
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
            }

            if ((orderProcessSettings == null) || (hasAlreadyBeenConvertedToOrderBefore))
            {
                return true;
            }

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
            var paymentMethodSettings = await orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
            OrderProcessSettingsModel orderProcessSettings = null;
            
            if (orderProcessId > 0)
            {
                orderProcessSettings = await orderProcessesService.GetOrderProcessSettingsAsync(orderProcessId);
                if (orderProcessSettings == null || orderProcessSettings.Id == 0 || paymentMethodSettings == null || paymentMethodSettings.Id == 0)
                {
                    logger.LogError($"Called HandlePaymentServiceProviderWebhookAsync with invalid orderProcessId ({orderProcessId}) and/or invalid paymentMethodId ({paymentMethodId}). Full URL: {HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)}");
                    return false;
                }    
            }

            // Create the correct service for the payment service provider using the factory.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentMethodSettings.PaymentServiceProvider.Type);
            paymentServiceProviderService.LogPaymentActions = paymentMethodSettings.PaymentServiceProvider.LogAllRequests;

            var invoiceNumber = paymentServiceProviderService.GetInvoiceNumberFromRequest();
            var conceptOrders = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(invoiceNumber);

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
            var steps = await orderProcessesService.GetAllStepsGroupsAndFieldsAsync(orderProcessId);
                
            // Build the fail, success and pending URLs.
            var (failUrl, successUrl, pendingUrl) = BuildUrls(orderProcessSettings, steps);
            
            if (orderProcessSettings == null || orderProcessSettings.Id == 0)
            {
                logger.LogError($"Called HandlePaymentReturnAsync with invalid orderProcessId ({orderProcessId}). Full URL: {HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)}");
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = failUrl
                };
            }
            
            return await HandlePaymentReturnAsync(orderProcessesService, orderProcessId, paymentMethodId, failUrl, successUrl, pendingUrl, orderProcessSettings);
        }

        /// <inheritdoc />
        public async Task<PaymentReturnResult> HandlePaymentReturnAsync(IOrderProcessesService orderProcessesService, ulong orderProcessId, ulong paymentMethodId, string failUrl, string successUrl, string pendingUrl, OrderProcessSettingsModel orderProcessSettings)
        {
            var paymentMethodSettings = await orderProcessesService.GetPaymentMethodAsync(paymentMethodId);
            
            if (paymentMethodSettings == null || paymentMethodSettings.Id == 0)
            {
                logger.LogError($"Called HandlePaymentReturnAsync with invalid paymentMethodId ({paymentMethodId}). Full URL: {HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)}");
                return new PaymentReturnResult
                {
                    Action = PaymentResultActions.Redirect,
                    ActionData = failUrl
                };
            }
            
            if (!String.IsNullOrEmpty(failUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.FailUrl = failUrl;    
            }

            if (!String.IsNullOrEmpty(successUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.SuccessUrl = successUrl;    
            }
            if (!String.IsNullOrEmpty(pendingUrl))
            {
                paymentMethodSettings.PaymentServiceProvider.PendingUrl = pendingUrl;    
            }


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
            return files.MaxBy(file => file.Id);
        }

        /// <inheritdoc />
        public Task<PaymentRequestResult> PaymentRequestBeforeOutAsync(List<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            // We do nothing here. This function is meant to overwrite in projects so custom code snippets can be executed.
            // Use the decorator pattern to create an overwrite of IOrderProcessesService and then add custom code in this snippet.
            return Task.FromResult(new PaymentRequestResult { Successful = true });
        }
        
        /// <inheritdoc />
        public Task<PaymentRequestResult> PaymentRequestBeforeOutAsync(List<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, PaymentMethodSettingsModel paymentMethodSettings)
        {
            // We do nothing here. This function is meant to overwrite in projects so custom code snippets can be executed.
            // Use the decorator pattern to create an overwrite of IOrderProcessesService and then add custom code in this snippet.
            return Task.FromResult(new PaymentRequestResult { Successful = true });
        }

        /// <inheritdoc />
        public Task<bool> PaymentStatusUpdateBeforeCommunicationAsync(WiserItemModel main, List<WiserItemModel> lines, OrderProcessSettingsModel orderProcessSettings, bool wasHandledBefore, bool isSuccessfulStatus)
        {
            // We do nothing here. This function is meant to overwrite in projects so custom code snippets can be executed.
            // Use the decorator pattern to create an overwrite of IOrderProcessesService and then add custom code in this snippet.
            return Task.FromResult(true);
        }
        
        /// <inheritdoc />
        public Task<bool> PaymentStatusUpdateBeforeCommunicationAsync(WiserItemModel main, List<WiserItemModel> lines, bool wasHandledBefore, bool isSuccessfulStatus)
        {
            // We do nothing here. This function is meant to overwrite in projects so custom code snippets can be executed.
            // Use the decorator pattern to create an overwrite of IOrderProcessesService and then add custom code in this snippet.
            return Task.FromResult(true);
        }

        /// <summary>
        /// Converts a <see cref="DataRow"/> to a <see cref="PaymentMethodSettingsModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns>A <see cref="PaymentMethodSettingsModel"/> with the data from the <see cref="DataRow"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When using a PaymentServiceProvider that we don't support.</exception>
        private async Task<PaymentMethodSettingsModel> DataRowToPaymentMethodSettingsModelAsync(DataRow dataRow)
        {
            // Build the payment settings model.
            var fee = dataRow.Field<decimal?>("paymentMethodFee") ?? 0;
            var minimalAmountProperty = dataRow.Field<decimal?>(Constants.PaymentMethodMinimalAmountProperty) ?? 0;
            var maximumAmountProperty = dataRow.Field<decimal?>(Constants.PaymentMethodMaximumAmountProperty) ?? 0;

            var result = new PaymentMethodSettingsModel
            {
                Id = dataRow.Field<ulong>("paymentMethodId"),
                Title = dataRow.Field<string>("paymentMethodTitle"),
                Fee = fee,
                Visibility = EnumHelpers.ToEnum<OrderProcessFieldVisibilityTypes>(dataRow.Field<string>("paymentMethodVisibility") ?? "Always"),
                ExternalName = dataRow.Field<string>("paymentMethodExternalName"),
                UseMinimalAmountCheck =  Convert.ToBoolean(dataRow.Field<Int64>(Constants.PaymentMethodUseMinimalAmountProperty)),
                UseMaximumAmountCheck =  Convert.ToBoolean(dataRow.Field<Int64>(Constants.PaymentMethodUseMaximumAmountProperty)),
                MinimalAmountCheck =  minimalAmountProperty,
                MaximumAmountCheck = maximumAmountProperty,
                PaymentServiceProvider = new PaymentServiceProviderSettingsModel
                {
                    // Settings that are the same for all PSPs.
                    Id = dataRow.Field<ulong>("paymentServiceProviderId"),
                    Title = dataRow.Field<string>("paymentServiceProviderTitle"),
                    Type = EnumHelpers.ToEnum<PaymentServiceProviders>(dataRow.Field<string>("paymentServiceProviderType") ?? "0"),
                    LogAllRequests = dataRow.Field<string>("paymentServiceProviderLogAllRequests") == "1",
                    OrdersCanBeSetDirectlyToFinished = dataRow.Field<string>("paymentServiceProviderSetOrdersDirectlyToFinished") == "1",
                    SkipPaymentWhenOrderAmountEqualsZero = dataRow.Field<string>("paymentServiceProviderSkipWhenOrderAmountEqualsZero") == "1",
                    SuccessUrl = dataRow.Field<string>("paymentServiceProviderSuccessUrl"),
                    PendingUrl = dataRow.Field<string>("paymentServiceProviderPendingUrl"),
                    FailUrl = dataRow.Field<string>("paymentServiceProviderFailUrl") 
                }
            };

            if (String.IsNullOrEmpty(result.ExternalName))
            {
                result.ExternalName = result.Title;
            }

            // Build the PSP settings model based on the type of PSP.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(result.PaymentServiceProvider.Type);
            result.PaymentServiceProvider = await paymentServiceProviderService.GetProviderSettingsAsync(result.PaymentServiceProvider);

            return result;
        }

        /// <summary>
        /// Get all values we need for sending an e-mail, such as an order confirmation.
        /// </summary>
        /// <param name="orderProcessSettings">The order process settings.</param>
        /// <param name="conceptOrder">The <see cref="WiserItemModel"/> of the (concept) order.</param>
        /// <param name="conceptOrderLines">A list of <see cref="WiserItemModel"/> with all (concept) order lines.</param>
        /// <param name="forMerchantMail">Optional: Set to true when this is meant for an e-mail that is sent to the merchant, or false when this is for an e-mail to the user. Default value is false.</param>
        /// <param name="forAttachment">Optional: Set to true if you're calling this method to get the template for an e-mail attachment, such as an invoice PDF.</param>
        /// <returns>An <see cref="EmailValues"/> with all data needed to send the e-mail.</returns>
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

            // Get customer basket email instead of the potentially linked user email address.
            if (!String.IsNullOrWhiteSpace(orderProcessSettings.EmailAddressProperty) && !String.IsNullOrWhiteSpace(conceptOrder.GetDetailValue(orderProcessSettings.EmailAddressProperty)))
            {
                userEmailAddress = conceptOrder.GetDetailValue(orderProcessSettings.EmailAddressProperty);
            }

            // Get merchant basket email instead of the potentially linked merchant email address.
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
        /// Build URLs that can be used in the order process, such as the URL for a successful payment and an URL for a failed payment.
        /// </summary>
        /// <param name="orderProcessSettings">The settings of the current order process.</param>
        /// <param name="steps">The list of steps from the current order process.</param>
        /// <returns>The new URLs.</returns>
        private (string FailUrl, string SuccessUrl, string PendingUrl) BuildUrls(OrderProcessSettingsModel orderProcessSettings, List<OrderProcessStepModel> steps)
        {
            var failUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)) { Path = orderProcessSettings.FixedUrl };
            var successUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)) { Path = orderProcessSettings.FixedUrl };
            var pendingUrl = new UriBuilder(HttpContextHelpers.GetBaseUri(httpContextAccessor?.HttpContext)) { Path = orderProcessSettings.FixedUrl };

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

        /// <summary>
        /// Delete concept orders. This should be called if an error occurs before the user is sent to the PSP.
        /// </summary>
        /// <param name="conceptOrders"></param>
        private async Task DeleteConceptOrdersAsync(List<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders)
        {
            if (conceptOrders == null)
            {
                return;
            }

            foreach (var (main, lines) in conceptOrders)
            {
                await wiserItemsService.DeleteAsync(main.Id, entityType: Constants.ConceptOrderEntityType, skipPermissionsCheck: true);
                foreach (var line in lines)
                {
                    await wiserItemsService.DeleteAsync(line.Id, entityType: Constants.OrderLineEntityType, skipPermissionsCheck: true);
                }
            }
        }
    }
}