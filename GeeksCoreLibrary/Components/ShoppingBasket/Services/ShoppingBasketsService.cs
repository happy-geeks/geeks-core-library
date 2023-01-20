using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.OrderProcess.Enums;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Services
{
    public class ShoppingBasketsService : IShoppingBasketsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<ShoppingBasketsService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IAccountsService accountsService;
        private readonly ITemplatesService templatesService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly ITempDataDictionaryFactory tempDataDictionaryFactory;
        private readonly ILanguagesService languagesService;

        private SortedList<int, decimal> vatFactorsByRate;

        public ShoppingBasketsService(IOptions<GclSettings> gclSettings, ILogger<ShoppingBasketsService> logger, IDatabaseConnection databaseConnection, IHttpContextAccessor httpContextAccessor, IObjectsService objectsService, IWiserItemsService wiserItemsService, IAccountsService accountsService, ITemplatesService templatesService, IStringReplacementsService stringReplacementsService, ITempDataDictionaryFactory tempDataDictionaryFactory, ILanguagesService languagesService)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.wiserItemsService = wiserItemsService;
            this.accountsService = accountsService;
            this.templatesService = templatesService;
            this.stringReplacementsService = stringReplacementsService;
            this.tempDataDictionaryFactory = tempDataDictionaryFactory;
            this.languagesService = languagesService;
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByUniquePaymentNumberAsync(string uniquePaymentNumber)
        {
            var result = new List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>();
            if (String.IsNullOrWhiteSpace(uniquePaymentNumber))
            {
                return result;
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(OrderProcess.Models.Constants.OrderEntityType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("uniquePaymentNumber", uniquePaymentNumber);
            var query = $@"SELECT `order`.id
FROM `{tablePrefix}{WiserTableNames.WiserItem}` AS `order`
JOIN `{tablePrefix}{WiserTableNames.WiserItemDetail}` AS uniquepaymentnumber ON uniquepaymentnumber.item_id = `order`.id AND uniquepaymentnumber.`key` = '{OrderProcess.Models.Constants.UniquePaymentNumberProperty}' AND uniquepaymentnumber.`value` = ?uniquePaymentNumber
WHERE `order`.entity_type IN ('{OrderProcess.Models.Constants.OrderEntityType}', '{OrderProcess.Models.Constants.ConceptOrderEntityType}');";
            var getBasketIdsResult = await databaseConnection.GetAsync(query, true);

            if (getBasketIdsResult.Rows.Count == 0)
            {
                return result;
            }

            foreach (DataRow dataRow in getBasketIdsResult.Rows)
            {
                var itemId = dataRow.Field<ulong>("id");
                if (itemId == 0)
                {
                    continue;
                }

                var linkTypeOrderLineToOrder = await wiserItemsService.GetLinkTypeAsync(OrderProcess.Models.Constants.OrderEntityType, OrderProcess.Models.Constants.OrderLineEntityType);
                if (linkTypeOrderLineToOrder == 0)
                {
                    linkTypeOrderLineToOrder = Constants.BasketLineToBasketLinkType;
                }

                result.Add((await wiserItemsService.GetItemDetailsAsync(itemId, entityType: OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true), await wiserItemsService.GetLinkedItemDetailsAsync(itemId, linkTypeOrderLineToOrder, OrderProcess.Models.Constants.OrderLineEntityType, itemIdEntityType: OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true)));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>> GetOrdersByPspTransactionIdAsync(string pspTransactionId)
        {
            var result = new List<(WiserItemModel Order, List<WiserItemModel> OrderLines)>();
            if (String.IsNullOrWhiteSpace(pspTransactionId))
            {
                return result;
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(OrderProcess.Models.Constants.OrderEntityType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("pspTransactionId", pspTransactionId);
            var query = $@"SELECT `order`.id
FROM `{tablePrefix}{WiserTableNames.WiserItem}` AS `order`
JOIN `{tablePrefix}{WiserTableNames.WiserItemDetail}` AS pspTransactionId ON pspTransactionId.item_id = `order`.id AND pspTransactionId.`key` = '{OrderProcess.Models.Constants.PaymentProviderTransactionId}' AND pspTransactionId.`value` = ?pspTransactionId
WHERE `order`.entity_type IN ('{OrderProcess.Models.Constants.OrderEntityType}', '{OrderProcess.Models.Constants.ConceptOrderEntityType}');";
            var getBasketIdsResult = await databaseConnection.GetAsync(query, true);

            if (getBasketIdsResult.Rows.Count == 0)
            {
                return result;
            }

            foreach (DataRow dataRow in getBasketIdsResult.Rows)
            {
                var itemId = dataRow.Field<ulong>("id");
                if (itemId == 0)
                {
                    continue;
                }

                var linkTypeOrderLineToOrder = await wiserItemsService.GetLinkTypeAsync(OrderProcess.Models.Constants.OrderEntityType, OrderProcess.Models.Constants.OrderLineEntityType);
                if (linkTypeOrderLineToOrder == 0)
                {
                    linkTypeOrderLineToOrder = Constants.BasketLineToBasketLinkType;
                }

                result.Add((await wiserItemsService.GetItemDetailsAsync(itemId, entityType: OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true), await wiserItemsService.GetLinkedItemDetailsAsync(itemId, linkTypeOrderLineToOrder, OrderProcess.Models.Constants.OrderLineEntityType, itemIdEntityType: OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true)));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync()
        {
            var checkoutBasketsCookieName = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_cookieName", Constants.DefaultCookieName);

            var settings = await GetSettingsAsync();
            return await GetShoppingBasketsAsync(checkoutBasketsCookieName, settings);
        }

        /// <inheritdoc />
        public async Task<List<(WiserItemModel Main, List<WiserItemModel> Lines)>> GetShoppingBasketsAsync(string cookieName, ShoppingBasketCmsSettingsModel settings)
        {
            var result = new List<(WiserItemModel Main, List<WiserItemModel> Lines)>();

            var cookieValue = HttpContextHelpers.ReadCookie(httpContextAccessor.HttpContext, cookieName);
            var basketIds = cookieValue.DecryptWithAesWithSalt(gclSettings.ShoppingBasketEncryptionKey).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(id => Convert.ToUInt64(id)).ToArray();

            foreach (var basketId in basketIds)
            {
                var basket = await wiserItemsService.GetItemDetailsAsync(basketId, entityType: Constants.BasketEntityType, skipPermissionsCheck: true);
                var lines = await wiserItemsService.GetLinkedItemDetailsAsync(basketId, Constants.BasketLineToBasketLinkType, Constants.BasketLineEntityType, itemIdEntityType: Constants.BasketEntityType, skipPermissionsCheck: true);
                result.Add((basket, lines));
            }

            return result;
        }

        /// <inheritdoc />
        public ulong GetBasketItemId(string cookieName)
        {
            var cookieValue = HttpContextHelpers.ReadCookie(httpContextAccessor.HttpContext, cookieName);
            return DecryptBasketItemId(cookieValue);
        }

        /// <inheritdoc />
        public ulong DecryptBasketItemId(string encryptedId)
        {
            try
            {
                return !String.IsNullOrWhiteSpace(encryptedId) ? Convert.ToUInt64(encryptedId.DecryptWithAesWithSalt(gclSettings.ShoppingBasketEncryptionKey)) : 0UL;
            }
            catch (Exception exception)
            {
                logger.LogError("An error occurred while trying to decrypt the basket ID '{encryptedId}': {exception}", encryptedId, exception);
                return 0;
            }
        }

        /// <inheritdoc />
        public string EncryptBasketItemId(ulong itemId)
        {
            return itemId.ToString(CultureInfo.InvariantCulture).EncryptWithAesWithSalt(gclSettings.ShoppingBasketEncryptionKey);
        }

        /// <inheritdoc />
        public decimal CalculateCouponValue(WiserItemModel coupon, decimal totalProductsPrice, bool maxDiscountIsTotalAmountProducts = false, decimal currentDiscountAmount = 0M)
        {
            var minPurchasePrice = coupon.GetDetailValue<decimal>(CouponConstants.MinPurchasePriceKey);
            if (totalProductsPrice < minPurchasePrice)
            {
                return 0M;
            }

            var discountAmount = coupon.GetDetailValue<decimal>(CouponConstants.DiscountAmountKey);
            if (discountAmount > 0M)
            {
                if (!maxDiscountIsTotalAmountProducts)
                {
                    return discountAmount;
                }

                return discountAmount > (totalProductsPrice + currentDiscountAmount) ? totalProductsPrice + currentDiscountAmount : discountAmount;
            }

            var discountPercentage = coupon.GetDetailValue<decimal>(CouponConstants.DiscountPercentageKey);
            var calculatedDiscount = discountPercentage / 100M * totalProductsPrice;
            if (maxDiscountIsTotalAmountProducts && calculatedDiscount > (totalProductsPrice + currentDiscountAmount))
            {
                calculatedDiscount = totalProductsPrice + currentDiscountAmount;
            }

            return calculatedDiscount;
        }

        /// <inheritdoc />
        public async Task<bool> UseCouponAsync(WiserItemModel coupon, decimal totalProductsPrice)
        {
            if (coupon == null || coupon.Id == 0)
            {
                return false;
            }

            var keepRemainderOfCoupon = (await objectsService.FindSystemObjectByDomainNameAsync("COUPON_KeepRemainderOfCoupon")).Equals("true");
            var discountAmount = coupon.GetDetailValue<decimal>(CouponConstants.DiscountAmountKey);
            if (totalProductsPrice > 0 && keepRemainderOfCoupon && coupon.GetDetailValue<int>(CouponConstants.MaxUseCountKey) == 1 && discountAmount > totalProductsPrice)
            {
                var remainder = (discountAmount - totalProductsPrice).ToString(CultureInfo.InvariantCulture);
                coupon.SetDetail(CouponConstants.DiscountAmountKey, remainder);
            }
            else
            {
                var currentUsedCount = coupon.GetDetailValue<int>(CouponConstants.UsedCountKey);
                coupon.SetDetail(CouponConstants.UsedCountKey, (currentUsedCount + 1).ToString());
            }
            await wiserItemsService.SaveAsync(coupon, skipPermissionsCheck: true);

            return true;
        }

        /// <inheritdoc />
        public async Task RecalculateVariablesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string skipType = null, bool createNewTransaction = true)
        {
            logger.LogTrace("GCL ShoppingBasket RecalculateVariablesAsync - skipping type: {skipType}", skipType ?? "N/A");

            if (!skipType.InList("shipping_costs", "paymentmethod_costs", "coupon", Constants.BasketLineCouponType))
            {
                await RecalculateCouponsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
                await CalculateShippingCostsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
                await CalculatePaymentMethodCostsAsync(shoppingBasket, basketLines, settings, createNewTransaction);

                // Second pass for coupons; in case prices have changed in the shipping costs and payment costs update.
                await RecalculateCouponsAsync(shoppingBasket, basketLines, settings, createNewTransaction);
            }

            // Check if any standardized free product actions are applicable.
            await CheckForFreeProductAsync(shoppingBasket, basketLines, settings, createNewTransaction);

            await SaveAsync(shoppingBasket, basketLines, settings, createNewTransaction);
        }

        /// <inheritdoc />
        public async Task<(WiserItemModel ShoppingBasket, List<WiserItemModel> BasketLines, string BasketLineValidityMessage, string BasketLineStockActionMessage)> LoadAsync(ShoppingBasketCmsSettingsModel settings, ulong itemId = 0UL, string encryptedItemId = "", bool connectToAccount = true, bool recursiveCall = false)
        {
            var shoppingBasket = new WiserItemModel();
            var basketLines = new List<WiserItemModel>();

            var basketLineValidityMessage = "";
            var basketLineStockActionMessage = "";

            if (itemId == 0UL)
            {
                if (String.IsNullOrWhiteSpace(encryptedItemId) && !String.IsNullOrWhiteSpace(settings.CookieName))
                {
                    encryptedItemId = httpContextAccessor.HttpContext?.Request.Cookies[settings.CookieName];
                }

                if (!String.IsNullOrWhiteSpace(encryptedItemId))
                {
                    if (DecryptBasketItemId(encryptedItemId) == 0)
                    {
                        return (shoppingBasket, basketLines, String.Empty, String.Empty);
                    }
                }
            }

            var user = await accountsService.GetUserDataFromCookieAsync();

            var loadBasketFromUser = false;
            var loadedBasketFromCookie = false;

            if (settings.MultipleBasketsPossible && itemId == 0 && String.IsNullOrWhiteSpace(settings.GetBasketQuery))
            {
                settings.GetBasketQuery = (await templatesService.GetTemplateAsync(name: "GetBasketQuery", type: TemplateTypes.Query)).Content;
            }

            if (settings.MultipleBasketsPossible && itemId == 0 && !String.IsNullOrWhiteSpace(settings.GetBasketQuery))
            {
                var extraReplacements = new Dictionary<string, object>
                {
                    { "Account_MainUserId", user.MainUserId },
                    { "Account_UserId", user.UserId },
                    { "AccountWiser2_MainUserId", user.MainUserId },
                    { "AccountWiser2_UserId", user.UserId }
                };
                var query = stringReplacementsService.DoHttpRequestReplacements(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, stringReplacementsService.DoSessionReplacements(stringReplacementsService.DoReplacements(settings.GetBasketQuery, extraReplacements, forQuery: true)), stripNotExistingVariables: false, forQuery: true), true);
                var queryResult = await databaseConnection.GetAsync(query, true);

                if (queryResult.Rows.Count > 0 && UInt64.TryParse(Convert.ToString(queryResult.Rows[0][0]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tempItemId))
                {
                    itemId = tempItemId;
                }
            }
            else
            {
                if (itemId == 0 && !String.IsNullOrWhiteSpace(settings.CookieName))
                {
                    itemId = GetBasketItemId(settings.CookieName);
                    loadedBasketFromCookie = itemId > 0;
                }

                if (itemId == 0 && !recursiveCall)
                {
                    loadBasketFromUser = true;
                }
            }

            if (!loadBasketFromUser && itemId > 0)
            {
                // Get details on basket level.
                shoppingBasket = await wiserItemsService.GetItemDetailsAsync(itemId, entityType: Constants.BasketEntityType, skipPermissionsCheck: true);

                if (settings.BasketLineStockAction)
                {
                    var basketLineStockActionQuery = (await templatesService.GetTemplateAsync(0, "BasketLineStockAction", TemplateTypes.Query)).Content;
                    if (!String.IsNullOrWhiteSpace(basketLineStockActionQuery))
                    {
                        logger.LogTrace("UpdateLineDetailsViaLineStockActionQuery");
                    }

                    basketLineStockActionMessage = await UpdateLineDetailsViaLineStockActionQuery(shoppingBasket, basketLines, settings, basketLineStockActionQuery);
                }

                if (shoppingBasket is not {EntityType: Constants.BasketEntityType})
                {
                    shoppingBasket = new WiserItemModel();
                    if (loadedBasketFromCookie)
                    {
                        loadBasketFromUser = true;
                    }
                }
                else
                {
                    if (shoppingBasket.Id == 0)
                    {
                        basketLines = new List<WiserItemModel>();
                    }
                    else
                    {
                        basketLines = await wiserItemsService.GetLinkedItemDetailsAsync(shoppingBasket.Id, Constants.BasketLineToBasketLinkType, Constants.BasketLineEntityType, itemIdEntityType: Constants.BasketEntityType, skipPermissionsCheck: true);

                        // UniqueUuid is not used anymore for baskets; Update basket lines to set the UniqueUuid value
                        // to a separate detail called "uniqueid". UniqueUuid is not cleared though.
                        foreach (var basketLine in basketLines.Where(basketLine => !basketLine.ContainsDetail("uniqueid") && !String.IsNullOrWhiteSpace(basketLine.UniqueUuid)))
                        {
                            basketLine.SetDetail("uniqueid", basketLine.UniqueUuid);
                            await wiserItemsService.UpdateAsync(basketLine.Id, basketLine, skipPermissionsCheck: true);
                        }
                    }

                    if (basketLines.Count == 0 && !recursiveCall)
                    {
                        loadBasketFromUser = true;
                    }

                    if (!loadBasketFromUser)
                    {
                        // Retrieve the TempData object, but make sure it doesn't cause a NullReferenceException.
                        var tempData = tempDataDictionaryFactory?.GetTempData(httpContextAccessor.HttpContext);
                        var dataKey = $"ShoppingBasketQueriesExecuted_{settings.CookieName}";
                        var allowGeneralQueries = tempData == null || !tempData.ContainsKey(dataKey) || Convert.ToInt32(tempData[dataKey]) != 1;

                        // Get extra details on line level, overwrite existing details.
                        if (String.IsNullOrWhiteSpace(settings.ExtraLineFieldsQuery) && allowGeneralQueries)
                        {
                            settings.ExtraLineFieldsQuery = (await templatesService.GetTemplateAsync(name: "BasketExtraLineFields", type: TemplateTypes.Query)).Content;
                        }

                        await UpdateLineDetailsViaExtraQueryAsync(shoppingBasket, basketLines, settings, settings.ExtraLineFieldsQuery);

                        // Get extra details on basket level, overwrite existing details.
                        if (String.IsNullOrEmpty(settings.ExtraMainFieldsQuery) && allowGeneralQueries)
                        {
                            // See if there's a template in the QUERY folder called "BasketExtraMainFields".
                            settings.ExtraMainFieldsQuery = (await templatesService.GetTemplateAsync(name: "BasketExtraMainFields", type: TemplateTypes.Query)).Content;
                        }

                        await UpdateMainDetailsViaExtraQueryAsync(shoppingBasket, basketLines, settings, settings.ExtraMainFieldsQuery);

                        if (settings.BasketLineValidityCheck && allowGeneralQueries)
                        {
                            var basketLineValidityCheckQuery = (await templatesService.GetTemplateAsync(0, "BasketLineValidityCheck", TemplateTypes.Query)).Content;
                            if (!String.IsNullOrWhiteSpace(basketLineValidityCheckQuery))
                            {
                                logger.LogTrace("UpdateLineDetailsViaLineValidityCheckQuery");
                            }

                            var (updatedBasketLines, message) = await UpdateLineDetailsViaLineValidityCheckQueryAsync(shoppingBasket, basketLines, settings, basketLineValidityCheckQuery);
                            basketLines = updatedBasketLines;
                            basketLineValidityMessage = message;
                        }

                        if (allowGeneralQueries)
                        {
                            tempData[dataKey] = 1;
                        }

                        if (connectToAccount)
                        {
                            var userId = user.MainUserId;
                            if (userId == 0)
                            {
                                userId = accountsService.GetRecentlyCreateAccountId();
                            }

                            if (userId > 0 && !settings.MultipleBasketsPossible && shoppingBasket.EntityType == Constants.BasketEntityType)
                            {
                                foreach (var basketItemId in (await wiserItemsService.GetLinkedItemIdsAsync(userId, Constants.BasketToUserLinkType, Constants.BasketEntityType, skipPermissionsCheck: true)).Where(basketItemId => basketItemId != shoppingBasket.Id))
                                {
                                    await DeleteAsync(basketItemId);
                                }
                            }

                            await LinkBasketToUserAsync(settings, userId, shoppingBasket);
                        }
                    }
                }
            }

            if (loadBasketFromUser)
            {
                // Check if the user is logged in and has basket from account.
                if (user is { MainUserId: > 0 } && !settings.MultipleBasketsPossible)
                {
                    var linkedBaskets = await wiserItemsService.GetLinkedItemIdsAsync(user.MainUserId, Constants.BasketToUserLinkType, Constants.BasketEntityType, skipPermissionsCheck: true);
                    var basketId = linkedBaskets.FirstOrDefault(id => id > 0);
                    if (basketId > 0)
                    {
                        (shoppingBasket, basketLines, _, _) = await LoadAsync(settings, basketId, "", false, true);
                        WriteEncryptedIdToCookie(shoppingBasket, settings);
                    }
                }
            }

            logger.LogTrace("GCL ShoppingBasket: Finished loading basket");

            return (shoppingBasket, basketLines, basketLineValidityMessage, basketLineStockActionMessage);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> SaveAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            var newBasket = false;
            var user = await accountsService.GetUserDataFromCookieAsync();

            if (String.IsNullOrWhiteSpace(shoppingBasket.EntityType) || shoppingBasket.Id == 0UL)
            {
                shoppingBasket.AddedOn = DateTime.Now;
                shoppingBasket.EntityType = Constants.BasketEntityType;
                shoppingBasket.AddedBy = "GCL";
                newBasket = true;
            }

            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();
            try
            {
                shoppingBasket = await wiserItemsService.SaveAsync(shoppingBasket, alwaysSaveValues: true, saveHistory: false, createNewTransaction: false, skipPermissionsCheck: true);

                var lineIds = new List<ulong>();

                foreach (var line in basketLines)
                {
                    if (String.IsNullOrWhiteSpace(line.EntityType) || line.Id == 0UL)
                    {
                        line.EntityType = Constants.BasketLineEntityType;
                        line.AddedBy = "GCL";
                    }

                    var lineSaveResult = await wiserItemsService.SaveAsync(line, shoppingBasket.Id, Constants.BasketLineToBasketLinkType, alwaysSaveValues: true, saveHistory: false, createNewTransaction: false, skipPermissionsCheck: true);
                    line.Id = lineSaveResult.Id;

                    lineIds.Add(line.Id);
                }

                await wiserItemsService.RemoveLinkedItemsAsync(shoppingBasket.Id, Constants.BasketLineToBasketLinkType, lineIds, entityType: Constants.BasketLineEntityType, createNewTransaction: !createNewTransaction, skipPermissionsCheck: true);

                if (newBasket)
                {
                    if (user is { MainUserId: > 0 })
                    {
                        await wiserItemsService.AddItemLinkAsync(shoppingBasket.Id, user.MainUserId, Constants.BasketToUserLinkType, skipPermissionsCheck: true);
                    }
                    else
                    {
                        var newlyCreatedAccount = accountsService.GetRecentlyCreateAccountId();
                        if (newlyCreatedAccount > 0)
                        {
                            await wiserItemsService.AddItemLinkAsync(shoppingBasket.Id, newlyCreatedAccount, Constants.BasketToUserLinkType, skipPermissionsCheck: true);
                        }
                    }
                }

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }

            // Write basket item ID to cookie.
            if (!shoppingBasket.EntityType.InList(StringComparer.OrdinalIgnoreCase, OrderProcess.Models.Constants.ConceptOrderEntityType, OrderProcess.Models.Constants.OrderEntityType))
            {
                WriteEncryptedIdToCookie(shoppingBasket, settings);
            }

            logger.LogTrace("Basket written to database.");

            return shoppingBasket;
        }

        /// <inheritdoc />
        public async Task<(ulong ConceptOrderId, WiserItemModel ConceptOrder, List<WiserItemModel> ConceptOrderLines)> MakeConceptOrderFromBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, OrderProcessBasketToConceptOrderMethods basketToConceptOrderMethod)
        {
            var user = await accountsService.GetUserDataFromCookieAsync();
            var userId = user.MainUserId;
            var createItemLinkBetweenBasketLineAndProduct = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_AlsoCreateItemLinkBetweenBasketLineAndProduct")).Equals("true", StringComparison.OrdinalIgnoreCase);
            var linkTypeOrderLineToOrder = await wiserItemsService.GetLinkTypeAsync(OrderProcess.Models.Constants.OrderEntityType, OrderProcess.Models.Constants.OrderLineEntityType);

            var userEntityType = user.EntityType;
            if (String.IsNullOrWhiteSpace(userEntityType))
            {
                userEntityType = await objectsService.FindSystemObjectByDomainNameAsync("userEntityType", defaultResult: "relatie");
            }

            var linkTypeOrderToUser = await wiserItemsService.GetLinkTypeAsync(userEntityType, OrderProcess.Models.Constants.OrderEntityType);
            var newLines = new List<WiserItemModel>();

            if (linkTypeOrderToUser == 0)
            {
                linkTypeOrderToUser = Constants.BasketToUserLinkType;
            }

            if (linkTypeOrderLineToOrder == 0)
            {
                linkTypeOrderLineToOrder = Constants.BasketLineToBasketLinkType;
            }

            if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_LinkTypeProductToOrderLine", Constants.ProductToOrderLineLinkType.ToString()), out var productToBasketLineLinkType) || productToBasketLineLinkType <= 0)
            {
                productToBasketLineLinkType = Constants.ProductToOrderLineLinkType;
            }

            if (userId == 0UL)
            {
                userId = accountsService.GetRecentlyCreateAccountId();
                if (userId == 0UL)
                {
                    userId = (await wiserItemsService.GetLinkedItemIdsAsync(shoppingBasket.Id, Constants.BasketToUserLinkType, reverse: true, skipPermissionsCheck: true)).FirstOrDefault();
                }

                if (userId > 0UL)
                {
                    var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(Account.Models.Constants.DefaultEntityType);
                    databaseConnection.AddParameter("userId", userId);
                    var getEntityTypeResult = await databaseConnection.GetAsync($"SELECT entity_type FROM `{tablePrefix}{WiserTableNames.WiserItem}` WHERE id = ?userId", true);
                    if (getEntityTypeResult.Rows.Count > 0)
                    {
                        linkTypeOrderToUser = await wiserItemsService.GetLinkTypeAsync(getEntityTypeResult.Rows[0].Field<string>("entity_type"), OrderProcess.Models.Constants.OrderEntityType);
                        if (linkTypeOrderToUser == 0)
                        {
                            linkTypeOrderToUser = Constants.BasketToUserLinkType;
                        }
                    }
                }
            }

            // Make and save concept order.
            var conceptOrder = new WiserItemModel
            {
                Id = basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert ? shoppingBasket.Id : 0,
                EntityType = OrderProcess.Models.Constants.ConceptOrderEntityType, 
                Details = new List<WiserItemDetailModel>(shoppingBasket.Details)
            };

            if (basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert)
            {
                await wiserItemsService.ChangeEntityTypeAsync(conceptOrder.Id, shoppingBasket.EntityType, conceptOrder.EntityType, skipPermissionsCheck: true, resetAddedOnDate: true);
                
                // Change link types if they are different between baskets and orders.
                if (Constants.BasketLineToBasketLinkType != linkTypeOrderLineToOrder)
                {
                    await wiserItemsService.ChangeLinkTypesAsync(conceptOrder.Id, Constants.BasketLineToBasketLinkType, linkTypeOrderLineToOrder, skipPermissionsCheck: true);
                }

                if (userId > 0 && Constants.BasketToUserLinkType != linkTypeOrderToUser)
                {
                    await wiserItemsService.ChangeLinkTypeAsync(userId, Constants.BasketToUserLinkType, linkTypeOrderToUser, conceptOrder.Id, skipPermissionsCheck: true);
                }
            }

            // Save all fields, also the readonly fields, so actual prices etc. will be saved to the database.
            foreach (var detail in conceptOrder.Details)
            {
                detail.Id = basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert ? detail.Id : 0;
                detail.Changed = true;
                if (detail.ReadOnly)
                {
                    detail.ReadOnly = false;
                }
            }

            if (userId > 0)
            {
                await wiserItemsService.SaveAsync(conceptOrder, userId, linkTypeOrderToUser, alwaysSaveValues: true, skipPermissionsCheck: true);
            }
            else
            {
                await wiserItemsService.SaveAsync(conceptOrder, alwaysSaveValues: true, skipPermissionsCheck: true);
            }

            foreach (var line in basketLines)
            {
                var conceptLine = new WiserItemModel
                {
                    Id = basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert ? line.Id : 0,
                    EntityType = OrderProcess.Models.Constants.OrderLineEntityType,
                    Details = line.Details,
                    Title = line.Title
                };
                
                if (basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert)
                {
                    await wiserItemsService.ChangeEntityTypeAsync(conceptLine.Id, line.EntityType, conceptLine.EntityType, skipPermissionsCheck: true, resetAddedOnDate: true);
                }

                // Save all fields, also the readonly fields, so actual prices etc. will be saved to the database.
                foreach (var detail in conceptLine.Details)
                {
                    detail.Id = basketToConceptOrderMethod == OrderProcessBasketToConceptOrderMethods.Convert ? detail.Id : 0;
                    detail.Changed = true;
                    if (detail.ReadOnly)
                    {
                        detail.ReadOnly = false;
                    }
                }

                conceptLine = await wiserItemsService.SaveAsync(conceptLine, conceptOrder.Id, linkTypeOrderLineToOrder, alwaysSaveValues: true, skipPermissionsCheck: true);
                
                if (basketToConceptOrderMethod != OrderProcessBasketToConceptOrderMethods.Convert)
                {
                    if (createItemLinkBetweenBasketLineAndProduct)
                    {
                        var productId = conceptLine.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                        if (productId > 0)
                        {
                            await wiserItemsService.AddItemLinkAsync(productId, conceptLine.Id, productToBasketLineLinkType, skipPermissionsCheck: true);
                        }
                    }
                    
                    // Check if child item links of the order lines should be copied over to the concept order.
                    var copyBasketLinesLinkedItems = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_CopyLinkedItemsToConceptOrderLines")).Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (copyBasketLinesLinkedItems)
                    {
                        await databaseConnection.ExecuteAsync($@"
                        INSERT INTO `{WiserTableNames.WiserItemLink}` (item_id, destination_item_id, ordering, type)
                        SELECT item_id, {conceptLine.Id}, ordering, type
                        FROM `{WiserTableNames.WiserItemLink}`
                        WHERE destination_item_id = {line.Id} AND type <> {productToBasketLineLinkType}");
                    }

                    // Check if parent item links of the order lines should be copied over to the concept order.
                    var copyBasketLinesLinkedToItems = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_CopyLinkedToItemsToConceptOrderLines")).Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (copyBasketLinesLinkedToItems)
                    {
                        await databaseConnection.ExecuteAsync($@"
                        INSERT INTO `{WiserTableNames.WiserItemLink}` (item_id, destination_item_id, ordering, type)
                        SELECT {conceptLine.Id}, destination_item_id, ordering, type
                        FROM `{WiserTableNames.WiserItemLink}`
                        WHERE item_id = {line.Id} AND type <> {productToBasketLineLinkType}");
                    }
                }

                newLines.Add(conceptLine);
            }

            var createConceptOrderQuery = (await templatesService.GetTemplateAsync(0, "AfterCreateConceptOrder", TemplateTypes.Query)).Content;
            if (!String.IsNullOrWhiteSpace(createConceptOrderQuery))
            {
                var query = stringReplacementsService.DoSessionReplacements(createConceptOrderQuery, true);

                var replacementData = new Dictionary<string, object>
                {
                    { "orderId", conceptOrder.Id },
                    { "linkType", linkTypeOrderLineToOrder },
                    { "userId", userId }
                };

                query = stringReplacementsService.DoReplacements(query, replacementData, forQuery: true);
                query = await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, query, forQuery: true);
                query = stringReplacementsService.DoHttpRequestReplacements(query, true);

                await databaseConnection.ExecuteAsync(query);
            }

            if (basketToConceptOrderMethod != OrderProcessBasketToConceptOrderMethods.Convert)
            {
                // Check if child item links (except basket lines) should be copied over to the concept order.
                var copyBasketLinkedItems = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_CopyLinkedItemsToConceptOrder")).Equals("true", StringComparison.OrdinalIgnoreCase);
                if (copyBasketLinkedItems)
                {
                    await databaseConnection.ExecuteAsync($@"
                    INSERT INTO `{WiserTableNames.WiserItemLink}` (item_id, destination_item_id, ordering, type)
                    SELECT item_id, {conceptOrder.Id}, ordering, type
                    FROM `{WiserTableNames.WiserItemLink}`
                    WHERE destination_item_id = {shoppingBasket.Id} AND type <> 5002");
                }

                // Check if parent item links (except user) should be copied over to the concept order.
                var copyBasketLinkedToItems = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_CopyLinkedItemsToConceptOrder")).Equals("true", StringComparison.OrdinalIgnoreCase);
                if (copyBasketLinkedToItems)
                {
                    await databaseConnection.ExecuteAsync($@"
                    INSERT INTO `{WiserTableNames.WiserItemLink}` (item_id, destination_item_id, ordering, type)
                    SELECT {conceptOrder.Id}, destination_item_id, ordering, type
                    FROM `{WiserTableNames.WiserItemLink}`
                    WHERE item_id = {shoppingBasket.Id} AND type <> {Constants.BasketToUserLinkType}");
                }
            }

            return (conceptOrder.Id, conceptOrder, newLines);
        }

        /// <inheritdoc />
        public async Task ConvertConceptOrderToOrderAsync(WiserItemModel conceptOrder, ShoppingBasketCmsSettingsModel settings)
        {
            try
            {
                await databaseConnection.BeginTransactionAsync();

                await wiserItemsService.ChangeEntityTypeAsync(conceptOrder.Id, OrderProcess.Models.Constants.ConceptOrderEntityType, OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true, resetAddedOnDate: true);

                // Check if there is a AfterCreateConceptOrder query in the templates module and execute this query if present.
                var afterConvertToOrderQuery = (await templatesService.GetTemplateAsync(0, "AfterConvertToOrder", TemplateTypes.Query)).Content;
                if (!String.IsNullOrWhiteSpace(afterConvertToOrderQuery))
                {
                    var query = stringReplacementsService.DoSessionReplacements(afterConvertToOrderQuery, true);

                    var replacementData = new Dictionary<string, object> { { "orderId", conceptOrder.Id } };

                    var orderLineToOrderLinkType = await wiserItemsService.GetLinkTypeAsync(OrderProcess.Models.Constants.OrderEntityType, OrderProcess.Models.Constants.OrderLineEntityType);
                    var orderLines = await wiserItemsService.GetLinkedItemDetailsAsync(conceptOrder.Id, orderLineToOrderLinkType, OrderProcess.Models.Constants.OrderLineEntityType, itemIdEntityType: OrderProcess.Models.Constants.OrderEntityType, skipPermissionsCheck: true);

                    query = stringReplacementsService.DoReplacements(query, replacementData, forQuery: true);
                    query = await ReplaceBasketInTemplateAsync(conceptOrder, orderLines, settings, query, forQuery: true);
                    query = stringReplacementsService.DoHttpRequestReplacements(query, true);

                    await databaseConnection.ExecuteAsync(query);
                }

                await databaseConnection.CommitTransactionAsync();
            }
            catch
            {
                await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<string> ReplaceBasketInTemplateAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string template, bool replaceUserAccountVariables = false, bool stripNotExistingVariables = true, IDictionary<string, string> userDetails = null, bool isForConfirmationEmail = false, IDictionary<string, object> additionalReplacementData = null, bool forQuery = false)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            shoppingBasket ??= new WiserItemModel();
            basketLines ??= new List<WiserItemModel>();

            var repeatVars = new[] { "<!--{repeat:lines~?(.*?)}-->(.*?)<!--{/repeat:lines.*?}-->", "{repeat:lines~?(.*?)}(.*?){/repeat:lines.*?}" };
            var priceVars = new[] { "{price~(.*?)}", "{singleprice~(.*?)}", "{pricewithoutfactor~(.*?)}", "{singlepricewithoutfactor~(.*?)}" };
            var cultureName = shoppingBasket.ContainsDetail("valutaCulture") ? shoppingBasket.GetDetailValue("valutaCulture") : "nl-NL";

            logger.LogTrace("GCL ShoppingBasket: Start ReplaceBasketInTemplate");

            if (isForConfirmationEmail)
            {
                // Get extra details on basket line level, overwrite existing details.
                var query = (await templatesService.GetTemplateAsync(name: "BasketExtraLineFieldsForConfirmationEmail", type: TemplateTypes.Query)).Content;
                await UpdateLineDetailsViaExtraQueryAsync(shoppingBasket, basketLines, settings, query);

                // Get extra details on basket level, overwrite existing details.
                query = (await templatesService.GetTemplateAsync(name: "BasketExtraMainFieldsForConfirmationEmail", type: TemplateTypes.Query)).Content;
                await UpdateMainDetailsViaExtraQueryAsync(shoppingBasket, basketLines, settings, query);
            }

            foreach (var repeatVar in repeatVars)
            {
                logger.LogTrace("GCL ShoppingBasket: Start repeatVar");

                foreach (Match regexMatch in Regex.Matches(template, repeatVar, RegexOptions.Singleline))
                {
                    var lineTypes = regexMatch.Groups[1].Value.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var subTemplate = regexMatch.Groups[2].Value;
                    var linesTemplatePart = new StringBuilder();

                    var index = 0;
                    foreach (var line in basketLines)
                    {
                        if (lineTypes.Length > 0 && !line.GetDetailValue("type").InList(lineTypes))
                        {
                            index++;
                            continue;
                        }

                        var replaceData = line.GetSortedList(true);
                        replaceData["rowindex"] = index.ToString(CultureInfo.InvariantCulture);

                        var lineTemplate = subTemplate;
                        lineTemplate = stringReplacementsService.DoReplacements(lineTemplate, replaceData, forQuery);

                        foreach (var priceVar in priceVars)
                        {
                            foreach (Match priceRegexMatch in Regex.Matches(lineTemplate, priceVar, RegexOptions.Singleline))
                            {
                                var suffixArray = priceRegexMatch.Groups[1].Value.Split('~');
                                var vatRate = -1;
                                var numberFormat = "N2";
                                var localCultureName = cultureName;

                                if (suffixArray.Length == 0)
                                {
                                    continue;
                                }

                                var priceParts = suffixArray[0].Split('|', StringSplitOptions.RemoveEmptyEntries);

                                var priceTypeString = priceParts.Length > 0 ? priceParts[0] : suffixArray[0];
                                var priceType = ParseStringToPriceType(priceTypeString);

                                if (priceParts.Length > 1)
                                {
                                    vatRate = Convert.ToInt32(priceParts[1]);
                                }

                                if (suffixArray.Length > 1)
                                {
                                    // Take number format from string.
                                    numberFormat = suffixArray[1];

                                    if (suffixArray.Length > 2)
                                    {
                                        // Take culture name from string.
                                        localCultureName = suffixArray[2];
                                    }
                                }

                                var price = await GetLinePriceAsync(shoppingBasket, line, settings, priceType, priceVar.Contains("single"), false, vatRate, priceVar.Contains("withoutfactor"));
                                lineTemplate = lineTemplate.Replace(priceRegexMatch.Value, price.ToString(numberFormat, CultureInfo.CreateSpecificCulture(localCultureName)));
                            }
                        }

                        linesTemplatePart.Append(lineTemplate);
                    }

                    template = template.Replace(regexMatch.Value, linesTemplatePart.ToString());
                }

                logger.LogTrace("GCL ShoppingBasket: End repeatVar");
            }

            // Replace main variables.
            template = stringReplacementsService.DoReplacements(template, shoppingBasket.GetSortedList(true), forQuery);

            logger.LogTrace("GCL ShoppingBasket: End replace main variables");

            // Replace calculated variables on basket level.
            foreach (Match countMatch in Regex.Matches(template, "{count~(.*?)}", RegexOptions.Singleline))
            {
                template = template.Replace(countMatch.Value, GetLines(basketLines, countMatch.Groups[1].Value).Count.ToString());
            }
            foreach (Match totalCountMatch in Regex.Matches(template, "{totalcount~(.*?)}", RegexOptions.Singleline))
            {
                template = template.Replace(totalCountMatch.Value, GetTotalQuantity(basketLines, totalCountMatch.Groups[1].Value).ToString(CultureInfo.InvariantCulture));
            }

            logger.LogTrace("GCL ShoppingBasket: End replace calculated variables on basket level");

            // Replace all price types on basket level.
            foreach (Match priceMatch in Regex.Matches(template, "{price~(.*?)}", RegexOptions.Singleline))
            {
                var suffixArray = priceMatch.Groups[1].Value.Split('~');
                var lineTypes = suffixArray[0].Split(',');
                var vatRate = -1;
                var numberFormat = "N2";
                var localCultureName = cultureName;

                logger.LogTrace("GCL ShoppingBasket: Start replace price variable: {priceMatch}", priceMatch.Value);

                if (suffixArray.Length <= 1)
                {
                    throw new Exception($"GCL ShoppingBasket: Unsupported price variable format on basket level: {priceMatch.Value}. Use format: {{price~<linetype>~<pricetype>}}");
                }

                var priceParts = suffixArray[1].Split('|', StringSplitOptions.RemoveEmptyEntries);
                var priceTypeString = priceParts.Length > 0 ? priceParts[0] : suffixArray[1];
                var priceType = ParseStringToPriceType(priceTypeString);

                if (priceParts.Length > 1)
                {
                    vatRate = Convert.ToInt32(priceParts[1]);
                }

                if (suffixArray.Length > 2)
                {
                    // Take number format from string.
                    numberFormat = suffixArray[2];

                    if (suffixArray.Length > 3)
                    {
                        // Take culture name from string.
                        localCultureName = suffixArray[3];
                    }
                }

                var price = 0M;
                foreach (var lineType in lineTypes)
                {
                    price += await GetPriceAsync(shoppingBasket, basketLines, settings, priceType, lineType.Replace("all", ""), vatRate);
                }

                template = template.Replace(priceMatch.Value, price.ToString(numberFormat, CultureInfo.CreateSpecificCulture(localCultureName)));
            }

            logger.LogTrace("GCL ShoppingBasket: End replacing price variables");

            // Replace var {vatlines} by HTML based on vatPercentageTemplate for each rate present in shopping basket.
            if (template.Contains("{vatlines}"))
            {
                var vatLines = new StringBuilder();

                foreach (var vatRate in GetUniqueVatRates(basketLines, settings))
                {
                    var rule = await GetVatRuleByRateAsync(shoppingBasket, settings, vatRate);

                    var vatTemplate = settings.VatPercentageTemplate;

                    // Format {price~<numberFormat>~<culture>} or just {price}.
                    foreach (Match priceMatch in Regex.Matches(vatTemplate, "{price~?(.*?)}", RegexOptions.Singleline))
                    {
                        var numberFormat = "N2";
                        var localCultureName = cultureName;

                        var formatParts = priceMatch.Groups[1].Value.Split('~', StringSplitOptions.RemoveEmptyEntries);
                        if (formatParts.Length > 0)
                        {
                            numberFormat = formatParts[0];
                            if (formatParts.Length > 1)
                            {
                                localCultureName = formatParts[1];
                            }
                        }

                        vatTemplate = vatTemplate.Replace(priceMatch.Value, (await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.VatOnly, "", vatRate)).ToString(numberFormat, CultureInfo.CreateSpecificCulture(localCultureName)));
                    }

                    // Format {percentage~<numberFormat>~<culture>} or just {percentage}.
                    foreach (Match priceMatch in Regex.Matches(vatTemplate, "{price~?(.*?)}", RegexOptions.Singleline))
                    {
                        var numberFormat = "N2";
                        var localCultureName = cultureName;

                        var formatParts = priceMatch.Groups[1].Value.Split('~', StringSplitOptions.RemoveEmptyEntries);
                        if (formatParts.Length > 0)
                        {
                            numberFormat = formatParts[0];
                            if (formatParts.Length > 1)
                            {
                                localCultureName = formatParts[1];
                            }
                        }

                        vatTemplate = vatTemplate.Replace(priceMatch.Value, rule.Percentage.ToString(numberFormat, CultureInfo.CreateSpecificCulture(localCultureName)));
                    }

                    vatLines.Append(vatTemplate);
                }

                template = template.Replace("{vatlines}", vatLines.ToString());
            }

            logger.LogTrace("GCL ShoppingBasket: End replacing vatlines");

            if (userDetails?.Count > 0)
            {
                template = stringReplacementsService.DoReplacements(template, userDetails, forQuery);
            }
            else if (replaceUserAccountVariables)
            {
                var details = await GetUserDetailsAsync();
                if (details is { Count: > 0 })
                {
                    userDetails = details;
                    template = stringReplacementsService.DoReplacements(template, userDetails, forQuery);
                }
            }

            logger.LogTrace("GCL ShoppingBasket: End replacing user variables");

            // Replace delivery methods.
            if (template.Contains("{deliverymethods}"))
            {
                template = template.Replace("{deliverymethods}", await GetDeliveryMethodsAsync(shoppingBasket, basketLines, settings));
            }

            logger.LogTrace("GCL ShoppingBasket: End replacing delivery methods");

            // Replace payment methods.
            if (template.Contains("{paymentmethods}"))
            {
                template = template.Replace("{paymentmethods}", await GetPaymentMethodsAsync(shoppingBasket, basketLines, settings));
            }

            logger.LogTrace("GCL ShoppingBasket: End replacing payment methods");

            // Replace additional replacement data, if available.
            if (additionalReplacementData is { Count: > 0 })
            {
                template = stringReplacementsService.DoReplacements(template, additionalReplacementData, forQuery: forQuery);

                logger.LogTrace("GCL ShoppingBasket: End replacing additional data");
            }

            template = await templatesService.DoReplacesAsync(template);

            logger.LogTrace("GCL ShoppingBasket: End evaluating template");

            // Strip variables from template if not replaced.
            if (stripNotExistingVariables)
            {
                var regex = new Regex(@"{[^\]}\s]*}");
                template = regex.Replace(template, "");
            }

            logger.LogTrace("GCL ShoppingBasket: End ReplaceBasketInTemplate");

            return template;
        }

        /// <inheritdoc />
        public async Task<decimal> GetPriceAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, string lineType = "", int onlyIfVatRate = -1, bool includeDiscountGettingVat = true)
        {
            var price = 0M;

            var pricesIncludesVat = Convert.ToBoolean(await objectsService.FindSystemObjectByDomainNameAsync("PricesIncludeVat", "true"));
            var user = await accountsService.GetUserDataFromCookieAsync();

            if (priceType == ShoppingBasket.PriceTypes.PspPriceInVat || priceType == ShoppingBasket.PriceTypes.PspPriceExVat)
            {
                price = await GetPriceAsync(shoppingBasket, basketLines, settings, priceType == ShoppingBasket.PriceTypes.PspPriceInVat ? ShoppingBasket.PriceTypes.InVatInDiscount : ShoppingBasket.PriceTypes.ExVatInDiscount, lineType, onlyIfVatRate, includeDiscountGettingVat);
                if (price > 0)
                {
                    var depositPercentagePropertyName = await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_DepositPercentagePropertyName");
                    var userDetails = await GetUserDetailsAsync();
                    if (!String.IsNullOrWhiteSpace(depositPercentagePropertyName))
                    {
                        var depositPercentageValue = "";

                        if (user.MainUserId > 0 && userDetails.ContainsKey(depositPercentagePropertyName))
                        {
                            depositPercentageValue = userDetails[depositPercentagePropertyName];
                        }
                        else
                        {
                            var userId = accountsService.GetRecentlyCreateAccountId();
                            if (userId == 0)
                            {
                                var userEntityType = await objectsService.FindSystemObjectByDomainNameAsync("userEntityType", "relatie");
                                var linkTypeToUse = Constants.BasketToUserLinkType;

                                if (shoppingBasket.EntityType != Constants.BasketEntityType)
                                {
                                    linkTypeToUse = await wiserItemsService.GetLinkTypeAsync(userEntityType, OrderProcess.Models.Constants.OrderEntityType);
                                }

                                userId = (await wiserItemsService.GetLinkedItemIdsAsync(shoppingBasket.Id, linkTypeToUse, reverse: true, skipPermissionsCheck: true)).FirstOrDefault();
                            }

                            if (userId > 0)
                            {
                                var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(Account.Models.Constants.DefaultEntityType);
                                databaseConnection.ClearParameters();
                                databaseConnection.AddParameter("userId", userId);
                                databaseConnection.AddParameter("depositPercentagePropertyName", depositPercentagePropertyName);
                                var getValueResult = await databaseConnection.GetAsync($"SELECT `value` FROM `{tablePrefix}{WiserTableNames.WiserItemDetail}` WHERE item_id = ?userId AND `key` = ?depositPercentagePropertyName", true);
                                if (getValueResult.Rows.Count > 0)
                                {
                                    depositPercentageValue = getValueResult.Rows[0].Field<string>("value");
                                }
                            }
                        }

                        if (!String.IsNullOrWhiteSpace(depositPercentageValue) && Decimal.TryParse(depositPercentageValue.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var depositPercentage) && depositPercentage > 0)
                        {
                            price = (price / 100) * depositPercentage;
                        }
                    }
                }
            }
            else if (priceType == ShoppingBasket.PriceTypes.VatOnly)
            {
                if (onlyIfVatRate == -1)
                {
                    // Get VAT of all rates (sum).
                    foreach (var vatRate in GetUniqueVatRates(basketLines, settings))
                    {
                        if (pricesIncludesVat)
                        {
                            var inVatPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, includeDiscountGettingVat ? ShoppingBasket.PriceTypes.InVatInDiscount : ShoppingBasket.PriceTypes.InVatExDiscount, lineType, vatRate);
                            var factor = await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate);
                            price += Math.Round(inVatPrice / (1 + factor) * factor, 2, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            var exVatPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, includeDiscountGettingVat ? ShoppingBasket.PriceTypes.ExVatInDiscount : ShoppingBasket.PriceTypes.ExVatExDiscount, lineType, vatRate);
                            price += Math.Round(exVatPrice * await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate), 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
                else
                {
                    // Get VAT of specific rate.
                    if (pricesIncludesVat)
                    {
                        var inVatPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, includeDiscountGettingVat ? ShoppingBasket.PriceTypes.InVatInDiscount : ShoppingBasket.PriceTypes.InVatExDiscount, lineType, onlyIfVatRate);
                        var factor = await GetVatFactorByRateAsync(shoppingBasket, settings, onlyIfVatRate);
                        price += Math.Round(inVatPrice / (1 + factor) * factor, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        var exVatPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, includeDiscountGettingVat ? ShoppingBasket.PriceTypes.ExVatInDiscount : ShoppingBasket.PriceTypes.ExVatExDiscount, lineType, onlyIfVatRate);
                        price += Math.Round(exVatPrice * await GetVatFactorByRateAsync(shoppingBasket, settings, onlyIfVatRate), 2, MidpointRounding.AwayFromZero);
                    }
                }
            }
            else
            {
                switch (pricesIncludesVat)
                {
                    case true when priceType == ShoppingBasket.PriceTypes.ExVatExDiscount || priceType == ShoppingBasket.PriceTypes.ExVatInDiscount:
                    {
                        var priceInVat = await GetPriceAsync(shoppingBasket, basketLines, settings, priceType == ShoppingBasket.PriceTypes.ExVatExDiscount ? ShoppingBasket.PriceTypes.InVatExDiscount : ShoppingBasket.PriceTypes.InVatInDiscount, lineType, onlyIfVatRate);
                        price = priceInVat - await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.VatOnly, lineType, onlyIfVatRate);
                        break;
                    }
                    case false when priceType == ShoppingBasket.PriceTypes.InVatExDiscount || priceType == ShoppingBasket.PriceTypes.InVatInDiscount:
                    {
                        var priceExVat = await GetPriceAsync(shoppingBasket, basketLines, settings, priceType == ShoppingBasket.PriceTypes.InVatExDiscount ? ShoppingBasket.PriceTypes.ExVatExDiscount : ShoppingBasket.PriceTypes.ExVatInDiscount, lineType, onlyIfVatRate);
                        price = priceExVat + await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.VatOnly, lineType, onlyIfVatRate);
                        break;
                    }
                    default:
                    {
                        var lines = String.IsNullOrWhiteSpace(lineType) ? basketLines : basketLines.Where(l => l.GetDetailValue("type") == lineType).ToList();

                        foreach (var line in lines)
                        {
                            price += await GetLinePriceAsync(shoppingBasket, line, settings, priceType, false, true, onlyIfVatRate);
                        }

                        break;
                    }
                }
            }

            return Math.Round(price, 2, MidpointRounding.AwayFromZero);
        }

        /// <inheritdoc />
        public async Task<decimal> CalculateShippingCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            var shippingCosts = 0M;

            if (GetLines(basketLines, "product").Count == 0)
            {
                RemoveShippingCostsLine(basketLines);
                return shippingCosts;
            }

            var couponLines = GetLines(basketLines, Constants.BasketLineCouponType);
            if (couponLines.Count > 0)
            {
                foreach (var couponLine in couponLines)
                {
                    var coupon = await wiserItemsService.GetItemDetailsAsync(couponLine.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty), skipPermissionsCheck: true);
                    if (coupon.GetDetailValue<bool>(CouponConstants.FreeShippingCostsKey))
                    {
                        RemoveShippingCostsLine(basketLines);
                        return shippingCosts;
                    }
                }
            }

            var shippingCostsQuery = await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_ShippingCostsQuery");
            if (String.IsNullOrWhiteSpace(shippingCostsQuery))
            {
                return shippingCosts;
            }

            var getShippingCostsResult = await databaseConnection.GetAsync(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, shippingCostsQuery, true, forQuery: true), true);
            if (getShippingCostsResult.Rows.Count <= 0)
            {
                return shippingCosts;
            }

            var propertyName = getShippingCostsResult.Rows[0].Field<string>("propertyname") ?? "";
            var shippingCostsType = getShippingCostsResult.Rows[0].Field<string>("type") ?? "";

            var rawValue = Convert.ToString(getShippingCostsResult.Rows[0]["costs"]) ?? "";
            if (Decimal.TryParse(rawValue.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var costs))
            {
                if (String.IsNullOrWhiteSpace(propertyName) && shippingCostsType.Contains("shipping"))
                {
                    propertyName = "shippingcosts";
                }

                switch (shippingCostsType)
                {
                    case "fixedvalue":
                    {
                        shippingCosts = costs;
                        break;
                    }
                    case "fixedpercentage":
                    {
                        var totalBasketPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.InVatInDiscount, "product");
                        shippingCosts = (costs / 100) * totalBasketPrice;

                        break;
                    }
                    case "highestproductshipping":
                    {
                        var tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Select(line => line.GetDetailValue<decimal>(propertyName)).Prepend(0M).Max();

                        shippingCosts = tempValue;
                        break;
                    }
                    case "lowestproductshipping":
                    {
                        var tempValue = basketLines.Count > 0 ? 1000000M : 0M;

                        tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Select(line => line.GetDetailValue<decimal>(propertyName)).Prepend(tempValue).Min();

                        shippingCosts = tempValue;
                        break;
                    }
                    case "fixedvaluepluslowestproductshipping":
                    {
                        var tempValue = basketLines.Count > 0 ? 1000000M : 0M;

                        shippingCosts = costs;

                        tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Select(line => line.GetDetailValue<decimal>(propertyName)).Prepend(tempValue).Min();

                        shippingCosts += tempValue;
                        break;
                    }
                    case "averageproductshipping":
                    {
                        var tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Sum(line => line.GetDetailValue<decimal>(propertyName) * line.GetDetailValue<decimal>("quantity"));

                        shippingCosts = tempValue / GetLines(basketLines, "product").Count;
                        break;
                    }
                    case "sumproductshipping":
                    {
                        var tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Sum(line => line.GetDetailValue<decimal>(propertyName) * line.GetDetailValue<decimal>("quantity"));

                        shippingCosts = tempValue;
                        break;
                    }
                    case "highestproductshippingwithfixedvalue":
                    {
                        shippingCosts = costs;

                        var tempValue = basketLines.Where(line => !String.IsNullOrWhiteSpace(line.GetDetailValue(propertyName))).Select(line => line.GetDetailValue<decimal>(propertyName)).Prepend(0M).Max();

                        shippingCosts += tempValue;
                        break;
                    }
                    default:
                    {
                        logger.LogTrace("GCL ShoppingBasket CalculateShippingCostsWiser2 - No shipping costs type specified");
                        break;
                    }
                }
            }

            if (shippingCosts > 0)
            {
                var includesVat = getShippingCostsResult.Rows[0].Field<string>("includesvat") ?? "0";
                var vatRate = getShippingCostsResult.Rows[0].Field<string>("vatrate") ?? "1";
                var friendlyName = getShippingCostsResult.Rows[0].Field<string>("title") ?? "";

                var id = Convert.ToString(getShippingCostsResult.Rows[0]["id"]) ?? "";

                var shippingCostsLine = GetLines(basketLines, "shipping_costs").FirstOrDefault();

                if (shippingCostsLine != null)
                {
                    if (shippingCostsLine.ContainsDetail("uniqueid") && shippingCostsLine.GetDetailValue("uniqueid") == id && shippingCostsLine.GetDetailValue<decimal>("price") == shippingCosts)
                    {
                        return shippingCosts;
                    }

                    shippingCostsLine.SetDetail("uniqueid", id);
                    shippingCostsLine.SetDetail("price", shippingCosts.ToString(CultureInfo.InvariantCulture));
                    shippingCostsLine.SetDetail("includesvat", includesVat);
                    shippingCostsLine.SetDetail("vatrate", vatRate);
                    shippingCostsLine.SetDetail("description", friendlyName);
                }
                else
                {
                    var details = new Dictionary<string, string>
                    {
                        ["price"] = shippingCosts.ToString(CultureInfo.InvariantCulture),
                        ["includesvat"] = includesVat,
                        ["vatrate"] = vatRate,
                        ["description"] = friendlyName
                    };
                    await AddLineAsync(shoppingBasket, basketLines, settings, id, type: "shipping_costs", lineDetails: details, createNewTransaction: createNewTransaction);
                }
            }
            else
            {
                RemoveShippingCostsLine(basketLines);
            }

            return shippingCosts;
        }

        /// <inheritdoc />
        public async Task RecalculateCouponsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            // Whether the discount will be divided over all products.
            var divideDiscountOverProducts = (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_divide_discount_over_products")).Equals("true", StringComparison.OrdinalIgnoreCase);

            if (divideDiscountOverProducts)
            {
                // Start by resetting basket line prices to their original prices.
                // This needs to be done before checking if there are any coupons in the basket.
                foreach (var line in basketLines.Where(line => line.GetDetailValue("type") != Constants.BasketLineCouponType && line.ContainsDetail(Constants.OriginalPricePropertyName)))
                {
                    line.SetDetail(settings.PricePropertyName, line.GetDetailValue<decimal>(Constants.OriginalPricePropertyName));
                }
            }

            var couponLines = GetLines(basketLines, "coupon");
            if (couponLines.Count == 0)
            {
                return;
            }

            logger.LogTrace("Recalculating coupons");
            
            var discountGiven = 0M;
            foreach (var couponCode in couponLines.Select(line => line.GetDetailValue("code")))
            {
                var coupon = await GetCouponAsync(couponCode);
                if (coupon == null) continue;

                var couponResult = await HandleCouponAsync(shoppingBasket, basketLines, settings, coupon, divideDiscountOverProducts);
                await UpdateCouponAsync(shoppingBasket, basketLines, settings, couponResult, discountGiven, divideDiscountOverProducts, createNewTransaction);

                var couponLine = couponLines.SingleOrDefault(line => line.GetDetailValue("code") == couponCode);
                var couponDiscount = couponLine?.GetDetailValue<decimal>("price") ?? 0M;
                discountGiven += couponDiscount;

                logger.LogTrace("Recalculated coupon {couponCode}: {couponDiscount}", couponCode, couponResult.Discount);
            }

            logger.LogTrace("End recalculating coupons");
        }

        /// <inheritdoc />
        public async Task UpdateCouponAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, HandleCouponResultModel couponResult, decimal currentDiscount = 0M, bool divideDiscountOverProducts = false, bool createNewTransaction = true)
        {
            var couponDiscount = couponResult.Discount;
            var couponProductId = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_productid");
            var couponId = couponProductId;
            if (String.IsNullOrWhiteSpace(couponId))
            {
                couponId = couponResult.Coupon.Id.ToString();
            }

            if (!couponResult.Valid)
            {
                // Coupon is not valid anymore.
                if (!couponResult.DoRemove) return;

                // Remove this coupon from the basket.
                var couponLine = basketLines.SingleOrDefault(line => line.GetDetailValue("type") == Constants.BasketLineCouponType && line.GetDetailValue(Constants.ConnectedItemIdProperty) == couponId);
                if (couponLine == null) return;

                // Also remove the "coupon_discount_X" property from the basket line.
                if (divideDiscountOverProducts)
                {
                    var productCouponDiscountPropertyName = Constants.ProductCouponDiscountPropertyNamePrefix + couponResult.Coupon?.GetDetailValue(CouponConstants.Code);
                    foreach (var basketLine in basketLines.Where(basketLine => basketLine.ContainsDetail(productCouponDiscountPropertyName)))
                    {
                        // Setting the value to null or an empty string will cause the save function in WiserItemsService
                        // to remove the detail instead of saving it with an empty value.
                        basketLine.SetDetail(productCouponDiscountPropertyName, null);
                    }
                }

                await RemoveLinesAsync(shoppingBasket, basketLines, settings, new[] { couponId });
                return;
            }
            
            var couponIncludesVat = (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_inc_vat", "false")).Equals("true", StringComparison.OrdinalIgnoreCase);
            var couponVatRateSetting = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_vat_rate");

            if (couponResult.OnlyChangePrice)
            {
                foreach (var line in GetLines(basketLines, Constants.BasketLineCouponType).Where(line => line.GetDetailValue(Constants.ConnectedItemIdProperty) == couponId))
                {
                    line.SetDetail(divideDiscountOverProducts ? "total_discount" : "price", (couponDiscount * -1).ToString(CultureInfo.InvariantCulture));
                    logger.LogTrace("Changed coupon price to: {couponDiscount}", couponDiscount * -1);
                }

                await SaveAsync(shoppingBasket, basketLines, settings, createNewTransaction);
            }
            else
            {
                var details = new Dictionary<string, string>
                {
                    { "includesvat", couponIncludesVat ? "1" : "0" },
                    { "vatrate", couponVatRateSetting },
                    { "code", couponResult.Coupon.GetDetailValue(CouponConstants.Code) },
                    { "description", "Kortingscode" },
                    { Constants.CouponDividedOverProductsPropertyName, divideDiscountOverProducts ? "1" : "0" }
                };

                if (divideDiscountOverProducts)
                {
                    details.Add("total_discount", (couponResult.Discount * -1).ToString(CultureInfo.InvariantCulture));
                    details.Add("price", "0");
                }
                else
                {
                    details.Add("price", (couponResult.Discount * -1).ToString(CultureInfo.InvariantCulture));
                }

                await AddLineAsync(shoppingBasket, basketLines, settings, couponId, Convert.ToUInt64(couponId), 1, Constants.BasketLineCouponType, details, createNewTransaction);
            }

            if (divideDiscountOverProducts && couponResult.ValidForItems != null)
            {
                var discountRemaining = couponResult.Discount;
                var productCouponDiscountPropertyName = Constants.ProductCouponDiscountPropertyNamePrefix + couponResult.Coupon?.GetDetailValue(CouponConstants.Code);

                WiserItemModel lastItemLine = null;

                // Product prices must now be updated as well.
                foreach (var itemId in couponResult.ValidForItems)
                {
                    var line = basketLines.FirstOrDefault(line => line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty) == itemId);
                    if (line == null) continue;

                    // Always calculate over the original price.
                    var lineOriginalPrice = line.GetDetailValue<decimal>(Constants.OriginalPricePropertyName);
                    var lineQuantity = line.GetDetailValue<int>(settings.QuantityPropertyName);
                    var lineTotalPrice = lineOriginalPrice * lineQuantity;
                    if (lineTotalPrice == 0) continue;

                    lastItemLine = line;

                    // Determine how much discount this product will receive. It will always be relative to the total price of the basket line.
                    // The current discount is added to the total products price because it's a negative value (so it's subtracting the discount).
                    var linePricePercentage = lineTotalPrice / (couponResult.TotalProductsPrice + currentDiscount);
                    var discount = Math.Round((couponResult.Discount * linePricePercentage) / lineQuantity, 2);

                    // Update the price of the product.
                    line.SetDetail(settings.PricePropertyName, (lineOriginalPrice - discount).ToString(CultureInfo.InvariantCulture));
                    line.SetDetail(productCouponDiscountPropertyName, (-discount).ToString(CultureInfo.InvariantCulture));

                    discountRemaining -= discount;
                }

                if (discountRemaining != 0 && lastItemLine != null)
                {
                    lastItemLine.SetDetail(settings.PricePropertyName, (lastItemLine.GetDetailValue<decimal>(settings.PricePropertyName) - discountRemaining).ToString(CultureInfo.InvariantCulture));
                    lastItemLine.SetDetail(productCouponDiscountPropertyName, (lastItemLine.GetDetailValue<decimal>(productCouponDiscountPropertyName) - discountRemaining).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        /// <inheritdoc />
        public async Task<decimal> CalculatePaymentMethodCostsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = true)
        {
            var paymentMethodCosts = 0M;

            if (GetLines(basketLines, "product").Count <= 0 || String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue("paymentmethod")))
            {
                // Remove line.
                RemovePaymentMethodCostsLine(basketLines);
                return paymentMethodCosts;
            }

            var couponLines = GetLines(basketLines, Constants.BasketLineCouponType);
            if (couponLines.Count > 0)
            {
                foreach (var couponLine in couponLines)
                {
                    var coupon = await wiserItemsService.GetItemDetailsAsync(Convert.ToUInt64(couponLine.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty)), skipPermissionsCheck: true);
                    if (coupon.ContainsDetail("freepaymentserviceprovidercosts") && coupon.GetDetailValue<bool>("freepaymentserviceprovidercosts"))
                    {
                        RemovePaymentMethodCostsLine(basketLines);
                        return paymentMethodCosts;
                    }
                }
            }

            var paymentMethodCostsQuery = await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_PaymentCostsQuery");

            logger.LogTrace("Calculating paymentmethodcosts");

            if (String.IsNullOrWhiteSpace(paymentMethodCostsQuery))
            {
                return paymentMethodCosts;
            }

            var getPaymentMethodCostsResult = await databaseConnection.GetAsync(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, paymentMethodCostsQuery, true, forQuery: true), true);

            if (getPaymentMethodCostsResult.Rows.Count == 0)
            {
                return paymentMethodCosts;
            }

            var rawValue = Convert.ToString(getPaymentMethodCostsResult.Rows[0]["costs"]) ?? "";
            if (Decimal.TryParse(rawValue.Replace(",", "."), out var costs))
            {
                var isPercentage = Convert.ToString(getPaymentMethodCostsResult.Rows[0]["ispercentage"]) ?? "";
                if (isPercentage == "1")
                {
                    var totalBasketPrice = await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.InVatInDiscount, "product");
                    paymentMethodCosts = (costs / 100) * totalBasketPrice;
                }
                else
                {
                    paymentMethodCosts = costs;
                }
            }

            if (paymentMethodCosts > 0)
            {
                var includesVat = getPaymentMethodCostsResult.Rows[0].Field<string>("includesvat") ?? "0";
                var vatRate = getPaymentMethodCostsResult.Rows[0].Field<string>("vatrate") ?? "1";
                var friendlyName = getPaymentMethodCostsResult.Rows[0].Field<string>("friendlyname") ?? "";

                var id = getPaymentMethodCostsResult.Rows[0].Field<string>("id") ?? "";
                var paymentMethodCostsLine = GetLines(basketLines, "paymentmethod_costs").FirstOrDefault();

                if (paymentMethodCostsLine != null)
                {
                    if (paymentMethodCostsLine.ContainsDetail("uniqueid") && paymentMethodCostsLine.GetDetailValue("uniqueid") == id && paymentMethodCostsLine.GetDetailValue<decimal>("price") == paymentMethodCosts)
                    {
                        return paymentMethodCosts;
                    }

                    paymentMethodCostsLine.SetDetail("uniqueid", id);
                    paymentMethodCostsLine.SetDetail("price", paymentMethodCosts.ToString(CultureInfo.InvariantCulture));
                    paymentMethodCostsLine.SetDetail("includesvat", includesVat);
                    paymentMethodCostsLine.SetDetail("vatrate", vatRate);
                    paymentMethodCostsLine.SetDetail("description", friendlyName);
                    logger.LogTrace("Calculating paymentmethodcosts - Changed existing rule");
                }
                else
                {
                    var details = new Dictionary<string, string>
                    {
                        ["price"] = paymentMethodCosts.ToString(CultureInfo.InvariantCulture),
                        ["includesvat"] = includesVat,
                        ["vatrate"] = vatRate,
                        ["description"] = friendlyName
                    };
                    await AddLineAsync(shoppingBasket, basketLines, settings, id, type: "paymentmethod_costs", lineDetails: details, createNewTransaction: createNewTransaction);
                }
            }
            else
            {
                // Remove line.
                RemovePaymentMethodCostsLine(basketLines);
            }

            return paymentMethodCosts;
        }

        /// <inheritdoc />
        public async Task<List<WiserItemModel>> RemoveLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, ICollection<string> itemIdsOrUniqueIds)
        {
            if (itemIdsOrUniqueIds == null || !itemIdsOrUniqueIds.Any())
            {
                return basketLines;
            }

            var linesToRemove = new List<WiserItemModel>();

            foreach (var line in basketLines)
            {
                // Check separated into three separate if-statements for readability.
                if (line.ContainsDetail("uniqueid") && itemIdsOrUniqueIds.Any(id => id == line.GetDetailValue("uniqueid")))
                    linesToRemove.Add(line);
                else if (line.Id > 0 && itemIdsOrUniqueIds.Any(id => id == line.Id.ToString()))
                    linesToRemove.Add(line);
                else if (line.ContainsDetail(Constants.ConnectedItemIdProperty) && itemIdsOrUniqueIds.Any(id => id == line.GetDetailValue(Constants.ConnectedItemIdProperty)))
                    linesToRemove.Add(line);
            }

            foreach (var line in linesToRemove)
            {
                basketLines.Remove(line);
            }

            // Check if coupons should be deleted. If the total price of all products is 0, coupons will be deleted.
            // Product lines are all lines that are not coupons, shipping costs, or payment method costs.
            var tempLines = basketLines.Where(line => !line.GetDetailValue("type").InList(StringComparer.OrdinalIgnoreCase, "shipping_costs", "paymentmethod_costs", Constants.BasketLineCouponType)).ToList();
            var tempTotal = 0M;
            foreach (var tempLine in tempLines)
            {
                tempTotal += await GetLinePriceAsync(shoppingBasket, tempLine, settings, ShoppingBasket.PriceTypes.InVatExDiscount);
            }

            if (tempTotal <= 0)
            {
                linesToRemove.Clear();
                linesToRemove.AddRange(GetLines(basketLines, Constants.BasketLineCouponType));
                foreach (var line in linesToRemove)
                {
                    basketLines.Remove(line);
                }
            }

            await RecalculateVariablesAsync(shoppingBasket, basketLines, settings);

            return basketLines;
        }

        /// <inheritdoc />
        public async Task AddLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0UL, int quantity = 1, string type = "product", IDictionary<string, string> lineDetails = null, bool createNewTransaction = true)
        {
            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();
            try
            {
                if (!String.IsNullOrWhiteSpace(settings.SqlQuery))
                {
                    var sqlQuery = settings.SqlQuery;
                    sqlQuery = await templatesService.HandleIncludesAsync(sqlQuery, false, null, false, true);
                    sqlQuery = sqlQuery.Replace("{itemid}", itemId.ToString());
                    sqlQuery = sqlQuery.Replace("{quantity}", quantity.ToString(CultureInfo.InvariantCulture));

                    sqlQuery = await stringReplacementsService.DoAllReplacementsAsync(sqlQuery, null, true, true, false, true);

                    var getItemDetailsResult = await databaseConnection.GetAsync(sqlQuery, true);
                    if (getItemDetailsResult.Rows.Count > 0)
                    {
                        var details = getItemDetailsResult.Columns.Cast<DataColumn>().Where(dataColumn => dataColumn.ColumnName != "id").ToDictionary(dataColumn => dataColumn.ColumnName, dataColumn => Convert.ToString(getItemDetailsResult.Rows[0][dataColumn]));

                        lineDetails ??= new Dictionary<string, string>();
                        foreach (var (key, value) in details)
                        {
                            lineDetails[key] = value;
                        }
                    }
                }

                var addItemLine = AddLineInternal(basketLines, settings, uniqueId, itemId, quantity, type, lineDetails);

                // Write changes to database.
                await SaveAsync(shoppingBasket, basketLines, settings, false);

                var alsoCreateItemLink = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_AlsoCreateItemLinkBetweenBasketLineAndProduct")).Equals("true", StringComparison.OrdinalIgnoreCase);
                if (alsoCreateItemLink)
                {
                    if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_LinkTypeProductToOrderLine", Constants.ProductToOrderLineLinkType.ToString()), out var productToBasketLinkType))
                    {
                        productToBasketLinkType = Constants.ProductToOrderLineLinkType;
                    }

                    var productId = addItemLine.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                    if (productId > 0)
                    {
                        await wiserItemsService.AddItemLinkAsync(productId, addItemLine.Id, productToBasketLinkType, skipPermissionsCheck: true);
                    }
                }

                await ExecuteAddToBasketQuery(shoppingBasket, basketLines, settings);

                // Reload basket (for getting item details of added item).
                if (!String.IsNullOrEmpty(settings.ExtraMainFieldsQuery) || !String.IsNullOrEmpty(settings.ExtraLineFieldsQuery))
                {
                    (shoppingBasket, basketLines, _, _) = await LoadAsync(settings, shoppingBasket.Id);
                }

                await RecalculateVariablesAsync(shoppingBasket, basketLines, settings, type, false);

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AddLinesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, IList<AddToShoppingBasketModel> items, bool createNewTransaction = true)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            if (createNewTransaction) await databaseConnection.BeginTransactionAsync();
            try
            {
                var createLinksFor = new List<WiserItemModel>();
                foreach (var item in items)
                {
                    if (!String.IsNullOrWhiteSpace(settings.SqlQuery))
                    {
                        var sqlQuery = settings.SqlQuery;
                        sqlQuery = await templatesService.HandleIncludesAsync(sqlQuery, false, null, false, true);
                        sqlQuery = sqlQuery.Replace("{itemid}", item.ItemId.ToString());
                        sqlQuery = sqlQuery.Replace("{quantity}", item.Quantity.ToString(CultureInfo.InvariantCulture));

                        sqlQuery = await stringReplacementsService.DoAllReplacementsAsync(sqlQuery, null, true, true, false, true);

                        var getItemDetailsResult = await databaseConnection.GetAsync(sqlQuery, true);
                        if (getItemDetailsResult.Rows.Count > 0)
                        {
                            var details = getItemDetailsResult.Columns.Cast<DataColumn>().Where(dataColumn => dataColumn.ColumnName != "id").ToDictionary(dataColumn => dataColumn.ColumnName, dataColumn => Convert.ToString(getItemDetailsResult.Rows[0][dataColumn]));

                            item.LineDetails ??= new Dictionary<string, string>();
                            foreach (var (key, value) in details)
                            {
                                item.LineDetails[key] = value;
                            }
                        }
                    }

                    var addItemLine = AddLineInternal(basketLines, settings, item.UniqueId, item.ItemId, item.Quantity, item.Type, item.LineDetails);
                    createLinksFor.Add(addItemLine);
                }

                // Write changes to database.
                await SaveAsync(shoppingBasket, basketLines, settings, false);

                if (createLinksFor.Count > 0)
                {
                    var alsoCreateItemLink = (await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_AlsoCreateItemLinkBetweenBasketLineAndProduct")).Equals("true", StringComparison.OrdinalIgnoreCase);
                    if (alsoCreateItemLink)
                    {
                        if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_LinkTypeProductToOrderLine", Constants.ProductToOrderLineLinkType.ToString()), out var productToBasketLinkType))
                        {
                            productToBasketLinkType = Constants.ProductToOrderLineLinkType;
                        }

                        foreach (var item in createLinksFor)
                        {
                            var productId = item.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                            if (productId <= 0)
                            {
                                continue;
                            }

                            await wiserItemsService.AddItemLinkAsync(productId, item.Id, productToBasketLinkType, skipPermissionsCheck: true);
                        }
                    }
                }

                await ExecuteAddToBasketQuery(shoppingBasket, basketLines, settings);

                if (!String.IsNullOrEmpty(settings.ExtraMainFieldsQuery) || !String.IsNullOrEmpty(settings.ExtraLineFieldsQuery))
                {
                    (shoppingBasket, basketLines, _, _) = await LoadAsync(settings, shoppingBasket.Id);
                }

                // Recalculate shipping costs, coupons etc. after getting the extra fields (price can be selected with extra fields query).
                await RecalculateVariablesAsync(shoppingBasket, basketLines, settings, items.First().Type, false);

                if (createNewTransaction) await databaseConnection.CommitTransactionAsync();
            }
            catch
            {
                if (createNewTransaction) await databaseConnection.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateLineAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, UpdateItemModel item)
        {
            if (item == null)
            {
                return;
            }

            // Find existing basket line.
            var lineToUpdate = basketLines.Find(l => l.Id == item.LineId);
            if (lineToUpdate == null)
            {
                return;
            }

            // Enrich the line details if a query is present.
            if (!String.IsNullOrWhiteSpace(settings.SqlQuery))
            {
                var sqlQuery = settings.SqlQuery;
                sqlQuery = await templatesService.HandleIncludesAsync(sqlQuery, false, null, false, true);
                sqlQuery = sqlQuery.Replace("{itemid}", lineToUpdate.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty).ToString());
                sqlQuery = sqlQuery.Replace("{quantity}", lineToUpdate.GetDetailValue<int>(settings.QuantityPropertyName).ToString());
                sqlQuery = await stringReplacementsService.DoAllReplacementsAsync(sqlQuery, null, true, true, false, true);

                var getItemDetailsResult = await databaseConnection.GetAsync(sqlQuery, true);
                if (getItemDetailsResult.Rows.Count > 0)
                {
                    var details = getItemDetailsResult.Columns.Cast<DataColumn>().Where(dataColumn => dataColumn.ColumnName != "id").ToDictionary(dataColumn => dataColumn.ColumnName, dataColumn => Convert.ToString(getItemDetailsResult.Rows[0][dataColumn]));

                    item.LineDetails ??= new Dictionary<string, string>();
                    foreach (var (key, value) in details)
                    {
                        item.LineDetails[key] = value;
                    }
                }
            }

            // Unique ID can be updated if it was provided in the JSON.
            if (!String.IsNullOrWhiteSpace(item.UniqueId))
            {
                lineToUpdate.SetDetail("uniqueid", item.UniqueId);
            }

            if (item.LineDetails != null)
            {
                foreach (var (key, value) in item.LineDetails.Where(ld => !ld.Key.InList("uniqueid", Constants.ConnectedItemIdProperty, "quantity", "type")))
                {
                    lineToUpdate.SetDetail(key, value);
                }
            }

            // Write changes to database.
            await SaveAsync(shoppingBasket, basketLines, settings);

            if (!String.IsNullOrEmpty(settings.ExtraMainFieldsQuery) || !String.IsNullOrEmpty(settings.ExtraLineFieldsQuery))
            {
                (shoppingBasket, basketLines, _, _) = await LoadAsync(settings, shoppingBasket.Id);
            }

            // Recalculate sendcosts, coupons etc. after getting the extra fields (price can be selected with extra fields query).
            await RecalculateVariablesAsync(shoppingBasket, basketLines, settings, lineToUpdate.GetDetailValue("type"));
        }

        /// <inheritdoc />
        public async Task<HandleCouponResultModel> AddCouponToBasketAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode = "", bool createNewTransaction = true)
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                return new HandleCouponResultModel { ResultCode = ShoppingBasket.HandleCouponResults.HttpContextUnavailable };
            }

            var divideDiscountOverProducts = (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_divide_discount_over_products")).Equals("true", StringComparison.OrdinalIgnoreCase);
            HandleCouponResultModel handleCouponResult;

            if (!String.IsNullOrWhiteSpace(couponCode))
            {
                handleCouponResult = await HandleCouponAsync(shoppingBasket, basketLines, settings, couponCode, divideDiscountOverProducts);
            }
            else
            {
                couponCode = HttpContextHelpers.GetRequestValue(httpContext, "couponcode", false);
                if (String.IsNullOrWhiteSpace(couponCode))
                {
                    return new HandleCouponResultModel { ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode };
                }

                handleCouponResult = await HandleCouponAsync(shoppingBasket, basketLines, settings, couponCode, divideDiscountOverProducts);
            }

            // Check if the maximum amount of coupons has been reached yet.
            if (Int64.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("BASKET_numberOfCouponsAllowed", "0"), out var nrOfCouponsAllowed) && nrOfCouponsAllowed > 0)
            {
                var nrOfCoupons = basketLines.Count(line => line.GetDetailValue("type") == Constants.BasketLineCouponType);

                logger.LogTrace("Nr of coupons used: {nrOfCoupons}", nrOfCoupons);

                if (nrOfCoupons >= nrOfCouponsAllowed)
                {
                    logger.LogTrace("Reached maximum amount of coupons.");
                    return new HandleCouponResultModel { ResultCode = ShoppingBasket.HandleCouponResults.MaximumCouponsReached };
                }
            }

            if (handleCouponResult.Valid)
            {
                await UpdateCouponAsync(shoppingBasket, basketLines, settings, handleCouponResult, 0M, divideDiscountOverProducts, createNewTransaction);
            }

            return handleCouponResult;
        }

        /// <inheritdoc />
        public List<WiserItemModel> GetLines(List<WiserItemModel> basketLines, string lineType)
        {
            if (basketLines == null)
            {
                return new List<WiserItemModel>();
            }

            if (!basketLines.Any() || String.IsNullOrWhiteSpace(lineType))
            {
                return basketLines;
            }

            return basketLines.Where(line => line != null && line.GetDetailValue("type").Equals(lineType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <inheritdoc />
        public async Task<IList<VatRule>> GetVatRulesAsync()
        {
            var vatRules = new List<VatRule>();

            try
            {
                databaseConnection.ClearParameters();
                var getVatRulesResult = await databaseConnection.GetAsync($@"
                    SELECT
                        vatrule.id,
                        IFNULL(country.`value`, '') AS country,
                        IFNULL(b2b.`value`, '') AS b2b,
                        COALESCE(vatratecode.`value`, vatrate.`value`, '') AS vatrate,
                        IFNULL(percentage.`value`, '') AS percentage
                    FROM `{WiserTableNames.WiserItem}` AS vatrule
                    LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS country ON country.item_id = vatrule.id AND country.`key` = 'country'
                    LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS b2b ON b2b.item_id = vatrule.id AND b2b.`key` = 'b2b'
                    LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS vatrate ON vatrate.item_id = vatrule.id AND vatrate.`key` = 'vatrate'
                    LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS vatratecode ON vatratecode.item_id = vatrate.`value` AND vatratecode.`key` = 'code'
                    LEFT JOIN `{WiserTableNames.WiserItemDetail}` AS percentage ON percentage.item_id = vatrule.id AND percentage.`key` = 'percentage'
                    WHERE vatrule.entity_type = 'vatrule'
                    ORDER BY country.`value` DESC, b2b.`value` DESC", true);

                foreach (DataRow row in getVatRulesResult.Rows)
                {
                    var rule = new VatRule { Country = row.Field<string>("country") };

                    var ruleB2BValue = row.Field<string>("b2b");
                    if (Int32.TryParse(ruleB2BValue, out var ruleB2B)) rule.B2B = ruleB2B;

                    var ruleVatRateValue = row.Field<string>("vatrate");
                    if (Int32.TryParse(ruleVatRateValue, out var ruleVatRate)) rule.VatRate = ruleVatRate;

                    var rulePercentageValue = row.Field<string>("percentage");
                    if (Decimal.TryParse(rulePercentageValue, out var rulePercentage)) rule.Percentage = rulePercentage;

                    vatRules.Add(rule);
                }

                logger.LogDebug("{count} VAT rules loaded from database.", vatRules.Count);
            }
            catch (Exception exception)
            {
                logger.LogError("Error loading VAT rules. Error message: {exception}", exception);
            }

            return vatRules;
        }

        /// <inheritdoc />
        public async Task UpdateBasketLineQuantityAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string itemIdOrUniqueId, decimal quantity)
        {
            if (settings.RemoveItemWhenQuantityIsZero && quantity <= 0M)
            {
                await RemoveLinesAsync(shoppingBasket, basketLines, settings, new[] { itemIdOrUniqueId });
            }

            if (quantity > settings.MaxItemQuantity)
            {
                quantity = settings.MaxItemQuantity;
            }

            foreach (var line in basketLines.Where(line => (line.ContainsDetail("uniqueid") && line.GetDetailValue("uniqueid") == itemIdOrUniqueId) || (line.Id > 0 && line.Id.ToString() == itemIdOrUniqueId) || (line.ContainsDetail(Constants.ConnectedItemIdProperty) && line.GetDetailValue(Constants.ConnectedItemIdProperty) == itemIdOrUniqueId)))
            {
                line.SetDetail(settings.QuantityPropertyName, quantity.ToString(CultureInfo.InvariantCulture));
            }

            await RecalculateVariablesAsync(shoppingBasket, basketLines, settings);
        }

        /// <inheritdoc />
        public async Task<ShoppingBasketCmsSettingsModel> GetSettingsAsync()
        {
            var cookieAgeSetting = await objectsService.FindSystemObjectByDomainNameAsync("basketCookieAgeInDays");
            if (!Int32.TryParse(cookieAgeSetting, out var cookieAgeInDays))
            {
                cookieAgeInDays = 7;
            }

            return new()
            {
                CookieName = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_cookieName", Constants.DefaultCookieName),
                B2BPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_B2bPropertyName"),
                CountryPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_CountryPropertyName"),
                DiscountPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_DiscountPropertyName", "discount"),
                FactorPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_FactorPropertyName", "factor"),
                IncludesVatPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_IncludesVatPropertyName", "includesvat"),
                PricePropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_PricePropertyName", "price"),
                QuantityPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_QuantityProductDataProperty", "quantity"),
                VatRatePropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_VatrateProductDataProperty", "vatrate"),
                CookieAgeInDays = cookieAgeInDays,
                HandleRequest = true
            };
        }

        /// <inheritdoc />
        public async Task CheckForFreeProductAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, bool createNewTransaction = false)
        {
            var freeProductActions = await GetFreeProductActionsAsync();
            if (freeProductActions.Count == 0)
            {
                return;
            }

            var addToBasketQuery = await objectsService.FindSystemObjectByDomainNameAsync("CHECKOUT_FreeActionProductQuery");
            if (String.IsNullOrWhiteSpace(addToBasketQuery))
            {
                addToBasketQuery = await objectsService.FindSystemObjectByDomainNameAsync("W2CHECKOUT_FreeActionProductQuery");
                if (String.IsNullOrWhiteSpace(addToBasketQuery))
                {
                    logger.LogWarning("Free action product error: No query set for adding free products! Set one up in the settings module in Wiser.");
                    return;
                }
            }

            var basketTotalAmount = await GetPriceAsync(shoppingBasket, basketLines, settings);
            foreach (var actionItem in freeProductActions)
            {
                var freeProductId = actionItem.GetDetailValue<ulong>("gratis_product");
                var freeActionText = actionItem.GetDetailValue("tekst_balk");
                var minimumAmount = actionItem.GetDetailValue<decimal>("min_bedrag");
                var maximumAmount = actionItem.GetDetailValue<decimal>("max_bedrag");
                var actionProductId = actionItem.GetDetailValue<ulong>("actie_product");

                var actionIsValid = false;

                if (actionProductId > 0)
                {
                    foreach (var line in basketLines)
                    {
                        var basketProductItemId = line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);

                        if (basketProductItemId == actionProductId)
                        {
                            actionIsValid = true;

                            if (maximumAmount != 0 || minimumAmount != 0)
                            {
                                actionIsValid = basketTotalAmount >= minimumAmount && (maximumAmount == 0 || basketTotalAmount <= maximumAmount);
                            }
                        }
                    }
                }
                else
                {
                    actionIsValid = basketTotalAmount >= minimumAmount && (maximumAmount == 0 || basketTotalAmount <= maximumAmount);
                }

                if (!actionIsValid)
                {
                    var toRemove = basketLines.Where(line => line.GetDetailValue("wiser2_free_product_action_id") == actionItem.Id.ToString()).Select(line => line.Id.ToString()).ToList();
                    await RemoveLinesAsync(shoppingBasket, basketLines, settings, toRemove);
                    continue;
                }

                if (basketLines.Any(line => line.GetDetailValue("wiser2_free_product_action_id") == actionItem.Id.ToString()))
                {
                    // Product is already added.
                    continue;
                }

                var tempQuery = addToBasketQuery;

                var queryReplacements = new Dictionary<string, string>()
                {
                    { "quantity", "1" },
                    { "itemid", freeProductId.ToString() },
                    { "language_code", languagesService.CurrentLanguageCode }
                };

                tempQuery = stringReplacementsService.DoReplacements(tempQuery, queryReplacements, true);

                var freeActionProductQueryResult = await databaseConnection.GetAsync(tempQuery);
                if (freeActionProductQueryResult.Rows.Count == 0)
                {
                    continue;
                }

                var details = new Dictionary<string, string>();

                foreach (DataColumn column in freeActionProductQueryResult.Columns)
                {
                    if (!column.ColumnName.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add(column.ColumnName, Convert.ToString(freeActionProductQueryResult.Rows[0][column]));
                    }
                }

                // Price should always be 0, no matter what the query returns.
                details[settings.PricePropertyName] = "0";

                var replacementData = new Dictionary<string, object>
                {
                    {"minimumAmount", minimumAmount},
                    {"maximumAmount", maximumAmount},
                    {"remainder", minimumAmount - basketTotalAmount}
                };

                freeActionText = stringReplacementsService.DoReplacements(freeActionText, replacementData);

                // Add the action details.
                details["wiser2_is_free_action_product"] = "1";
                details["wiser2_free_product_action_id"] = actionItem.Id.ToString();
                details["wiser2_free_action_show_banner"] = String.IsNullOrWhiteSpace(freeActionText) ? "0" : "1";
                details["wiser2_free_action_banner"] = freeActionText;

                await AddLineAsync(shoppingBasket, basketLines, settings, lineDetails: details, createNewTransaction: createNewTransaction);
            }
        }

        /// <inheritdoc />
        public async Task<IList<WiserItemModel>> GetFreeProductActionsAsync()
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("environment", (int)gclSettings.Environment);
            var getActionsResult = await databaseConnection.GetAsync($@"
                SELECT id, entity_type
                FROM `{WiserTableNames.WiserItem}`
                WHERE entity_type = 'actie' AND published_environment & ?environment = ?environment");

            var result = new List<WiserItemModel>(getActionsResult.Rows.Count);

            // No items, return empty list.
            if (getActionsResult.Rows.Count == 0)
            {
                return result;
            }

            // Populate list.
            foreach (DataRow dataRow in getActionsResult.Rows)
            {
                var itemId = dataRow.Field<ulong>("id");
                var entityType = dataRow.Field<string>("entity_type");

                result.Add(await wiserItemsService.GetItemDetailsAsync(itemId, entityType: entityType, skipPermissionsCheck: true));
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<VatRule> GetVatRuleByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate)
        {
            var httpContext = httpContextAccessor.HttpContext;

            var userDetails = await GetUserDetailsAsync();
            if (userDetails.ContainsKey("pay_btw") && userDetails["pay_btw"] == "0")
            {
                return new VatRule { Percentage = 0M, VatRate = vatRate };
            }

            var vatRules = await GetVatRulesAsync();
            if (vatRules == null || vatRules.Count == 0)
            {
                return new VatRule();
            }

            var vatRule = vatRules.FirstOrDefault(r => r.VatRate == vatRate);
            if (vatRule == null)
            {
                return new VatRule();
            }

            var doReturnCountry = false;
            var doReturnB2B = false;

            // Handle country.
            if (!String.IsNullOrWhiteSpace(vatRule.Country))
            {
                var countryCookieName = await objectsService.FindSystemObjectByDomainNameAsync("W2_CountryCookieName");

                if (!String.IsNullOrWhiteSpace(countryCookieName))
                {
                    if (!String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(settings.CountryPropertyName)) && shoppingBasket.GetDetailValue(settings.CountryPropertyName) == vatRule.Country)
                    {
                        doReturnCountry = true;
                    }
                    else if (userDetails.ContainsKey(settings.CountryPropertyName) && !String.IsNullOrWhiteSpace(userDetails[settings.CountryPropertyName]) && userDetails[settings.CountryPropertyName] == vatRule.Country)
                    {
                        doReturnCountry = true;
                    }
                    else if (HttpContextHelpers.ReadCookie(httpContext, countryCookieName) == vatRule.Country)
                    {
                        doReturnCountry = true;
                    }
                }
            }
            else
            {
                doReturnCountry = true;
            }

            // Handle B2B.
            if (vatRule.B2B >= 0)
            {
                switch (vatRule.B2B)
                {
                    case 1 when !String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(settings.B2BPropertyName)):
                    case 0 when String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(settings.B2BPropertyName)):
                    case 1 when userDetails.ContainsKey(settings.B2BPropertyName) && !String.IsNullOrWhiteSpace(userDetails[settings.B2BPropertyName]):
                    case 0 when userDetails.ContainsKey(settings.B2BPropertyName) && String.IsNullOrWhiteSpace(userDetails[settings.B2BPropertyName]):
                        doReturnB2B = true;
                        break;
                }
            }
            else
            {
                doReturnB2B = true;
            }

            if (doReturnCountry && doReturnB2B)
            {
                return vatRule;
            }

            return new VatRule();
        }

        /// <inheritdoc />
        public async Task<decimal> GetLinePriceAsync(WiserItemModel shoppingBasket, WiserItemModel line, ShoppingBasketCmsSettingsModel settings, ShoppingBasket.PriceTypes priceType = ShoppingBasket.PriceTypes.InVatInDiscount, bool singlePrice = false, bool round = false, int onlyIfVatRate = -1, bool withoutFactor = false, bool useOriginalPrice = false)
        {
            var output = 0M;
            var quantity = 1;
            var factor = 1M;
            var price = 0M;
            var priceIncludesVat = Convert.ToBoolean(await objectsService.FindSystemObjectByDomainNameAsync("W2_PricesIncludeVat", "true"));
            var vatRate = 1;
            var discount = 0M;

            if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.QuantityPropertyName)))
            {
                quantity = line.GetDetailValue<int>(settings.QuantityPropertyName);
            }

            if (!withoutFactor && !String.IsNullOrWhiteSpace(line.GetDetailValue(settings.FactorPropertyName)))
            {
                factor = line.GetDetailValue<decimal>(settings.FactorPropertyName);
            }

            if (useOriginalPrice)
            {
                if (!String.IsNullOrWhiteSpace(line.GetDetailValue(Constants.OriginalPricePropertyName)))
                {
                    price = line.GetDetailValue<decimal>(Constants.OriginalPricePropertyName);
                }
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.PricePropertyName)))
                {
                    price = line.GetDetailValue<decimal>(settings.PricePropertyName);
                }
            }

            if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.IncludesVatPropertyName)))
            {
                priceIncludesVat = line.GetDetailValue<bool>(settings.IncludesVatPropertyName);
            }

            if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.VatRatePropertyName)))
            {
                vatRate = line.GetDetailValue<int>(settings.VatRatePropertyName);
            }

            if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.DiscountPropertyName)))
            {
                discount = line.GetDetailValue<decimal>(settings.DiscountPropertyName);
            }

            // Calculate price.
            if (onlyIfVatRate > -1 && onlyIfVatRate != vatRate)
            {
                return 0M;
            }

            switch (priceType)
            {
                case ShoppingBasket.PriceTypes.InVatExDiscount:
                case ShoppingBasket.PriceTypes.InVatInDiscount:
                    output = priceIncludesVat
                        ? price
                        : price * (1 + await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate));

                    if (priceType == ShoppingBasket.PriceTypes.InVatInDiscount)
                    {
                        output *= 1 - (discount / 100);
                    }

                    break;
                case ShoppingBasket.PriceTypes.ExVatExDiscount:
                case ShoppingBasket.PriceTypes.ExVatInDiscount:
                    output = priceIncludesVat
                        ? price / (1 + await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate))
                        : price;

                    if (priceType == ShoppingBasket.PriceTypes.ExVatInDiscount)
                    {
                        output *= 1 - (discount / 100);
                    }

                    break;
                case ShoppingBasket.PriceTypes.DiscountInVat:
                    output = priceIncludesVat
                        ? price * (discount / 100) * -1
                        : price * (1 + await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate)) * (discount / 100) * -1;

                    break;
                case ShoppingBasket.PriceTypes.DiscountExVat:
                    output = priceIncludesVat
                        ? price / (1 + await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate)) * (discount / 100) * -1
                        : price * (discount / 100) * -1;

                    break;
                case ShoppingBasket.PriceTypes.VatOnly:
                    output = priceIncludesVat
                        ? price / (1 + await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate)) * await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate)
                        : price * await GetVatFactorByRateAsync(shoppingBasket, settings, vatRate);

                    break;
            }

            output = output * (singlePrice ? 1 : quantity) * factor;

            return round ? Math.Round(output, 2, MidpointRounding.AwayFromZero) : output;
        }

        /// <inheritdoc />
        public async Task<decimal> GetVatFactorByRateAsync(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings, int vatRate)
        {
            vatFactorsByRate ??= new SortedList<int, decimal>();

            if (!vatFactorsByRate.ContainsKey(vatRate))
            {
                vatFactorsByRate.Add(vatRate, (await GetVatRuleByRateAsync(shoppingBasket, settings, vatRate)).Percentage / 100);
            }

            return vatFactorsByRate[vatRate];
        }

        /// <inheritdoc />
        public async Task<string> GetCheckoutObjectValueAsync(string propertyName, string defaultResult = "")
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync(propertyName);
            if (String.IsNullOrEmpty(result))
            {
                result = await objectsService.FindSystemObjectByDomainNameAsync($"W2{propertyName}");
            }

            return String.IsNullOrEmpty(result) ? defaultResult : result;
        }

        /// <inheritdoc />
        public async Task LinkBasketToUserAsync(ShoppingBasketCmsSettingsModel basketSettings, ulong userId, WiserItemModel shoppingBasket, bool deleteCookieIfBasketIsLinkedToSomeoneElse = true)
        {
            if (userId <= 0)
            {
                return;
            }

            var linkedUsers = await wiserItemsService.GetLinkedItemIdsAsync(wiserItemsService, shoppingBasket.Id, Constants.BasketToUserLinkType, reverse: true, skipPermissionsCheck: true);
            if (linkedUsers.Any())
            {
                if (deleteCookieIfBasketIsLinkedToSomeoneElse && linkedUsers.All(id => id != userId))
                {
                    // Delete the cookie if this basket is linked to a different user.
                    httpContextAccessor.HttpContext?.Response.Cookies.Delete(basketSettings.CookieName);
                }

                return;
            }

            // Connect this basket to the user.
            await wiserItemsService.AddItemLinkAsync(shoppingBasket.Id, userId, Constants.BasketToUserLinkType, skipPermissionsCheck: true);
        }

        /// <inheritdoc />
        public async Task<WiserItemModel> GetCouponAsync(string couponCode)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("couponCode", couponCode);
            var getCouponIdResult = await databaseConnection.GetAsync($@"
SELECT coupon.id
FROM `{WiserTableNames.WiserItem}` AS coupon
JOIN `{WiserTableNames.WiserItemDetail}` AS `code` ON `code`.item_id = coupon.id AND `code`.`key` = 'code' AND `code`.`value` = ?couponCode
WHERE coupon.entity_type = 'coupon'", true);

            if (getCouponIdResult.Rows.Count == 0)
            {
                return null;
            }

            var couponId = getCouponIdResult.Rows[0].Field<ulong>("id");
            return await wiserItemsService.GetItemDetailsAsync(couponId, entityType: Constants.CouponEntityType, skipPermissionsCheck: true);
        }

        /// <inheritdoc />
        public async Task<bool> IsCouponValidAsync(string couponCode, decimal basketTotal)
        {
            var coupon = await GetCouponAsync(couponCode);
            return IsCouponValid(coupon, basketTotal);
        }

        /// <inheritdoc />
        public bool IsCouponValid(WiserItemModel coupon, decimal basketTotal)
        {
            if (coupon.Id == 0)
            {
                return false;
            }

            var httpContext = httpContextAccessor.HttpContext;

            // Validate date range.
            var validFrom = coupon.ContainsDetail(CouponConstants.ValidFromKey) ? DateTime.ParseExact(coupon.GetDetailValue(CouponConstants.ValidFromKey), "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.MinValue;
            var validUntil = coupon.ContainsDetail(CouponConstants.ValidUntilKey) ? DateTime.ParseExact(coupon.GetDetailValue(CouponConstants.ValidUntilKey), "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.MaxValue;
            if (validFrom > DateTime.Now || validUntil < DateTime.Now)
            {
                return false;
            }

            // Validate usage count.
            var usedCount = coupon.ContainsDetail(CouponConstants.UsedCountKey) ? coupon.GetDetailValue<int>(CouponConstants.UsedCountKey) : 0;
            var maxUseCount = coupon.ContainsDetail(CouponConstants.MaxUseCountKey) ? coupon.GetDetailValue<int>(CouponConstants.MaxUseCountKey) : 0;
            if (maxUseCount > 0 && usedCount >= maxUseCount)
            {
                return false;
            }

            // Validate minimum purchase price.
            var minPurchasePrice = coupon.ContainsDetail(CouponConstants.MinPurchasePriceKey) ? coupon.GetDetailValue<decimal>(CouponConstants.MinPurchasePriceKey) : 0M;
            if (minPurchasePrice > 0 && basketTotal < minPurchasePrice)
            {
                return false;
            }

            // Validate domain.
            var domain = coupon.ContainsDetail(CouponConstants.DomainKey) ? coupon.GetDetailValue(CouponConstants.DomainKey) : "";
            if (domain == "" || httpContext?.Request == null)
            {
                return true;
            }

            var domainList = domain.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().FindAll(it => it != "0" && it != "");
            return domainList.Count == 0 || domainList.Contains(HttpContextHelpers.GetHostName(httpContextAccessor.HttpContext));
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ulong basketItemId)
        {
            await DeleteLinesAsync(basketItemId);
            await wiserItemsService.DeleteAsync(basketItemId, entityType: Constants.BasketEntityType, skipPermissionsCheck: true);
        }

        /// <inheritdoc />
        public async Task DeleteLinesAsync(ulong basketItemId)
        {
            var lines = await wiserItemsService.GetLinkedItemIdsAsync(basketItemId, Constants.BasketLineToBasketLinkType, Constants.BasketLineEntityType);
            foreach (var basketLineItemId in lines)
            {
                await wiserItemsService.DeleteAsync(basketLineItemId, entityType: Constants.BasketLineEntityType, skipPermissionsCheck: true);
            }
        }

        #region Private functions (helper functions)

        private void WriteEncryptedIdToCookie(WiserItemModel shoppingBasket, ShoppingBasketCmsSettingsModel settings)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var expires = DateTimeOffset.Now.AddDays(settings.CookieAgeInDays);
            var encryptedId = EncryptBasketItemId(shoppingBasket.Id);
            HttpContextHelpers.WriteCookie(httpContext, settings.CookieName, encryptedId, expires, isEssential: true);
        }

        private async Task UpdateLineDetailsViaExtraQueryAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string query)
        {
            if (String.IsNullOrWhiteSpace(query))
            {
                return;
            }

            var user = await accountsService.GetUserDataFromCookieAsync();
            var extraReplacements = new Dictionary<string, object>
            {
                { "linktype", 5002 },
                { "Account_MainUserId", user.MainUserId },
                { "Account_UserId", user.UserId },
                { "AccountWiser2_MainUserId", user.MainUserId },
                { "AccountWiser2_UserId", user.UserId }
            };
            query = stringReplacementsService.DoHttpRequestReplacements(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, stringReplacementsService.DoSessionReplacements(stringReplacementsService.DoReplacements(query, extraReplacements, forQuery: true), true), stripNotExistingVariables: false, forQuery: true), true);

            var queryResult = await databaseConnection.GetAsync(query, true);

            if (queryResult.Rows.Count == 0)
            {
                return;
            }

            var containsReadOnlyColumn = queryResult.Columns.Contains("readonly");
            var containsKeyColumn = queryResult.Columns.Contains("key");
            var containsValueColumn = queryResult.Columns.Contains("value");

            foreach (DataRow dataRow in queryResult.Rows)
            {
                var id = dataRow.Field<ulong>("id");
                var line = GetLine(basketLines, id);

                if (line == null)
                {
                    continue;
                }

                if (containsKeyColumn && containsValueColumn)
                {
                    var key = dataRow.Field<string>("key");
                    var value = dataRow.Field<string>("value");

                    line.SetDetail(key, value, readOnly: !containsReadOnlyColumn || Convert.ToBoolean(dataRow["readonly"]), markChangedAsFalse: true);
                }
                else
                {
                    foreach (DataColumn dataColumn in dataRow.Table.Columns)
                    {
                        if (dataColumn.ColumnName.Equals("id"))
                        {
                            continue;
                        }

                        if (dataColumn.ColumnName.EndsWith("_save"))
                        {
                            line.SetDetail(dataColumn.ColumnName[..^5], Convert.ToString(dataRow[dataColumn]), markChangedAsFalse: true);
                        }
                        else
                        {
                            line.SetDetail(dataColumn.ColumnName, Convert.ToString(dataRow[dataColumn]), readOnly: true, markChangedAsFalse: true);
                        }
                    }
                }
            }
        }

        private async Task UpdateMainDetailsViaExtraQueryAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string query)
        {
            if (String.IsNullOrWhiteSpace(query))
            {
                return;
            }

            var user = await accountsService.GetUserDataFromCookieAsync();
            var extraReplacements = new Dictionary<string, object>
            {
                { "Account_MainUserId", user.MainUserId },
                { "Account_UserId", user.UserId },
                { "AccountWiser2_MainUserId", user.MainUserId },
                { "AccountWiser2_UserId", user.UserId }
            };
            query = stringReplacementsService.DoHttpRequestReplacements(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, stringReplacementsService.DoSessionReplacements(stringReplacementsService.DoReplacements(query, extraReplacements, forQuery: true), true), stripNotExistingVariables: false, forQuery: true), true);

            var queryResult = await databaseConnection.GetAsync(query, true);

            if (queryResult.Rows.Count == 0)
            {
                return;
            }

            var containsReadOnlyColumn = queryResult.Columns.Contains("readonly");
            var containsIdColumn = queryResult.Columns.Contains("readonly");
            var containsKeyColumn = queryResult.Columns.Contains("key");
            var containsValueColumn = queryResult.Columns.Contains("value");

            foreach (DataRow dataRow in queryResult.Rows)
            {
                if (containsIdColumn)
                {
                    var id = dataRow.Field<ulong>("id");

                    if (shoppingBasket.Id != id)
                    {
                        continue;
                    }
                }

                if (containsKeyColumn && containsValueColumn)
                {
                    var key = dataRow.Field<string>("key");
                    var value = dataRow.Field<string>("value");

                    shoppingBasket.SetDetail(key, value, readOnly: !containsReadOnlyColumn || Convert.ToBoolean(dataRow["readonly"]), markChangedAsFalse: true);
                }
                else
                {
                    foreach (DataColumn dataColumn in queryResult.Columns)
                    {
                        if (dataColumn.ColumnName.Equals("id"))
                        {
                            continue;
                        }

                        if (dataColumn.ColumnName.EndsWith("_save"))
                        {
                            shoppingBasket.SetDetail(dataColumn.ColumnName.Replace("_save", ""), Convert.ToString(dataRow[dataColumn]), markChangedAsFalse: true);
                        }
                        else
                        {
                            shoppingBasket.SetDetail(dataColumn.ColumnName, Convert.ToString(dataRow[dataColumn]), readOnly: true, markChangedAsFalse: true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="couponCode"></param>
        /// <param name="divideDiscountOverProducts"></param>
        /// <returns></returns>
        private async Task<HandleCouponResultModel> HandleCouponAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string couponCode, bool divideDiscountOverProducts = false)
        {
            var result = new HandleCouponResultModel();
            
            if (String.IsNullOrWhiteSpace(couponCode))
            {
                result.ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode;
                return result;
            }

            var coupon = await GetCouponAsync(couponCode);
            if (coupon == null || coupon.Id == 0)
            {
                result.ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode;
                return result;
            }

            return await HandleCouponAsync(shoppingBasket, basketLines, settings, coupon, divideDiscountOverProducts);
        }

        /// <summary>
        /// Validates and calculates the discount for a given coupon.
        /// </summary>
        /// <param name="shoppingBasket"></param>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="coupon"></param>
        /// <param name="divideDiscountOverProducts"></param>
        /// <returns></returns>
        private async Task<HandleCouponResultModel> HandleCouponAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, WiserItemModel coupon, bool divideDiscountOverProducts = false)
        {
            var result = new HandleCouponResultModel { Coupon = coupon };
            
            if (coupon == null || coupon.Id == 0)
            {
                result.ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode;
                return result;
            }

            var excludedItems = new List<CouponExcludedItemModel>();
            var invalidItemsQuery = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_invalid_items_query");
            if (!String.IsNullOrWhiteSpace(invalidItemsQuery))
            {
                try
                {
                    databaseConnection.AddParameter("couponCode", coupon.GetDetailValue(CouponConstants.Code));
                    databaseConnection.AddParameter("basketId", shoppingBasket.Id);
                    var getExcludedItemsResult = await databaseConnection.GetAsync(invalidItemsQuery);

                    if (getExcludedItemsResult.Columns.Contains("excluded_item_id"))
                    {
                        foreach (var dataRow in getExcludedItemsResult.Rows.Cast<DataRow>())
                        {
                            var excludedItemId = Convert.ToUInt64(dataRow["excluded_item_id"]);
                            var excludedItemName = String.Empty;
                            if (getExcludedItemsResult.Columns.Contains("excluded_item_name"))
                            {
                                excludedItemName = dataRow.Field<string>("excluded_item_name");
                            }

                            excludedItems.Add(new CouponExcludedItemModel { ItemId = excludedItemId, Name = excludedItemName });
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error executing the 'coupon invalid items query'.");
                }
            }

            var discountOnlyOnProducts = coupon.GetDetailValue<bool>(CouponConstants.DiscountOnlyOnProductsKey);

            var totalPrice = 0M;
            if (discountOnlyOnProducts)
            {
                foreach (var line in GetLines(basketLines, "product"))
                {
                    var itemId = line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                    if (excludedItems.Any(item => item.ItemId == itemId))
                    {
                        continue;
                    }

                    totalPrice += await GetLinePriceAsync(shoppingBasket, line, settings, useOriginalPrice: divideDiscountOverProducts);
                }
            }
            else
            {
                foreach (var line in basketLines.Where(l => l.GetDetailValue("type") != Constants.BasketLineCouponType))
                {
                    var itemId = line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                    if (excludedItems.Any(item => item.ItemId == itemId))
                    {
                        continue;
                    }

                    totalPrice += await GetLinePriceAsync(shoppingBasket, line, settings);
                }
            }

            var existingCoupon = GetLines(basketLines, Constants.BasketLineCouponType).FirstOrDefault(l => l.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty) == coupon.Id);

            // Check if the coupon is only valid for certain items.
            var discountOnSpecificItems = false;
            var couponItemLinkType = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_productLinkType", "800");
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("couponId", coupon.Id);
            databaseConnection.AddParameter("couponItemLinkType", couponItemLinkType);
            var linkedProductsQueryResult = await databaseConnection.GetAsync($"SELECT item_id AS linkedItemId FROM `{WiserTableNames.WiserItemLink}` WHERE destination_item_id = ?couponId AND type = ?couponItemLinkType", true);
            if (linkedProductsQueryResult.Rows.Count > 0)
            {
                discountOnSpecificItems = true;
            }

            var isBusinessToBusiness = shoppingBasket.GetDetailValue<bool>("IsB2B");
            if (!IsCouponValid(coupon, totalPrice))
            {
                logger.LogTrace("Coupon is invalid");
                result.ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode;
                return result;
            }

            var maxDiscountIsTotalAmountProducts = (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_maxdiscountistotalamountproducts", "true")).Equals("true", StringComparison.OrdinalIgnoreCase);
            var calculateOverPriceWithoutVat = isBusinessToBusiness || (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_calculateoverpricewithoutvat")).Equals("true", StringComparison.OrdinalIgnoreCase);

            logger.LogTrace("Valid coupon added to shopping basket - maxDiscountIsTotalAmountProducts: {maxDiscountIsTotalAmountProducts} - calculateOverPriceWithoutVat: {calculateOverPriceWithoutVat}", maxDiscountIsTotalAmountProducts, calculateOverPriceWithoutVat);

            var totalProductsPrice = 0M;
            var validForItems = new List<ulong>();

            if (discountOnSpecificItems)
            {
                logger.LogTrace("Coupon only valid for specified Products");

                foreach (DataRow dataRow in linkedProductsQueryResult.Rows)
                {
                    var itemId = dataRow["linkedItemId"].ToString();
                    // Check if coupon linked item is in the basket.
                    foreach (var line in basketLines)
                    {
                        if (line.GetDetailValue(Constants.ConnectedItemIdProperty) != itemId) continue;

                        // Product is linked, add to total product amount used to calculate discount.
                        if (isBusinessToBusiness)
                        {
                            totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, ShoppingBasket.PriceTypes.ExVatExDiscount);
                        }
                        else
                        {
                            totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, calculateOverPriceWithoutVat ? ShoppingBasket.PriceTypes.ExVatExDiscount : ShoppingBasket.PriceTypes.InVatExDiscount);
                        }
                    }
                }
            }
            else if (discountOnlyOnProducts)
            {
                foreach (var line in GetLines(basketLines, "product"))
                {
                    var itemId = line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                    if (excludedItems.Any(item => item.ItemId == itemId)) continue;

                    validForItems.Add(itemId);

                    if (isBusinessToBusiness)
                    {
                        totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, ShoppingBasket.PriceTypes.ExVatExDiscount, useOriginalPrice: true);
                    }
                    else
                    {
                        totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, calculateOverPriceWithoutVat ? ShoppingBasket.PriceTypes.ExVatExDiscount : ShoppingBasket.PriceTypes.InVatExDiscount, useOriginalPrice: true);
                    }
                }
            }
            else
            {
                var productLines = basketLines.Where(l => l.GetDetailValue("type") != Constants.BasketLineCouponType);
                foreach (var line in productLines)
                {
                    var noDiscount = line.GetDetailValue(settings.ItemExcludedFromDiscountPropertyName);
                    if (!String.IsNullOrWhiteSpace(noDiscount) && noDiscount.Equals("1")) continue;

                    var itemId = line.GetDetailValue<ulong>(Constants.ConnectedItemIdProperty);
                    if (excludedItems.Any(item => item.ItemId == itemId)) continue;

                    validForItems.Add(itemId);

                    if (isBusinessToBusiness)
                    {
                        totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, ShoppingBasket.PriceTypes.ExVatExDiscount, useOriginalPrice: true);
                    }
                    else
                    {
                        totalProductsPrice += await GetLinePriceAsync(shoppingBasket, line, settings, calculateOverPriceWithoutVat ? ShoppingBasket.PriceTypes.ExVatExDiscount : ShoppingBasket.PriceTypes.InVatExDiscount, useOriginalPrice: true);
                    }
                }
            }

            var currentDiscountAmount = await GetPriceAsync(shoppingBasket, basketLines, settings, ShoppingBasket.PriceTypes.InVatExDiscount, Constants.BasketLineCouponType, includeDiscountGettingVat: false);
            var discount = CalculateCouponValue(coupon, totalProductsPrice, maxDiscountIsTotalAmountProducts, currentDiscountAmount);

            var useRounding = (await objectsService.FindSystemObjectByDomainNameAsync("BASKET_coupon_use_rounding")).Equals("true", StringComparison.OrdinalIgnoreCase);
            if (useRounding)
            {
                discount = Math.Round(discount, 2, MidpointRounding.AwayFromZero);
            }

            if (existingCoupon != null)
            {
                if (discount * -1 == existingCoupon.GetDetailValue<decimal>("price"))
                {
                    result.ResultCode = ShoppingBasket.HandleCouponResults.CouponAlreadyAdded;
                }
                else
                {
                    result.Valid = true;
                    result.Discount = discount;
                    result.ResultCode = ShoppingBasket.HandleCouponResults.CouponDiscountUpdated;
                    result.OnlyChangePrice = true;
                    result.TotalProductsPrice = totalProductsPrice;
                    result.ValidForItems = validForItems;
                    result.ExcludedItems = excludedItems;
                }

                return result;
            }

            var freePaymentMethodCostsCoupon = coupon.GetDetailValue<bool>(CouponConstants.FreePaymentServiceProviderCostsKey);
            var freeShippingCostsCoupon = coupon.GetDetailValue<bool>(CouponConstants.FreeShippingCostsKey);
            var isBusinessToBusinessCoupon = coupon.GetDetailValue<bool>(CouponConstants.BusinessToBusinessKey);

            if (isBusinessToBusinessCoupon == isBusinessToBusiness && (discount != 0 || freePaymentMethodCostsCoupon || freeShippingCostsCoupon))
            {
                result.Valid = true;
                result.Discount = discount;
                result.ResultCode = ShoppingBasket.HandleCouponResults.CouponAccepted;
                result.TotalProductsPrice = totalProductsPrice;
                result.ValidForItems = validForItems;
                result.ExcludedItems = excludedItems;
            }
            else
            {
                result.ResultCode = ShoppingBasket.HandleCouponResults.InvalidCouponCode;
                result.DoRemove = true;
            }

            return result;
        }


        /// <summary>
        /// Internal function used to add or update a line. This function does not save changes or recalculate values.
        /// </summary>
        /// <param name="basketLines"></param>
        /// <param name="settings"></param>
        /// <param name="uniqueId"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        /// <param name="type"></param>
        /// <param name="lineDetails"></param>
        private static WiserItemModel AddLineInternal(ICollection<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string uniqueId = null, ulong itemId = 0UL, int quantity = 1, string type = "product", IDictionary<string, string> lineDetails = null)
        {
            if (String.IsNullOrEmpty(uniqueId))
            {
                uniqueId = itemId.ToString();
            }

            WiserItemModel line = null;

            // Check for an existing basket line with the same unique id and update the line if found.
            var existingLine = basketLines.FirstOrDefault(basketLine => basketLine.ContainsDetail("uniqueid") && basketLine.GetDetailValue("uniqueid") == uniqueId);
            if (existingLine != null)
            {
                existingLine.SetDetail(settings.QuantityPropertyName, (existingLine.GetDetailValue<int>(settings.QuantityPropertyName) + quantity).ToString());

                if (lineDetails != null)
                {
                    foreach (var (key, value) in lineDetails)
                    {
                        existingLine.SetDetail(key, value);
                    }
                }

                line = existingLine;
            }

            if (line != null)
            {
                return line;
            }

            // No existing line; create a new one.
            line = new WiserItemModel();
            line.SetDetail("uniqueid", uniqueId);
            line.SetDetail(Constants.ConnectedItemIdProperty, itemId.ToString());
            line.SetDetail(settings.QuantityPropertyName, quantity.ToString(CultureInfo.InvariantCulture));
            if (lineDetails != null)
            {
                foreach (var (key, value) in lineDetails.Where(ld => !ld.Key.InList("uniqueid", Constants.ConnectedItemIdProperty, "type")))
                {
                    line.SetDetail(key, value);
                }
            }

            if (String.IsNullOrEmpty(line.GetDetailValue("type")))
            {
                line.SetDetail("type", type);
            }
            
            // Save the price value in the original price property as well.
            if (line.ContainsDetail(settings.PricePropertyName))
            {
                line.SetDetail(Constants.OriginalPricePropertyName, line.GetDetailValue(settings.PricePropertyName));
            }

            basketLines.Add(line);

            return line;
        }

        private void RemovePaymentMethodCostsLine(List<WiserItemModel> basketLines)
        {
            var item = GetLines(basketLines, "paymentmethod_costs").FirstOrDefault();
            if (item == null)
            {
                return;
            }

            basketLines.Remove(item);
            logger.LogTrace("Calculating paymentmethodcosts - Removed rule no products or paymentmethod");
        }

        private void RemoveShippingCostsLine(List<WiserItemModel> basketLines)
        {
            var item = GetLines(basketLines, "shipping_costs").FirstOrDefault();
            if (item == null)
            {
                return;
            }

            basketLines.Remove(item);
        }

        private static WiserItemModel GetLine(IReadOnlyCollection<WiserItemModel> basketLines, ulong id)
        {
            if (basketLines == null || !basketLines.Any())
            {
                return null;
            }

            return basketLines.FirstOrDefault(line => line != null && line.Id == id);
        }

        /// <summary>
        /// Get total quantity of a specific line type (optional).
        /// </summary>
        /// <param name="basketLines"></param>
        /// <param name="lineType">Optional: If you want to get the total of certain types, such as 'product', enter that value here.</param>
        /// <returns>The total quantity.</returns>
        private decimal GetTotalQuantity(List<WiserItemModel> basketLines, string lineType = "")
        {
            return GetLines(basketLines, lineType).Sum(line => line?.GetDetailValue<decimal>("quantity") ?? 0M);
        }

        private async Task<IDictionary<string, string>> GetUserDetailsAsync()
        {
            var user = await accountsService.GetUserDataFromCookieAsync();

            if (user == null || user.UserId == 0)
            {
                return new Dictionary<string, string>(0);
            }

            if (user.MainUserId == user.UserId)
            {
                return (await wiserItemsService.GetItemDetailsAsync(user.MainUserId, entityType: user.EntityType, skipPermissionsCheck: true)).GetSortedList(true);
            }

            var result = (await wiserItemsService.GetItemDetailsAsync(user.MainUserId, entityType: user.EntityType, skipPermissionsCheck: true)).GetSortedList(true);

            (await wiserItemsService.GetItemDetailsAsync(user.UserId, entityType: user.EntityType, skipPermissionsCheck: true)).GetSortedList(true).ToList().ForEach(entry => result[entry.Key] = entry.Value);

            return result;
        }

        /// <summary>
        /// Returns a list of unique VAT rates present in the shopping basket lines.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<int> GetUniqueVatRates(IEnumerable<WiserItemModel> lines, ShoppingBasketCmsSettingsModel settings)
        {
            var vatRates = new List<int>();
            foreach (var line in lines)
            {
                var vatRate = 1;
                if (!String.IsNullOrWhiteSpace(line.GetDetailValue(settings.VatRatePropertyName)))
                {
                    vatRate = Convert.ToInt32(line.GetDetailValue(settings.VatRatePropertyName));
                }

                if (!vatRates.Contains(vatRate))
                {
                    vatRates.Add(vatRate);
                }
            }
            return vatRates;
        }

        /// <summary>
        /// Attempts to parse a string to a <see cref="ShoppingBasket.PriceTypes"/> value. If initial parsing fails, a secondary attempt is done using the legacy price types. If both fail, an exception is thrown.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static ShoppingBasket.PriceTypes ParseStringToPriceType(string input)
        {
            if (Enum.TryParse(input, out ShoppingBasket.PriceTypes enumResult))
            {
                return enumResult;
            }

            if (!Enum.TryParse(input, out ShoppingBasket.LegacyPriceTypes legacyEnumResult))
            {
                throw new Exception($"GCL ShoppingBasket: Unknown price type in variabele: {input}");
            }

            return ConvertLegacyPriceType(legacyEnumResult);
        }

        private static ShoppingBasket.PriceTypes ConvertLegacyPriceType(ShoppingBasket.LegacyPriceTypes legacyPriceType)
        {
            return legacyPriceType switch
            {
                ShoppingBasket.LegacyPriceTypes.In_VAT_In_Discount => ShoppingBasket.PriceTypes.InVatInDiscount,
                ShoppingBasket.LegacyPriceTypes.In_VAT_Ex_Discount => ShoppingBasket.PriceTypes.InVatExDiscount,
                ShoppingBasket.LegacyPriceTypes.Ex_VAT_In_Discount => ShoppingBasket.PriceTypes.ExVatInDiscount,
                ShoppingBasket.LegacyPriceTypes.Ex_VAT_Ex_Discount => ShoppingBasket.PriceTypes.ExVatExDiscount,
                ShoppingBasket.LegacyPriceTypes.VAT => ShoppingBasket.PriceTypes.VatOnly,
                ShoppingBasket.LegacyPriceTypes.Discount_In_VAT => ShoppingBasket.PriceTypes.DiscountInVat,
                ShoppingBasket.LegacyPriceTypes.Discount_Ex_VAT => ShoppingBasket.PriceTypes.DiscountExVat,
                ShoppingBasket.LegacyPriceTypes.PspPrice_In_VAT => ShoppingBasket.PriceTypes.PspPriceInVat,
                ShoppingBasket.LegacyPriceTypes.PspPrice_Ex_Vat => ShoppingBasket.PriceTypes.PspPriceExVat,
                _ => throw new NotSupportedException($"GCL ShoppingBasket: Cannot convert LegacyPriceType '{legacyPriceType:G}'")
            };
        }

        private async Task<string> BuildMethodsHtmlAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string queryObjectName, string templateName)
        {
            var methodsQuery = await objectsService.FindSystemObjectByDomainNameAsync(queryObjectName);
            var output = new StringBuilder();

            if (String.IsNullOrWhiteSpace(methodsQuery))
            {
                return output.ToString();
            }

            var template = (await templatesService.GetTemplateAsync(0, templateName)).Content;
            var queryResult = await databaseConnection.GetAsync(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, methodsQuery, true, forQuery: true), true);

            foreach (DataRow dataRow in queryResult.Rows)
            {
                var html = template;
                html = await stringReplacementsService.DoAllReplacementsAsync(html, dataRow, settings.HandleRequest, settings.EvaluateIfElseInTemplates, settings.RemoveUnknownVariables);
                html = await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, html);
                output.Append(html);
            }

            return output.ToString();
        }

        private async Task<string> GetPaymentMethodsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings)
        {
            return await BuildMethodsHtmlAsync(shoppingBasket, basketLines, settings, "W2CHECKOUT_PaymentMethodsQuery", "betaalmethode");
        }

        private async Task<string> GetDeliveryMethodsAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings)
        {
            return await BuildMethodsHtmlAsync(shoppingBasket, basketLines, settings, "W2CHECKOUT_DeliveryMethodsQuery", "verzendmethode");
        }

        /// <summary>
        /// Executes query and all (basket line) ids returned by query will be removed from the basket.
        /// </summary>
        /// <param name="settings">The shopping basket settings for this basket.</param>
        /// <param name="shoppingBasket">A <see cref="WiserItemModel"/> object that represents the shopping basket.</param>
        /// <param name="basketLines">A list of <see cref="WiserItemModel"/> objects that represent the shopping basket lines.</param>
        /// <param name="query">The query to execute to get the extra details.</param>
        /// <returns></returns>
        private async Task<(List<WiserItemModel> UpdatedBasketLines, string Message)> UpdateLineDetailsViaLineValidityCheckQueryAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string query)
        {
            // Exit if no query given.
            if (String.IsNullOrWhiteSpace(query))
            {
                logger.LogTrace("Empty Query for BasketLineValidityCheckQuery");
                return (basketLines, String.Empty);
            }

            // Replacements in query.
            var user = await accountsService.GetUserDataFromCookieAsync();

            query = stringReplacementsService.DoSessionReplacements(query, true);
            var replacementsData = new Dictionary<string, object>
            {
                { "linktype", 5002 },
                { "Account_MainUserId", user.MainUserId },
                { "Account_UserId", user.UserId },
                { "AccountWiser2_MainUserId", user.MainUserId },
                { "AccountWiser2_UserId", user.UserId }
            };
            query = stringReplacementsService.DoReplacements(query, replacementsData, forQuery: true);
            query = await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, query, stripNotExistingVariables: false, forQuery: true);
            query = stringReplacementsService.DoHttpRequestReplacements(query, true);

            var validityQueryResult = await databaseConnection.GetAsync(query, true);

            // Exit if no result from query.
            if (validityQueryResult.Rows.Count == 0)
            {
                logger.LogTrace("No result for BasketLineValidityCheckQuery");
                return (basketLines, String.Empty);
            }

            // Build list of returned IDs.
            var messages = new List<string>();
            var linesToRemove = new List<string>();
            var hasMessageColumn = validityQueryResult.Columns.Contains("message");
            foreach (DataRow dataRow in validityQueryResult.Rows)
            {
                var lineId = Convert.ToUInt64(dataRow["id"]);

                // Check if there is a line with the same ID as the returned ID.
                if (basketLines.Any(line => line.Id == lineId))
                {
                    logger.LogTrace("Add ID '{lineId}' to remove-list", lineId);
                    linesToRemove.Add(lineId.ToString());

                    if (hasMessageColumn)
                    {
                        var message = dataRow.Field<string>("message");
                        if (!messages.Contains(message))
                        {
                            messages.Add(message);
                        }
                    }
                }
                else
                {
                    logger.LogTrace("ID '{lineId}' does not belong to basket, ignored", lineId);
                }
            }

            if (linesToRemove.Count == 0)
            {
                logger.LogTrace("No IDs found for remove");
                return (basketLines, String.Empty);
            }

            logger.LogTrace("Remove lines with IDs from remove-list");
            var result = await RemoveLinesAsync(shoppingBasket, basketLines, settings, linesToRemove);

            return (result, String.Join("<br />", messages));
        }

        /// <summary>
        /// Executes a query that can manipulate the basket's contents.
        /// </summary>
        /// <param name="settings">The shopping basket settings for this basket.</param>
        /// <param name="shoppingBasket">A <see cref="WiserItemModel"/> object that represents the shopping basket.</param>
        /// <param name="basketLines">A list of <see cref="WiserItemModel"/> objects that represent the shopping basket lines.</param>
        /// <param name="query">The query to execute to get the extra details.</param>
        /// <returns></returns>
        private async Task<string> UpdateLineDetailsViaLineStockActionQuery(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings, string query)
        {
            // Exit if no query given.
            if (String.IsNullOrWhiteSpace(query))
            {
                logger.LogTrace("Empty Query for BasketLineStockActionQuery");
                return String.Empty;
            }

            // Replacements in query.
            var user = await accountsService.GetUserDataFromCookieAsync();

            query = stringReplacementsService.DoSessionReplacements(query, true);
            var replacementsData = new Dictionary<string, object>
            {
                { "linktype", 5002 },
                { "Account_MainUserId", user.MainUserId },
                { "Account_UserId", user.UserId },
                { "AccountWiser2_MainUserId", user.MainUserId },
                { "AccountWiser2_UserId", user.UserId }
            };
            query = stringReplacementsService.DoReplacements(query, replacementsData, forQuery: true);
            query = await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, query, stripNotExistingVariables: false, forQuery: true);
            query = stringReplacementsService.DoHttpRequestReplacements(query, true);

            var stockActionQueryResult = await databaseConnection.GetAsync(query, true);
            return stockActionQueryResult.Rows.Count == 0 ? String.Empty : Convert.ToString(stockActionQueryResult.Rows[0][0]);
        }

        /// <summary>
        /// Will attempt to execute the "add to basket" query. If no query is set, nothing happens.
        /// </summary>
        /// <returns></returns>
        private async Task ExecuteAddToBasketQuery(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, ShoppingBasketCmsSettingsModel settings)
        {
            if (String.IsNullOrWhiteSpace(settings.AddToBasketQuery))
            {
                return;
            }

            var user = await accountsService.GetUserDataFromCookieAsync();
            var extraReplacements = new Dictionary<string, object>
            {
                { "linktype", 5002 },
                { "Account_MainUserId", user.MainUserId },
                { "Account_UserId", user.UserId },
                { "AccountWiser2_MainUserId", user.MainUserId },
                { "AccountWiser2_UserId", user.UserId }
            };
            var query = stringReplacementsService.DoHttpRequestReplacements(await ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, settings, stringReplacementsService.DoSessionReplacements(stringReplacementsService.DoReplacements(settings.AddToBasketQuery, extraReplacements, forQuery: true), true), stripNotExistingVariables: false, forQuery: true), true);
            await databaseConnection.ExecuteAsync(query);
        }

        #endregion
    }
}
