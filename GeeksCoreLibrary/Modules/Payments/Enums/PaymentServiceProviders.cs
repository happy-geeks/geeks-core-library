namespace GeeksCoreLibrary.Modules.Payments.Enums
{
    public enum PaymentServiceProviders
    {
        Unknown,
        NoPsp,
        Buckaroo,
        MultiSafepay,
        CM,
        /// <summary>
        /// This used to be called RaboOmnikassa, but Rabobank changed the name in 2022.
        /// </summary>
        RaboSmartPay,
        AfterPay,
        Mollie,
        PayNl,
        PayPal,
        XMoney,
        Dimoco
    }
}