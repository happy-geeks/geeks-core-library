namespace GeeksCoreLibrary.Components.OrderProcess.Models
{
    public class RaboOmniKassaSettingsModel : PaymentServiceProviderSettingsModel
    {
        /// <summary>
        /// Gets or sets the refresh token for the current environment.
        /// </summary>
        public string RefreshToken { get; set; }


        /// <summary>
        /// Gets or sets the signing key for the current environment.
        /// </summary>
        public string SigningKey { get; set; }
    }
}