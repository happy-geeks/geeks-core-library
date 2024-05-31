namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class ShippingLabelRequestModel
{
    public string EncryptedOrderIds { get; set; }
    public ParcelType ParcelType { get; set; } = ParcelType.Standard;
}