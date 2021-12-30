using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Interfaces
{
    public interface IShoppingBasketsService
    {
        /// <summary>
        /// Get all baskets via unique payment number.
        /// </summary>
        /// <param name="uniquePaymentNumber">The value of the UniquePaymentNumber detail of Main.</param>
        /// <returns>A <see cref="List{WiserItemModel}"/> of <see cref="WiserItemModel"/> objects.</returns>
        Task<List<(WiserItemModel ShoppingBasket, List<WiserItemModel> BasketLines)>> GetOrdersByUniquePaymentNumberAsync(string uniquePaymentNumber);

        /// <summary>
        /// Gets all baskets through a cookie name.
        /// </summary>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync(string cookieName);

        /// <summary>
        /// Get a basket item ID from a cookie value.
        /// </summary>
        /// <param name="cookieName">The cookie name that contains the encrypted value of the basket item ID.</param>
        /// <returns>A <see cref="UInt64"/> representing the basket item ID.</returns>
        ulong GetBasketItemId(string cookieName);

        /// <summary>
        /// Decrypts a string and returns the item ID.
        /// </summary>
        /// <param name="encryptedId">An encrypted string.</param>
        /// <returns></returns>
        ulong DecryptBasketItemId(string encryptedId);

        /// <summary>
        /// Encrypts an item ID with AES, using the default encryption key for basket item IDs.
        /// </summary>
        /// <param name="itemId">The basket item ID to encrypt.</param>
        /// <returns>A Base64 string of the encrypted item ID.</returns>
        string EncryptBasketItemId(ulong itemId);

        /// <summary>
        /// Calculates the discount value of a coupon.
        /// </summary>
        /// <param name="coupon"></param>
        /// <param name="totalProductsPrice"></param>
        /// <param name="maxDiscountIsTotalAmountProducts"></param>
        /// <returns></returns>
        decimal CalculateCouponValue(WiserItemModel coupon, decimal totalProductsPrice, bool maxDiscountIsTotalAmountProducts = false, decimal currentDiscountAmount = 0M);

        /// <summary>
        /// Increments the use count of a coupon by 1 or, if enabled, updates the discount amount of the coupon to the remainder if the value of the coupon
        /// exceeds the total price of the basket (only works with fixed amounts and when the max use count is 1).
        /// </summary>
        /// <param name="coupon"></param>
        /// <param name="totalProductsPrice"></param>
        /// <returns></returns>
        Task<bool> UseCouponAsync(WiserItemModel coupon, decimal totalProductsPrice);

        /// <summary>
        /// Takes certain values from the request and adds them to the basket details.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        Task UpdateShoppingBasketWithRequestDataAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Create a concept order out of a basket.
        /// </summary>
        /// <returns></returns>
        Task<(ulong ConceptOrderId, WiserItemModel ConceptOrder, List<WiserItemModel> ConceptOrderLines)> MakeConceptOrderFromBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Turns a concept order into a final order.
        /// </summary>
        /// <returns></returns>
        Task ConvertConceptOrderToOrderAsync(WiserItemModel conceptOrder, ShoppingBasketCmsSettingsModel settings);

        Task<string> ReplaceBasketInTemplateAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string template, bool replaceUserAccountVariables = false, bool stripNotExistingVariables = true, IDictionary<string, string> userDetails = null, bool isForConfirmationEmail = false, IDictionary<string, object> additionalReplacementData = null);

        Task<decimal> GetPriceAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, string lineType = "", int onlyIfVatRate = -1, bool includeDiscountGettingVat = true);

        /// <summary>
        /// Function to recalculate the shipping-costs, re-evaluate the coupon, etc. after changing quantities, adding or removing products, etc.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="skipType">An optional parameter to skip lines of a certain type.</param>
        Task RecalculateVariablesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string skipType = null);

        /// <summary>
        /// Loads a basket from the database.
        /// </summary>
        Task<(WiserItemModel ShoppingBasket, List<WiserItemModel> BasketLines, string BasketLineValidityMessage, string BasketLineStockActionMessage)> LoadAsync(ShoppingBasketCmsSettingsModel settings, ulong itemId = 0UL, string encryptedItemId = "", bool connectToAccount = true, bool recursiveCall = false);

        /// <summary>
        /// Saves the current basket to the database.
        /// </summary>
        Task<WiserItemModel> SaveAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Calculates the shipping costs based on the shipping costs query defined in the settings module.
        /// </summary>
        /// <returns></returns>
        Task<decimal> CalculateShippingCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Calculates the payment method costs based on the payment method costs query defined in the settings module.
        /// </summary>
        /// <returns></returns>
        Task<decimal> CalculatePaymentMethodCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        Task<List<WiserItemModel>> RemoveLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ICollection<string> itemIdsOrUniqueIds);

        Task AddLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0UL, decimal quantity = 1M, string type = "product", IDictionary<string, string> lineDetails = null);

        Task AddLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, IList<AddToShoppingBasketModel> items);

        /// <summary>
        /// Attempts to update the quantity
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="itemIdOrUniqueId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task UpdateBasketLineQuantityAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string itemIdOrUniqueId, decimal quantity);

        Task<ShoppingBasket.HandleCouponResults> AddCouponToBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode = "", bool recalculateCoupon = false);

        /// <summary>
        /// Get lines of a specific type.
        /// </summary>
        /// <param name="basketLines"></param>
        /// <param name="lineType">The type of lines to look for.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/> objects that represent the order lines of the given type.</returns>
        List<WiserItemModel> GetLines(List<WiserItemModel> basketLines, string lineType);

        /// <summary>
        /// Checks if any of the free products are eligible and add, removes or updates the lines if applicable.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        Task CheckForFreeProductAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        Task<IList<WiserItemModel>> GetFreeProductActionsAsync();

        Task<IList<VatRule>> GetVatRulesAsync();

        /// <summary>
        /// Creates a <see cref="ShoppingBasketCmsSettingsModel"/> object with various settings retrieved from system objects.
        /// </summary>
        /// <returns>A <see cref="ShoppingBasketCmsSettingsModel"/> object.</returns>
        Task<ShoppingBasketCmsSettingsModel> GetSettingsAsync();
    }
}
