
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GeeksCoreLibrary.Modules.Communication.Models
{
    public class WhatsappBodyContentModel
    {
        /// <summary>
        /// Gets or sets the preview_url (to false whan sending a text message using whatsapp).
        /// </summary>
        [JsonPropertyName("preview_url")]
        public bool PreviewUrl { get; set; }

        /// <summary>
        /// Gets or sets text message content.
        /// </summary>
        [JsonPropertyName("body")]
        public string BodyContent { get; set; }
    }
}
