namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class ItineraryModel
    {
        public string Operator { get; set; }
        public string Departure { get; set; }
        public string Arrival { get; set; }
        public string RouteNumber { get; set; }
        public string DateOfTravel { get; set; }
        public long Price { get; set; }
        public string Currency { get; set; }
    }
}