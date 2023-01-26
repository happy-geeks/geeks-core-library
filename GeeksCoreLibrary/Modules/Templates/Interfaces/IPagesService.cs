using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace GeeksCoreLibrary.Modules.Templates.Interfaces
{
    public interface IPagesService
    {
        /// <summary>
        /// Gets the header template from Wiser that should be loaded on every page.
        /// </summary>
        /// <param name="url">The current URL of the page, to check the header regex and see if the header might need to be skipped for this page.</param>
        /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the header, will be added to this list.</param>
        /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the header, will be added to this list.</param>
        /// <returns>The string with the entire header HTML.</returns>
        Task<string> GetGlobalHeader(string url, List<int> javascriptTemplates, List<int> cssTemplates);
        
        /// <summary>
        /// Gets the footer template from Wiser that should be loaded on every page.
        /// </summary>
        /// <param name="url">The current URL of the page, to check the footer regex and see if the footer might need to be skipped for this page.</param>
        /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the footer, will be added to this list.</param>
        /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the footer, will be added to this list.</param>
        /// <returns>The string with the entire footer HTML.</returns>
        Task<string> GetGlobalFooter(string url, List<int> javascriptTemplates, List<int> cssTemplates);

        /// <summary>
        /// Creates a <see cref="PageViewModel"/> to use in views for building the HTML for the page.
        /// </summary>
        /// <param name="externalCss">A list with external CSS that should be added to this page.</param>
        /// <param name="cssTemplates">A list with IDs of (S)CSS templates, from the template module of Wiser, that should be added to this page.</param>
        /// <param name="externalJavascript">A list with external javascript that should be added to this page.</param>
        /// <param name="javascriptTemplates">A list with IDs of javascript templates, from the template module of Wiser, that should be added to this page.</param>
        /// <param name="bodyHtml">The complete HTML of the body of this page.</param>
        /// <param name="templateId">Optional: The ID of the template that is used for this page. Leave empty if you're creating a view model for something that is not a template from the Wiser template module.</param>
        /// <returns>The model for the view.</returns>
        Task<PageViewModel> CreatePageViewModelAsync(List<string> externalCss, List<int> cssTemplates, List<string> externalJavascript, List<int> javascriptTemplates, string bodyHtml, int templateId = 0);
        
        /// <summary>
        /// Sets the SEO meta data for the current page.
        /// </summary>
        void SetPageSeoData(string seoTitle = null, string seoDescription = null, string seoKeyWords = null, string seoCanonical = null, bool noIndex = false, bool noFollow = false, IEnumerable<string> robots = null, string previousPageLink = null, string nextPageLink = null);

        /// <summary>
        /// Sets the Open Graph meta data for the current page.
        /// </summary>
        /// <param name="openGraphValues">Dictionary with all Open Graph values. Keys must start with "opengraph_" or they will be ignored.</param>
        void SetOpenGraphData(IDictionary<string, string> openGraphValues);
    }
}
