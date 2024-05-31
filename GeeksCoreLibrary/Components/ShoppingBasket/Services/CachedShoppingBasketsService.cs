﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using LazyCache;
using Microsoft.Extensions.Options;
using OrderProcessConstants = GeeksCoreLibrary.Components.OrderProcess.Models.Constants;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Services
{
    /// <summary>
    /// The cached version of the ShoppingBasketsService. There aren't many parts that are cached, so most functions are pass-through functions.
    /// </summary>
    public class CachedShoppingBasketsService : IShoppingBasketsService
    {
        private readonly GclSettings gclSettings;
        private readonly IAppCache cache;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly ICacheService cacheService;
        private readonly IBranchesService branchesService;

        public CachedShoppingBasketsService(IOptions<GclSettings> gclSettings, IAppCache cache, IShoppingBasketsService shoppingBasketsService, ICacheService cacheService, IBranchesService branchesService)
        {
            this.gclSettings = gclSettings.Value;
            this.cache = cache;
            this.shoppingBasketsService = shoppingBasketsService;
            this.cacheService = cacheService;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByUniquePaymentNumberAsync(string uniquePaymentNumber)
        {
            return await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(uniquePaymentNumber);
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByPspTransactionIdAsync(string pspTransactionId)
        {
            return await shoppingBasketsService.GetOrdersByPspTransactionIdAsync(pspTransactionId);
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync()
        {
            return await shoppingBasketsService.GetShoppingBasketsAsync();
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync(string cookieName, ShoppingBasketCmsSettingsModel settings)
        {
            return await shoppingBasketsService.GetShoppingBasketsAsync(cookieName, settings);
        }

        /// <inheritdoc />
        public ulong GetBasketItemId(string cookieName)
        {
            return shoppingBasketsService.GetBasketItemId(cookieName);
        }

        /// <inheritdoc />
        public ulong DecryptBasketItemId(string encryptedId)
        {
            return shoppingBasketsService.DecryptBasketItemId(encryptedId);
        }

        /// <inheritdoc />
        public string EncryptBasketItemId(ulong itemId)
        {
            return shoppingBasketsService.EncryptBasketItemId(itemId);
        }

        /// <inheritdoc />
        public decimal CalculateCouponValue(WiserItemModel coupon, decimal totalProductsPrice, bool maxDiscountIsTotalAmountProducts = false, decimal currentDiscountAmount = 0)
        {
            return shoppingBasketsService.CalculateCouponValue(coupon, totalProductsPrice, maxDiscountIsTotalAmountProducts, currentDiscountAmount);
        }

        /// <inheritdoc />
        public async Task<bool> UseCouponAsync(WiserItemModel coupon, decimal totalProductsPrice)
        {
            return await shoppingBasketsService.UseCouponAsync(coupon, totalProductsPrice);
        }

        /// <inheritdoc />
        public async Task<(ulong ConceptOrderId, WiserItemModel ConceptOrder, List<WiserItemModel> ConceptOrderLines)> MakeConceptOrderFromBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, OrderProcessBasketToConceptOrderMethods basketToConceptOrderMethod)
        {
            return await shoppingBasketsService.MakeConceptOrderFromBasketAsync(shoppingBasket, basketLines, settings, basketToConceptOrderMethod);
        }
        
        /// <inheritdoc />
        public async Task RevertConceptOrderToBasketAsync(WiserItemModel conceptOrder, List<WiserItemModel> conceptOrderLines)
        {
            await shoppingBasketsService.RevertConceptOrderToBasketAsync(conceptOrder, conceptOrderLines);
        }

        /// <inheritdoc />
        public async Task ConvertConceptOrderToOrderAsync(WiserItemModel conceptOrder, ShoppingBasketCmsSettingsModel settings)
        {
            await shoppingBasketsService.ConvertConceptOrderToOrderAsync(conceptOrder, settings);
        }
        
        /// <inheritdoc />
        public async Task<string> ReplaceBasketInTemplateAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string template, bool replaceUserAccountVariables = false, bool stripNotExistingVariables = true, IDictionary<string, string> userDetails = null, bool isForConfirmationEmail = false, IDictionary<string, object> additionalReplacementData = null, bool forQuery = false)
        {
            return await shoppingBasketsService.ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, template, replaceUserAccountVariables, stripNotExistingVariables, userDetails, isForConfirmationEmail, additionalReplacementData, forQuery);
        }

        /// <inheritdoc />
        public async Task<decimal> GetPriceAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, string lineType = "", int onlyIfVatRate = -1, bool includeDiscountGettingVat = true)
        {
            return await shoppingBasketsService.GetPriceAsync(shoppingBasket, basketLines, settings, priceType, lineType, onlyIfVatRate, includeDiscountGettingVat);
        }

        /// <inheritdoc />
        public async Task<decimal> GetLinePriceAsync(WiserItemModel shoppingBasket, WiserItemModel line, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, bool singlePrice = false, bool round = false, int onlyIfVatRate = -1, bool withoutFactor = false, bool useOriginalPrice = false)
        {
            return await shoppingBasketsService.GetLinePriceAsync(shoppingBasket, line, settings, priceType, singlePrice, round, onlyIfVatRate, withoutFactor, useOriginalPrice);
        }

        /// <inheritdoc />
        public async Task RecalculateVariablesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string skipType = null, bool createNewTransaction = true)
        {
            await shoppingBasketsService.RecalculateVariablesAsync(shoppingBasket, basketLines, settings, skipType, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<(WiserItemModel ShoppingBasket, List<WiserItemModel> BasketLines, string BasketLineValidityMessage, string BasketLineStockActionMessage)> LoadAsync(ShoppingBasketCmsSettingsModel settings, ulong itemId = 0, string encryptedItemId = "", bool connectToAccount = true, bool recursiveCall = false, bool includeLines = true, int basketToUserLinkType = Constants.BasketToUserLinkType)
        {
            return await shoppingBasketsService.LoadAsync(settings, itemId, encryptedItemId, connectToAccount, recursiveCall, includeLines, basketToUserLinkType);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true, int basketToUserLinkType = Constants.BasketToUserLinkType)
        {
            return await shoppingBasketsService.SaveAsync(shoppingBasket, basketLines, settings, createNewTransaction, basketToUserLinkType);
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateShippingCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            return await shoppingBasketsService.CalculateShippingCostsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task RecalculateCouponsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            await shoppingBasketsService.RecalculateCouponsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task UpdateCouponAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, HandleCouponResultModel couponResult, decimal currentDiscount = 0M, bool divideDiscountOverProducts = false, bool createNewTransaction = true)
        {
            await shoppingBasketsService.UpdateCouponAsync(shoppingBasket, basketLines, settings, couponResult, currentDiscount, divideDiscountOverProducts, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<decimal> CalculatePaymentMethodCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            return await shoppingBasketsService.CalculatePaymentMethodCostsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> RemoveLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ICollection<string> itemIdsOrUniqueIds, bool createNewTransaction = true)
        {
            return await shoppingBasketsService.RemoveLinesAsync(shoppingBasket, basketLines, settings, itemIdsOrUniqueIds, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task AddLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0, int quantity = 1, string type = OrderProcessConstants.OrderLineProductType, IDictionary<string, string> lineDetails = null, bool createNewTransaction = true)
        {
            await shoppingBasketsService.AddLineAsync(shoppingBasket, basketLines, settings, uniqueId, itemId, quantity, type, lineDetails, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task AddLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, IList<AddToShoppingBasketModel> items, bool createNewTransaction = true)
        {
            await shoppingBasketsService.AddLinesAsync(shoppingBasket, basketLines, settings, items, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task UpdateLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, UpdateItemModel item)
        {
            await shoppingBasketsService.UpdateLineAsync(shoppingBasket, basketLines, settings, item);
        }

        /// <inheritdoc />
        public async Task UpdateBasketLineQuantityAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string itemIdOrUniqueId, decimal quantity)
        {
            await shoppingBasketsService.UpdateBasketLineQuantityAsync(shoppingBasket, basketLines, settings, itemIdOrUniqueId, quantity);
        }

        /// <inheritdoc />
        public async Task<HandleCouponResultModel> AddCouponToBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode = "", bool createNewTransaction = true)
        {
            return await shoppingBasketsService.AddCouponToBasketAsync(shoppingBasket, basketLines, settings, couponCode, createNewTransaction);
        }

        /// <inheritdoc />
        public List<WiserItemModel> GetLines(List<WiserItemModel> basketLines, string lineType)
        {
            return shoppingBasketsService.GetLines(basketLines, lineType);
        }

        /// <inheritdoc />
        public async Task CheckForFreeProductAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = false)
        {
            await shoppingBasketsService.CheckForFreeProductAsync(shoppingBasket, basketLines, settings, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<IList<WiserItemModel>> GetFreeProductActionsAsync()
        {
            var cacheName = $"GCLShoppingBasketFreeProductActions_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultShoppingBasketsCacheDuration;
                    return await shoppingBasketsService.GetFreeProductActionsAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.ShoppingBaskets));
        }

        /// <inheritdoc />
        public async Task<IList<VatRule>> GetVatRulesAsync()
        {
            var cacheName = $"GCLShoppingBasketVatRules_{branchesService.GetDatabaseNameFromCookie()}";
            return await cache.GetOrAddAsync(cacheName,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultShoppingBasketsCacheDuration;
                    return await shoppingBasketsService.GetVatRulesAsync();
                }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.ShoppingBaskets));
        }

        /// <inheritdoc />
        public async Task<ShoppingBasketCmsSettingsModel> GetSettingsAsync()
        {
            return await shoppingBasketsService.GetSettingsAsync();
        }

        /// <inheritdoc />
        public async Task<decimal> GetVatFactorByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate)
        {
            return await shoppingBasketsService.GetVatFactorByRateAsync(shoppingBasket, settings, vatRate);
        }

        /// <inheritdoc />
        public async Task<VatRule> GetVatRuleByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate)
        {
            return await shoppingBasketsService.GetVatRuleByRateAsync(shoppingBasket, settings, vatRate);
        }

        /// <inheritdoc />
        public async Task<string> GetCheckoutObjectValueAsync(string propertyName, string defaultResult = "")
        {
            return await shoppingBasketsService.GetCheckoutObjectValueAsync(propertyName, defaultResult);
        }

        /// <inheritdoc />
        public async Task LinkBasketToUserAsync(ShoppingBasketCmsSettingsModel basketSettings, ulong userId, WiserItemModel shoppingBasket, bool deleteCookieIfBasketIsLinkedToSomeoneElse = true)
        {
            await shoppingBasketsService.LinkBasketToUserAsync(basketSettings, userId, shoppingBasket, deleteCookieIfBasketIsLinkedToSomeoneElse);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetCouponAsync(string couponCode)
        {
            // Don't cache coupons, because they can change often, due to them keeping track of how often they're used.
            return await shoppingBasketsService.GetCouponAsync(couponCode);
        }

        /// <inheritdoc />
        public async Task<bool> IsCouponValidAsync(string couponCode, decimal basketTotal)
        {
            return await shoppingBasketsService.IsCouponValidAsync(couponCode, basketTotal);
        }

        /// <inheritdoc />
        public bool IsCouponValid(WiserItemModel coupon, decimal basketTotal)
        {
            return shoppingBasketsService.IsCouponValid(coupon, basketTotal);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ulong basketItemId)
        {
            await shoppingBasketsService.DeleteAsync(basketItemId);
        }

        /// <inheritdoc />
        public async Task DeleteLinesAsync(ulong basketItemId)
        {
            await shoppingBasketsService.DeleteLinesAsync(basketItemId);
        }

        /// <inheritdoc />
        public async Task<(string Html, string PdfDocumentOptions)> RenderBasketHtmlAsync(ulong templateId, WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string basketLineValidityMessage = "", string basketLineStockActionMessage = "")
        {
            return await shoppingBasketsService.RenderBasketHtmlAsync(templateId, shoppingBasket, basketLines, settings, basketLineValidityMessage, basketLineStockActionMessage);
        }

        /// <inheritdoc />
        public async Task<string> RenderBasketHtmlAsync(string htmlTemplate, WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string basketLineValidityMessage = "", string basketLineStockActionMessage = "")
        {
            return await shoppingBasketsService.RenderBasketHtmlAsync(htmlTemplate, shoppingBasket, basketLines, settings, basketLineValidityMessage, basketLineStockActionMessage);
        }
    }
}