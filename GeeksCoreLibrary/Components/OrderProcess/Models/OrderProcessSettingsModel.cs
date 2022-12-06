using EvoPdf;
using GeeksCoreLibrary.Components.OrderProcess.Enums;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// A model for an order/checkout process.
    /// </summary>
    public class OrderProcessSettingsModel : OrderProcessBaseModel
    {
        /// <summary>
        /// Gets or sets the URL for this order process.
        /// </summary>
        public string FixedUrl { get; set; }

        /// <summary>
        /// Gets or sets the amount of steps this order process has.
        /// </summary>
        public int AmountOfSteps { get; set; }

        /// <summary>
        /// Gets or sets the property / field that contains the e-mail address of the user in their account and/or order.
        /// </summary>
        public string EmailAddressProperty { get; set; }

        /// <summary>
        /// Gets or sets the property / field that contains the e-mail address of the merchant in the basket/order.
        /// </summary>
        public string MerchantEmailAddressProperty { get; set; }

        /// <summary>
        /// Gets or sets the ID of the e-mail template that should be used for status updates to the consumer for orders that used this PSP.
        /// </summary>
        public ulong StatusUpdateMailTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the e-mail attachment template that should be used for status updates to the consumer for orders that used this PSP.
        /// </summary>
        public ulong StatusUpdateInvoiceTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the e-mail template that should be used for status updates to the web shop for orders that used this PSP.
        /// </summary>
        public ulong StatusUpdateMailWebShopTemplateId { get; set; }

        /// <summary>
        /// Gets or sets whether the basket of the user should be cleared on the confirmation page.
        /// </summary>
        public bool ClearBasketOnConfirmationPage { get; set; }

        /// <summary>
        /// Gets or sets the header HTML.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the footer HTML.
        /// </summary>
        public string Footer { get; set; }

        /// <summary>
        /// Gets or sets the main template HTML.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Gets or sets the method for creating a concept order from a basket.
        /// </summary>
        public OrderProcessBasketToConceptOrderMethods BasketToConceptOrderMethod { get; set; } = OrderProcessBasketToConceptOrderMethods.CreateCopy;

        /// <summary>
        /// Gets or sets if the measurement protocol is active during checkout.
        /// </summary>
        public bool MeasurementProtocolActive { get; set; }

        /// <summary>
        /// Gets or sets the Json template that needs to be used for each item of the order.
        /// </summary>
        public string MeasurementProtocolItemJson { get; set; }

        /// <summary>
        /// Gets or sets the Json template for the 'begin_checkout' event.
        /// </summary>
        public string MeasurementProtocolBeginCheckoutJson { get; set; }

        /// <summary>
        /// Gets or sets the Json template for the 'add_payment_info' event.
        /// </summary>
        public string MeasurementProtocolAddPaymentInfoJson { get; set; }

        /// <summary>
        /// Gets or sets the Json template for the 'purchase' event.
        /// </summary>
        public string MeasurementProtocolPurchaseJson { get; set; }

        /// <summary>
        /// Gets or sets the measurement ID used in the request.
        /// </summary>
        public string MeasurementProtocolMeasurementId { get; set; }

        /// <summary>
        /// Gets or sets the API secret used in the request.
        /// </summary>
        public string MeasurementProtocolApiSecret { get; set; }
    }
}
