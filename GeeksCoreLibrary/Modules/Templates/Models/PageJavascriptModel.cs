using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class PageJavascriptModel
    {
        /// <summary>
        /// Gets or sets the list of external javascript files.
        /// </summary>
        public List<JavaScriptResource> ExternalJavascript { get; set; } = new();

        /// <summary>
        /// Gets or sets the file name of the general javascript from Wiser that needs to be loaded in the header on the top of the page.
        /// </summary>
        public string GeneralJavascriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the javascript for the current page that should be loaded in the header as an URL.
        /// </summary>
        public string PageStandardJavascriptFileName { get; set; }

        /// <summary>
        /// Gets or sets the JavaScript scripts for the current page that should be loaded in the header as inline scripts.
        /// </summary>
        public List<string> PageInlineHeadJavascript { get; set; }

        /// <summary>
        /// Gets or sets the file name of the general javascript from Wiser that needs to be loaded on the bottom of the page.
        /// </summary>
        public string GeneralFooterJavascriptFileName { get; set; }

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
