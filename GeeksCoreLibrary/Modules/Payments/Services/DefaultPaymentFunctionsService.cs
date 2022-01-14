using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Payments.Interfaces;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    public class DefaultPaymentFunctionsService : IPaymentFunctionsService, IScopedService
    {
        /// <inheritdoc />
        public async Task<bool> TransactionStartedAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> TransactionFinishedAsync(string invoiceNumber, bool successful, bool orderSetToFinished, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> TransactionUpdateAsync(string invoiceNumber, string pspAction, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders, object extraData)
        {
            return true;
        }

        /// <inheritdoc />
        public async Task TransactionReturnAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }

        /// <inheritdoc />
        public async Task TransactionBeforeOutAsync(string invoiceNumber, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }

        /// <inheritdoc />
        public async Task TransactionBeforeOutRedirectAsync(string invoiceNumber, string url, IList<(WiserItemModel Main, List<WiserItemModel> Lines)> orders)
        {
        }
    }
}
