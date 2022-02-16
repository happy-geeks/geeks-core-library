using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models
{
    public class CustomsModel
    {
        public List<CustomsContentModel> Content { get; set; }
        public string Currency { get; set; }
        public string HandleAsNonDeliverable { get; set; }
        public string Invoice { get; set; }
        public string InvoiceNumber { get; set; }
        public string ShipmentType { get; set; }
    }
}