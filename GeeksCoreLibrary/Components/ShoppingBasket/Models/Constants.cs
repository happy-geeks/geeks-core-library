namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public static class Constants
    {
        public const string ConnectedItemIdProperty = "connecteditemid";
        public const string CouponEntityType = "coupon";
        public const string BasketLineCouponType = "coupon";
        public const string BasketEntityType = "basket";
        public const string BasketLineEntityType = "basketline";
        /// <summary>
        /// The property name of a basket line which contains the original price (price without any discounts).
        /// </summary>
        public const string OriginalPricePropertyName = "original_price";
        /// <summary>
        /// The property name of a coupon basket line that determines if its discount was divided over all products.
        /// </summary>
        public const string CouponDividedOverProductsPropertyName = "divided_over_products";
        /// <summary>
        /// The discount a product received from a coupon. The code of the coupon will be appended to this property name.
        /// </summary>
        public const string ProductCouponDiscountPropertyNamePrefix = "coupon_discount_";
        public const int BasketToUserLinkType = 5010;
        public const int BasketLineToBasketLinkType = 5002;
        public const int ProductToOrderLineLinkType = 5030;

        #region Default settings

        internal const string DefaultCookieName = "shoppingBasket";
        internal const int DefaultCookieAgeInDays = 7;
        internal const string DefaultQuantityPropertyName = "quantity";
        internal const string DefaultFactorPropertyName = "factor";
        internal const string DefaultPricePropertyName = "price";
        internal const string DefaultIncludesVatPropertyName = "includesvat";
        internal const string DefaultVatRatePropertyName = "vatrate";
        internal const string DefaultDiscountPropertyName = "discount";
        internal const string DefaultItemExcludedFromDiscountPropertyName = "no_discount";
        internal const int DefaultMaxItemQuantity = 100;

        internal const string DefaultTemplate = @"<!-- There must always be an element with ID GclShoppingBasketContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id=""GclShoppingBasketContainer{contentId}"">
    <h2>Mijn winkelwagen</h2>
    {repeat:lines}
        <div class=""basketRow"">
            Id: {id}<br />
            Prijs: {price~In_VAT_In_Discount}
        </div>    
    {/repeat:lines}
    <div>Totaal: {price~all~In_VAT_In_Discount}</div>
</div>";

        internal const string DefaultTemplatePrint = @"<!-- There must always be an element with ID GclShoppingBasketContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id=""GclShoppingBasketContainer{contentId}"">
    <h2>Mijn winkelwagen</h2>
    {repeat:lines}
        <div class=""basketRow"">
            Id: {id}<br />
            Prijs: {price~In_VAT_In_Discount}
        </div>    
    {/repeat:lines}
    <div>Totaal: {price~all~In_VAT_In_Discount}</div>
</div>";

        internal const string DefaultTemplateJavaScript = @"function setupHttpRequest{contentId}_{basketId}(container, method, contentType, extraQueryStringParameters) {
    const url = '/GclComponent.gcl?contentId={contentId}&callMethod=' + method + '&ombouw=false&type=ShoppingBasket' + (extraQueryStringParameters || '');

    const xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', contentType || 'application/x-www-form-urlencoded');
    xhr.setRequestHeader('X-CSRF-TOKEN', document.getElementById('RequestVerificationToken').value);
    xhr.addEventListener('load', function() {
        if (xhr.status !== 200) {
            alert('Request failed');
        } else {
            const div = document.createElement('div');
            div.innerHTML = xhr.responseText;

            let divContainer = div.querySelector('#GclShoppingBasketContainer{contentId}_{basketId}');
            if (!divContainer) {
                divContainer = div.querySelector('#GclShoppingBasketContainer{contentId}');
            }

            container.innerHTML = divContainer.innerHTML;
            initializeBasket{contentId}_{basketId}();
        }
    });

    return xhr;
}

function initializeBasket{contentId}_{basketId}() {
    let container = document.getElementById('GclShoppingBasketContainer{contentId}');
    if (!container) {
        container = document.getElementById('GclShoppingBasketContainer{contentId}_{basketId}');
    }

    // Handle removing basket lines
    const removeFromBasketButtons = container.querySelectorAll('.removeFromBasket');
    removeFromBasketButtons.forEach(removeFromBasketButton => {
        removeFromBasketButton.addEventListener('click', function (event) {
            event.preventDefault();

            const payload = [this.dataset.basketLineUniqueId];

            const xhr = setupHttpRequest{contentId}_{basketId}(container, 'HandleRemoveItemsMode', 'application/json', '&componentMode=4');
            xhr.send(JSON.stringify(payload));
        });
    });

    // Handle updating basket lines quantity
    const changeQuantityInputs = container.querySelectorAll('.changeQuantity');
    changeQuantityInputs.forEach(changeQuantityInput => {
        let timeout = null;
        changeQuantityInput.addEventListener('keyup', function (event) {
            // Clear the timeout if it has already been set. This will prevent the previous task from executing if it has been less than <MILLISECONDS>
            clearTimeout(timeout);

            const payload = JSON.stringify([{
                id: this.dataset.basketLineUniqueId,
                quantity: this.value
            }]);

            // Make a new timeout set to go off in 1000ms (1 second)
            timeout = setTimeout(function () {
                const xhr = setupHttpRequest{contentId}_{basketId}(container, 'HandleChangeQuantityMode', 'application/json', '&componentMode=3');
                xhr.send(payload);
            }, 1000);
        });
    });

    // Handle add coupon button.
    const addCouponButtons = container.querySelectorAll('.gclAddCoupon');
    addCouponButtons.forEach(addCouponButton => {
        addCouponButton.addEventListener('click', function (event) {
            const couponCodeInput = container.querySelector('.gclCouponCode');
            if (!couponCodeInput) return;

            const couponCode = couponCodeInput.value.trim();
            if (couponCode === '') return;

            const payload = `couponcode=${couponCode}`;

            const xhr = setupHttpRequest{contentId}_{basketId}(container, 'HandleAddCouponMode', 'application/x-www-form-urlencoded', '&componentMode=12');
            xhr.send(payload);
        });
    });
}

initializeBasket{contentId}_{basketId}();";

        #endregion
    }
}
