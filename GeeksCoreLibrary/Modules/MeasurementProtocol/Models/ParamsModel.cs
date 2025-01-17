using System.Collections.Generic;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.MeasurementProtocol.Models;

/// <summary>
/// A model for the parameters of a Google Measurement Protocol event.
/// </summary>
public class ParamsModel
{
    /// <summary>
    /// The currency in 3-letter ISO 4217 format.
    /// </summary>
    [JsonProperty("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Gets or sets the unique ID for the transaction.
    /// </summary>
    [JsonProperty("transaction_id")]
    public string TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the total price, inclusive discount.
    /// </summary>
    [JsonProperty("value")]
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets an affiliation to designate a supplying company or brick and mortar store location.
    /// </summary>
    [JsonProperty("affiliation")]
    public string Affiliation { get; set; }

    /// <summary>
    /// Gets or sets the coupons associated with the event.
    /// </summary>
    [JsonProperty("coupon")]
    public string Coupon { get; set; }

    /// <summary>
    /// Gets or sets the costs for shipping.
    /// </summary>
    [JsonProperty("shipping")]
    public decimal Shipping { get; set; }

    /// <summary>
    /// Gets or sets the tax amount.
    /// </summary>
    [JsonProperty("tax")]
    public decimal Tax { get; set; }

    /// <summary>
    /// Gets or sets the chosen payment method.
    /// </summary>
    [JsonProperty("payment_type")]
    public string PaymentType { get; set; }

    /// <summary>
    /// Gets or sets the items of the event.
    /// </summary>
    [JsonProperty("items")]
    public List<ItemModel> Items { get; set; }
}