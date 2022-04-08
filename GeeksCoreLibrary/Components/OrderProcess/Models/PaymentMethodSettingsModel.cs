using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for settings for a payment method.
    /// </summary>
    public class PaymentMethodSettingsModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the external name for the payment method. This is the name that will be sent to the PSP.
        /// </summary>
        public string ExternalName { get; set; }

        /// <summary>
        /// Gets or sets the PSP that should be used for this payment method.
        /// </summary>
        public PaymentServiceProviderSettingsModel PaymentServiceProvider { get; set; }

        /// <summary>
        /// Gets or sets the fee that the user needs to pay to use this payment method.
        /// </summary>
        public decimal Fee { get; set; }
        
        /// <summary>
        /// Gets or sets when the field should be visible.
        /// </summary>
        public OrderProcessFieldVisibilityTypes Visibility { get; set; }
    }
}
