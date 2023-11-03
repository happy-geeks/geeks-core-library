using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    /// <summary>
    /// A service for handling payments via an PSP.
    /// </summary>
    public interface IPaymentServiceProviderService
    {
        /// <summary>
        /// Gets or sets whether or not to log all payment actions to the database (log_psp).
        /// </summary>
        public bool LogPaymentActions { get; set; }

        /// <summary>
        /// Starts a new payment request at the PSP.
        /// </summary>
        /// <param name="conceptOrders">A list of one or more (concept) orders that the user is going to pay for.</param>
        /// <param name="userDetails">The details of the user that is going to pay.</param>
        /// <param name="paymentMethodSettings">The payment method that the user selected.</param>
        /// <param name="invoiceNumber">The invoice number for the order, this will be sent to the PSP.</param>
        /// <returns>A <see cref="PaymentRequestResult"/> with the results of the payment request.</returns>
        Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> conceptOrders, WiserItemModel userDetails, PaymentMethodSettingsModel paymentMethodSettings, string invoiceNumber);

        /// <summary>
        /// Processes a status update (webhook) from the PSP.
        /// </summary>
        /// <param name="orderProcessSettings">The settings for the order process that is being used.</param>
        /// <param name="paymentMethodSettings">The settings for the payment method that the user selected.</param>
        /// <returns>A <see cref="StatusUpdateResult"/> with the results from the status update.</returns>
        Task<StatusUpdateResult> ProcessStatusUpdateAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings);

        /// <summary>
        /// Determines what to do after a user is returned to the webshop after a payment. This is used for payment service providers that do not offer specific return URL's for multiple
        /// states, like successful state, error state, cancel state, etc.
        /// </summary>
        /// <param name="orderProcessSettings">The settings for the order process that is being used.</param>
        /// <param name="paymentMethodSettings">The settings for the payment method that the user selected.</param>
        Task<PaymentReturnResult> HandlePaymentReturnAsync(OrderProcessSettingsModel orderProcessSettings, PaymentMethodSettingsModel paymentMethodSettings)
        {
            // Default implementation is to do nothing and return a PaymentReturnResult object with its action set to None.
            return Task.FromResult(new PaymentReturnResult
            {
                Action = PaymentResultActions.None
            });
        }

        /// <summary>
        /// Gets the settings for the current payment service provider. This requires a <see cref="PaymentServiceProviderSettingsModel"/> with basic properties set, like the ID.
        /// This will then return a model with all the settings for the specific PSP.
        /// </summary>
        /// <param name="paymentServiceProviderSettings">The basic settings for the payment service provider.</param>
        Task<PaymentServiceProviderSettingsModel> GetProviderSettingsAsync(PaymentServiceProviderSettingsModel paymentServiceProviderSettings);

        /// <summary>
        /// Gets the invoice number from the request.
        /// Each PSP sends this number in their own way, this method will get the number from the request based on which PSP is being used.
        /// </summary>
        /// <returns>The invoice number.</returns>
        string GetInvoiceNumberFromRequest();
    }
}