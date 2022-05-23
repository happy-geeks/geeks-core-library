using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces
{
    public interface IMeasurementProtocolService
    {
        Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings);

        Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string paymentMethodId);

        Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string transactionId);
    }
}
