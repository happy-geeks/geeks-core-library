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
        Vpay = 87
    }
}
