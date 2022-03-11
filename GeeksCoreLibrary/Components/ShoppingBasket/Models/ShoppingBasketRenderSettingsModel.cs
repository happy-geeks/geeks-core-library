using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    internal class ShoppingBasketRenderSettingsModel
    {
        [DefaultValue(@"<!-- There must always be an element with ID GclShoppingBasketContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id=""GclShoppingBasketContainer{contentId}"">
    <h2>Mijn winkelwagen</h2>
    {repeat:lines}
        <div class=""basketRow"">
            Id: {id}<br />
            Prijs: {price~In_VAT_In_Discount}
        </div>    
    {/repeat:lines}
    <div>Totaal: {price~all~In_VAT_In_Discount}</div>
</div>")]
        internal string Template { get; }

        [DefaultValue(@"<!-- There must always be an element with ID GclShoppingBasketContainer{contentId}, all fields within are sent to the server via ajax, unless you also overwrite the TemplateJavascript. -->
<div id=""GclShoppingBasketContainer{contentId}"">
    <h2>Mijn winkelwagen</h2>
    {repeat:lines}
        <div class=""basketRow"">
            Id: {id}<br />
            Prijs: {price~In_VAT_In_Discount}
        </div>    
    {/repeat:lines}
    <div>Totaal: {price~all~In_VAT_In_Discount}</div>
</div>")]
        internal string TemplatePrint { get; }

        [DefaultValue(@"function setupHttpRequest{contentId}_{basketId}(container, method, contentType, extraQueryStringParameters, callBack) {
    callBack = callBack || function() {};
    var url = '/GclComponent.gcl?contentId={contentId}&callMethod=' + method + '&trace=false&ombouw=false&type=ShoppingBasket' + (extraQueryStringParameters || '');

    var xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', contentType || 'application/x-www-form-urlencoded');
    xhr.setRequestHeader('X-CSRF-TOKEN', document.getElementById('RequestVerificationToken').value);
    xhr.onload = function() {
        if (xhr.status !== 200) {
            alert('Request failed');
        } else {
            const div = document.createElement('div');
            div.innerHTML = xhr.responseText;

            let divContainer = div.querySelector('#GclShoppingBasketContainer{contentId}');
            if (!divContainer) {
                divContainer = div.querySelector('#GclShoppingBasketContainer{contentId}_{basketId}');
            }

            container.innerHTML = divContainer.innerHTML;
            initializeBasket{contentId}_{basketId}();
        }
    };

    return xhr;
}

function initializeBasket{contentId}_{basketId}() {
    let container = document.getElementById('GclShoppingBasketContainer{contentId}');
    if (!container) {
        container = document.getElementById('GclShoppingBasketContainer{contentId}_{basketId}');
    }

    // Handle removing basket lines
    const removeFromBasket = container.querySelectorAll('.removeFromBasket');
    for (i = 0; i < removeFromBasket.length; i++) {
        removeFromBasket[i].addEventListener('click', function(event) {
            event.preventDefault();

            const payload = [this.dataset.basketLineUniqueId];

            var xhr = setupHttpRequest{contentId}_{basketId}(container, 'RemoveFromBasket', 'application/json', '&componentMode=4');
            xhr.send(JSON.stringify(payload));
        });
    }
 
    // Handle updating basket lines quantity
    const changeQuantity = container.querySelectorAll('.changeQuantity');
    for (i = 0; i < changeQuantity.length; i++) {
        let timeout = null;
        changeQuantity[i].addEventListener('keyup', function(event) {
            // Clear the timeout if it has already been set. This will prevent the previous task from executing if it has been less than <MILLISECONDS>
            clearTimeout(timeout);

            const payload = JSON.stringify([{
                id: this.dataset.basketLineUniqueId,
                quantity: this.value
            }]);

            // Make a new timeout set to go off in 1000ms (1 second)
            timeout = setTimeout(function () {
                var xhr = setupHttpRequest{contentId}_{basketId}(container, 'ChangeQuantity', 'application/json', '&componentMode=3');
                xhr.send(payload);
            }, 1000);
        });
    }
}

initializeBasket{contentId}_{basketId}();")]
        internal string TemplateJavaScript { get; }

        [DefaultValue("shoppingbasket")]
        internal string CookieName { get; }

        [DefaultValue(7)]
        internal int CookieAgeInDays { get; }

        [DefaultValue("basket")]
        internal string BasketEntityName { get; }

        [DefaultValue("basketline")]
        internal string BasketLineEntityName { get; }

        [DefaultValue("quantity")]
        internal string QuantityPropertyName { get; }

        [DefaultValue("factor")]
        internal string FactorPropertyName { get; }

        [DefaultValue("price")]
        internal string PricePropertyName { get; }

        [DefaultValue("includesvat")]
        internal string IncludesVatPropertyName { get; }

        [DefaultValue("vatrate")]
        internal string VatRatePropertyName { get; }

        [DefaultValue("discount")]
        internal string DiscountPropertyName { get; }

        [DefaultValue(100)]
        internal decimal MaxItemQuantity { get; }
    }
}
