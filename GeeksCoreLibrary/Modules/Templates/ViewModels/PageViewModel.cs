using GeeksCoreLibrary.Modules.Seo.Models;
using GeeksCoreLibrary.Modules.Templates.Models;

namespace GeeksCoreLibrary.Modules.Templates.ViewModels
{
    public class PageViewModel
    {
        /// <summary>
        /// Gets or sets the HTML of the body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets all the CSS for a web page.
        /// </summary>
        public PageCssModel Css { get; set; } = new();

        /// <summary>
        /// Gets or sets all the javascript for a web page.
        /// </summary>
        public PageJavascriptModel Javascript { get; set; } = new();

        /// <summary>
        /// Gets or sets all meta data for a web page.
        /// </summary>
        public PageMetaDataModel MetaData { get; set; } = new();

        /// <summary>
        /// Gets or sets Google Analytics scripts for the web site.
        /// </summary>
        public PageGoogleAnalyticsModel GoogleAnalytics { get; set; } = new();
    }
}
