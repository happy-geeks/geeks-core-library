using GeeksCoreLibrary.Modules.Templates.Enums;

namespace GeeksCoreLibrary.Modules.Templates.Models;

/// <summary>
/// A custom HTML snippet that can contain just HTML, or javascript or CSS.
/// These snippets can be added globally for all pages via the module "Master data" (Stamgegevens), or per page via the template module. 
/// </summary>
public class PageWidgetModel
{
    /// <summary>
    /// Gets or sets the location of where the snippet should be added to the page.
    /// </summary>
    public PageWidgetLocations Location { get; set; }
    
    /// <summary>
    /// Gets or sets the HTML of the snippet.
    /// </summary>
    public string Html { get; set; }
}