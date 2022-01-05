using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class ResponseCustomerModel
    {
        public string CustomerNumber { get; set; }
        public string CustomerAccountId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<AddressModel> AddressList { get; set; }
    }
}