namespace GeeksCoreLibrary.Modules.PostalServices.NeDistri.Models;

/// <summary>
/// Model with data send with the request for generating a shipping label for an order
/// </summary>
public class ShippingLabelRequestModel
{
    /// <summary>
    /// 
    /// </summary>
    public string EncryptedOrderIds { get; set; }
    
    public string LabelType { get; set; }
    
    public int ColliAmount { get; set; }
    
    public int? UserCode { get; set; }

    public OrderType OrderType { get; set; } = OrderType.Shipment;
}