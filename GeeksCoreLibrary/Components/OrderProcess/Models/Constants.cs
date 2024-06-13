namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    /// <summary>
    /// Constants that are used in the order/checkout process.
    /// </summary>
    public class Constants
    {
        internal const string ComponentIdFormKey = "__orderProcess";

        #region Pages

        public const string CheckoutPage = "orderProcessCheckout.gcl";

        public const string PaymentInPage = "orderProcessPaymentIn.gcl";

        public const string PaymentOutPage = "orderProcessPaymentOut.gcl";

        public const string PaymentReturnPage = "orderProcessPaymentReturn.gcl";
        
        public const string DirectPaymentInPage = "directPaymentIn.gcl";

        public const string DirectPaymentOutPage = "directPaymentOut.gcl";

        public const string DirectPaymentReturnPage = "directPaymentReturn.gcl";

        public const string DownloadInvoicePage = "/orders/invoice/";

        #endregion

        #region Entity types

        public const string OrderProcessEntityType = "WiserOrderProcess";

        public const string StepEntityType = "WiserOrderProcessStep";

        public const string GroupEntityType = "WiserOrderProcessGroup";

        public const string FormFieldEntityType = "WiserFormField";

        public const string PaymentServiceProviderEntityType = "WiserPaymentProvider";

        public const string PaymentMethodEntityType = "WiserPaymentmethod";

        public const string ConceptOrderEntityType = "conceptorder";

        public const string OrderEntityType = "order";

        public const string OrderLineEntityType = "orderline";

        #endregion

        #region Link types

        public const int StepToProcessLinkType = 5070;

        public const int GroupToStepLinkType = 5071;

        public const int FieldToGroupLinkType = 5072;

        public const int PaymentMethodToOrderProcessLinkType = 5060;

        #endregion

        #region Fields for order process settings

        public const string OrderProcessUrlProperty = "orderprocessurl";

        public const string OrderProcessEmailAddressFieldProperty = "orderprocessemailaddressfield";

        public const string OrderProcessMerchantEmailAddressFieldProperty = "orderprocessmerchantemailaddressfield";

        public const string OrderProcessStatusUpdateTemplateProperty = "orderprocessstatusupdatetemplate";

        public const string OrderProcessStatusUpdateWebShopTemplateProperty = "orderprocessstatusupdatewebshoptemplate";

        public const string OrderProcessClearBasketOnConfirmationPageProperty = "orderprocessclearbasketonconfirmationpage";

        public const string OrderProcessStatusUpdateAttachmentTemplateProperty = "orderprocessinvoicetemplate";

        public const string OrderProcessHeaderProperty = "orderprocessheader";

        public const string OrderProcessFooterProperty = "orderprocessfooter";

        public const string OrderProcessTemplateProperty = "orderprocesshtmltemplate";

        public const string StepTypeProperty = "orderprocesssteptype";

        public const string StepTemplateProperty = "orderprocesssteptemplate";

        public const string StepHeaderProperty = "orderprocessstepheader";

        public const string StepFooterProperty = "orderprocessstepfooter";

        public const string StepConfirmButtonTextProperty = "orderprocessstepconfirmbuttontext";

        public const string StepPreviousStepLinkTextProperty = "orderprocessstepprevioussteplinktext";

        public const string StepRedirectUrlProperty = "orderprocessstepredirecturl";

        public const string StepHideInProgressProperty = "orderprocessstephideinprogress";

        public const string GroupTypeProperty = "orderprocessgrouptype";

        public const string GroupHeaderProperty = "orderprocessgroupheader";

        public const string GroupFooterProperty = "orderprocessgroupfooter";

        public const string GroupCssClassProperty = "orderprocessgroupcssclass";

        public const string FieldIdProperty = "formfieldid";

        public const string FieldLabelProperty = "formfieldlabel";

        public const string FieldPlaceholderProperty = "formfieldplaceholder";

        public const string FieldTypeProperty = "formfieldtype";

        public const string FieldInputTypeProperty = "formfieldinputtype";

        public const string FieldValuesGroupName = "formfieldvalues";

        public const string FieldMandatoryProperty = "formfieldmandatory";

        public const string FieldValidationPatternProperty = "formfieldregexcheck";

        public const string FieldVisibilityProperty = "formfieldvisible";

        public const string FieldErrorMessageProperty = "formfielderrormessage";

        public const string FieldCssClassProperty = "formfieldcssclass";

        public const string FieldSaveToProperty = "formfieldsaveto";

        public const string FieldRequiresUniqueValueProperty = "formfieldrequireuniquevalue";

        public const string FieldTabIndexProperty = "formfieldtabindex";

        public const string PaymentServiceProviderTypeProperty = "psptype";

        public const string PaymentServiceProviderLogAllRequestsProperty = "psplogallrequests";

        public const string PaymentServiceProviderOrdersCanBeSetDirectoryToFinishedProperty = "psporderscanbesetdirectlytofinished";

        public const string PaymentServiceProviderSkipWhenOrderAmountEqualsZeroProperty = "pspkippaymentwhenorderamountequalszero";
        
        public const string PaymentServiceProviderSuccessUrlProperty = "success_url";

        public const string PaymentServiceProviderFailUrlProperty = "fail_url";
        
        public const string PaymentServiceProviderPendingUrlProperty = "pending_url";
        
        public const string PaymentMethodServiceProviderProperty = "paymentmethodprovider";

        public const string PaymentMethodFeeProperty = "paymentmethodfee";

        public const string PaymentMethodVisibilityProperty = "paymentmethodvisible";

        public const string PaymentMethodLogoProperty = "paymentmethodlogo";

        public const string PaymentMethodProperty = "paymentMethod";

        public const string PaymentMethodExternalNameProperty = "paymentmethodexternalname";

        public const string PaymentMethodMinimalAmountProperty = "paymentmethodminimalamount";

        public const string PaymentMethodMaximumAmountProperty = "paymentmethodmaximumamount";

        public const string PaymentMethodUseMinimalAmountProperty = "paymentmethoduseminimalamount";

        public const string PaymentMethodUseMaximumAmountProperty = "paymentmethodusemaximumamount";

        public const string OrderProcessBasketToConceptOrderMethodProperty = "baskettoconceptordermethod";

        public const string MeasurementProtocolActiveProperty = "measurementprotocolactive";

        public const string MeasurementProtocolItemJsonProperty = "measurementprotocolitemjson";

        public const string MeasurementProtocolBeginCheckoutJsonProperty = "measurementprotocolbegincheckoutjson";

        public const string MeasurementProtocolAddPaymentInfoJsonProperty = "measurementprotocoladdpaymentinfojson";

        public const string MeasurementProtocolPurchaseJsonProperty = "measurementprotocolpurchasejson";

        public const string MeasurementProtocolMeasurementIdProperty = "measurementprotocolmeasurementid";

        public const string MeasurementProtocolApiSecretProperty = "measurementprotocolapisecret";

        #endregion

        #region Fields for basket and orders

        public const string PaymentMethodNameProperty = "PaymentMethodName";

        public const string PaymentProviderProperty = "PaymentProvider";

        public const string PaymentMethodIssuerProperty = "PaymentMethodIssuer";

        public const string PaymentProviderNameProperty = "paymentProviderName";

        public const string UniquePaymentNumberProperty = "UniquePaymentNumber";

        public const string InvoiceNumberProperty = "InvoiceNumber";

        public const string LanguageCodeProperty = "LanguageCode";

        public const string CountryCodeProperty = "CountryCode";

        public const string IsTestOrderProperty = "IsTestOrder";

        public const string PaymentHistoryProperty = "PaymentHistory";

        public const string PaymentProviderTransactionId = "PspTransactionId";

        public const string PaymentProviderTransactionStatus = "PspTransactionStatus";

        public const string ShippingMethodProperty = "ShippingMethod";

        public const string StatusUpdateMailAttachmentProperty = "MailAttachmentTemplate";

        public const string StatusUpdateMailWebShopProperty = "MailToWebShopTemplate";

        public const string StatusUpdateMailToConsumerProperty = "MailToConsumerTemplate";

        public const string PaymentCompleteProperty = "PaymentComplete";

        public const string InvoiceHtmlProperty = "InvoiceHtml";

        public const string InvoicePdfProperty = "InvoicePdf";

        public const string EmailAddressProperty = "email";

        public const string PhoneNumberProperty = "phone";

        public const string ImageUrlProperty = "ImageUrl";

        public const string DescriptionProperty = "Description";

        #endregion

        #region Fields for mail/pdf templates

        public const string MailTemplateBodyProperty = "template";

        public const string MailTemplateSubjectProperty = "subject";

        public const string MailTemplateToAddressProperty = "mailto";

        public const string MailTemplateSenderEmailProperty = "sender_email";

        public const string MailTemplateSenderNameProperty = "sender_name";

        public const string MailTemplateBccProperty = "bcc";

        public const string MailTemplateReplyToAddressProperty = "replyto_email";

        public const string MailTemplateReplyToNameProperty = "replyto_name";

        public const string PdfTemplateFileNameProperty = "file_name";

        public const string PdfTemplateOrientationProperty = "orientation";

        #endregion

        #region Request values

        public const string ActiveStepRequestKey = "activeStep";

        public const string ErrorFromPaymentOutRequestKey = "errorFromPaymentOut";

        public const string OrderProcessIdRequestKey = "orderProcessId";

        public const string SelectedPaymentMethodRequestKey = "paymentMethodId";

        public const string OrderIdRequestKey = "orderId";

        #endregion

        #region Replacements

        public const string ErrorReplacement = "{error}";

        public const string ErrorTypeReplacement = "{errorType}";

        public const string ErrorMessageReplacement = "{errorMessage}";

        public const string ErrorClassReplacement = "{errorClass}";

        public const string ProgressReplacement = "{progress}";

        public const string HeaderReplacement = "{header}";

        public const string FooterReplacement = "{footer}";

        public const string StepsReplacement = "{steps}";

        public const string StepReplacement = "{step}";

        public const string GroupsReplacement = "{groups}";

        public const string FieldsReplacement = "{fields}";

        public const string PaymentMethodsReplacement = "{paymentMethods}";

        public const string FieldOptionsReplacement = "{options}";

        #endregion

        #region Template names

        public const string InvoiceNumberQueryTemplate = "InvoiceNumberQuery";

        #endregion

        #region Order line types

        public const string OrderLineCouponType = "coupon";

        public const string OrderLineProductType = "product";

        #endregion
    }
}