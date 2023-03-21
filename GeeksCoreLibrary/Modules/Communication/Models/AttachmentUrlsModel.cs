using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GeeksCoreLibrary.Modules.Communication.Models
{
    public class AttachmentUrlsModel
    {
        /// <summary>
        /// Gets or sets the url of the attachment.
        /// </summary>
        [JsonPropertyName("link")]
        public string Url { get; set; }
    }
}
