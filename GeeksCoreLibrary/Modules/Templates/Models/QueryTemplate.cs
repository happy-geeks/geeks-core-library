namespace GeeksCoreLibrary.Modules.Templates.Models;

public class QueryTemplate : Template
{
    public QueryGroupingSettings GroupingSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets if this query template is used for URL rewrites or redirects.
    /// </summary>
    public bool UsedForRedirect { get; set; } = false;
}