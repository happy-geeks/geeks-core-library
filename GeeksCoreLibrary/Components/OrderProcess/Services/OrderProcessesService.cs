using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.OrderProcess.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Constants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.OrderProcess.Services
{
    public class OrderProcessesService : IOrderProcessesService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;

        public OrderProcessesService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<OrderProcessSettingsModel> GetOrderProcessViaFixedUrlAsync(string fixedUrl)
        {
            if (String.IsNullOrWhiteSpace(fixedUrl))
            {
                throw new ArgumentNullException(nameof(fixedUrl));
            }
            
            var query = @$"SELECT 
                                orderProcess.id,
	                            IFNULL(titleSeo.value, orderProcess.title) AS name
                            FROM {WiserTableNames.WiserItem} AS orderProcess
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS fixedUrl ON fixedUrl.item_id = orderProcess.id AND fixedUrl.`key` = '{Constants.OrderProcessUrlProperty}'
                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS titleSeo ON titleSeo.item_id = orderProcess.id AND titleSeo.`key` = '{CoreConstants.SeoTitlePropertyName}'
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

            var firstRow = dataTable.Rows[0];
            return new OrderProcessSettingsModel
            {
                Id = firstRow.Field<ulong>("id"),
                Title = firstRow.Field<string>("name"),
                FixedUrl = fixedUrl
            };
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
        public async Task<List<PaymentMethodSettingsModel>> GetPaymentMethodsAsync(ulong orderProcessId)
        {
            if (orderProcessId == 0)
            {
                throw new ArgumentNullException(nameof(orderProcessId));
            }

            var results = new List<PaymentMethodSettingsModel>();
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

            foreach (DataRow dataRow in dataTable.Rows)
            {
                Decimal.TryParse(dataRow.Field<string>("paymentMethodFee")?.Replace(",", "."), NumberStyles.Any, new CultureInfo("en-US"), out var fee);
                results.Add(new PaymentMethodSettingsModel
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
                });
            }

            return results;
        }
    }
}
