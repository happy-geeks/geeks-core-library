using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class HotelModel
    {
        public string Company { get; set; }
        public AddressModel Address { get; set; }
        public string Checkin { get; set; }
        public string Checkout { get; set; }
        public List<CustomerModel> Guests { get; set; }
        public long NumberOfRooms { get; set; }
        public long Price { get; set; }
        public string Currency { get; set; }
        public string BookingReference { get; set; }
    }
}