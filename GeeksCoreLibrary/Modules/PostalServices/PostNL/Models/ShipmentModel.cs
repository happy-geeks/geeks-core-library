using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class ShipmentModel
{
    public List<AddressModel> Addresses { get; set; }
    public DimensionModel Dimension { get; set; }
    public string ProductCodeDelivery { get; set; }
    public List<ContactModel> Contacts { get; set; }
    public string Barcode { get; set; }
    public string Remark { get; set; }
    public CustomsModel Customs { get; set; }
}