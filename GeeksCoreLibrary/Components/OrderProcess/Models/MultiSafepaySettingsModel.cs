namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    public class MultiSafepaySettingsModel : PaymentServiceProviderSettingsModel
    {
        /// <summary>
        /// Gets or sets the API key for the current environment.
        /// </summary>
        public string ApiKey { get; set; }
    }
}