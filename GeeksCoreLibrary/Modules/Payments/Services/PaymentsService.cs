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
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using Microsoft.Extensions.Options;

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
        public async Task<PaymentRequestResult> HandlePaymentRequestAsync()
        {
            if (httpContextAccessor.HttpContext == null || !httpContextAccessor.HttpContext.Request.HasFormContentType)
            {
                return new PaymentRequestResult
                {
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    Successful = false,
                    ErrorMessage = "No http context found."
                };
            }

            // Retrieve baskets.
            var checkoutBasketsCookieName = await GetCheckoutObjectValueAsync("CHECKOUT_CheckoutBasketsCookieName");
            if (String.IsNullOrWhiteSpace(checkoutBasketsCookieName))
            {
                checkoutBasketsCookieName = "checkout_baskets";
            }

            var shoppingBaskets = await shoppingBasketsService.GetShoppingBasketsAsync(checkoutBasketsCookieName);

            var orderId = 0UL;

            // Determine invoice number by format.
            var invoiceNumber = "";
            var invoiceNumberFormat = await objectsService.FindSystemObjectByDomainNameAsync("ORDER_invoicenumberFormat");
            if (!String.IsNullOrWhiteSpace(invoiceNumberFormat))
            {
                invoiceNumber = DateTime.Now.ToString(invoiceNumberFormat);
            }

            // Get current user.
            var user = await accountsService.GetUserDataFromCookieAsync();
            var userDetails = user.UserId > 0 ? await wiserItemsService.GetItemDetailsAsync(user.UserId) : new WiserItemModel();

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var newShoppingBaskets = new List<(WiserItemModel Main, List<WiserItemModel> Lines)>();
            foreach (var (main, lines) in shoppingBaskets)
            {
                await shoppingBasketsService.UpdateShoppingBasketWithRequestDataAsync(main, basketSettings);
                var (conceptOrderId, conceptOrder, conceptOrderLines) = await shoppingBasketsService.MakeConceptOrderFromBasketAsync(main, lines, basketSettings);

                newShoppingBaskets.Add((conceptOrder, conceptOrderLines));

                orderId = conceptOrderId;
            }

            // Update the baskets list with the newly created concept orders.
            shoppingBaskets = newShoppingBaskets;

            var paymentMethodData = shoppingBaskets.First().Main.GetDetailValue("paymentmethod");
            if (String.IsNullOrWhiteSpace(paymentMethodData))
            {
                throw new Exception("Cannot handle payment request: No payment method set.");
            }

            var paymentMethodParts = paymentMethodData.Split('_');

            // The payment method should contain 2 parts, the name of the PSP and the name of the payment method, separated by an underscore. E.g.:
            // Buckaroo_Ideal
            if (paymentMethodParts.Length < 2)
            {
                return new PaymentRequestResult
                {
                    Successful = false,
                    Action = PaymentRequestActions.Redirect,
                    ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                    ErrorMessage = $"Invalid payment method '{paymentMethodData}'"
                };
            }

            // Check if the PSP name is available in the PSP enum.
            // Support for legacy names (aka the JCL names) is available through a helper function.
            if (!Enum.TryParse(paymentMethodParts[0], true, out PaymentServiceProviders paymentServiceProvider))
            {
                // Maybe an old legacy name is used (like "BUCK" instead of "Buckaroo"). Check that here.
                paymentServiceProvider = LegacyMappingsHelper.GetPaymentServiceProviderByLegacyName(paymentMethodParts[0]);

                if (paymentServiceProvider == PaymentServiceProviders.Unknown)
                {
                    logger.LogDebug($"PaymentService: Unknown payment service provider name: {paymentMethodParts[0]}");

                    // Not a supported PSP; return user to the error page.
                    return new PaymentRequestResult
                    {
                        Successful = false,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                        ErrorMessage = "Unknown PSP"
                    };
                }
            }

            if (!Enum.TryParse(paymentMethodParts[1], true, out PaymentMethods paymentMethod))
            {
                paymentMethod = LegacyMappingsHelper.GetPaymentMethodByLegacyName(paymentMethodParts[1]);

                if (paymentMethod == PaymentMethods.Unknown)
                {
                    logger.LogDebug($"PaymentService: Unknown payment method: {paymentMethodParts[1]}");

                    // Not a supported payment method; return user to the error page.
                    return new PaymentRequestResult
                    {
                        Successful = false,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                        ErrorMessage = "Unknown payment method"
                    };
                }
            }

            // Update the "paymentmethod" key in the basket so it doesn't use the legacy type anymore.
            foreach (var (main, lines) in shoppingBaskets)
            {
                main.SetDetail("paymentmethod", $"{paymentServiceProvider:G}_{paymentMethod:G}");
                await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
            }

            var paymentMethodsQuery = await objectsService.FindSystemObjectByDomainNameAsync("PSP_paymentMethodQuery");
            var paymentMethodCheckWithQuery = await objectsService.FindSystemObjectByDomainNameAsync("PSP_paymentMethodCheckWithQuery");
            var userPaymentAllowed = false;

            if (!String.IsNullOrWhiteSpace(paymentMethodsQuery) && paymentMethodCheckWithQuery.InList("true", "1"))
            {
                paymentMethodsQuery = await stringReplacementsService.DoAllReplacementsAsync(paymentMethodsQuery, removeUnknownVariables: false);

                // Retrieve total amount and replace it inside the query template.
                var totalPrice = 0M;
                foreach (var (main, lines) in shoppingBaskets)
                {
                    totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings);
                }

                paymentMethodsQuery = paymentMethodsQuery.Replace("{totalAmountOrder}", totalPrice.ToString(CultureInfo.InvariantCulture));

                var getPaymentMethodsResult = await databaseConnection.GetAsync(paymentMethodsQuery);
                var containsAllowedColumn = getPaymentMethodsResult.Columns.Contains("isAllowedToPay");

                // No result means the user is now allowed to pay.
                if (getPaymentMethodsResult.Rows.Count > 0)
                {
                    foreach (DataRow paymentMethodDataRow in getPaymentMethodsResult.Rows)
                    {
                        if (!paymentMethodDataRow.Field<string>("paymentmethod").Equals(paymentMethodData, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        userPaymentAllowed = !containsAllowedColumn || Convert.ToBoolean(paymentMethodDataRow["isAllowedToPay"]);
                    }
                }

                if (!userPaymentAllowed)
                {
                    return new PaymentRequestResult
                    {
                        Successful = false,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_PaymentStartFailed"),
                        ErrorMessage = "This user is not allowed to pay"
                    };
                }
            }

            // TODO: Add support for pre-made orders.

            var invoiceNumberQuery = await objectsService.FindSystemObjectByDomainNameAsync("ORDER_invoicenumberQuery");
            if (!String.IsNullOrWhiteSpace(invoiceNumberQuery))
            {
                invoiceNumberQuery = invoiceNumberQuery.Replace("{oid}", orderId.ToString());
                var getInvoiceNumberResult = await databaseConnection.GetAsync(invoiceNumberQuery);
                if (getInvoiceNumberResult.Rows.Count > 0)
                {
                    invoiceNumber = Convert.ToString(getInvoiceNumberResult.Rows[0][0]);
                }
            }

            if (String.IsNullOrWhiteSpace(invoiceNumber))
            {
                invoiceNumber = orderId.ToString();
            }

            var uniquePaymentNumberWithoutDate = (await GetCheckoutObjectValueAsync("CHECKOUT_UniquePaymentNumberWithoutDate")).Equals("1");
            var uniquePaymentNumber = uniquePaymentNumberWithoutDate ? invoiceNumber : $"{invoiceNumber}-{DateTime.Now:yyyyMMddHHmmss}";

            foreach (var (main, lines) in shoppingBaskets)
            {
                main.SetDetail("UniquePaymentNumber", uniquePaymentNumber);

                var invoiceNumberPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_InvoicenumberPropertyName");
                if (!String.IsNullOrWhiteSpace(invoiceNumberPropertyName))
                {
                    main.SetDetail(invoiceNumberPropertyName, invoiceNumber);
                }

                var languageCodePropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_LanguageCodePropertyName", "languagecode");
                if (!String.IsNullOrWhiteSpace(languageCodePropertyName))
                {
                    main.SetDetail(languageCodePropertyName, languagesService?.CurrentLanguageCode ?? "");
                }

                var pspPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_PspPropertyName", "psp");
                main.SetDetail(pspPropertyName, paymentServiceProvider.ToString("G"));
                await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
            }

            var setOrderToFinished = false;

            var paymentMethodsDirectToFinished = (await objectsService.FindSystemObjectByDomainNameAsync("PSP_paymentMethodsDirectToFinished")).Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (paymentMethodsDirectToFinished.Length > 0 && (paymentMethodsDirectToFinished.Contains(paymentMethod.ToString("G"), StringComparer.OrdinalIgnoreCase) || paymentMethodsDirectToFinished.Contains(paymentMethod.ToString("D"), StringComparer.Ordinal)))
            {
                setOrderToFinished = true;
            }

            // Check if the order is a test order.
            var isTestOrderPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_isTestOrderPropertyName", "istestorder");
            var isTestOrder = gclSettings.Environment.InList(Environments.Test, Environments.Development);
            var convertConceptOrderToOrder = false;

            foreach (var (main, lines) in shoppingBaskets)
            {
                main.SetDetail(isTestOrderPropertyName, isTestOrder.ToString());

                if (setOrderToFinished)
                {
                    if (paymentServiceProvider == PaymentServiceProviders.NoPsp)
                    {
                        convertConceptOrderToOrder = true;
                    }
                }
                else
                {
                    await shoppingBasketsService.SaveAsync(main, lines, basketSettings);
                }
            }

            // TODO: Add one-click checkout functionality.

            // Increment use count of redeemed coupons.
            foreach (var (main, lines) in shoppingBaskets)
            {
                foreach (var basketLine in shoppingBasketsService.GetLines(lines, "coupon"))
                {
                    var couponItemId = basketLine.GetDetailValue<ulong>("connecteditemid");
                    if (couponItemId == 0)
                    {
                        continue;
                    }

                    var couponItem = await wiserItemsService.GetItemDetailsAsync(couponItemId);
                    if (couponItem is not { Id: > 0 })
                    {
                        continue;
                    }

                    var totalBasketPrice = await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings, lineType: "product");
                    await shoppingBasketsService.UseCouponAsync(couponItem, totalBasketPrice);
                }
            }

            // TODO: Call "TransactionBeforeOut" site function.

            var skipPaymentWhenOrderAmountEqualsZero = (await objectsService.FindSystemObjectByDomainNameAsync("PSP_skipPaymentWhenOrderAmountEqualsZero")).InList("1", "true");
            if (skipPaymentWhenOrderAmountEqualsZero)
            {
                var totalPrice = 0M;
                foreach (var (main, lines) in shoppingBaskets)
                {
                    totalPrice += await shoppingBasketsService.GetPriceAsync(main, lines, basketSettings);
                }

                if (totalPrice == 0M)
                {
                    if (convertConceptOrderToOrder)
                    {
                        foreach (var (main, _) in shoppingBaskets)
                        {
                            await shoppingBasketsService.ConvertConceptOrderToOrderAsync(main, basketSettings);
                            // TODO: Call "TransactionFinished" site function.
                        }
                    }

                    return new PaymentRequestResult
                    {
                        Successful = true,
                        Action = PaymentRequestActions.Redirect,
                        ActionData = await objectsService.FindSystemObjectByDomainNameAsync("PSP_successURL")
                    };
                }
            }

            // Get the correct service based on name.
            var paymentServiceProviderService = paymentServiceProviderServiceFactory.GetPaymentServiceProviderService(paymentServiceProvider);
            paymentServiceProviderService.LogPaymentActions = (await objectsService.FindSystemObjectByDomainNameAsync("log_all_psp_requests")).Equals("true");

            if (setOrderToFinished)
            {
                await ProcessStatusUpdateAsync(shoppingBaskets, "Success", true, convertConceptOrderToOrder);
            }

            return await paymentServiceProviderService.HandlePaymentRequestAsync(shoppingBaskets, userDetails, paymentMethod, invoiceNumber);
        }

        /// <inheritdoc />
        public async Task<bool> ProcessStatusUpdateAsync(ICollection<(WiserItemModel Main, List<WiserItemModel> Lines)> shoppingBaskets, string newStatus, bool isSuccessfulStatus, bool convertConceptOrderToOrder = true)
        {
            var mailsToSendToUser = new List<SingleCommunicationModel>();
            var mailsToSendToMerchant = new List<SingleCommunicationModel>();
            var mailAttachment = await objectsService.FindSystemObjectByDomainNameAsync("PSP_mailAttachment");
            var paymentHistoryPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_PaymentHistoryPropertyName", "paymenthistory");

            //string invoiceNumber;

            //if (!String.IsNullOrWhiteSpace(shoppingBaskets.First().Main.GetDetailValue("UniquePaymentNumber")))
            //{
            //    invoiceNumber = shoppingBaskets.First().Main.GetDetailValue("UniquePaymentNumber");
            //}

            var basketSettings = await shoppingBasketsService.GetSettingsAsync();

            var orderEntityType = await objectsService.FindSystemObjectByDomainNameAsync("orderEntityType", "order");
            //var orderLineEntityType = await objectsService.FindSystemObjectByDomainNameAsync("orderLineEntityType", "orderline");

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
            var mailBodyPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_MailBodyPropertyName", "template");
            var mailSubjectPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_MailSubjectPropertyName", "subject");
            var mailToPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_MailToPropertyName", "mailto");
            var languageCodePropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_LanguageCodePropertyName", "languagecode");
            var emailAddressPropertyName = await GetCheckoutObjectValueAsync("CHECKOUT_EmailAddressPropertyName", "emailaddress");
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

            return new EmailValues
            {
                Content = templateContent,
                Subject = templateSubject,
                User = new CommunicationReceiverModel { Address = userEmailAddress },
                Merchant = new CommunicationReceiverModel { Address = merchantEmailAddress }
            };
        }

        /// <summary>
        /// Retrieves an object by key. If the result is empty, it will try again by prepending "W2" to the key name to check if a legacy key is set.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultResult"></param>
        /// <returns></returns>
        private async Task<string> GetCheckoutObjectValueAsync(string propertyName, string defaultResult = "")
        {
            var result = await objectsService.FindSystemObjectByDomainNameAsync(propertyName);
            if (String.IsNullOrEmpty(result))
            {
                result = await objectsService.FindSystemObjectByDomainNameAsync($"W2{propertyName}");
            }

            return String.IsNullOrEmpty(result) ? defaultResult : result;
        }
    }
}
