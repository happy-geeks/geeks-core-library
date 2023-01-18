using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Interfaces
{
    public interface IShoppingBasketsService
    {
        /// <summary>
        /// Get all orders via unique payment number.
        /// </summary>
        /// <param name="uniquePaymentNumber">The value of the UniquePaymentNumber detail of the order item.</param>
        /// <returns>A <see cref="List{WiserItemModel}"/> of <see cref="WiserItemModel"/> objects.</returns>
        Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByUniquePaymentNumberAsync(string uniquePaymentNumber);
        
        /// <summary>
        /// Get all orders via unique payment number.
        /// </summary>
        /// <param name="pspTransactionId">The value of the PspTransactionId detail of the order item.</param>
        /// <returns>A <see cref="List{WiserItemModel}"/> of <see cref="WiserItemModel"/> objects.</returns>
        Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByPspTransactionIdAsync(string pspTransactionId);

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
        Task<(ulong ConceptOrderId, WiserItemModel ConceptOrder, List<WiserItemModel> ConceptOrderLines)> MakeConceptOrderFromBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, OrderProcessBasketToConceptOrderMethods basketToConceptOrderMethod);

        /// <summary>
        /// Turns a concept order into a final order.
        /// </summary>
        Task ConvertConceptOrderToOrderAsync(WiserItemModel conceptOrder, ShoppingBasketCmsSettingsModel settings);

        /// <summary>
        /// Replaces shopping basket variables in a template.
        /// </summary>
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="basketLines">The basket lines items.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="template">The HTML template that should be parsed.</param>
        /// <param name="replaceUserAccountVariables">Optional: Whether the variables of the currently logged in user should be also be replaced.</param>
        /// <param name="stripNotExistingVariables">Optional: Whether variables that weren't replaced should be removed.</param>
        /// <param name="userDetails">Optional: A list of variables about the current user. If this value is not null and contains at least one value, it will be used and <paramref name="replaceUserAccountVariables"/> will be ignored.</param>
        /// <param name="isForConfirmationEmail">Optional: Whether the replacements are meant for an order confirmation email.</param>
        /// <param name="additionalReplacementData">Optional: Some additional data to be used in the replacements.</param>
        /// <param name="forQuery">Optional: Whether the replacements are meant for a MySQL query.</param>
        /// <returns>The template with the shopping basket's variables replaced.</returns>
        Task<string> ReplaceBasketInTemplateAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string template, bool replaceUserAccountVariables = false, bool stripNotExistingVariables = true, IDictionary<string, string> userDetails = null, bool isForConfirmationEmail = false, IDictionary<string, object> additionalReplacementData = null, bool forQuery = false);

        /// <summary>
        /// Gets the total price of the shopping basket.
        /// </summary>
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="basketLines">The basket lines items.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="priceType">Optional: What sort of price should be returned (including or excluding VAT, and including or excluding discount).</param>
        /// <param name="lineType">Optional: A filter to only count the price of a certain line type. Set to null or empty to use all types.</param>
        /// <param name="onlyIfVatRate">Optional: Only return the price if the VAT rate of the product matches this value. Set to -1 to disable this check.</param>
        /// <param name="includeDiscountGettingVat">Optional: Whether the discount should be included when attempting to determine the VAT of the price.</param>
        /// <returns>The calculated price as a decimal.</returns>
        Task<decimal> GetPriceAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, string lineType = "", int onlyIfVatRate = -1, bool includeDiscountGettingVat = true);
        
        /// <summary>
        /// Gets the total price of a single basket line.
        /// </summary>
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="line">The basket line whose price should be retrieved.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="priceType">Optional: What sort of price should be returned (including or excluding VAT, and including or excluding discount).</param>
        /// <param name="singlePrice">Optional: Whether it should be the price of a single item.</param>
        /// <param name="round">Optional: Whether the price should be rounded down to the nearest 2 decimals.</param>
        /// <param name="onlyIfVatRate">Optional: Only return the price if the VAT rate of the product matches this value. Set to -1 to disable this check.</param>
        /// <param name="withoutFactor">Optional: Whether the price factor should be excluded in the price calculation.</param>
        /// <param name="useOriginalPrice">Optional: Whether the "original_price" value should be used instead of the regular price value.</param>
        /// <returns>The calculated price as a decimal.</returns>
        Task<decimal> GetLinePriceAsync(WiserItemModel shoppingBasket, WiserItemModel line, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, bool singlePrice = false, bool round = false, int onlyIfVatRate = -1, bool withoutFactor = false, bool useOriginalPrice = false);

        /// <summary>
        /// Function to recalculate the shipping-costs, re-evaluate the coupon, etc. after changing quantities, adding or removing products, etc.
        /// </summary>
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="basketLines">The basket lines items.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="skipType">An optional parameter to skip lines of a certain type.</param>
        /// <param name="createNewTransaction">Will be passed to the CalculateShippingCostsAsync, CalculatePaymentMethodCostsAsync, and SaveAsync calls.</param>
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
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="basketLines">The basket lines items.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="createNewTransaction">Will be passed to the AddLineAsync call.</param>
        /// <returns>The calculated shipping costs.</returns>
        Task<decimal> CalculateShippingCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true);

        /// <summary>
        /// Calculates the payment method costs based on the payment method costs query defined in the settings module.
        /// </summary>
        /// <param name="shoppingBasket">The main shopping basket item.</param>
        /// <param name="basketLines">The basket lines items.</param>
        /// <param name="settings">The settings of the shopping basket.</param>
        /// <param name="createNewTransaction">Will be passed to the AddLineAsync call.</param>
        /// <returns>The calculated payment method costs.</returns>
        Task<decimal> CalculatePaymentMethodCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true);

        /// <summary>
        /// Recalculates added coupons.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the <see cref="ShoppingBasket"/> component that called this function.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        Task RecalculateCouponsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true);

        /// <summary>
        /// Handles the result of HandleCoupon so that the coupon is properly added, updated, or removed.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the <see cref="ShoppingBasket"/> component that called this function.</param>
        /// <param name="couponResult">The result of the coupon that was validated and handled.</param>
        /// <param name="currentDiscount">Optional: The current discount already added to the basket.</param>
        /// <param name="divideDiscountOverProducts">Optional: Whether the discount of coupons is being divided over all products.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        Task UpdateCouponAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, HandleCouponResultModel couponResult, decimal currentDiscount = 0M, bool divideDiscountOverProducts = false, bool createNewTransaction = true);
        
        /// <summary>
        /// Removes multiple items from the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the <see cref="ShoppingBasket"/> component that called this function.</param>
        /// <param name="itemIdsOrUniqueIds">List of unique ID or item IDs to remove.</param>
        /// <returns>The removed lines.</returns>
        Task<List<WiserItemModel>> RemoveLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ICollection<string> itemIdsOrUniqueIds);

        /// <summary>
        /// Adds multiple items to the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the <see cref="ShoppingBasket"/> component that called this function.</param>
        /// <param name="uniqueId">The unique ID of the item. If null or empty, the <paramref name="itemId"/> value will be used.</param>
        /// <param name="itemId">The ID of the item that will be added.</param>
        /// <param name="quantity">The quantity that should be added.</param>
        /// <param name="type">The type of the item. Defaults to "product".</param>
        /// <param name="lineDetails">Additional properties for the basket line.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        Task AddLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0UL, int quantity = 1, string type = "product", IDictionary<string, string> lineDetails = null, bool createNewTransaction = true);

        /// <summary>
        /// Adds multiple items to the basket.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the <see cref="ShoppingBasket"/> component that called this function.</param>
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
        /// Attempts to update the quantity.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="itemIdOrUniqueId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task UpdateBasketLineQuantityAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string itemIdOrUniqueId, decimal quantity);

        /// <summary>
        /// Will attempt to add a coupon to the shopping basket by checking if the request variable "couponcode" is present and using that value to see if it matches with a valid coupon.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="couponCode">The coupon code to check and process.</param>
        /// <param name="createNewTransaction">Will be passed to the SaveAsync call.</param>
        /// <returns></returns>
        Task<HandleCouponResultModel> AddCouponToBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode = "", bool createNewTransaction = true);

        /// <summary>
        /// Get lines of a specific type.
        /// </summary>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="lineType">The type of lines to look for.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/> objects that represent the order lines of the given type.</returns>
        List<WiserItemModel> GetLines(List<WiserItemModel> basketLines, string lineType);

        /// <summary>
        /// Checks if any of the free products are eligible and add, removes or updates the lines if applicable.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="basketLines">The current basket lines.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="createNewTransaction">Whether the function should create a new database transaction.</param>
        Task CheckForFreeProductAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A list of <see cref="WiserItemModel"/> of products that are free as the result of the free products actions.</returns>
        Task<IList<WiserItemModel>> GetFreeProductActionsAsync();

        /// <summary>
        /// Gets all VAT rules.
        /// </summary>
        /// <returns>A list of <see cref="VatRule"/> objects.</returns>
        Task<IList<VatRule>> GetVatRulesAsync();

        /// <summary>
        /// Returns the VAT factor by given VAT rate, depending on the actual information and requirements of the rule.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="vatRate">The VAT rate used in the lookup.</param>
        /// <returns>A decimal representing the VAT factor.</returns>
        Task<decimal> GetVatFactorByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate);
        
        /// <summary>
        /// Returns the VAT rule by given VAT rate, depending on the actual information and requirements of the rule.
        /// </summary>
        /// <param name="shoppingBasket">The current basket.</param>
        /// <param name="settings">The settings of the ShoppingBasket component that requested this update.</param>
        /// <param name="vatRate">The VAT rate used in the lookup.</param>
        /// <returns>A <see cref="VatRule"/> object.</returns>
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

        /// <summary>
        /// Deletes a basket and all it's basket lines from the database.
        /// </summary>
        /// <param name="basketItemId">The ID of the basket to delete.</param>
        Task DeleteAsync(ulong basketItemId);

        /// <summary>
        /// Deletes a basket and all it's basket lines from the database.
        /// </summary>
        /// <param name="basketItemId">The ID of the basket to delete.</param>
        Task DeleteLinesAsync(ulong basketItemId);
    }
}
