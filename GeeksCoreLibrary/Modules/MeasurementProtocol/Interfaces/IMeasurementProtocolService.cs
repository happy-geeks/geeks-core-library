using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Interfaces
{
    public interface IMeasurementProtocolService
    {
        /// <summary>
        /// Send the "begin_checkout" event to Google Analytics.
        /// </summary>
        /// <param name="orderProcessSettings">The settings of the orde process.</param>
        /// <param name="shoppingBasket">The shopping basket to use for the event.</param>
        /// <param name="shoppingBasketLines">The shopping basket lines to use for the event.</param>
        /// <param name="shoppingBasketSettings">The settings of the shopping basket.</param>
        /// <returns></returns>
        Task BeginCheckoutEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings);

        /// <summary>
        /// Send the "add_payment_info" event to Google Analytics.
        /// </summary>
        /// <param name="orderProcessSettings">The settings of the orde process.</param>
        /// <param name="shoppingBasket">The shopping basket to use for the event.</param>
        /// <param name="shoppingBasketLines">The shopping basket lines to use for the event.</param>
        /// <param name="shoppingBasketSettings">The settings of the shopping basket.</param>
        /// <param name="paymentMethodId">The ID of the selected payment method.</param>
        /// <returns></returns>
        Task AddPaymentInfoEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string paymentMethodId);

        /// <summary>
        /// Send the "purchase" event to Google Analytics.
        /// </summary>
        /// <param name="orderProcessSettings">The settings of the orde process.</param>
        /// <param name="shoppingBasket">The shopping basket to use for the event.</param>
        /// <param name="shoppingBasketLines">The shopping basket lines to use for the event.</param>
        /// <param name="shoppingBasketSettings">The settings of the shopping basket.</param>
        /// <param name="transactionId">The ID of the payment transaction.</param>
        /// <returns></returns>
        Task PurchaseEventAsync(OrderProcessSettingsModel orderProcessSettings, WiserItemModel shoppingBasket, List<WiserItemModel> shoppingBasketLines, ShoppingBasketCmsSettingsModel shoppingBasketSettings, string transactionId);
    }
}
