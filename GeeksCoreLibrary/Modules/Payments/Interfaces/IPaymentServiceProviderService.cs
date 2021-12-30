using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Payments.Enums;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentServiceProviderService
    {
        public bool LogPaymentActions { get; set; }

        Task<PaymentRequestResult> HandlePaymentRequestAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, WiserItemModel userDetails, PaymentMethods paymentMethod, string invoiceNumber);

        Task<StatusUpdateResult> ProcessStatusUpdateAsync();
    }
}
