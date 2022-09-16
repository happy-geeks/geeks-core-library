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
        /// Creates a <see cref="PageViewModel"/>.
        /// </summary>
        /// <param name="externalCss"></param>
        /// <param name="cssTemplates"></param>
        /// <param name="externalJavascript"></param>
        /// <param name="javascriptTemplates"></param>
        /// <param name="newBodyHtml"></param>
        /// <returns></returns>
        Task<PageViewModel> CreatePageViewModelAsync(List<string> externalCss, List<int> cssTemplates, List<string> externalJavascript, List<int> javascriptTemplates, string newBodyHtml);
        
        /// <summary>
        /// Sets the SEO meta data for the current page.
        /// </summary>
        void SetPageSeoData(string seoTitle = null, string seoDescription = null, string seoKeyWords = null, string seoCanonical = null, bool noIndex = false, bool noFollow = false, IEnumerable<string> robots = null);

        /// <summary>
        /// Sets the Open Graph meta data for the current page.
        /// </summary>
        /// <param name="openGraphValues">Dictionary with all Open Graph values. Keys must start with "opengraph_" or they will be ignored.</param>
        void SetOpenGraphData(IDictionary<string, string> openGraphValues);
    }
}
