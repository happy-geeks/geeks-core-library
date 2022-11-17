using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuckarooSdk.DataTypes.Response.Status;
using CM.Text;
using CM.Text.BusinessMessaging;
using CM.Text.BusinessMessaging.Model;
using CM.Text.BusinessMessaging.Model.MultiChannel;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.Communication.Models.SmtPeter;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using HtmlAgilityPack;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using RestSharp;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Messaging;

namespace GeeksCoreLibrary.Modules.Communication.Services
{
    public class CommunicationsService : ICommunicationsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<CommunicationsService> logger;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseConnection databaseConnection;

        public CommunicationsService(IOptions<GclSettings> gclSettings, ILogger<CommunicationsService> logger, IWiserItemsService wiserItemsService, IDatabaseConnection databaseConnection)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.wiserItemsService = wiserItemsService;
            this.databaseConnection = databaseConnection;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(string receiver, string subject, string body, string receiverName = null, string cc = null, string bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null)
        {
            var receivers = new List<CommunicationReceiverModel>();
            var receiverAddresses = receiver.Split(';');
            var receiverNames = (receiverName ?? "").Split(";");
            for (var i = 0; i < receiverAddresses.Length; i++)
            {
                var receiverModel = new CommunicationReceiverModel { Address = receiverAddresses[i] };
                if (receiverNames.Length > i)
                {
                    receiverModel.DisplayName = receiverNames[i];
                }

                receivers.Add(receiverModel);
            }

            var bccAddresses = new List<string>();
            if (!String.IsNullOrWhiteSpace(bcc))
            {
                bccAddresses.AddRange(bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            var ccAddresses = new List<string>();
            if (!String.IsNullOrWhiteSpace(cc))
            {
                ccAddresses.AddRange(cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            await SendEmailAsync(receivers, subject, body, ccAddresses, bccAddresses, replyTo, replyToName, sender, senderName, sendDate, attachments);
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(IEnumerable<CommunicationReceiverModel> receivers, string subject, string body, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null)
        {
            await SendEmailAsync(new SingleCommunicationModel
            {
                Receivers = receivers,
                Subject = subject,
                Content = body,
                Cc = cc,
                Bcc = bcc,
                ReplyTo = replyTo,
                ReplyToName = replyToName,
                Sender = sender,
                SenderName = senderName,
                SendDate = sendDate,
                WiserItemFiles = attachments
            });
        }

        /// <inheritdoc />
        public async Task<int> SendEmailAsync(SingleCommunicationModel communication)
        {
            // This is done to validate the e-mail address(es). If an e-mail address is not valid, this will throw an exception, which we want so that we don't add invalid communications to the table.
            foreach (var receiverModel in communication.Receivers)
            {
                var mailAddress = new System.Net.Mail.MailAddress(receiverModel.Address, receiverModel.DisplayName);
            }

            if (communication.Cc != null)
            {
                foreach (var cc in communication.Cc)
                {
                    var mailAddress = new System.Net.Mail.MailAddress(cc);
                }
            }

            if (communication.Bcc != null)
            {
                foreach (var bcc in communication.Bcc)
                {
                    var mailAddress = new System.Net.Mail.MailAddress(bcc);
                }
            }

            communication.Id = 0;
            communication.Type = CommunicationTypes.Email;
            return await AddOrUpdateSingleCommunicationAsync(communication);
        }

        /// <inheritdoc />
        public async Task<int> AddOrUpdateSingleCommunicationAsync(SingleCommunicationModel communication)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("communication_id", communication.CommunicationId);
            databaseConnection.AddParameter("receiver", String.Join(";", communication.Receivers.Select(r => r.Address)));
            databaseConnection.AddParameter("receiver_name", String.Join(";", communication.Receivers.Select(r => r.DisplayName)));
            databaseConnection.AddParameter("subject", communication.Subject);
            databaseConnection.AddParameter("content", communication.Content);
            databaseConnection.AddParameter("uploaded_file", communication.UploadedFile);
            databaseConnection.AddParameter("uploaded_filename", communication.UploadedFileName);
            databaseConnection.AddParameter("communicationtype", communication.Type.ToString());
            databaseConnection.AddParameter("creation_date", communication.CreationDate);
            databaseConnection.AddParameter("reply_to", communication.ReplyTo);
            databaseConnection.AddParameter("reply_to_name", communication.ReplyToName);
            databaseConnection.AddParameter("sender", communication.Sender);
            databaseConnection.AddParameter("sender_name", communication.SenderName);
            databaseConnection.AddParameter("send_date", communication.SendDate ?? DateTime.Now);

            if (communication.ProcessedDate.HasValue)
            {
                databaseConnection.AddParameter("processed_date", communication.ProcessedDate);
            }

            if (communication.Cc != null && communication.Cc.Any())
            {
                databaseConnection.AddParameter("cc", String.Join(";", communication.Cc));
            }

            if (communication.Bcc != null && communication.Bcc.Any())
            {
                databaseConnection.AddParameter("bcc", String.Join(";", communication.Bcc));
            }

            if (communication.AttachmentUrls != null && communication.AttachmentUrls.Any())
            {
                databaseConnection.AddParameter("attachment_urls", String.Join(Environment.NewLine, communication.AttachmentUrls));
            }

            if (communication.WiserItemFiles != null && communication.WiserItemFiles.Any())
            {
                databaseConnection.AddParameter("wiser_item_files", String.Join(",", communication.WiserItemFiles));
            }

            return await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserCommunicationGenerated, communication.Id);
        }

        /// <inheritdoc />
        public async Task SendEmailDirectlyAsync(SingleCommunicationModel communication, int timeout = 120_000)
        {
            await SendEmailDirectlyAsync(communication, gclSettings.SmtpSettings, timeout);
        }
        
        /// <inheritdoc />
        public async Task SendEmailDirectlyAsync(SingleCommunicationModel communication, SmtpSettings smtpSettings, int timeout = 120_000)
        {
            // Build attachments list.
            var attachments = await GetAttachmentsAsync(communication);

            switch (smtpSettings.Provider)
            {
                case EmailServiceProviders.Smtp:
                    await SendSmtpEmailDirectlyAsync(communication, smtpSettings, attachments, timeout);
                    break;
                case EmailServiceProviders.SmtPeterRestApi:
                    await SendSmtPeterEmailDirectlyAsync(communication, smtpSettings, attachments, timeout);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(smtpSettings.Provider), smtpSettings.Provider.ToString());
            }
        }

        /// <summary>
        /// Send the email directly using an SMTP server.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smtpSettings">The SMTP settings to use.</param>
        /// <param name="attachments">The attachments to send with the email.</param>
        /// <param name="timeout">The timeout in milliseconds before it's considered to take too long. The default timeout equals to 2 minutes. This is the same default timeout that MailKit uses.</param>
        private async Task SendSmtpEmailDirectlyAsync(SingleCommunicationModel communication, SmtpSettings smtpSettings, List<(string FileName, byte[] FileBytes)> attachments, int timeout)
        {
            var sender = new MailboxAddress(communication.SenderName ?? smtpSettings.SenderName, communication.Sender ?? smtpSettings.SenderEmailAddress);
            var receivers = new List<MailboxAddress>(communication.Receivers.Count());
            receivers.AddRange(communication.Receivers.Select(receiver => new MailboxAddress(receiver.DisplayName, receiver.Address)));
            
            var message = new MimeMessage();

            message.From.Add(sender);
            message.To.AddRange(receivers);

            if (communication.Cc != null && communication.Cc.Any())
            {
                message.Cc.AddRange(communication.Cc.Select(address => new MailboxAddress(address, address)));
            }

            if (communication.Bcc != null && communication.Bcc.Any())
            {
                message.Bcc.AddRange(communication.Bcc.Select(address => new MailboxAddress(address, address)));
            }

            message.Subject = communication.Subject ?? "";

            if (!String.IsNullOrWhiteSpace(communication.ReplyTo))
            {
                message.ReplyTo.Add(new MailboxAddress(communication.ReplyToName ?? "", communication.ReplyTo));
            }

            // Build the body of the message.
            var builder = new BodyBuilder();

            if (attachments != null && attachments.Any())
            {
                foreach (var (fileName, fileBytes) in attachments)
                {
                    builder.Attachments.Add(fileName, fileBytes);
                }
            }

            builder.HtmlBody = communication.Content;

            // Set the body of the message.
            message.Body = builder.ToMessageBody();

            // Send the message.
            var secureSocketOptions = smtpSettings.UseSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable;

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpSettings.Host, smtpSettings.Port, secureSocketOptions);
            await client.AuthenticateAsync(smtpSettings.Username, smtpSettings.Password);

            client.Timeout = timeout;
            var result = await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        /// <summary>
        /// Send the email directly using the SmtPeter Rest API.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smtpSettings">The SMTP settings to use.</param>
        /// <param name="attachments">The attachments to send with the email.</param>
        /// <param name="timeout">The timeout in milliseconds before it's considered to take too long. The default timeout equals to 2 minutes. This is the same default timeout that MailKit uses.</param>
        private async Task SendSmtPeterEmailDirectlyAsync(SingleCommunicationModel communication, SmtpSettings smtpSettings, List<(string FileName, byte[] FileBytes)> attachments, int timeout)
        {
            // Recipients contains the "To" emails, "CC" emails and the "BCC" emails.
            var recipients = new List<string>(communication.Receivers.Select(x => x.Address));
            recipients.AddRange(communication.Cc.ToList());
            recipients.AddRange(communication.Bcc.ToList());

            // Create the "from" string.
            string from;
            if (!String.IsNullOrWhiteSpace(communication.Sender))
            {
                from = !String.IsNullOrWhiteSpace(communication.SenderName) ? $"{communication.SenderName} <{communication.Sender}>" : communication.Sender;
            }
            else
            {
                from = !String.IsNullOrWhiteSpace(smtpSettings.SenderName) ? $"{smtpSettings.SenderName} <{smtpSettings.SenderEmailAddress}>" : smtpSettings.SenderEmailAddress;
            }

            var requestBody = new SmtPeterRequestModel()
            {
                From = from,
                To = new List<string>(communication.Receivers.Select(x => String.IsNullOrWhiteSpace(x.DisplayName) ? x.Address : $"{x.DisplayName} <{x.Address}>")),
                Cc = communication.Cc.ToList(),
                Recipients = recipients,
                ReplyTo = String.IsNullOrWhiteSpace(communication.ReplyToName) ? communication.ReplyTo : $"{communication.ReplyToName} <{communication.ReplyTo}>", 
                Subject = communication.Subject,
                Html = communication.Content
            };

            if (attachments != null && attachments.Any())
            {
                requestBody.Attachments = new List<SmtPeterRquestAttachmentModel>();
                
                foreach (var attachment in attachments)
                {
                    requestBody.Attachments.Add(new SmtPeterRquestAttachmentModel()
                    {
                        Data = Convert.ToBase64String(attachment.FileBytes),
                        Name = attachment.FileName
                    });
                }
            }
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            using var response = await client.PostAsJsonAsync($"https://www.smtpeter.com/v1/send?access_token={smtpSettings.SmtPeterSettings.ApiAccessToken}", requestBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (response.IsSuccessStatusCode)
            {
                return;
            }
            
            // If request was not success throw the body as an exception.
            throw new Exception(await response.Content.ReadAsStringAsync());
        }
        
        private async Task<List<(string FileName, byte[] FileBytes)>> GetAttachmentsAsync(SingleCommunicationModel communication)
        {
            var totalAttachments = (communication.AttachmentUrls?.Count ?? 0) + (communication.WiserItemFiles?.Count ?? 0 + (!String.IsNullOrWhiteSpace(communication.UploadedFileName) && communication.UploadedFile != null ? 1 : 0));

            if (totalAttachments == 0)
            {
                return null;
            }

            var attachments = new List<(string FileName, byte[] FileBytes)>(totalAttachments);
            
            if (!String.IsNullOrWhiteSpace(communication.UploadedFileName) && communication.UploadedFile != null)
            {
                attachments.Add((communication.UploadedFileName, communication.UploadedFile));
            }

            using var webClient = new WebClient();
            if (communication.AttachmentUrls?.Count > 0)
            {
                foreach (var attachmentUrl in communication.AttachmentUrls)
                {
                    attachments.Add((Path.GetFileName(attachmentUrl), await webClient.DownloadDataTaskAsync(attachmentUrl)));
                }
            }

            if (communication.WiserItemFiles?.Count <= 0)
            {
                return attachments;
            }

            var wiserItemFiles = await wiserItemsService.GetItemFilesAsync(communication.WiserItemFiles?.ToArray());
            foreach (var wiserItemFile in wiserItemFiles)
            {
                byte[] fileBytes;
                if (!String.IsNullOrWhiteSpace(wiserItemFile.ContentUrl))
                {
                    fileBytes = await webClient.DownloadDataTaskAsync(wiserItemFile.ContentUrl);
                }
                else
                {
                    fileBytes = wiserItemFile.Content;
                }
                attachments.Add((Path.GetFileName(wiserItemFile.FileName), fileBytes));
            }

            return attachments;
        }

        /// <inheritdoc />
        public async Task SendSmsAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null)
        {
            var receivers = new List<CommunicationReceiverModel>();
            var receiverAddresses = receiver.Split(';');
            foreach (var receiverAddress in receiverAddresses)
            {
                var receiverModel = new CommunicationReceiverModel { Address = receiverAddress };
                receivers.Add(receiverModel);
            }

            await SendSmsAsync(receivers, body, sender, senderName, sendDate);
        }

        /// <inheritdoc />
        public async Task SendSmsAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null)
        {
            await SendSmsAsync(new SingleCommunicationModel()
            {
                Receivers = receivers,
                Content = body,
                Sender = sender,
                SenderName = senderName,
                SendDate = sendDate
            });
        }

        /// <inheritdoc />
        public async Task SendSmsAsync(SingleCommunicationModel communication)
        {
            communication.Id = 0;
            communication.Type = CommunicationTypes.Sms;
            communication.Subject ??= "";
            await AddOrUpdateSingleCommunicationAsync(communication);
        }

        /// <inheritdoc />
        public async Task SendSmsDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings)
        {
            foreach (var receiver in communication.Receivers)
            {
                switch (smsSettings.Provider)
                {
                    case SmsServiceProviders.Twilio:
                        await SendTwilioSmsDirectlyAsync(communication, smsSettings, receiver.Address);
                        break;
                    case SmsServiceProviders.Cm:
                        await SendCmSmsDirectlyAsync(communication, smsSettings, receiver.Address);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(smsSettings.Provider), smsSettings.Provider.ToString());
                }
            }
        }

        /// <summary>
        /// Send a text message using Twilio.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smsSettings">The sms settings to use.</param>
        /// <param name="receiverPhoneNumber">The phone number to send the text message to.</param>
        private async Task SendTwilioSmsDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings, string receiverPhoneNumber)
        {
            TwilioClient.Init(smsSettings.ProviderId, smsSettings.AuthenticationToken);

            if (receiverPhoneNumber.StartsWith("00"))
            {
                // Phone number looks something like "0031612345678".
                receiverPhoneNumber = receiverPhoneNumber.Substring(2);
            }
            else if (!receiverPhoneNumber.StartsWith("+"))
            {
                throw new ArgumentException("Phone number is missing the country code.");
            }
            
            // Now "sanitize" the phone number by removing all whitespace characters and hyphens.
            receiverPhoneNumber = Regex.Replace(receiverPhoneNumber, @"\D+", "");
            // The regex will remove the + symbol as well, so it needs to be put back.
            receiverPhoneNumber = receiverPhoneNumber.Insert(0, "+");
            
            var response = await MessageResource.CreateAsync(
                body: communication.Content,
                from: communication.Sender ?? smsSettings.SenderPhoneNumber,
                to: receiverPhoneNumber);
            
            var successfulStatuses = new[]
            {
                MessageResource.StatusEnum.Accepted,
                MessageResource.StatusEnum.Sent,
                MessageResource.StatusEnum.Queued
            };

            if (successfulStatuses.Contains(response.Status))
            {
                return;
            }

            // If request was not success throw the error message as an exception.
            throw new Exception(response.ErrorMessage);
        }

        /// <summary>
        /// Send a text message using CM.
        /// </summary>
        /// <param name="communication">The <see cref="SingleCommunicationModel"/> object to use as the basis to send the email.</param>
        /// <param name="smsSettings">The text message settings to use.</param>
        /// <param name="receiverPhoneNumber">The phone number to send the text message to.</param>
        private async Task SendCmSmsDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings, string receiverPhoneNumber)
        {
            var apiKey = Guid.Parse(smsSettings.ProviderId);
            if (receiverPhoneNumber.StartsWith("+"))
            {
                // Phone number looks something like "+31612345678".
                receiverPhoneNumber = receiverPhoneNumber.Substring(1).Insert(0, "00");
            }
            else if (!receiverPhoneNumber.StartsWith("00"))
            {
                throw new ArgumentException("Phone number is missing the country code.");
            }

            // Now "sanitize" the phone number by removing all non-digit characters.
            receiverPhoneNumber = Regex.Replace(receiverPhoneNumber, @"\D+", "");
            var cmConnection = new TextClient(apiKey);
            var senderName = communication.SenderName ?? smsSettings.SenderName;
            if (Regex.IsMatch(senderName, "^\\d+$") && senderName.Length > 17)
            {
                senderName = senderName.Substring(0, 17);
            }
            else if (senderName.Length > 11)
            {
                senderName = senderName.Split(' ')[0].Substring(0, Math.Min(11, senderName.Split(' ')[0].Length));
            }

            var response = await cmConnection.SendMessageAsync(communication.Content, senderName, new[] {receiverPhoneNumber}, null);
            if (response.statusCode == TextClientStatusCode.Ok)
            {
                return;
            }

            // If request was not success throw the status message as an exception.
            throw new Exception(response.statusMessage);
        }

        /// <inheritdoc />
        public async Task SendWhatsAppAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null)
        {
            var receivers = new List<CommunicationReceiverModel>();
            var receiverAddresses = receiver.Split(';');
            foreach (var receiverAddress in receiverAddresses)
            {
                var receiverModel = new CommunicationReceiverModel { Address = receiverAddress };
                receivers.Add(receiverModel);
            }

            await SendWhatsAppAsync(receivers, body, sender, senderName, sendDate, attachments);
        }

        /// <inheritdoc />
        public async Task SendWhatsAppAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null)
        {
            await SendWhatsAppAsync(new SingleCommunicationModel()
            {
                Receivers = receivers,
                Content = body,
                Sender = sender,
                SenderName = senderName,
                SendDate = sendDate,
                AttachmentUrls = attachments

            });
        }

        /// <inheritdoc />
        public async Task SendWhatsAppAsync(SingleCommunicationModel communication)
        {
            communication.Id = 0;
            communication.Type = CommunicationTypes.WhatsApp;
            communication.Subject ??= "";
            await AddOrUpdateSingleCommunicationAsync(communication);
        }
        public async Task SendWhatsAppDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings)
        {
            foreach (var receiver in communication.Receivers)
            {
                switch (smsSettings.Provider)
                {
                    case SmsServiceProviders.Cm:
                        await SendCmWhatsAppDirectlyAsync(communication, smsSettings, receiver.Address);
                        break;
                    case SmsServiceProviders.Meta:
                        await SendMetaWhatsAppDirectlyAsync(communication, smsSettings, receiver.Address);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(smsSettings.Provider), smsSettings.Provider.ToString());
                }
            }
        }
        private async Task SendCmWhatsAppDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings, string receiverPhoneNumber)
        {
            var apiKey = Guid.Parse(smsSettings.ProviderId);
            if (receiverPhoneNumber.StartsWith("+"))
            {
                // Phone number looks something like "+31612345678".
                receiverPhoneNumber = receiverPhoneNumber.Substring(1).Insert(0, "00");
            }
            else if (!receiverPhoneNumber.StartsWith("00"))
            {
                throw new ArgumentException("Phone number is missing the country code.");
            }

            // Now "sanitize" the phone number by removing all non-digit characters.
            receiverPhoneNumber = Regex.Replace(receiverPhoneNumber, @"\D+", "");

            var cmConnection = new TextClient(apiKey);

            var senderName = communication.SenderName ?? smsSettings.SenderName;
            if (Regex.IsMatch(senderName, "^\\d+$") && senderName.Length > 17)
            {
                senderName = senderName.Substring(0, 17);
            }
            else if (senderName.Length > 11)
            {
                senderName = senderName.Split(' ')[0].Substring(0, Math.Min(11, senderName.Split(' ')[0].Length));
            }
            var builder = new MessageBuilder(communication.Content, senderName, new[] { receiverPhoneNumber });
            builder.WithAllowedChannels(Channel.WhatsApp);
            var message = builder.Build();

            var response = await cmConnection.SendMessageAsync(message);

            if (response.statusCode == TextClientStatusCode.Ok)
            {
                return;
            }

            // If request was not success throw the status message as an exception.
            throw new Exception(response.statusMessage);
        }
         
    
    private async Task SendMetaWhatsAppDirectlyAsync(SingleCommunicationModel communication, SmsSettings smsSettings, string receiverPhoneNumber)
    {    
        var apiToken = smsSettings.ProviderId;
        var phoneNumberId = smsSettings.PhoneNumberId;
        var resource = "https://graph.facebook.com/v14.0/" + phoneNumberId + "/messages";

            if (receiverPhoneNumber.StartsWith("+") || receiverPhoneNumber.StartsWith("00"))
            {
                if (receiverPhoneNumber.StartsWith("00"))
                {
                    // Phone number looks something like "0031612345678".
                    receiverPhoneNumber = receiverPhoneNumber.Remove(0, 1).Remove(0, 1);
                }
                else
                {
                    // Phone number looks something like "+31612345678".
                    receiverPhoneNumber = receiverPhoneNumber.Remove(0, 1);
                }
             }
            else if (!receiverPhoneNumber.StartsWith("00"))
             {
              throw new ArgumentException("Phone number is missing the country code.");
             }
            
        // Now "sanitize" the phone number by removing all non-digit characters.
        receiverPhoneNumber = Regex.Replace(receiverPhoneNumber, @"\D+", "");
        var senderName = communication.SenderName ?? smsSettings.SenderName;
        if (Regex.IsMatch(senderName, "^\\d+$") && senderName.Length > 17)
        {
            senderName = senderName.Substring(0, 17);
        }
        else if (senderName.Length > 11)
        {
            senderName = senderName.Split(' ')[0].Substring(0, Math.Min(11, senderName.Split(' ')[0].Length));
        }

        if (!String.IsNullOrEmpty(communication.Content))
        {
                var metaConnection = new RestClient();
                var request = new RestRequest(resource, Method.Post);
                request.AddHeader("Authorization", "Bearer " + apiToken);
                request.AddJsonBody(new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = receiverPhoneNumber,
                    type = "text",
                    text = new
                    {
                     preview_url = false,
                     body = communication.Content
                    }
                });

                var response = await metaConnection.ExecuteAsync(request);

                foreach (var url in communication.AttachmentUrls)
                {
                    var typeUrl = "";
                    if (url.Contains(".jpeg") || url.Contains(".png") || url.Contains(".jpg"))
                    {
                        typeUrl = "image";
                    }
                    if (url.Contains(".pdf") || url.Contains(".csv") || url.Contains(".txt") || url.Contains(".xls") || 
                        url.Contains(".xlsx") || url.Contains(".doc") || url.Contains(".docx") || url.Contains(".pptx") || 
                        url.Contains(".ppt") || url.Contains(".xml"))
                    {
                        typeUrl = "document";

                    }
                    if (url.Contains(".mp3"))
                    {
                        typeUrl = "audio";

                    }
                    if (url.Contains(".mp4"))
                    {
                        typeUrl = "video";

                    }

                    if (!String.IsNullOrEmpty(typeUrl))
                    {
                        request = new RestRequest(resource, Method.Post);
                        request.AddHeader("Authorization", "Bearer " + apiToken);
                        request.AddJsonBody(new
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            to = receiverPhoneNumber,
                            type = typeUrl,

                            image = typeUrl != "image" ? null : new
                            {
                                link = url
                            },
                            document = typeUrl != "document" ? null : new
                            {
                                link = url
                            },
                            audio = typeUrl != "audio" ? null : new
                            {
                                link = url
                            },
                            video = typeUrl != "video" ? null : new
                            {
                                link = url
                            },
                        });
                        response = await metaConnection.ExecuteAsync(request);
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                       return;
                    }

                    // If request was not success throw the status message as an exception.
                    throw new Exception(response.ErrorMessage);
                }

          if (response.StatusCode == HttpStatusCode.OK)
          {
             return;
          }
           //If request was not success throw the status message as an exception.
           throw new Exception(response.ErrorMessage);
         }
           
        }



    }
}

