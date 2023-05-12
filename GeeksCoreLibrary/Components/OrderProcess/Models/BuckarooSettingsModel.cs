using GeeksCoreLibrary.Modules.Payments.Enums.Buckaroo;

namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    public class BuckarooSettingsModel : PaymentServiceProviderSettingsModel
    {
        /// <summary>
        /// Gets or sets the website key for the current environment.
        /// </summary>
        public string WebsiteKey { get; set; }

        /// <summary>
        /// Gets or sets the secret key for the current environment.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the push content type Buckaroo will use to send push requests.
        /// </summary>
        public PushContentTypes PushContentType { get; set; }

        /// <summary>
        /// Gets or sets the hash method that is used to sign the push request. This is only used when the push content type is set to <see cref="PushContentTypes.HttpPost"/>.
        /// </summary>
        public HashMethods HashMethod { get; set; }
    }
}