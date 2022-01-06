using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class RentalModel
    {
        public string Company { get; set; }
        public AddressModel PickupLocation { get; set; }
        public AddressModel DropoffLocation { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<CustomerModel> Drivers { get; set; }
        public long Price { get; set; }
        public string Currency { get; set; }
        public string BookingReference { get; set; }
    }
}