using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models;

public class HandleCouponResultModel
{
    /// <summary>
    /// Gets or sets whether the coupon is valid.
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Gets or sets the discount of the coupon.
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// Gets or sets the validation result code.
    /// </summary>
    public ShoppingBasket.HandleCouponResults ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WiserItemModel"/> object representing the coupon that was handled.
    /// </summary>
    public WiserItemModel Coupon { get; set; }

    /// <summary>
    /// Gets or sets whether only the price should be updated of the coupon, in case the coupon was already added
    /// but the discount is different.
    /// </summary>
    public bool OnlyChangePrice { get; set; }

    /// <summary>
    /// Gets or sets whether the coupon should be removed from the basket.
    /// </summary>
    public bool DoRemove { get; set; }

    /// <summary>
    /// Gets or sets the total price that the discount was calculated over.
    /// </summary>
    public decimal TotalProductsPrice { get; set; }

    /// <summary>
    /// Gets or sets a list of item IDs for which this coupon is valid. In most cases this will be all products.
    /// </summary>
    public List<ulong> ValidForItems { get; set; }

    /// <summary>
    /// List of items that were excluded from the discount. The Key is the ID of the item that was excluded, and
    /// the Value is the name/description of the item.
    /// </summary>
    public List<CouponExcludedItemModel> ExcludedItems { get; set; }
}