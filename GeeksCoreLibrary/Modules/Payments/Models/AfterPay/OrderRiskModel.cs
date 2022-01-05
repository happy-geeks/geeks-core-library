namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class OrderRiskModel
    {
        public string ChannelType { get; set; }
        public string DeliveryType { get; set; }
        public string TicketDeliveryMethod { get; set; }
        public PublicTransportModel Airline { get; set; }
        public PublicTransportModel Bus { get; set; }
        public PublicTransportModel Train { get; set; }
        public PublicTransportModel Ferry { get; set; }
        public RentalModel Rental { get; set; }
        public HotelModel Hotel { get; set; }
    }
}