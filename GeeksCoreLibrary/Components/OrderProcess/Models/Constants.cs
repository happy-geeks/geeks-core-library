namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Constants that are used in the order/checkout process.
    /// </summary>
    public class Constants
    {
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

        public const string OrderProcessUrlProperty = "orderprocessurl";

        public const string GroupTypeProperty = "orderprocessgrouptype";

        public const string GroupHeaderProperty = "orderprocessgroupheader";

        public const string GroupFooterProperty = "orderprocessgroupfooter";

        public const string FieldIdProperty = "formfieldid";

        public const string FieldLabelProperty = "formfieldlabel";

        public const string FieldPlaceholderProperty = "formfieldplaceholder";

        public const string FieldTypeProperty = "formfieldtype";

        public const string FieldInputTypeProperty = "formfieldinputtype";

        public const string FieldValuesProperty = "formfieldvalues";

        public const string FieldMandatoryProperty = "formfieldmandatory";

        public const string FieldValidationPatternProperty = "formfieldregexcheck";

        public const string FieldVisibilityProperty = "formfieldvisible";

        #endregion
    }
}
