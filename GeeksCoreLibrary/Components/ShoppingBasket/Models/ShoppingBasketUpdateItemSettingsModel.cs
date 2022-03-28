using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    internal class ShoppingBasketUpdateItemSettingsModel
    {
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
