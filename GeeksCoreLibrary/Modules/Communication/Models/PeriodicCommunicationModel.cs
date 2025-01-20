namespace GeeksCoreLibrary.Modules.Communication.Models;

public class PeriodicCommunicationModel
{
    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name/description for the period communication.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ID of the data selector that gets the receivers for this communication.
    /// This is optional, you can also save a hard-coded list of receivers in <see cref="ReceiverList"/>.
    /// </summary>
    public int ReceiverSelectionId { get; set; }

    /// <summary>
    /// Gets or sets the list of receivers. This should be a serialized JSON array.
    /// This is optional, you can also save an ID for a data selector in <see cref="ReceiverSelectionId"/>.
    /// TODO: Figure out how exactly this should look and document it and create a model for it.
    /// </summary>
    public string ReceiverList { get; set; }

    /// <summary>
    /// Gets or sets whether to send this communication via e-mail.
    /// </summary>
    public bool SendEmail { get; set; }

    /// <summary>
    /// Gets or sets the ID of the template for the body of the e-mail.
    /// Only used if <see cref="SendEmail"/> is set to <see langword="true" />.
    /// </summary>
    public ulong EmailTemplateId { get; set; }

    // TODO: Add the rest of the properties from wiser_communication.
}