using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class PageGoogleAnalyticsModel
    {
        /// <summary>
        /// Gets or sets external scripts that will be added in the head, as close to the opening head tag as possible.
        /// </summary>
        public List<JavaScriptResourceModel> HeadJavaScriptResources { get; set; } = new();

        /// <summary>
        /// Gets or sets inline scripts that will be added in the head, as close to the opening head tag as possible.
        /// </summary>
        public string InlineHeadJavaScript { get; set; }
        
        /// <summary>
        /// Gets or sets the HTML that is to be placed in a noscript tag. This will be placed as close to the opening body tag as possible,
        /// but will be placed underneath any other body script resources or inline body scripts.
        /// </summary>
        public string InlineBodyNoScript { get; set; }
    }
}
