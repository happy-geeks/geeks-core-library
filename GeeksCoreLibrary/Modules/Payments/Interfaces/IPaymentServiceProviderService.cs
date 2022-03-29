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
        /// <param name="shoppingBaskets">A list of one or more shopping baskets that the user is going to pay for.</param>
        /// <param name="userDetails">The details of the user that is going to pay.</param>
        /// <param name="paymentMethod">The payment method that the user selected.</param>
        /// <param name="invoiceNumber">The invoice number for the order, this will be sent to the PSP.</param>
        /// <returns>A <see cref="PaymentRequestResult"/> with the results of the payment request.</returns>
        Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber);

        /// <summary>
        /// Processes a status update (webhook) from the PSP.
        /// </summary>
        /// <returns>A <see cref="StatusUpdateResult"/> with the results from the status update.</returns>
        Task<StatusUpdateResult> ProcessStatusUpdateAsync();
    }
}
