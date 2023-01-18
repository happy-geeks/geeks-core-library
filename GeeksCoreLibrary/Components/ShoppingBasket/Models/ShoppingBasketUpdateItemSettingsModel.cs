using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    internal class ShoppingBasketUpdateItemSettingsModel
    {
        [DefaultValue(Constants.DefaultCookieName)]
        internal string CookieName { get; }

        [DefaultValue(Constants.DefaultCookieAgeInDays)]
        internal int CookieAgeInDays { get; }

        [DefaultValue(Constants.DefaultQuantityPropertyName)]
        internal string QuantityPropertyName { get; }
        
        [DefaultValue(Constants.DefaultFactorPropertyName)]
        internal string FactorPropertyName { get; }
        
        [DefaultValue(Constants.DefaultPricePropertyName)]
        internal string PricePropertyName { get; }
        
        [DefaultValue(Constants.DefaultIncludesVatPropertyName)]
        internal string IncludesVatPropertyName { get; }
        
        [DefaultValue(Constants.DefaultVatRatePropertyName)]
        internal string VatRatePropertyName { get; }
        
        [DefaultValue(Constants.DefaultDiscountPropertyName)]
        internal string DiscountPropertyName { get; }
        
        [DefaultValue(Constants.DefaultItemExcludedFromDiscountPropertyName)]
        internal string ItemExcludedFromDiscountPropertyName { get; }

        [DefaultValue(Constants.DefaultMaxItemQuantity)]
        internal decimal MaxItemQuantity { get; }
    }
}
