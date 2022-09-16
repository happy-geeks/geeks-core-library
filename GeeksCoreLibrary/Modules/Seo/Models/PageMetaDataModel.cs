using System.Collections.Generic;

namespace GeeksCoreLibrary.Modules.Seo.Models
{
    public class PageMetaDataModel
    {
        /// <summary>
        /// Gets or sets the title of the page.
        /// </summary>
        public string PageTitle { get; set; }

        /// <summary>
        /// Gets or sets the global page title suffix, which is a suffix that gets appended to the page's own title.
        /// </summary>
        public string GlobalPageTitleSuffix { get; set; }

        /// <summary>
        /// Gets or sets all standard meta tags with name and content for a page.
        /// </summary>
        public Dictionary<string, string> MetaTags { get; set; } = new();

        /// <summary>
        /// Gets or sets the Open Graph meta tags for a page. Note: Keys in this dictionary should not start with "og:" otherwise the property
        /// attribute in the meta tag will start with "og:og:".
        /// </summary>
        public Dictionary<string, string> OpenGraphMetaTags { get; set; } = new();

        /// <summary>
        /// Gets or sets the canonical URL.
        /// </summary>
        public string Canonical { get; set; }

        /// <summary>
        /// Gets or sets the SEO text for the current page. This is HTML that can be added to the body HTML.
        /// If the body HTML contains a variable like '\[{seomodule_content}\|(.*?)\]', then that will be replaced with this text.
        /// </summary>
        public string SeoText { get; set; }
        
        /// <summary>
        /// Gets or sets the H1 text for the current page. This is HTML that can be added to the body HTML.
        /// If the body HTML contains a variable like '\[{seomodule_h1header}\|(.*?)\]', then that will be replaced with this text.
        /// </summary>
        public string H1Text { get; set; }
        
        /// <summary>
        /// Gets or sets the H2 text for the current page. This is HTML that can be added to the body HTML.
        /// If the body HTML contains a variable like '\[{seomodule_h2header}\|(.*?)\]', then that will be replaced with this text.
        /// </summary>
        public string H2Text { get; set; }
        
        /// <summary>
        /// Gets or sets the H3 text for the current page. This is HTML that can be added to the body HTML.
        /// If the body HTML contains a variable like '\[{seomodule_h3header}\|(.*?)\]', then that will be replaced with this text.
        /// </summary>
        public string H3Text { get; set; }
    }
}
