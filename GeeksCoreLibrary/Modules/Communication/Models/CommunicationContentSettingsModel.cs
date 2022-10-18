using GeeksCoreLibrary.Modules.Communication.Enums;

namespace GeeksCoreLibrary.Modules.Communication.Models;

/// <summary>
/// A model for content specific settings for communications (of the table wiser_communication).
/// </summary>
public class CommunicationContentSettingsModel
{
    /// <summary>
    /// Gets or sets the type of communication (ie E-mail, SMS etc).
    /// </summary>
    public CommunicationTypes Type { get; set; }
    
    /// <summary>
    /// Gets or sets the contents of the SMS/email/etc.
    /// </summary>
    public string Content { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the e-mail template (this should be a Wiser item).
    /// </summary>
    public ulong TemplateId { get; set; }
    
    /// <summary>
    /// Gets or sets the subject of the e-mail.
    /// </summary>
    public string Subject { get; set; }
    
    /// <summary>
    /// Gets or sets the selector for the receiver.
    /// This is basically a replacement variable to get the correct property from the data selector or query that contains the e-mail address / phone number etc.
    /// </summary>
    public string Selector { get; set; }
}