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

        [DefaultValue(@"function setupHttpRequest{contentId}(container, method, extraQueryStringParameters, callBack) {
    callBack = callBack || function() {};
    var url = '/GclComponent.gcl?contentId={contentId}&callMethod=' + method + '&trace=false&ombouw=false&type=ShoppingBasket' + (extraQueryStringParameters || '');

    var xhr = new XMLHttpRequest();
    xhr.open('POST', url);
    xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    xhr.onload = function() {
        if (xhr.status !== 200) {
            alert('Request failed');
        } else {
            var div = document.createElement('div');
            div.innerHTML = xhr.responseText;
            container.innerHTML = document.getElementById('GclShoppingBasketContainer{contentId}').innerHTML;
            initializeBasket{contentId}();
        }
    };

    return xhr;
}

function initializeBasket{contentId}() {
    var container = document.getElementById('GclShoppingBasketContainer{contentId}');
    
    // Handle removing basket lines
    var removeFromBasket = container.querySelectorAll('.removeFromBasket');
    for (i = 0; i < removeFromBasket.length; i++) {
        removeFromBasket[i].addEventListener('click', function(event) {
            event.preventDefault();
            
            var xhr = setupHttpRequest{contentId}(container, 'RemoveFromBasket', '&basketLineId=' + this.getAttribute('data-basketlineid'));
            xhr.send();
        });
    }
 
    // Handle updating basket lines quantity
    var changeQuantity = container.querySelectorAll('.changeQuantity');
    for (i = 0; i < changeQuantity.length; i++) {
        let timeout = null;
        changeQuantity[i].addEventListener('keyup', function(event) {
            // Clear the timeout if it has already been set. This will prevent the previous task from executing if it has been less than <MILLISECONDS>
            clearTimeout(timeout);

            let _this = this;

           // Make a new timeout set to go off in 1000ms (1 second)
           timeout = setTimeout(function () {
                var xhr = setupHttpRequest{contentId}(container, 'ChangeQuantity', '&basketLineId=' + _this.getAttribute('data-basketlineid') + '&newQuantity=' + _this.value);
                xhr.send();
           }, 1000);
        });
    }
}

initializeBasket{contentId}();")]
        internal string TemplateJavaScript { get; }

        [DefaultValue("shoppingbasket")]
        internal string CookieName { get; }

        [DefaultValue(7)]
        internal int CookieAgeInDays { get; }

        [DefaultValue("basket")]
        internal string BasketEntityName { get; }

        [DefaultValue("basketline")]
        internal string BasketLineEntityName { get; }

        //[DefaultValue(5002)]
        //internal int LinkTypeBasketLineToBasket { get; }

        //[DefaultValue(5010)]
        //internal int LinkTypeBasketToUser { get; }

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
