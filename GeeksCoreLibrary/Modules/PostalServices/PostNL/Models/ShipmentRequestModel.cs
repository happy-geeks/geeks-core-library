using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models;

public class ShipmentRequestModel
{
    public CustomerModel Customer { get; set; }
    public MessageModel Message { get; set; }
    public List<ShipmentModel> Shipments { get; set; }
}