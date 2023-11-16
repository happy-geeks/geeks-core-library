using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models
{
    public class PageCssModel
    {
        /// <summary>
        /// Gets or sets the list of external CSS files.
        /// </summary>
        public List<PageResourceModel> ExternalCss { get; set; } = new();

        /// <summary>
        /// Gets or sets the file name of the CSS for all pages, that is loaded via the header on top of the page.
        /// </summary>
        public string GeneralStandardCssFileName { get; set; }

        /// <summary>
        /// Gets or sets the CSS for all pages that should be loaded in the header as inline CSS.
        /// </summary>
        public string GeneralInlineHeadCss { get; set; }

        /// <summary>
        /// Gets or sets the CSS for all pages that should be loaded asynchronous at the bottom of the page.
        /// </summary>
        public string GeneralAsyncFooterCssFileName { get; set; }

        /// <summary>
        /// Gets or sets the CSS for all pages that should be synchronous sync at the bottom of the page.
        /// </summary>
        public string GeneralSyncFooterCssFileName { get; set; }

        /// <summary>
        /// Gets or sets the file name of the CSS for the current page, that is loaded via the header on top of the page.
        /// </summary>
        public string PageStandardCssFileName { get; set; }

        /// <summary>
        /// Gets or sets the CSS for the current page that should be loaded in the header as inline CSS.
        /// </summary>
        public string PageInlineHeadCss { get; set; }

        /// <summary>
        /// Gets or sets the CSS for the current page that should be loaded asynchronous at the bottom of the page.
        /// </summary>
        public string PageAsyncFooterCssFileName { get; set; }

        /// <summary>
        /// Gets or sets the CSS for the current page that should be synchronous sync at the bottom of the page.
        /// </summary>
        public string PageSyncFooterCssFileName { get; set; }
    }
}