using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentFunctionsService
    {
        Task<bool> TransactionStartedAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders);

        Task<bool> TransactionFinishedAsync(string invoiceNumber, bool successful, bool orderSetToFinished, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders);

        Task<bool> TransactionUpdateAsync(string invoiceNumber, string pspAction, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders, object extraData);

        Task TransactionReturnAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders);

        Task TransactionBeforeOutAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders);

        Task TransactionBeforeOutRedirectAsync(string invoiceNumber, string url, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders);
    }
}
