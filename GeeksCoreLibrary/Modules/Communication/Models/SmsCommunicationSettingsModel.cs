namespace GeeksCoreLibrary.Modules.Communication.Models;

/// <summary>
/// A model for SMS specific settings for communications (of the table wiser_communication).
/// </summary>
public class SmsCommunicationSettingsModel
{
    /// <summary>
    /// Gets or sets the contents of the SMS.
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// Gets or sets the selector for the phone number.
    /// This is basically a replacement variable to get the correct property from the data selector or query that contains the phone number.
    /// </summary>
    public string PhoneNumberSelector { get; set; }
}