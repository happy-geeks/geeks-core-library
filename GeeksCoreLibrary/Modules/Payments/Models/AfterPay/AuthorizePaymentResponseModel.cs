using System;
using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{

    public class AuthorizePaymentResponseModel
    {
        public string Outcome { get; set; }
        public ResponseCustomerModel Customer { get; set; }
        public ResponseCustomerModel DeliveryCustomer { get; set; }
        public string SecureLoginUrl { get; set; }
        public List<RiskCheckMessageModel> RiskCheckMessages { get; set; }
        public Guid ReservationId { get; set; }
        public Guid CheckoutId { get; set; }
        public string ExpirationDate { get; set; }
    }
}
