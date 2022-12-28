using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.ShoppingBasket.Models;

public class HandleCouponResultModel
{
    public bool Valid { get; set; }

    public decimal Discount { get; set; }

    public ShoppingBasket.HandleCouponResults ResultCode { get; set; }

    public WiserItemModel Coupon { get; set; }

    public bool OnlyChangePrice { get; set; }

    public bool DoRemove { get; set; }

    /// <summary>
    /// The total price that the discount was calculated over.
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