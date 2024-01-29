using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class PageJavascriptModel
    {
        /// <summary>
        /// Gets or sets the list of external javascript files.
        /// </summary>
        public List<JavaScriptResourceModel> ExternalJavascript { get; set; } = new();

        /// <summary>
        /// Gets or sets the javascript for all pages that should be loaded in the header as an URL.
        /// </summary>
        public string GeneralStandardJavaScriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript scripts for all pages that should be loaded in the header as inline scripts.
        /// </summary>
        public string GeneralInlineHeadJavaScript { get; set; }

        /// <summary>
        /// Gets or sets the file name of the javascript for the all pages that needs to be loaded asynchronous at the bottom of the page.
        /// </summary>
        public string GeneralAsyncFooterJavaScriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the file names of the javascript for all pages that needs to be loaded synchronous at the bottom of the page.
        /// This is a list instead of a single string due to the possibility of two different files being loaded in the footer.
        /// </summary>
        public List<string> GeneralSyncFooterJavaScriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the javascript for the current page that should be loaded in the header as an URL.
        /// </summary>
        public string PageStandardJavascriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript scripts for the current page that should be loaded in the header as inline scripts.
        /// </summary>
        public List<string> PageInlineHeadJavascript { get; set; }

        /// <summary>
        /// Gets or sets the file name of the javascript for the current page that needs to be loaded asynchronous at the bottom of the page.
        /// </summary>
        public string PageAsyncFooterJavascriptFileName { get; set; }
        
        /// <summary>
        /// Gets or sets the file name of the javascript for the current page that needs to be loaded synchronous at the bottom of the page.
        /// </summary>
        public string PageSyncFooterJavascriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the javascript for the current page that should be loaded in the body as inline script.
        /// </summary>
        public List<string> PagePluginInlineJavascriptSnippets { get; set; } = new();
    }
}
