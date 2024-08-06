using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for settings for a PSP.
    /// </summary>
    public class PaymentServiceProviderSettingsModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the type of PSP.
        /// </summary>
        public PaymentServiceProviders Type { get; set; }

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
        /// Gets or sets the currency to use with the PSP.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Gets or sets the locale to use with the PSP.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the URL to send the user to after a successful payment/order.
        /// </summary>
        public string SuccessUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL for the WebHook of the PSP, to send status updates to us.
        /// </summary>
        public string WebhookUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to send the user to after a failed or cancelled payment/order.
        /// </summary>
        public string FailUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to send the user to if we have to decide on-the-fly where to send the user to.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to send the user to if their payment is still pending.
        /// </summary>
        public string PendingUrl { get; set; }
    }
}