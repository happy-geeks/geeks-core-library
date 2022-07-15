namespace GeeksCoreLibrary.Modules.Communication.Models.SmtPeter;

public class SmtPeterRquestAttachmentModel
{
    /// <summary>
    /// Gets or sets the Base64 encoded bytes of the file.
    /// </summary>
    public string Data { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string Name { get; set; }
}