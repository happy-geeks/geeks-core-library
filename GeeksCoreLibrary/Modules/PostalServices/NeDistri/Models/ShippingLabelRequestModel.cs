namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// Model with data send with the request for generating a shipping label for an order
/// </summary>
public class ShippingLabelRequestModel
{
    /// <summary>
    /// Comma seperated list of order ids
    /// </summary>
    public string EncryptedOrderIds { get; set; }
    
    /// <summary>
    /// Type of label. This is the string that will get send as label type to NE Distriservice
    /// </summary>
    public string LabelType { get; set; }
    
    /// <summary>
    /// Colli amount
    /// </summary>
    public int ColliAmount { get; set; }
    
    /// <summary>
    /// User code to use in case a login has multiple users attached to it
    /// </summary>
    public int? UserCode { get; set; }

    /// <summary>
    /// Type of order. This can be a normal shipment or a return shipment from the customer.
    /// </summary>
    public OrderType OrderType { get; set; } = OrderType.Shipment;
}