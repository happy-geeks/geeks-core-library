using EvoPdf;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for settings for a PSP.
    /// </summary>
    public class PaymentServiceProviderSettingsModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets whether or not to log all requests done to the API of the PSP to the database.
        /// </summary>
        public bool LogAllRequests { get; set; }

        /// <summary>
        /// Gets or sets whether orders made via this PSP should be set directory to finished (and therefor skip the actual PSP).
        /// </summary>
        public bool OrdersCanBeSetDirectlyToFinished { get; set; }

        /// <summary>
        /// Gets or sets whether order with a total price of 0 should skip the PSP and be directly marked as finished.
        /// </summary>
        public bool SkipPaymentWhenOrderAmountEqualsZero { get; set; }

        /// <summary>
        /// Gets or sets the URL to send the user to after a successful payment/order.
        /// </summary>
        public string SuccessUrl { get; set; }
    }
}
