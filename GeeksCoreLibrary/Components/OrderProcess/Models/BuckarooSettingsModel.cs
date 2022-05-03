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
    }
}