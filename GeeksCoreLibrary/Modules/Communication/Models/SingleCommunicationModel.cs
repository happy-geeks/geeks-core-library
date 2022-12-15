using System;
using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Models
{
    public class SingleCommunicationModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the corresponding communication row from "wiser_communication", if applicable.
        /// </summary>
        public int CommunicationId { get; set; }

        /// <summary>
        /// Gets or sets the communication type.
        /// See <see cref="CommunicationTypes"/> for all possible values.
        /// </summary>
        public CommunicationTypes Type { get; set; }

        /// <summary>
        /// Gets or sets the receivers of the communication.
        /// E.g. an e-mailadres for e-mail message or a phone number for an SMS message.
        /// </summary>
        public IEnumerable<CommunicationReceiverModel> Receivers { get; set; }

        /// <summary>
        /// Gets or sets the CC for the communication.
        /// Only used for e-mail messages.
        /// </summary>
        public IEnumerable<string> Cc { get; set; }

        /// <summary>
        /// Gets or sets the BCC for the communication.
        /// Only used for e-mail messages.
        /// </summary>
        public IEnumerable<string> Bcc { get; set; }

        /// <summary>
        /// Gets or sets the reply to address. This is the address that the message will be sent to if someone hits reply on the message they received.
        /// Only used for e-mail messages.
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the reply to name. This is the display name of the address that the message will be sent to if someone hits reply on the message they received.
        /// Only used for e-mail messages.
        /// </summary>
        public string ReplyToName { get; set; }

        /// <summary>
        /// Gets or sets the sender of the message. Leave empty or <see langword="null"/> to use the default sender.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the sender name of the message. Leave empty or <see langword="null"/> to use the default sender name.
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Gets or sets the subject of the communication.
        /// Not used for SMS messages.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// E.g. the body of an e-mail message, or the content of an SMS message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the file to be sent with the communication.
        /// </summary>
        public byte[] UploadedFile { get; set; }

        /// <summary>
        /// Gets or sets the name of the file to be sent with the communication.
        /// </summary>
        public string UploadedFileName { get; set; }

        /// <summary>
        /// Gets or sets a list of attachments for the communication.
        /// </summary>
        public List<string> AttachmentUrls { get; set; }

        /// <summary>
        /// Gets or sets the wiser item files. One or more IDs from wiser_itemfile that should be sent with the communication as attachments. Only works for e-mail communications.
        /// </summary>
        public List<ulong> WiserItemFiles { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this communication was created.
        /// </summary>
        public DateTime? CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the date and time that this communication should get sent to the receiver.
        /// This can be used to setup messages to be sent in the future.
        /// </summary>
        public DateTime? SendDate { get; set; }

        /// <summary>
        /// Gets or sets the date and time this communication has been processed by the WTS.
        /// This is the time that the message was actually sent.
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the status code.
        /// If the value is "Ok", then the message was sent successfully.
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// This is mostly debug information.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets how many times it has been attempted to send this message.
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last attempt.
        /// Is <see langword="null"/> if no attempts have been made yet.
        /// </summary>
        public DateTime? LastAttempt { get; set; }
    }
}
