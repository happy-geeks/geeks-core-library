using GeeksCoreLibrary.Modules.Communication.Models;

namespace GeeksCoreLibrary.Modules.Payments.Models
{
    public class EmailValues
    {
        public string Content { get; set; }

        public string Subject { get; set; }

        public CommunicationReceiverModel Sender { get; set; }

        public CommunicationReceiverModel Merchant { get; set; }

        public CommunicationReceiverModel User { get; set; }

        public string Bcc { get; set; }

        public CommunicationReceiverModel ReplyTo { get; set; }
    }
}
