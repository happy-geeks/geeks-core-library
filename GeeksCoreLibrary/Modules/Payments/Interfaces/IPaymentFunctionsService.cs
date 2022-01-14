using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Payments.Interfaces
{
    public interface IPaymentFunctionsService
    {
        async Task<bool> TransactionStartedAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
            return true;
        }

        async Task<bool> TransactionFinishedAsync(string invoiceNumber, bool successful, bool orderSetToFinished, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
            return true;
        }

        async Task<bool> TransactionUpdateAsync(string invoiceNumber, string pspAction, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders, object extraData)
        {
            return true;
        }

        async Task TransactionReturnAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }

        async Task TransactionBeforeOutAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }

        async Task TransactionBeforeOutRedirectAsync(string invoiceNumber, string url, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }
    }
}
