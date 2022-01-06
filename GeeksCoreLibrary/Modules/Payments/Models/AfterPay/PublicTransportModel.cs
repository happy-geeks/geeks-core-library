using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class PublicTransportModel
    {
        public List<PassengerModel> Passengers { get; set; }
        public List<ItineraryModel> Itineraries { get; set; }
        public InsuranceModel Insurance { get; set; }
        public string BookingReference { get; set; }
    }
}