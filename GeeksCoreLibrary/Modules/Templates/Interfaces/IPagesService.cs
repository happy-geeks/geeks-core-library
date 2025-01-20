using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace GeeksCoreLibrary.Modules.Templates.Interfaces;

public interface IPagesService
{
    /// <summary>
    /// Get the rendered template. Must supply either an ID or a name.
    /// </summary>
    /// <param name="id">Optional: The ID of the template to get.</param>
    /// <param name="name">Optional: The name of the template to get.</param>
    /// <param name="type">Optional: The type of template that is being searched for. Only used in combination with name. Default value is null, which is all template types.</param>
    /// <param name="parentId">Optional: The ID of the parent of the template to get.</param>
    /// <param name="parentName">Optional: The name of the parent of template to get.</param>
    /// <param name="skipPermissions">Optional: Whether to skip the check if the user has the permissions to see the template</param>
    /// <param name="templateContent">The content of the template to use instead of retrieving it. If a value has been given, no replacements will be done.</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The template, with all replacements done.</returns>
    Task<Template> GetRenderedTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool skipPermissions = false, string templateContent = null, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Gets the header template from Wiser that should be loaded on every page.
    /// </summary>
    /// <param name="url">The current URL of the page, to check the header regex and see if the header might need to be skipped for this page.</param>
    /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the header, will be added to this list.</param>
    /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the header, will be added to this list.</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The string with the entire header HTML.</returns>
    Task<string> GetGlobalHeader(string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Gets the header template from Wiser that should be loaded on every page.
    /// </summary>
    /// <param name="service">The <see cref="IPagesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetRenderedTemplateAsync() in this method.</param>
    /// <param name="url">The current URL of the page, to check the header regex and see if the header might need to be skipped for this page.</param>
    /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the header, will be added to this list.</param>
    /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the header, will be added to this list.</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The string with the entire header HTML.</returns>
    Task<string> GetGlobalHeader(IPagesService service, string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Gets the footer template from Wiser that should be loaded on every page.
    /// </summary>
    /// <param name="url">The current URL of the page, to check the footer regex and see if the footer might need to be skipped for this page.</param>
    /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the footer, will be added to this list.</param>
    /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the footer, will be added to this list.</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The string with the entire footer HTML.</returns>
    Task<string> GetGlobalFooter(string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Gets the footer template from Wiser that should be loaded on every page.
    /// </summary>
    /// <param name="service">The <see cref="IPagesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetRenderedTemplateAsync() in this method.</param>
    /// <param name="url">The current URL of the page, to check the footer regex and see if the footer might need to be skipped for this page.</param>
    /// <param name="javascriptTemplates">A list of javascript templates. Any javascript templates that are linked to the footer, will be added to this list.</param>
    /// <param name="cssTemplates">A list of css templates. Any css templates that are linked to the footer, will be added to this list.</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The string with the entire footer HTML.</returns>
    Task<string> GetGlobalFooter(IPagesService service, string url, List<int> javascriptTemplates, List<int> cssTemplates, bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Creates a <see cref="PageViewModel"/> to use in views for building the HTML for the page.
    /// </summary>
    /// <param name="externalCss">A list with external CSS that should be added to this page.</param>
    /// <param name="cssTemplates">A list with IDs of (S)CSS templates, from the template module of Wiser, that should be added to this page.</param>
    /// <param name="externalJavascript">A list with external javascript that should be added to this page.</param>
    /// <param name="javascriptTemplates">A list with IDs of javascript templates, from the template module of Wiser, that should be added to this page.</param>
    /// <param name="bodyHtml">The complete HTML of the body of this page.</param>
    /// <param name="templateId">Optional: The ID of the template that is used for this page. Leave empty if you're creating a view model for something that is not a template from the Wiser template module.</param>
    /// <param name="useGeneralLayout">Optional: Whether the page should use the website's general layout.</param>
    /// <returns>The model for the view.</returns>
    Task<PageViewModel> CreatePageViewModelAsync(List<PageResourceModel> externalCss, List<int> cssTemplates, List<PageResourceModel> externalJavascript, List<int> javascriptTemplates, string bodyHtml, int templateId = 0, bool useGeneralLayout = true);

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