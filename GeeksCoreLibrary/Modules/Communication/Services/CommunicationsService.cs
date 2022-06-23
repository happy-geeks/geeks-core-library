using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
        
        public async Task SendEmailDirectlyAsync(SingleCommunicationModel communication, SmtpSettings smtpSettings, int timeout = 120_000)
        {
            var sender = new MailboxAddress(communication.SenderName, communication.Sender);
            var receivers = new List<MailboxAddress>(communication.Receivers.Count());
            receivers.AddRange(communication.Receivers.Select(receiver => new MailboxAddress(receiver.DisplayName, receiver.Address)));

            // Build attachments list.
            var attachments = await GetAttachmentsAsync(communication);

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

            if (!String.IsNullOrWhiteSpace(communication.UploadedFileName) && communication.UploadedFile != null)
            {
                builder.Attachments.Add(communication.UploadedFileName, communication.UploadedFile);
            }

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

        private async Task<List<(string FileName, byte[] FileBytes)>> GetAttachmentsAsync(SingleCommunicationModel communication)
        {
            var totalAttachments = (communication.AttachmentUrls?.Count ?? 0) + (communication.WiserItemFiles?.Count ?? 0);

            if (totalAttachments == 0)
            {
                return null;
            }

            var attachments = new List<(string FileName, byte[] FileBytes)>(totalAttachments);

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
    }
}
