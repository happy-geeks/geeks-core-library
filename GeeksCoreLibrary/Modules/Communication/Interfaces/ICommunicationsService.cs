using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Models;

namespace GeeksCoreLibrary.Modules.Communication.Interfaces
{
    public interface ICommunicationsService
    {
        /// <summary>
        /// Send an e-mail to someone.
        /// </summary>
        /// <param name="receiver">The e-mail address(es) that should receive this e-mail. You can add multiple receivers by separating them with a semicolon.</param>
        /// <param name="subject">The subject of the e-mail.</param>
        /// <param name="body">The body of the e-mail. This should be HTML.</param>
        /// <param name="receiverName">Optional: The display name(s) of the receivers. You can add multiple receivers by separating them with a semicolon. They should be added in the same order as the e-mail addresses. Leave empty or <see langword="null"/> to not use a display name. Default is <see langowrd="null" />.</param>
        /// <param name="cc">Optional: The CC. Default is <see langowrd="null" />.</param>
        /// <param name="bcc">Optional: The BCC. Default is <see langowrd="null" />.</param>
        /// <param name="replyTo">Optional: This is the address that the message will be sent to if someone hits reply on the message they received. Default is <see langowrd="null" />.</param>
        /// <param name="replyToName">Optional: This is the display name of the address that the message will be sent to if someone hits reply on the message they received. Default is <see langowrd="null" />.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this e-mail should get sent. Leave null to send it right away.</param>
        /// <param name="attachments">Optional: A list of attachments to add to the e-mail. This should be IDs from the table "wiser_itemfile". Default is <see langowrd="null" />.</param>
        Task SendEmailAsync(string receiver, string subject, string body, string receiverName = null, string cc = null, string bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null);

        /// <summary>
        /// Send an e-mail to someone.
        /// </summary>
        /// <param name="receivers">The e-mail address(es) that should receive this e-mail.</param>
        /// <param name="subject">The subject of the e-mail.</param>
        /// <param name="body">The body of the e-mail. This should be HTML.</param>
        /// <param name="cc">Optional: The CC. Default is <see langowrd="null" />.</param>
        /// <param name="bcc">Optional: The BCC. Default is <see langowrd="null" />.</param>
        /// <param name="replyTo">Optional: This is the address that the message will be sent to if someone hits reply on the message they received. Default is <see langowrd="null" />.</param>
        /// <param name="replyToName">Optional: This is the display name of the address that the message will be sent to if someone hits reply on the message they received. Default is <see langowrd="null" />.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this e-mail should get sent. Leave null to send it right away.</param>
        /// <param name="attachments">Optional: A list of attachments to add to the e-mail. This should be IDs from the table "wiser_itemfile". Default is <see langowrd="null" />.</param>
        Task SendEmailAsync(IEnumerable<CommunicationReceiverModel> receivers, string subject, string body, IEnumerable<string> cc, IEnumerable<string> bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null);

        /// <summary>
        /// Send an e-mail to someone.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the e-mail.</param>
        Task<int> SendEmailAsync(SingleCommunicationModel communication);

        /// <summary>
        /// Adds or updates a single communication item to the table. This can be anything we support, like an e-mail or an SMS.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the communication.</param>
        /// <returns>The ID of the communication.</returns>
        Task<int> AddOrUpdateSingleCommunicationAsync(SingleCommunicationModel communication);

        /// <summary>
        /// Uses an SMTP server to send an email directly.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="timeout">The timeout in milliseconds before it's considered to take too long. The default timeout equals to 2 minutes. This is the same default timeout that MailKit uses.</param>
        /// <returns></returns>
        Task SendEmailDirectlyAsync(SingleCommunicationModel communication, int timeout = 120_000);
        
        /// <summary>
        /// Uses an SMTP server to send an email directly.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smtpSettings">The SMTP settings to use.</param>
        /// <param name="timeout">The timeout in milliseconds before it's considered to take too long. The default timeout equals to 2 minutes. This is the same default timeout that MailKit uses.</param>
        /// <returns></returns>
        Task SendEmailDirectlyAsync(SingleCommunicationModel communication, SmtpSettings smtpSettings, int timeout = 120_000);

        /// <summary>
        /// Send an SMS to someone.
        /// </summary>
        /// <param name="receiver">The phone number(s) that should receive this SMS. You can add multiple receivers by separating them with a semicolon.</param>
        /// <param name="body">The body of the SMS.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this SMS should get sent. Leave null to send it right away.</param>
        Task SendSmsAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null);
        
        /// <summary>
        /// Send an SMS to someone.
        /// </summary>
        /// <param name="receivers">The phone number(s) that should receive this SMS.</param>
        /// <param name="body">The body of the SMS.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this SMS should get sent. Leave null to send it right away.</param>
        Task SendSmsAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null);
        
        /// <summary>
        /// Send an SMS to someone.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the SMS.</param>
        Task SendSmsAsync(SingleCommunicationModel communication);
        
        /// <summary>
        /// Uses an SMS provider to send an SMS directly.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smsSettings">The sms settings to use.</param>
        /// <returns></returns>
        Task SendSmsDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings);
    }
}
