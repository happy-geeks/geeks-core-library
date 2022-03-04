namespace GeeksCoreLibrary.Modules.Payments.Enums
{
    public enum PaymentMethods
    {
        Unknown = 0,
        /// <summary>
        /// Dutch online payment method supported by most banks and most payment service providers.
        /// </summary>
        Ideal = 10,
        /// <summary>
        /// The ordered goods will be paid on delivery.
        /// </summary>
        CashOnDelivery = 20,
        /// <summary>
        /// Credit card.
        /// </summary>
        Mastercard = 52,
        /// <summary>
        /// Worldwide payments system that supports online money transfers.
        /// </summary>
        PayPal = 70,
        /// <summary>
        /// Credit card.
        /// </summary>
        Visa = 51,
        /// <summary>
        /// Bancontact, for Belgian customers.
        /// </summary>
        Bancontact = 77,
        /// <summary>
        /// Payment will be handled afterwards when the retailer sends the user an invoice.
        /// </summary>
        OnInvoice = 80,
        /// <summary>
        /// The good will be paid in cash.
        /// </summary>
        Cash = 90,
        /// <summary>
        /// The goods will be paid for using a debit card.
        /// </summary>
        DebitCard = 91,
        /// <summary>
        /// The goods will be paid with Sofort Banking.
        /// </summary>
        SofortBanking = 78,
        /// <summary>
        /// Giropay, for German customers.
        /// </summary>
        Giropay = 11,
        /// <summary>
        /// Electronic Payment Standard for Austrian customers.
        /// </summary>
        EPS = 128,
        /// <summary>
        /// The goods will be paid with Trustly.
        /// </summary>
        Trustly = 200,
        /// <summary>
        /// The customer pays with their Apple wallet.
        /// </summary>
        ApplePay = 134,
        /// <summary>
        /// Payment will be wired manually by the customer.
        /// </summary>
        WireTransfer = 201,
        /// <summary>
        /// The goods will be paid later.
        /// </summary>
        Afterpay = 75,
        /// <summary>
        /// The goods will be paid with a maestro debit card or a prepaid card.
        /// </summary>
        Maestro = 122,
        /// <summary>
        /// The goods will be paid with a Single Euro Payments Area debit card.
        /// </summary>
        Vpay = 87,
        /// <summary>
        /// Flexible payments where the shopper can control the amount they are willing to pay per month (with a minimum that is decided by the PSP).
        /// This is used in AfterPay (and possibly others in the future).
        /// </summary>
        FlexPayment = 202,
        /// <summary>
        /// The customer will pay a fixed amount per month, for a duration of X months.
        /// This is used in AfterPay (and possibly others in the future).
        /// </summary>
        FixedInstallments = 203,
        /// <summary>
        /// Providing convenience for shoppers who frequently make small purchases, AfterPay can consolidate all these purchases into a single invoice.
        /// The most common use case is a monthly invoice, which is typically suitable for transportation tickets or digital streaming services, but the frequency of the invoicing can be agreed separately.
        /// While the actual purchases are made on different dates, the payment terms of 14 days start from the date on which the consolidated invoice is issued.
        /// </summary>
        ConsolidatedInvoice = 204,
        /// <summary>
        /// During peak seasons, such as Christmas, merchants may use campaign invoicing, which allows shoppers extended payment terms per purchase or a fixed due date.
        /// AfterPay offers standard campaigns depending on the season. Merchants are able to set up specific campaigns through separate agreements. All campaigns can be retrieved by calling the AfterPay API.
        /// For example, Christmas campaign A would be displayed at checkout as “Buy now, pay after Christmas” and payment is due by January 31, regardless of the actual purchase date.
        /// </summary>
        CampaignInvoice = 205,
        /// <summary>
        /// The goods will be paid through Belfius Pay Button, an online payment method of Belfius.
        /// </summary>
        Belfius = 206,
        /// <summary>
        /// The goods will be paid through KBC Payment Button, an online payment method of KBC.
        /// </summary>
        KBC = 207,
        /// <summary>
        /// The goods will be paid using a gift card.
        /// </summary>
        GiftCard = 12,
        /// <summary>
        /// The goods will be paid through Direct Debit (like SEPA Direct Debit).
        /// </summary>
        DirectDebit = 31,
        /// <summary>
        /// Przelewy24, for Polish customers.
        /// </summary>
        Przelewy24 = 208
    }
}
