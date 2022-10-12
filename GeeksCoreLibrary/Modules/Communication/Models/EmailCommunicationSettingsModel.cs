namespace GeeksCoreLibrary.Modules.Communication.Models;

/// <summary>
/// A model for e-mail specific settings for communications (of the table wiser_communication).
/// </summary>
public class EmailCommunicationSettingsModel
{
    /// <summary>
    /// Gets or sets the ID of the e-mail template (this should be a Wiser item).
    /// </summary>
    public ulong TemplateId { get; set; }
    
    /// <summary>
    /// Gets or sets the subject of the e-mail.
    /// </summary>
    public string Subject { get; set; }
    
    /// <summary>
    /// Gets or sets the body of the e-mail.
    /// </summary>
    public string Body { get; set; }
    
    /// <summary>
    /// Gets or sets the selector for the e-mail address.
    /// This is basically a replacement variable to get the correct property from the data selector or query that contains the e-mail address.
    /// </summary>
    public string EmailAddressSelector { get; set; }
}