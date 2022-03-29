using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Enums;
using GeeksCoreLibrary.Modules.Payments.Helpers;
using GeeksCoreLibrary.Modules.Payments.Interfaces;
using GeeksCoreLibrary.Modules.Payments.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.OrderProcess.Models;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using Microsoft.Extensions.Options;
using Constants = GeeksCoreLibrary.Modules.Payments.Models.Constants;

namespace GeeksCoreLibrary.Modules.Payments.Services
{
    public class PaymentsService : IPaymentsService, IScopedService
    {
        private readonly ILogger<PaymentsService> logger;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly ICommunicationsService communicationsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IAccountsService accountsService;
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly ILanguagesService languagesService;

        public PaymentsService(ILogger<PaymentsService> logger, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor, IDatabaseConnection databaseConnection, IObjectsService objectsService, ICommunicationsService communicationsService, IWiserItemsService wiserItemsService, IAccountsService accountsService, IShoppingBasketsService shoppingBasketsService, IStringReplacementsService stringReplacementsService, IPaymentServiceProviderServiceFactory paymentServiceProviderServiceFactory, IWebHostEnvironment webHostEnvironment, IHtmlToPdfConverterService htmlToPdfConverterService, ILanguagesService languagesService)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.communicationsService = communicationsService;
            this.wiserItemsService = wiserItemsService;
            this.accountsService = accountsService;
            this.shoppingBasketsService = shoppingBasketsService;
            this.stringReplacementsService = stringReplacementsService;
            this.paymentServiceProviderServiceFactory = paymentServiceProviderServiceFactory;
            this.webHostEnvironment = webHostEnvironment;
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.languagesService = languagesService;
        }

        /// <inheritdoc />
        public async Task<bool> HandleStatusUpdateAsync()
        {
            var paymentServiceProvider = GetPaymentServiceProvider();
            var shoppingBaskets = await shoppingBasketsService.GetOrdersByUniquePaymentNumberAsync(GetInvoiceNumber(paymentServiceProvider));

            // Create the correct service for the payment service provider using the factory.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentServiceProvider);
            paymentServiceProviderService.LogPaymentActions = (await objectsService.FindSystemObjectByDomainNameAsync("log_all_psp_requests")).Equals("true");

            // Let the payment service provider service handle the status update.
            var pspUpdateResult = await paymentServiceProviderService.ProcessStatusUpdateAsync();

            var result = await ProcessStatusUpdateAsync(shoppingBaskets, pspUpdateResult.Status, pspUpdateResult.Successful);

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            foreach (var (main, lines) in shoppingBaskets)
            {
                await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
            }

            return result;
        }

        private string GetInvoiceNumber(PaymentServiceProviders paymentServiceProvider)
        {
            if (paymentServiceProvider == PaymentServiceProviders.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(paymentServiceProvider), "Unknown payment service provider.");
            }

