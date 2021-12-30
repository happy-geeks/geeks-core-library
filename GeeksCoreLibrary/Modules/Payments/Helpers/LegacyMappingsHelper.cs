using GeeksCoreLibrary.Modules.Payments.Enums;
using System;

namespace GeeksCoreLibrary.Modules.Payments.Helpers
{
    public static class LegacyMappingsHelper
    {
        public static PaymentServiceProviders GetPaymentServiceProviderByLegacyName(string legacyName)
        {
            return legacyName switch
            {
                "BUCK" => PaymentServiceProviders.Buckaroo,
                "MSP" => PaymentServiceProviders.MultiSafepay,
                "CM" => PaymentServiceProviders.CM,
                _ => PaymentServiceProviders.Unknown
            };
        }

        public static PaymentMethods GetPaymentMethodByLegacyName(string legacyName)
        {
            return legacyName switch
            {
                "10" => PaymentMethods.Ideal,
                "51" => PaymentMethods.Visa,
                "52" => PaymentMethods.Mastercard,
                "70" => PaymentMethods.PayPal,
                "77" => PaymentMethods.Bancontact,
                _ => PaymentMethods.Unknown
            };
        }

        public static string GetBuckarooIssuer(string issuerValue)
        {
            return issuerValue switch
            {
                "1" => BuckarooSdk.Services.Ideal.Constants.Issuers.AbnAmro,
                "2" => BuckarooSdk.Services.Ideal.Constants.Issuers.AsnBank,
                "5" => BuckarooSdk.Services.Ideal.Constants.Issuers.IngBank,
                "6" => BuckarooSdk.Services.Ideal.Constants.Issuers.RaboBank,
                "7" => BuckarooSdk.Services.Ideal.Constants.Issuers.SnsBank,
                "9" => BuckarooSdk.Services.Ideal.Constants.Issuers.TriodosBank,
                "10" => BuckarooSdk.Services.Ideal.Constants.Issuers.VanLanschot,
                "11" => BuckarooSdk.Services.Ideal.Constants.Issuers.Knab,
                "12" => BuckarooSdk.Services.Ideal.Constants.Issuers.Bunq,
                _ => null
            };
        }
    }
}
