namespace GeeksCoreLibrary.Modules.Templates.Models;

public class DynamicContent
{
    /// <summary>
    /// Gets or sets the content ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the dynamic content.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the title of the component that was set in Wiser.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the full settings JSON of the dynamic content.
    /// </summary>
    public string SettingsJson { get; set; }

    /// <summary>
    /// Gets or sets the component mode the component of the dynamic content should be running in.
    /// </summary>
    public string ComponentMode { get; set; }

    /// <summary>
    /// Gets or sets the current version of the dynamic content.
    /// </summary>
    public int Version { get; set; }
}