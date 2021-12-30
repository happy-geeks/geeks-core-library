using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentsService
    {
        Task<PaymentRequestResult> HandlePaymentRequestAsync();

        Task<bool> HandleStatusUpdateAsync();

        Task<bool> ProcessStatusUpdateAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true);
    }
}
