using EvoPdf;

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
        /// Gets or sets the ID of the e-mail template that should be used for status updates to the consumer for orders that used this PSP.
        /// </summary>
        public ulong StatusUpdateMailTemplateId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the e-mail attachment template that should be used for status updates to the consumer for orders that used this PSP.
        /// </summary>
        public ulong StatusUpdateMailAttachmentTemplateId { get; set; }
        
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
    }
}
