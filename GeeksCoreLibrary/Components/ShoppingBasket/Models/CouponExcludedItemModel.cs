namespace GeeksCoreLibrary.Components.ShoppingBasket.Models;

/// <summary>
/// Represents an item in the shopping basket that is excluded from a coupon discount calculation.
/// </summary>
public class CouponExcludedItemModel
{
    /// <summary>
    /// The item ID of the basket item.
    /// </summary>
    public ulong ItemId { get; set; }

    /// <summary>
    /// The name or description of the basket item that is excluded from the discount calculation.
    /// </summary>
    public string Name { get; set; }
}