using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    public class ShoppingBasketAddItemSettingsModel
    {
        [DefaultValue("shoppingbasket")]
        public string CookieName { get; }

        [DefaultValue(7)]
        public int CookieAgeInDays { get; }

        [DefaultValue("basket")]
        public string BasketEntityName { get; }

        [DefaultValue("basketline")]
        public string BasketLineEntityName { get; }

        [DefaultValue("quantity")]
        public string QuantityPropertyName { get; }
        
        [DefaultValue("factor")]
        public string FactorPropertyName { get; }
        
        [DefaultValue("price")]
        public string PricePropertyName { get; }
        
        [DefaultValue("includesvat")]
        public string IncludesVatPropertyName { get; }
        
        [DefaultValue("vatrate")]
        public string VatRatePropertyName { get; }
        
        [DefaultValue("discount")]
        public string DiscountPropertyName { get; }

        [DefaultValue(100)]
        internal decimal MaxItemQuantity { get; }
    }
}
