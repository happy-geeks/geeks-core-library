using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models;

/// <summary>
/// A model for an item for the Google Measurement Protocol.
/// </summary>
public class ItemModel
{
    /// <summary>
    /// Gets or sets the ID of the item.
    /// </summary>
    [JsonProperty("item_id")]
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the name of the item.
    /// </summary>
    [JsonProperty("item_name")]
    public string ItemName { get; set; }

    /// <summary>
    /// Gets or sets a product affiliation to designate a supplying company or brick and mortar store location.
    /// </summary>
    [JsonProperty("affiliation")]
    public string Affiliation { get; set; }

    /// <summary>
    /// Gets or sets the coupon associated with the item.
    /// </summary>
    [JsonProperty("coupon")]
    public string Coupon { get; set; }

    /// <summary>
    /// Gets or sets the currency in 3-letter ISO 4217 format.
    /// </summary>
    [JsonProperty("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Gets or sets the monetary discount amount of the item.
    /// </summary>
    [JsonProperty("discount")]
    public double Discount { get; set; }

    /// <summary>
    /// Gets or sets the index of the item in a list.
    /// </summary>
    [JsonProperty("index")]
    public long Index { get; set; }

    /// <summary>
    /// Gets or sets the brand.
    /// </summary>
    [JsonProperty("item_brand")]
    public string ItemBrand { get; set; }

    /// <summary>
    /// Gets or sets the category. First value in the hierarchy.
    /// </summary>
    [JsonProperty("item_category")]
    public string ItemCategory { get; set; }

    /// <summary>
    /// Gets or sets the second category in the hierarchy.
    /// </summary>
    [JsonProperty("item_category2")]
    public string ItemCategoryTwo { get; set; }

    /// <summary>
    /// Gets or sets the third category in the hierarchy.
    /// </summary>
    [JsonProperty("item_category3")]
    public string ItemCategoryThree { get; set; }

    /// <summary>
    /// Gets or sets the fourth category in the hierarchy.
    /// </summary>
    [JsonProperty("item_category4")]
    public string ItemCategoryFour { get; set; }

    /// <summary>
    /// Gets or sets the fifth category in the hierarchy.
    /// </summary>
    [JsonProperty("item_category5")]
    public string ItemCategoryFive { get; set; }

    /// <summary>
    /// Gets or sets the ID of the list the item is in.
    /// </summary>
    [JsonProperty("item_list_id")]
    public string ItemListId { get; set; }

    /// <summary>
    /// Gets or sets the name of the list the item is in.
    /// </summary>
    [JsonProperty("item_list_name")]
    public string ItemListName { get; set; }

    /// <summary>
    /// Gets or sets the variant.
    /// </summary>
    [JsonProperty("item_variant")]
    public string ItemVariant { get; set; }

    /// <summary>
    /// Gets or sets the location associated with the item. It's recommended to use the Google Place ID that corresponds to the associated item. A custom location ID can also be used.
    /// </summary>
    [JsonProperty("location_id")]
    public string LocationId { get; set; }

    /// <summary>
    /// Gets or sets the price, exclusive discount.
    /// </summary>
    [JsonProperty("price")]
    public double Price { get; set; }

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    [JsonProperty("quantity")]
    public long Quantity { get; set; }
}