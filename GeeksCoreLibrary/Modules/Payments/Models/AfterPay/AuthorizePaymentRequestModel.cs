namespace GeeksCoreLibrary.Modules.Payments.Models.AfterPay
{
    public class AuthorizePaymentRequestModel
    {
        /// <summary>
        /// Unique identifier of checkout process in UUID format. Required only in the Two-Step Authorize use-case.
        /// </summary>
        public string CheckoutId { get; set; }
        public PaymentModel Payment { get; set; }
        public CustomerModel Customer { get; set; }
        public CustomerModel DeliveryCustomer { get; set; }
        public OrderModel Order { get; set; }
        public string ParentTransactionReference { get; set; }
        public AdditionalDataModel AdditionalData { get; set; }
        public string YourReference { get; set; }
        public string OurReference { get; set; }
        public EinvoiceInformationModel EinvoiceInformation { get; set; }
        public string Nonce { get; set; }
    }
}
