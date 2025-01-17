using System.Text.Json.Serialization;

namespace GeeksCoreLibrary.Modules.Communication.Models;

public class WhatsAppSendMessageRequestModel
{
    /// <summary>
    /// Gets or sets the messaging product (to whatsapp).
    /// </summary>
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; }

    /// <summary>
    /// Gets or sets the provider (to individual).
    /// </summary>
    [JsonPropertyName("recipient_type")]
    public string RecipientType { get; set; }

    /// <summary>
    /// Gets or sets the receiver's phone number.
    /// </summary>
    [JsonPropertyName("to")]
    public string Receiver { get; set; }

    /// <summary>
    /// Gets or sets the type of the message (text or document/video/audio/image).
    /// </summary>
    [JsonPropertyName("text")]
    public WhatsappBodyContentModel Body { get; set; }

    /// <summary>
    /// Gets or sets the type of the message when it is not text (document/video/audio/image).
    /// </summary>
    [JsonPropertyName("type")]
    public string TypeMessage { get; set; }

    /// <summary>
    /// Gets or sets the type to (image).
    /// </summary>
    [JsonPropertyName("image")]
    public AttachmentUrlsModel TypeUrlImage { get; set; }

    /// <summary>
    /// Gets or sets the type to (document).
    /// </summary>
    [JsonPropertyName("document")]
    public AttachmentUrlsModel TypeUrlDocument { get; set; }

    /// <summary>
    /// Gets or sets the type to (audio).
    /// </summary>
    [JsonPropertyName("audio")]
    public AttachmentUrlsModel TypeUrlAudio { get; set; }

    /// <summary>
    /// Gets or sets the type to (video).
    /// </summary>
    [JsonPropertyName("video")]
    public AttachmentUrlsModel TypeUrlVideo { get; set; }
}