namespace GeeksCoreLibrary.Modules.Communication.Models
{
    public class CommunicationReceiverModel
    {
        /// <summary>
        /// Gets or sets the address of the receiver.
        /// This could be a phone number (for SMS/WhatsApp) or an e-mail address (for e-mail).
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the display name of the receiver. This is optional. If no value is entered, the Address will be used as the display name.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
