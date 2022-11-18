using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Models;

namespace GeeksCoreLibrary.Modules.Communication.Interfaces
{
    /// <summary>
    /// A service for doing everything with communications, such as saving settings for periodic communications and sending e-mail and SMS messages.
    /// </summary>
    public interface ICommunicationsService
    {
        /// <summary>
        /// Get the settings of a specific row from wiser_communication. 
        /// </summary>
        /// <param name="id">The ID of the communication settings to get.</param>
        /// <param name="nameOnly">Optional: Whether to only get the name or everything.</param>
        /// <returns>A <see cref="CommunicationSettingsModel"/> with the settings, or <see langword="null"/> if it doesn't exist.</returns>
        Task<CommunicationSettingsModel> GetSettingsAsync(int id, bool nameOnly = false);

        /// <summary>
        /// Get the settings of all communications of a specific type (such as SMS or e-mail).
        /// </summary>
        /// <param name="type">Optional: The <see cref="CommunicationTypes"/> to get the settings for. Leave null to get everything.</param>
        /// <param name="namesOnly">Optional: Whether to only get the names (and IDs) or everything.</param>
        /// <returns>A list of <see cref="CommunicationSettingsModel"/>.</returns>
        Task<List<CommunicationSettingsModel>> GetSettingsAsync(CommunicationTypes? type = null, bool namesOnly = false);

        /// <summary>
        /// Create new settings or updates existing settings (based on <see cref="CommunicationSettingsModel.Id"/>).
        /// </summary>
        /// <param name="settings">The <see cref="CommunicationSettingsModel"/> to create or update.</param>
        /// <param name="username">Optional: The user that did the save. This can be a user from Wiser, if this was updated via Wiser.</param>
        Task<CommunicationSettingsModel> SaveSettingsAsync(CommunicationSettingsModel settings, string username = "GCL");

        /// <summary>
        /// Delete a row of communication settings.
        /// </summary>
        /// <param name="id">The ID of the settings to delete.</param>
        /// <param name="username">Optional: The user that did the save. This can be a user from Wiser, if this was updated via Wiser.</param>
        Task DeleteSettingsAsync(int id, string username = "GCL");

        /// <summary>
        /// Check whether a communication with specified ID (still) exists. 
        /// </summary>
        /// <param name="id">The ID to check.</param>
        /// <returns>A boolean, indicating whether it exists or not.</returns>
        Task<bool> CommunicationExistsAsync(int id);
        
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

        /// <summary>
        /// Send a message using WhatsApp to someone.
        /// </summary>
        /// <param name="receiver">The phone number(s) that should receive this SMS. You can add multiple receivers by separating them with a semicolon.</param>
        /// <param name="body">The body of the SMS.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this SMS should get sent. Leave null to send it right away.</param>
        Task SendWhatsAppAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null);

        /// <summary>
        /// Send a message using WhatsApp to someone.
        /// </summary>
        /// <param name="receivers">The phone number(s) that should receive this SMS.</param>
        /// <param name="body">The body of the SMS.</param>
        /// <param name="sender">Optional: The sender of the message. Leave empty or <see langword="null"/> to use the default sender. Default is <see langowrd="null" />.</param>
        /// <param name="senderName">Optional: The sender name of the message. Leave empty or <see langword="null"/> to use the default sender name. Default is <see langowrd="null" />.</param>
        /// <param name="sendDate">Optional: The date and time that this SMS should get sent. Leave null to send it right away.</param>
        Task SendWhatsAppAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null);

        /// <summary>
        /// Send a message using WhatsApp to someone.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the SMS.</param>
        Task SendWhatsAppAsync(SingleCommunicationModel communication);

        /// <summary>
        /// Uses an WhatsApp provider to send message directly.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smsSettings">The sms settings to use.</param>
        /// <returns></returns>
        Task SendWhatsAppDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings);
    }
}