            return paymentServiceProvider switch
            {
                PaymentServiceProviders.Buckaroo => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "brq_invoicenumber"),
                PaymentServiceProviders.MultiSafepay => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "transactionid"),
                PaymentServiceProviders.RaboOmniKassa => HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "order_id"),
                _ => throw new ArgumentOutOfRangeException(nameof(paymentServiceProvider), $"Payment service provider '{paymentServiceProvider:G}' is not yet supported.")
            };
        }

        /// <summary>
        /// Attempts to determine the invoice number by checking for various 
        /// </summary>
        /// <returns></returns>
        private PaymentServiceProviders GetPaymentServiceProvider()
        {
            var paymentServiceProviderName = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "gcl_psp");
            if (String.IsNullOrWhiteSpace(paymentServiceProviderName))
            {
                paymentServiceProviderName = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "PSP");
            }

            // Try to parse value into a payment service provider enum value.
            if (!String.IsNullOrWhiteSpace(paymentServiceProviderName) && Enum.TryParse(paymentServiceProviderName, true, out PaymentServiceProviders paymentServiceProvider))
            {
                return paymentServiceProvider;
            }

            // Detect based on specific request values.
            var testValue = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "brq_invoicenumber");
            if (!String.IsNullOrWhiteSpace(testValue))
            {
                return PaymentServiceProviders.Buckaroo;
            }

            testValue = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "transactionid");
            if (!String.IsNullOrWhiteSpace(testValue))
            {
                return PaymentServiceProviders.MultiSafepay;
            }

            testValue = HttpContextHelpers.GetRequestValue(httpContextAccessor.HttpContext, "order_id");
            if (!String.IsNullOrWhiteSpace(testValue))
            {
                return PaymentServiceProviders.RaboOmniKassa;
            }

            return PaymentServiceProviders.Unknown;
        }

        /// <inheritdoc />
        public async Task<bool> ProcessStatusUpdateAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
        {
            var mailsToSendToUser = new List<SingleCommunicationModel>();
            var mailsToSendToMerchant = new List<SingleCommunicationModel>();
            var mailAttachment = await objectsService.FindSystemObjectByDomainNameAsync("PSP_mailAttachment");
            var paymentHistoryPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_PaymentHistoryPropertyName", "paymenthistory");
            var basketSettings = await shoppingBasketsService.GetSettingsAsync();
            var orderEntityType = await objectsService.FindSystemObjectByDomainNameAsync("orderEntityType", "order");

            var emailContent = "";
            var emailSubject = "";
            var attachmentTemplate = "";
            var userEmailAddress = "";
            var merchantEmailAddress = "";

            var attachments = new List<string>();

            var orderNotFinished = true;

            foreach (var (main, lines) in shoppingBaskets)
            {
                orderNotFinished = main.EntityType != orderEntityType;

                // Get email content and addresses.
                var mailValues = await GetMailValuesAsync(main, lines);
                if (mailValues != null)
                {
                    emailContent = mailValues.Content;
                    emailSubject = mailValues.Subject;
                    userEmailAddress = mailValues.User?.Address ?? "";
                    merchantEmailAddress = mailValues.Merchant?.Address ?? "";
                }

                // Get email content specifically for the merchant.
                mailValues = await GetMailValuesAsync(main, lines, true);
                string merchantEmailContent;
                string merchantEmailSubject;
                if (mailValues != null)
                {
                    merchantEmailContent = mailValues.Content;
                    merchantEmailSubject = mailValues.Subject;
                    merchantEmailAddress = mailValues.Merchant.Address;
                }
                else
                {
                    merchantEmailContent = emailContent;
                    merchantEmailSubject = emailSubject;
                }

                // Get email content specifically for the attachment.
                mailValues = await GetMailValuesAsync(main, lines, false, true);
                if (mailValues != null)
                {
                    attachmentTemplate = mailValues.Content;
                }

                main.SetDetail(paymentHistoryPropertyName, $"{DateTime.Now:yyyyMMddHHmmss} - {newStatus}", true);

                // If order is not finished yet and the payment was successful.
                if (orderNotFinished && isSuccessfulStatus && convertConceptOrderToOrder)
                {
                    await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                }

                if (!String.IsNullOrWhiteSpace(userEmailAddress) && !String.IsNullOrWhiteSpace(emailContent))
                {
                    mailsToSendToUser.Add(new SingleCommunicationModel
                    {
                        Content = emailContent,
                        Subject = emailSubject,
                        Receivers = new List<CommunicationReceiverModel> { new() { Address = userEmailAddress } }
                    });
                }

                if (!String.IsNullOrWhiteSpace(merchantEmailAddress) && !String.IsNullOrWhiteSpace(merchantEmailContent))
                {
                    mailsToSendToMerchant.Add(new SingleCommunicationModel
                    {
                        Content = merchantEmailContent,
                        Subject = merchantEmailSubject,
                        Receivers = new List<CommunicationReceiverModel> { new() { Address = merchantEmailAddress } }
                    });
                }
            }

            if (!String.IsNullOrWhiteSpace(mailAttachment) && !String.IsNullOrWhiteSpace(attachmentTemplate))
            {
                var mailValues = await GetMailValuesAsync(shoppingBaskets.First().Main, shoppingBaskets.First().Lines, false, true);
                if (mailValues != null)
                {
                    attachmentTemplate = mailValues.Content;
                }

                var attachmentFilename = await objectsService.FindSystemObjectByDomainNameAsync("PSP_mailAttachementFilename");
                attachmentFilename = !String.IsNullOrWhiteSpace(attachmentFilename)
                    ? $"{attachmentFilename.Replace("{orderid}", shoppingBaskets.First().Main.Id.ToString())}.pdf"
                    : $"PSP_mailAttachment_{shoppingBaskets.First().Main.Id}.pdf";

                var attachmentFileLocation = Path.Combine(webHostEnvironment.WebRootPath, "contentfiles", attachmentFilename);

                var pdfOrientationValue = await objectsService.FindSystemObjectByDomainNameAsync("PSP_mailAttachmentOrientation");

                var pdfOrientation = pdfOrientationValue.Equals("landscape", StringComparison.OrdinalIgnoreCase) ? EvoPdf.PdfPageOrientation.Landscape : EvoPdf.PdfPageOrientation.Portrait;
                var pdfFile = await htmlToPdfConverterService.ConvertHtmlStringToPdfAsync( new HtmlToPdfRequestModel { Html = attachmentTemplate, Orientation = pdfOrientation });
                await File.WriteAllBytesAsync(attachmentFileLocation, pdfFile.FileContents);

                // TODO: Attachments should be saved in item files. They cannot be added directly to the communications service.

                attachments.Add(attachmentFileLocation);
            }

            // TODO: Add customer attachments.

            if (orderNotFinished)
            {
                foreach (var mailToSend in mailsToSendToUser)
                {
                    if (isSuccessfulStatus && mailToSend.Receivers.Any() && !String.IsNullOrWhiteSpace(mailToSend.Content))
                    {
                        await communicationsService.SendEmailAsync(mailToSend);
                    }
                }

                var mailStatusUpdateToMerchant = (await objectsService.FindSystemObjectByDomainNameAsync("SendMailWebshopStatusUpdate", "true")).Equals("true", StringComparison.OrdinalIgnoreCase);
                foreach (var mailToSend in mailsToSendToMerchant)
                {
                    if (!mailToSend.Receivers.Any() || String.IsNullOrWhiteSpace(mailToSend.Content))
                    {
                        continue;
                    }

                    var bccEmailAddress = await objectsService.FindSystemObjectByDomainNameAsync("PSP_senttoBCCaddress");
                    var replyToEmailAddress = await objectsService.FindSystemObjectByDomainNameAsync("PSP_replytoaddress");
                    var replyToName = await objectsService.FindSystemObjectByDomainNameAsync("PSP_replytoname");
                    var senderEmailAddress = await objectsService.FindSystemObjectByDomainNameAsync("PSP_sentfromaddress");
                    var senderName = await objectsService.FindSystemObjectByDomainNameAsync("PSP_sentfromname");

                    mailToSend.Bcc = new[] { bccEmailAddress };
                    mailToSend.ReplyTo = replyToEmailAddress;
                    mailToSend.ReplyToName = replyToName;
                    mailToSend.Sender = senderEmailAddress;
                    mailToSend.SenderName = senderName;

                    if (mailStatusUpdateToMerchant)
                    {
                        mailToSend.Subject = $"{mailToSend.Subject} - status update";
                        mailToSend.Content = $"{newStatus}<br /><br />{mailToSend.Content}";
                    }

                    await communicationsService.SendEmailAsync(mailToSend);
                }
            }

            return true;
        }

        private async Task<EmailValues> GetMailValuesAsync(WiserItemModel shoppingBasket, List<WiserItemModel> basketLines, bool forMerchantMail = false, bool forAttachment = false)
        {
            var mailBodyPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_MailBodyPropertyName", "template");
            var mailSubjectPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_MailSubjectPropertyName", "subject");
            var mailToPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_MailToPropertyName", "mailto");
            var emailAddressBasketPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_EmailAddressBasketPropertyName");
            var languageCodePropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_LanguageCodePropertyName", "languagecode");
            var emailAddressPropertyName = await shoppingBasketsService.GetCheckoutObjectValueAsync("CHECKOUT_EmailAddressPropertyName", "emailaddress");
            var getMailToBasedOnDeliveryMethod = (await objectsService.FindSystemObjectByDomainNameAsync("GetMailAdresBasedOnDeliveryMethod", "false")).Equals("true", StringComparison.OrdinalIgnoreCase);

            var userEmailAddress = "";
            var merchantEmailAddress = "";

            var linkedUsers = await wiserItemsService.GetLinkedItemDetailsAsync(shoppingBasket.Id, reverse: true);

            string templatePropertyName;
            if (forAttachment)
            {
                templatePropertyName = "PSP_mailAttachment";
            }
            else if (forMerchantMail)
            {
                templatePropertyName = "PSP_mailtemplateWebshop";
            }
            else
            {
                templatePropertyName = "PSP_mailtemplate";
            }

            var templateItemId = 0UL;
            if (!String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(templatePropertyName)))
            {
                UInt64.TryParse(shoppingBasket.GetDetailValue(templatePropertyName), out templateItemId);
            }

            if (templateItemId == 0 && (!UInt64.TryParse(await objectsService.FindSystemObjectByDomainNameAsync(templatePropertyName), out templateItemId) || templateItemId == 0))
            {
                return null;
            }

            var languageCode = "";

            if (!String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(languageCodePropertyName)))
            {
                languageCode = shoppingBasket.GetDetailValue(languageCodePropertyName);
            }

            var user = await accountsService.GetUserDataFromCookieAsync();
            var templateItem = await wiserItemsService.GetItemDetailsAsync(templateItemId, languageCode: languageCode, userId: user.UserId) ?? await wiserItemsService.GetItemDetailsAsync(templateItemId, userId: user.UserId);

            var templateContent = templateItem.GetDetailValue(mailBodyPropertyName);
            var templateSubject = templateItem.GetDetailValue(mailSubjectPropertyName);

            if (getMailToBasedOnDeliveryMethod)
            {
                if (UInt64.TryParse(shoppingBasket.GetDetailValue("shippingMethod"), out var chosenDeliveryMethod) && chosenDeliveryMethod > 0)
                {
                    var chosenDeliveryMethodDetails = await wiserItemsService.GetItemDetailsAsync(chosenDeliveryMethod);
                    var chosenDeliveryMethodEmailAddress = chosenDeliveryMethodDetails.GetDetailValue("delivery_mailaddress");

                    if (!String.IsNullOrWhiteSpace(chosenDeliveryMethodEmailAddress))
                    {
                        merchantEmailAddress = chosenDeliveryMethodEmailAddress;
                    }
                }
            }
            else
            {
                merchantEmailAddress = templateItem.GetDetailValue(mailToPropertyName);
            }

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            // Do subject replacements.
            templateSubject = await shoppingBasketsService.ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, basketSettings, templateSubject, isForConfirmationEmail: true);

            // Do basket replacements.
            if (linkedUsers.Count > 0)
            {
                templateContent = await shoppingBasketsService.ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, basketSettings, templateContent, userDetails: linkedUsers.Last().GetSortedList(), isForConfirmationEmail: true);
                if (linkedUsers.Last().ContainsDetail(emailAddressPropertyName))
                {
                    userEmailAddress = linkedUsers.Last().GetDetailValue(emailAddressPropertyName);
                }
            }
            else
            {
                templateContent = await shoppingBasketsService.ReplaceBasketInTemplateAsync(shoppingBasket, basketLines, basketSettings, templateContent, isForConfirmationEmail: true);
            }
            //get customer basket email instead of the potentially linked user email address
            if (!String.IsNullOrWhiteSpace(emailAddressBasketPropertyName) && !String.IsNullOrWhiteSpace(shoppingBasket.GetDetailValue(emailAddressBasketPropertyName)))
            {
                userEmailAddress = shoppingBasket.GetDetailValue(emailAddressBasketPropertyName);
            }

            return new EmailValues
            {
                Content = templateContent,
                Subject = templateSubject,
                User = new CommunicationReceiverModel { Address = userEmailAddress },
                Merchant = new CommunicationReceiverModel { Address = merchantEmailAddress }
            };
        }
    }
}
