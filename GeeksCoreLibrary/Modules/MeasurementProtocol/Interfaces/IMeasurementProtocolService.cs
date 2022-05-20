using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces
{
    public interface IMeasurementProtocolService
    {
        Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice);

        Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice, string paymentMethodId);

        Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, List<WiserItemModel> shoppingBasketLines, decimal totalBasketPrice, decimal tax, string transactionId);
    }
}
