namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Constants that are used in the order/checkout process.
    /// </summary>
    public class Constants
    {
        #region Entity types

        public const string OrderProcessEntityType = "WiserOrderProcess";

        public const string PaymentProviderEntityType = "WiserPaymentprovider";

        public const string PaymentMethodEntityType = "WiserPaymentmethod";

        public const string GroupEntityType = "WiserOrderProcessGroup";

        public const string StepEntityType = "WiserOrderProcessStep";

        public const string FormFieldEntityType = "WiserFormField";

        #endregion

        #region Fields

        public const string OrderProcessUrlProperty = "orderprocessurl";

        #endregion
    }
}
