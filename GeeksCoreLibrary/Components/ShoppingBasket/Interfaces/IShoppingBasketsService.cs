using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Models;
using JetBrains.Annotations;

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
        /// <returns></returns>
        Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync();

        /// <summary>
        /// Gets all baskets through a cookie name.
        /// </summary>
        /// <param name="cookieName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync(string cookieName, ShoppingBasketCmsSettingsModel settings);

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
        /// <param name="currentDiscountAmount"></param>
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
        /// Create a concept order out of a basket.
        /// </summary>
        /// <returns></returns>
        Task<(ulong ConceptOrderId, WiserItemModel ConceptOrder, List<WiserItemModel> ConceptOrderLines)> MakeConceptOrderFromBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Turns a concept order into a final order.
        /// </summary>
        /// <returns></returns>
        Task ConvertConceptOrderToOrderAsync(WiserItemModel conceptOrder, ShoppingBasketCmsSettingsModel settings);

        Task<string> ReplaceBasketInTemplateAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string template, bool replaceUserAccountVariables = false, bool stripNotExistingVariables = true, IDictionary<string, string> userDetails = null, bool isForConfirmationEmail = false, IDictionary<string, object> additionalReplacementData = null, bool forQuery = false);

        Task<decimal> GetPriceAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, string lineType = "", int onlyIfVatRate = -1, bool includeDiscountGettingVat = true);
        
        /// <summary>
        /// Gets the total price of a single basket line.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="line"></param>
        /// <param name="settings"></param>
        /// <param name="priceType"></param>
        /// <param name="singlePrice"></param>
        /// <param name="round"></param>
        /// <param name="onlyIfVatRate"></param>
        /// <param name="withoutFactor"></param>
        /// <returns></returns>
        Task<decimal> GetLinePriceAsync(WiserItemModel shoppingBasket, WiserItemModel line, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, bool singlePrice = false, bool round = false, int onlyIfVatRate = -1, bool withoutFactor = false);

        /// <summary>
        /// Function to recalculate the shipping-costs, re-evaluate the coupon, etc. after changing quantities, adding or removing products, etc.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="skipType">An optional parameter to skip lines of a certain type.</param>
        /// <param name="createNewTransaction">Will be passed to the SaveAsync call.</param>
        Task RecalculateVariablesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string skipType = null, bool createNewTransaction = true);

        /// <summary>
        /// Loads a basket from the database.
        /// </summary>
        Task<(WiserItemModel ShoppingBasket, List<WiserItemModel> BasketLines, string BasketLineValidityMessage, string BasketLineStockActionMessage)> LoadAsync(ShoppingBasketCmsSettingsModel settings, ulong itemId = 0UL, string encryptedItemId = "", bool connectToAccount = true, bool recursiveCall = false);

        /// <summary>
        /// Saves the current basket to the database.
        /// </summary>
        Task<WiserItemModel> SaveAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true);

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

        /// <summary>
        /// Adds multiple items to the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that called this function.</param>
        /// <param name="uniqueId">The unique ID of the item. If null or empty, the <paramref name="itemId"/> value will be used.</param>
        /// <param name="itemId">The ID of the item that will be added.</param>
        /// <param name="quantity">The quantity that should be added.</param>
        /// <param name="type">The type of the item. Defaults to "product".</param>
        /// <param name="lineDetails">Additional properties for the basket line.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        /// <returns></returns>
        Task AddLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0UL, int quantity = 1, string type = "product", IDictionary<string, string> lineDetails = null, bool createNewTransaction = true);

        /// <summary>
        /// Adds multiple items to the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that called this function.</param>
        /// <param name="items">The data of the lines that will be added.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        /// <returns></returns>
        Task AddLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, IList<AddToShoppingBasketModel> items, bool createNewTransaction = true);

        /// <summary>
        /// Updates an existing line in the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="item">The data of the line that will be replaced. The <see cref="UpdateItemModel.LineId"/> property of this object will determine which line will be replaced.</param>
        /// <returns></returns>
        Task UpdateLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, UpdateItemModel item);

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

        /// <summary>
        /// Will attempt to add a coupon to the shopping basket by checking if the request variable "couponcode" is present and using that value to see if it matches with a valid coupon.
        /// </summary>
        /// <param name="shoppingBasket">A <see cref="WiserItemModel"/> that references the shopping basket.</param>
        /// <param name="basketLines">The current basket lines in the form of a list of <see cref="WiserItemModel"/> objects.</param>
        /// <param name="settings">The settings of the basket, either the global ones or the ones passed from a <see cref="ShoppingBasket"/> component.</param>
        /// <param name="couponCode">The coupon code to check and process.</param>
        /// <param name="createNewTransaction">Will be passed to the SaveAsync call.</param>
        /// <returns></returns>
        Task<ShoppingBasket.HandleCouponResults> AddCouponToBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode = "", bool createNewTransaction = true);

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

        Task<decimal> GetVatFactorByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate);
        
        /// <summary>
        /// Function returns the VAT rule by given VAT rate, depending on the actual information and requirements of the rule.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="settings"></param>
        /// <param name="vatRate"></param>
        /// <returns></returns>
        Task<VatRule> GetVatRuleByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate);

        /// <summary>
        /// Creates a <see cref="ShoppingBasketCmsSettingsModel"/> object with various settings retrieved from system objects.
        /// </summary>
        /// <returns>A <see cref="ShoppingBasketCmsSettingsModel"/> object.</returns>
        Task<ShoppingBasketCmsSettingsModel> GetSettingsAsync();

        /// <summary>
        /// Retrieves an object by key. If the result is empty, it will try again by prepending "W2" to the key name to check if a legacy key is set.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultResult"></param>
        /// <returns></returns>
        Task<string> GetCheckoutObjectValueAsync(string propertyName, string defaultResult = "");

        /// <summary>
        /// This will link a basket to a user, but only if that basket is not linked to any other user yet.
        /// </summary>
        /// <param name="basketSettings">The settings of the ShoppingBasket component.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="shoppingBasket">The basket.</param>
        /// <param name="deleteCookieIfBasketIsLinkedToSomeoneElse">Optional: Whether to delete the cookie that contains the basket ID, if the basket if linked to a different user. Default value is <see langword="true"/>. You should set this to <see langword="false"/> if you're linking a different basket than the one from the cookie.</param>
        Task LinkBasketToUserAsync(ShoppingBasketCmsSettingsModel basketSettings, ulong userId, WiserItemModel shoppingBasket, bool deleteCookieIfBasketIsLinkedToSomeoneElse = true);

        /// <summary>
        /// Gets a coupon via it's unique code.
        /// </summary>
        /// <param name="couponCode">The coupon code.</param>
        /// <returns>A <see cref="WiserItemModel"/> with the data of the coupon.</returns>
        Task<WiserItemModel> GetCouponAsync(string couponCode);
        
        /// <summary>
        /// Checks whether or not the given coupon is valid and can still be used.
        /// </summary>
        /// <param name="couponCode">The code of the coupon.</param>
        /// <param name="basketTotal">The total price of the basket.</param>
        /// <returns>A <see langword="bool"/> indicating whether the coupon is valid or not.</returns>
        Task<bool> IsCouponValidAsync(string couponCode, decimal basketTotal);
        
        /// <summary>
        /// Checks whether or not the given coupon is valid and can still be used.
        /// </summary>
        /// <param name="coupon">The <see cref="WiserItemModel"/> with the coupon.</param>
        /// <param name="basketTotal">The total price of the basket.</param>
        /// <returns>A <see langword="bool"/> indicating whether the coupon is valid or not.</returns>
        bool IsCouponValid(WiserItemModel coupon, decimal basketTotal);
    }
}
