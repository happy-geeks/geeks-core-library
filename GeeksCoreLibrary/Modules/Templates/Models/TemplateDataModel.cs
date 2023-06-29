using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class TemplateDataModel
    {
        /// <summary>
        /// Gets or sets the html content of the template.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the css content of the template.
        /// </summary>
        public string LinkedCss { get; set; }

        /// <summary>
        /// Gets or sets the js content of the template.
        /// </summary>
        public string LinkedJavascript { get; set; }

        /// <summary>
        /// Gets or sets a collection of URLs for CSS libraries, usually hosted on some CDN.
        /// </summary>
        public List<PageResource> ExternalCssFiles { get; init; } = new();

        /// <summary>
        /// Gets or sets a collection of URLs for JavaScript libraries, usually hosted on some CDN.
        /// </summary>
        public List<PageResource> ExternalJavaScriptFiles { get; init; } = new();
    }
}