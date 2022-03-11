using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models
{
    internal class ShoppingBasketClearSettingsModel
    {
        [DefaultValue("shoppingbasket")]
        internal string CookieName { get; }

        [DefaultValue(7)]
        internal int CookieAgeInDays { get; }

        [DefaultValue("basket")]
        internal string BasketEntityName { get; }

        [DefaultValue("basketline")]
        internal string BasketLineEntityName { get; }
    }
}
