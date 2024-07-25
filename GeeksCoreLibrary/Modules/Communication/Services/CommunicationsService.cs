using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CM.Text;
using CM.Text.BusinessMessaging;
using CM.Text.BusinessMessaging.Model;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Enums;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.Communication.Models.SmtPeter;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RestSharp;
using Newtonsoft.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace GeeksCoreLibrary.Modules.Communication.Services
{
    /// <inheritdoc cref="ICommunicationsService" />
    public class CommunicationsService : ICommunicationsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<CommunicationsService> logger;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;

        /// <summary>
        /// Creates a new instance of <see cref="CommunicationsService"/>.
        /// </summary>
        public CommunicationsService(IOptions<GclSettings> gclSettings, ILogger<CommunicationsService> logger, IWiserItemsService wiserItemsService, IDatabaseConnection databaseConnection, IDatabaseHelpersService databaseHelpersService)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.wiserItemsService = wiserItemsService;
            this.databaseConnection = databaseConnection;
            this.databaseHelpersService = databaseHelpersService;
        }

        /// <inheritdoc />
        public async Task<CommunicationSettingsModel> GetSettingsAsync(int id, bool nameOnly = false)
        {
            await UpdateCommunicationTableAsync();

            var otherColumns = @", 
receiver_list,
receivers_data_selector_id,
receivers_query_id,
content_data_selector_id,
content_query_id,
settings,
send_trigger_type,
trigger_start,
trigger_end,
trigger_time,
trigger_period_value,
trigger_period_type,
trigger_week_days,
trigger_day_of_month,
last_processed,
added_by,
added_on,
changed_by,
changed_on";

            var query = $@"SELECT
    id,
    name
    {(nameOnly ? "" : otherColumns)}
FROM {WiserTableNames.WiserCommunication}
WHERE id = ?id";

            databaseConnection.AddParameter("id", id);
            var dataTable = await databaseConnection.GetAsync(query);
            return dataTable.Rows.Count == 0 ? null : DataRowToCommunicationSettingsModel(dataTable.Rows[0], nameOnly);
        }

        /// <inheritdoc />
        public async Task<List<CommunicationSettingsModel>> GetSettingsAsync(CommunicationTypes? type = null, bool namesOnly = false)
        {
            await UpdateCommunicationTableAsync();

            var whereClause = "";
            if (type.HasValue)
            {
                whereClause = $"WHERE JSON_CONTAINS(JSON_EXTRACT(settings, '$[*].Type'), '{(int)type}')";
            }

            var otherColumns = @", 
receiver_list,
receivers_data_selector_id,
receivers_query_id,
content_data_selector_id,
content_query_id,
settings,
send_trigger_type,
trigger_start,
trigger_end,
trigger_time,
trigger_period_value,
trigger_period_type,
trigger_week_days,
trigger_day_of_month,
last_processed,
added_by,
added_on,
changed_by,
changed_on";

            var query = $@"SELECT
    id,
    name
    {(namesOnly ? "" : otherColumns)}
FROM {WiserTableNames.WiserCommunication}
{whereClause}
ORDER BY name ASC";

            var dataTable = await databaseConnection.GetAsync(query);
            var results = dataTable.Rows.Cast<DataRow>().Select(dataRow => DataRowToCommunicationSettingsModel(dataRow, namesOnly));
            return results.ToList();
        }

        /// <inheritdoc />
        public async Task<CommunicationSettingsModel> SaveSettingsAsync(CommunicationSettingsModel settings, string username = "GCL")
        {
            await UpdateCommunicationTableAsync();

            if (settings.SendTriggerType == SendTriggerTypes.Direct)
            {
                settings.TriggerStart = DateTime.Now;
            }

            databaseConnection.AddParameter("username", username);
            databaseConnection.AddParameter("id", settings.Id);
            databaseConnection.AddParameter("name", settings.Name);
            databaseConnection.AddParameter("receivers_data_selector_id", settings.ReceiversDataSelectorId);
            databaseConnection.AddParameter("receivers_query_id", settings.ReceiversQueryId);
            databaseConnection.AddParameter("content_data_selector_id", settings.ContentDataSelectorId);
            databaseConnection.AddParameter("content_query_id", settings.ContentQueryId);
            databaseConnection.AddParameter("receiver_list", String.Join(";", settings.ReceiversList));
            databaseConnection.AddParameter("settings", JsonConvert.SerializeObject(settings.Settings));
            databaseConnection.AddParameter("send_trigger_type", settings.SendTriggerType.ToString().ToLowerInvariant());
            databaseConnection.AddParameter("trigger_start", settings.TriggerStart);
            databaseConnection.AddParameter("trigger_end", settings.TriggerEnd);
            databaseConnection.AddParameter("trigger_time", settings.TriggerTime);
            databaseConnection.AddParameter("trigger_period_value", settings.TriggerPeriodValue);
            databaseConnection.AddParameter("trigger_period_type", settings.TriggerPeriodType?.ToString().ToLowerInvariant());
            databaseConnection.AddParameter("trigger_week_days", (int?)settings.TriggerWeekDays ?? 0);
            databaseConnection.AddParameter(settings.Id <= 0 ? "added_on" : "changed_on", DateTime.Now);
            databaseConnection.AddParameter(settings.Id <= 0 ? "added_by" : "changed_by", username);

            var queryPrefix = "SET @_username = ?username; ";
            if (settings.Id <= 0)
            {
                // Generate empty last processed list, because the WTS needs that.
                settings.LastProcessed = new List<LastProcessedModel>();
                foreach (var setting in settings.Settings)
                {
                    settings.LastProcessed.Add(new LastProcessedModel { Type = setting.Type });
                }

                databaseConnection.AddParameter("last_processed", JsonConvert.SerializeObject(settings.LastProcessed));

                var query = $@"{queryPrefix}
INSERT INTO {WiserTableNames.WiserCommunication}
(
    name,
    receiver_list,
    receivers_data_selector_id,
    receivers_query_id,
    content_data_selector_id,
    content_query_id,
    settings,
    send_trigger_type,
    trigger_start,
    trigger_end,
    trigger_time,
    trigger_period_value,
    trigger_period_type,
    trigger_week_days,
    last_processed,
    added_on,
    added_by
)
VALUES
(
    ?name,
    ?receiver_list,
    ?receivers_data_selector_id,
    ?receivers_query_id,
    ?content_data_selector_id,
    ?content_query_id,
    ?settings,
    ?send_trigger_type,
    ?trigger_start,
    ?trigger_end,
    ?trigger_time,
    ?trigger_period_value,
    ?trigger_period_type,
    ?trigger_week_days,
    ?last_processed,
    ?added_on,
    ?added_by
)";

                settings.Id = (int)await databaseConnection.InsertRecordAsync(query);
            }
            else
            {
                var query = $@"{queryPrefix}
UPDATE {WiserTableNames.WiserCommunication}
SET name = ?name,
    receiver_list = ?receiver_list,
    receivers_data_selector_id = ?receivers_data_selector_id,
    receivers_query_id = ?receivers_query_id,
    content_data_selector_id = ?content_data_selector_id,
    content_query_id = ?content_query_id,
    settings = ?settings,
    send_trigger_type = ?send_trigger_type,
    trigger_start = ?trigger_start,
    trigger_end = ?trigger_end,
    trigger_time = ?trigger_time,
    trigger_period_value = ?trigger_period_value,
    trigger_period_type = ?trigger_period_type,
    trigger_week_days = ?trigger_week_days,
    changed_on = ?changed_on,
    changed_by = ?changed_by
WHERE id = ?id";

                await databaseConnection.ExecuteAsync(query);
            }

            return settings;
        }

        /// <inheritdoc />
        public async Task DeleteSettingsAsync(int id, string username = "GCL")
        {
            await UpdateCommunicationTableAsync();

            databaseConnection.AddParameter("id", id);
            databaseConnection.AddParameter("username", username);
            var query = $"SET @_username = ?username; DELETE FROM {WiserTableNames.WiserCommunication} WHERE id = ?id";
            await databaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task<bool> CommunicationExistsAsync(int id)
        {
            await UpdateCommunicationTableAsync();

            databaseConnection.AddParameter("id", id);
            var query = $"SELECT NULL FROM {WiserTableNames.WiserCommunication} WHERE id = ?id";
            var dataTable = await databaseConnection.GetAsync(query);
            return dataTable.Rows.Count > 0;
        }

        /// <inheritdoc />
        public async Task<int> SendEmailAsync(string receiver, string subject, string body, string receiverName = null, string cc = null, string bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null)
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

            return await SendEmailAsync(receivers, subject, body, ccAddresses, bccAddresses, replyTo, replyToName, sender, senderName, sendDate, attachments);
        }

        /// <inheritdoc />
        public async Task<int> SendEmailAsync(IEnumerable<CommunicationReceiverModel> receivers, string subject, string body, IEnumerable<string> cc = null, IEnumerable<string> bcc = null, string replyTo = null, string replyToName = null, string sender = null, string senderName = null, DateTime? sendDate = null, List<ulong> attachments = null)
        {
            return await SendEmailAsync(new SingleCommunicationModel
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

            // If both username and password are not set the mail will be sent anonymously to the SMTP server.
            if (!String.IsNullOrWhiteSpace(smtpSettings.Username) || !String.IsNullOrWhiteSpace(smtpSettings.Password))
            {
                await client.AuthenticateAsync(smtpSettings.Username, smtpSettings.Password);
            }

            client.Timeout = timeout;
            await client.SendAsync(message);
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
            communication.StatusMessage = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // If request was not success throw the body as an exception.
            throw new Exception(await response.Content.ReadAsStringAsync());
        }

        private async Task<List<(string FileName, byte[] FileBytes)>> GetAttachmentsAsync(SingleCommunicationModel communication)
        {
            var totalAttachments = (communication.AttachmentUrls?.Count ?? 0) + (communication.WiserItemFiles?.Count ?? 0) + (!String.IsNullOrWhiteSpace(communication.UploadedFileName) && communication.UploadedFile != null ? 1 : 0);

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
                    var data = await webClient.DownloadDataTaskAsync(attachmentUrl);
                    var uri = new Uri(attachmentUrl);
                    var fileName = Path.GetFileName(uri.AbsolutePath);
                    if (webClient.ResponseHeaders?["Content-Disposition"] != null)
                    {
                        // Extract the filename from the Content-Disposition header
                        if (ContentDisposition.TryParse(webClient.ResponseHeaders["Content-Disposition"], out var contentDisposition))
                        {
                            fileName = Path.GetFileName(contentDisposition.FileName);
                        }
                    }

                    fileName = HttpUtility.UrlDecode(fileName);
                    attachments.Add((fileName, data));
                    webClient.ResponseHeaders?.Clear();
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
        public async Task<int> SendSmsAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null)
        {
            var receivers = new List<CommunicationReceiverModel>();
            var receiverAddresses = receiver.Split(';');
            foreach (var receiverAddress in receiverAddresses)
            {
                var receiverModel = new CommunicationReceiverModel { Address = receiverAddress };
                receivers.Add(receiverModel);
            }

            return await SendSmsAsync(receivers, body, sender, senderName, sendDate);
        }

        /// <inheritdoc />
        public async Task<int> SendSmsAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null)
        {
            return await SendSmsAsync(new SingleCommunicationModel()
            {
                Receivers = receivers,
                Content = body,
                Sender = sender,
                SenderName = senderName,
                SendDate = sendDate
            });
        }

        /// <inheritdoc />
        public async Task<int> SendSmsAsync(SingleCommunicationModel communication)
        {
            communication.Id = 0;
            communication.Type = CommunicationTypes.Sms;
            communication.Subject ??= "";
            return await AddOrUpdateSingleCommunicationAsync(communication);
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
        public async Task<int> SendWhatsAppAsync(string receiver, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null)
        {
            var receivers = new List<CommunicationReceiverModel>();
            var receiverAddresses = receiver.Split(';');
            foreach (var receiverAddress in receiverAddresses)
            {
                var receiverModel = new CommunicationReceiverModel { Address = receiverAddress };
                receivers.Add(receiverModel);
            }

            return await SendWhatsAppAsync(receivers, body, sender, senderName, sendDate, attachments);
        }

        /// <inheritdoc />
        public async Task<int> SendWhatsAppAsync(IEnumerable<CommunicationReceiverModel> receivers, string body, string sender = null, string senderName = null, DateTime? sendDate = null, List<string> attachments = null)
        {
            return await SendWhatsAppAsync(new SingleCommunicationModel()
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
        public async Task<int> SendWhatsAppAsync(SingleCommunicationModel communication)
        {
            communication.Id = 0;
            communication.Type = CommunicationTypes.WhatsApp;
            communication.Subject ??= "";
            return await AddOrUpdateSingleCommunicationAsync(communication);
        }

        /// <inheritdoc />
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
            var resource = $"https://graph.facebook.com/v14.0/{phoneNumberId}/messages";

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

            if (String.IsNullOrEmpty(communication.Content))
            {
                return;
            }
            else
            {
                var metaConnection = new RestClient();
                var request = new RestRequest(resource, Method.Post);
                request.AddHeader("Authorization", $"Bearer {apiToken}");

                request.AddJsonBody(new WhatsAppSendMessageRequestModel
                {
                    MessagingProduct = "whatsapp",
                    RecipientType = "individual",
                    Receiver = receiverPhoneNumber,
                    TypeMessage = "text",
                    Body = new WhatsappBodyContentModel
                    {
                        PreviewUrl = false,
                        BodyContent = communication.Content
                    }
                });

                var response = await metaConnection.ExecuteAsync(request);

                foreach (var url in communication.AttachmentUrls)
                {
                    var typeUrl = "";
                    switch (url)
                    {
                        case string a when a.Contains(".jpeg"):
                        case string b when b.Contains(".png"):
                        case string c when c.Contains(".jpg"):
                            typeUrl = "image";
                            break;
                        case string d when d.Contains(".pdf"):
                        case string e when e.Contains(".csv"):
                        case string f when f.Contains(".txt"):
                        case string g when g.Contains(".xls"):
                        case string h when h.Contains(".xlsx"):
                        case string i when i.Contains(".doc"):
                        case string j when j.Contains(".docx"):
                        case string k when k.Contains(".pptx"):
                        case string l when l.Contains(".ppt"):
                        case string m when m.Contains(".xml"):
                            typeUrl = "document";
                            break;
                        case string n when n.Contains(".mp3"):
                            typeUrl = "audio";
                            break;
                        case string o when o.Contains(".mp4"):
                            typeUrl = "video";
                            break;
                    }

                    if (!String.IsNullOrEmpty(typeUrl))
                    {
                        request = new RestRequest(resource, Method.Post);
                        request.AddHeader("Authorization", $"Bearer {apiToken}");
                        request.AddJsonBody(new WhatsAppSendMessageRequestModel
                        {
                            MessagingProduct = "whatsapp",
                            RecipientType = "individual",
                            Receiver = receiverPhoneNumber,
                            TypeMessage = typeUrl,
                            TypeUrlImage = typeUrl != "image" ? null : new AttachmentUrlsModel
                            { Url = url },
                            TypeUrlDocument = typeUrl != "document" ? null : new AttachmentUrlsModel
                            { Url = url },
                            TypeUrlAudio = typeUrl != "audio" ? null : new AttachmentUrlsModel
                            { Url = url},
                            TypeUrlVideo = typeUrl != "video" ? null : new AttachmentUrlsModel
                            { Url = url }
                        });

                        response = await metaConnection.ExecuteAsync(request);
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }

                    // If request (image/document/audio/video) was not success throw the status message as an exception.
                    throw new Exception($"image/document/audio/video has not been sent... {response.ErrorMessage}");
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
                //If request (communication.Content) was not success throw the status message as an exception.
                throw new Exception($"message content has not been sent... {response.ErrorMessage}");
             }
        }

        /// <summary>
        /// Update the wiser_communication table with any new columns or indexes that might have been added at some point.
        /// In october 2022 we overhauled the entire structure of the table, which the <see cref="IDatabaseHelpersService"/> cannot handle, so we rename the old table as backup and then recreate it.
        /// </summary>
        private async Task UpdateCommunicationTableAsync()
        {
            var tableChanges = await databaseHelpersService.GetLastTableUpdatesAsync(databaseConnection.ConnectedDatabase);
            if ((!tableChanges.ContainsKey(WiserTableNames.WiserCommunication) || tableChanges[WiserTableNames.WiserCommunication] < new DateTime(2022, 10, 18)) && await databaseHelpersService.TableExistsAsync(WiserTableNames.WiserCommunication))
            {
                // We changed the table wiser_communication a lot and the databaseHelpersService does not support renaming columns.
                // However, the old table was not used anywhere on production, so we rename the old table and then re-create it with the new structure.
                await databaseHelpersService.RenameTableAsync(WiserTableNames.WiserCommunication, $"_{WiserTableNames.WiserCommunication}_backup_{DateTime.Now:yyyy-MM-dd}");
            }

            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserCommunication});
        }

        /// <summary>
        /// Converts a <see cref="DataRow"/>, with data from the table wiser_communication, to a <see cref="CommunicationSettingsModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/>.</param>
        /// <param name="nameOnly">Optional: Whether to only get the name (and ID) or everything.</param>
        /// <returns>The <see cref="CommunicationSettingsModel"/>.</returns>
        private CommunicationSettingsModel DataRowToCommunicationSettingsModel(DataRow dataRow, bool nameOnly = false)
        {
            // All simple properties.
            var result = new CommunicationSettingsModel
            {
                Id = dataRow.Field<int>("id"),
                Name = dataRow.Field<string>("name")
            };

            if (nameOnly)
            {
                return result;
            }

            result.ReceiversDataSelectorId = dataRow.Field<int>("receivers_data_selector_id");
            result.ReceiversQueryId = dataRow.Field<int>("receivers_query_id");
            result.ContentDataSelectorId = dataRow.Field<int>("content_data_selector_id");
            result.ContentQueryId = dataRow.Field<int>("content_query_id");
            result.TriggerStart = dataRow.Field<DateTime?>("trigger_start");
            result.TriggerEnd = dataRow.Field<DateTime?>("trigger_end");
            result.TriggerTime = dataRow.Field<TimeSpan?>("trigger_time");
            result.TriggerPeriodValue = Convert.ToInt32(dataRow["trigger_period_value"]);
            result.TriggerWeekDays = dataRow.Field<TriggerWeekDays>("trigger_week_days");
            result.TriggerDayOfMonth = Convert.ToInt32(dataRow["trigger_day_of_month"]);
            result.AddedBy = dataRow.Field<string>("added_by");
            result.AddedOn = dataRow.Field<DateTime>("added_on");
            result.ChangedBy = dataRow.Field<string>("changed_by");
            result.ChangedOn = dataRow.Field<DateTime?>("changed_on");

            if (Enum.TryParse(typeof(TriggerPeriodTypes), dataRow.Field<string>("trigger_period_type"), true, out var triggerPeriodType) && triggerPeriodType != null)
            {
                result.TriggerPeriodType = (TriggerPeriodTypes) triggerPeriodType;
            }

            if (Enum.TryParse(typeof(SendTriggerTypes), dataRow.Field<string>("send_trigger_type"), true, out var sendTriggerType) && sendTriggerType != null)
            {
                result.SendTriggerType = (SendTriggerTypes) sendTriggerType;
            }

            // Settings are saved as JSON in database, so deserialize them here.
            var settings = dataRow.Field<string>("settings");
            try
            {
                if (!String.IsNullOrWhiteSpace(settings))
                {
                    result.Settings = JsonConvert.DeserializeObject<List<CommunicationContentSettingsModel>>(settings);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"An error occurred while trying to deserialize the settings of communication with ID '{result.Id}'");
            }

            // Last processed is saved as JSON in database, so deserialize them here.
            var lastProcessed = dataRow.Field<string>("last_processed");
            try
            {
                if (!String.IsNullOrWhiteSpace(lastProcessed))
                {
                    result.LastProcessed = JsonConvert.DeserializeObject<List<LastProcessedModel>>(lastProcessed);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"An error occurred while trying to deserialize the last processed data of communication with ID '{result.Id}'");
            }

            // Receivers are saved semicolon seperated in database, convert that to a list of strings.
            var receivers = dataRow.Field<string>("receiver_list");
            if (!String.IsNullOrWhiteSpace(receivers))
            {
                result.ReceiversList = receivers.Split(";", StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return result;
        }
    }
}