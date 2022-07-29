using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Communication.Models.SmtPeter;

public class SmtPeterRequestModel
{
    /// <summary>
    /// Gets or sets the recipients the email is send to.
    /// </summary>
    public List<string> Recipients { get; set; }

    /// <summary>
    /// Gets or sets who the email is from.
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// Gets or sets to who the email is send.
    /// </summary>
    public List<string> To { get; set; }

    /// <summary>
    /// Gets or sets to who the email is send as CC.
    /// </summary>
    public List<string> Cc { get; set; }

    /// <summary>
    /// Gets or sets to who needs to be replied.
    /// </summary>
    public string ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the subject of the email.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the HTML body of the email.
    /// </summary>
    public string Html { get; set; }

    /// <summary>
    /// Gets or sets the attachments to ben send with the email.
    /// </summary>
    public List<SmtPeterRquestAttachmentModel> Attachments { get; set; }
}