using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.PostalServices.PostNL.Models
{
    public class ResponseShipmentModel
    {
        public string Barcode { get; set; }
        public List<ErrorModel> Errors { get; set; }
        public List<ErrorModel> Warnings { get; set; }
        public List<LabelModel> Labels { get; set; }
        public string ProductCodeDelivery { get; set; }
    }
}