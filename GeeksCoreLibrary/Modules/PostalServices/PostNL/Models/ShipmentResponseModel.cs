using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models
{
    public class ShipmentResponseModel
    {
        public List<object> MergedLabels { get; set; }
        public List<ResponseShipmentModel> ResponseShipments { get; set; }
    }
}