using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Models;

public class SmsSettings
{
    /// <summary>
    /// Gets or sets the provider to handle the text message communication.
    /// </summary>
    public SmsServiceProviders Provider { get; set; }

    /// <summary>
    /// Gets or sets the ID to authenticate with the selected provider.
    /// </summary>
    public string ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the authentication token if the provider expects one.
    /// </summary>
    public string AuthenticationToken { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the sender to use if none has been provided with the <see cref="SingleCommunicationModel"/>.
    /// </summary>
    public string SenderPhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the sender to use if none has been provided with the <see cref="SingleCommunicationModel"/>.
    /// </summary>
    public string SenderName { get; set; }
}