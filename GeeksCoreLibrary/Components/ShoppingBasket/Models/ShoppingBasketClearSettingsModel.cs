using System.ComponentModel;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models;

internal class ShoppingBasketClearSettingsModel
{
    [DefaultValue(Constants.DefaultCookieName)]
    internal string CookieName { get; }

    [DefaultValue(Constants.DefaultCookieAgeInDays)]
    internal int CookieAgeInDays { get; }
}