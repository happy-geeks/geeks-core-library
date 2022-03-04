using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentsService
    {
        Task<PaymentRequestResult> HandlePaymentRequestAsync();

        Task<bool> HandleStatusUpdateAsync();

        Task<bool> ProcessStatusUpdateAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true);

        /// <summary>
        /// Determines what to do after a user is returned to the webshop after a payment. This is used for payment service providers that do not offer specific return URL's for multiple
        /// states, like successful state, error state, cancel state, etc.
        /// </summary>
        Task<PaymentReturnResult> HandlePaymentReturnAsync();
    }
}
