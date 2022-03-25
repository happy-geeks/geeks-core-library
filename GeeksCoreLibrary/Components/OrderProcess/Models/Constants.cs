namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Constants that are used in the order/checkout process.
    /// </summary>
    public class Constants
    {
        internal const string ComponentIdFormKey = "__orderProcess";

        #region Entity types

        public const string OrderProcessEntityType = "WiserOrderProcess";

        public const string StepEntityType = "WiserOrderProcessStep";

        public const string GroupEntityType = "WiserOrderProcessGroup";

        public const string FormFieldEntityType = "WiserFormField";

        public const string PaymentProviderEntityType = "WiserPaymentprovider";

        public const string PaymentMethodEntityType = "WiserPaymentmethod";

        #endregion

        #region Link types

        public const int StepToProcessLinkType = 5070;

        public const int GroupToStepLinkType = 5071;

        public const int FieldToGroupLinkType = 5072;

        #endregion

        #region Fields

        public const string StepHeaderProperty = "orderprocessstepheader";

        public const string StepFooterProperty = "orderprocessstepfooter";

        public const string StepConfirmButtonTextProperty = "orderprocessstepconfirmbuttontext";

        public const string OrderProcessUrlProperty = "orderprocessurl";

        public const string GroupTypeProperty = "orderprocessgrouptype";

        public const string GroupHeaderProperty = "orderprocessgroupheader";

        public const string GroupFooterProperty = "orderprocessgroupfooter";

        public const string FieldIdProperty = "formfieldid";

        public const string FieldLabelProperty = "formfieldlabel";

        public const string FieldPlaceholderProperty = "formfieldplaceholder";

        public const string FieldTypeProperty = "formfieldtype";

        public const string FieldInputTypeProperty = "formfieldinputtype";

        public const string FieldValuesGroupName = "formfieldvalues";

        public const string FieldMandatoryProperty = "formfieldmandatory";

        public const string FieldValidationPatternProperty = "formfieldregexcheck";

        public const string FieldVisibilityProperty = "formfieldvisible";

        public const string FieldErrorMessageProperty = "formfielderrormessage";

        #endregion

        #region Request values

        public const string ActiveStepRequestKey = "activeStep";

        #endregion

        #region Replacements

        public const string ErrorReplacement = "{error}";

        public const string ErrorTypeReplacement = "{errorType}";

        public const string ErrorMessageReplacement = "{errorMessage}";

        public const string ErrorClassReplacement = "{errorClass}";

        public const string ProgressReplacement = "{progress}";

        public const string HeaderReplacement = "{header}";

        public const string FooterReplacement = "{footer}";

        public const string StepsReplacement = "{steps}";

        public const string StepReplacement = "{step}";

        public const string GroupsReplacement = "{groups}";

        public const string FieldsReplacement = "{fields}";

        public const string FieldOptionsReplacement = "{options}";

        #endregion
    }
}
