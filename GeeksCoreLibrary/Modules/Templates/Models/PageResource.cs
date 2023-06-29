using System;

namespace GeeksCoreLibrary.Modules.Templates.Models;

public class PageResource
{
    /// <summary>
    /// Gets or sets the full (absolute) path to the file.
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the file.
    /// This will be added to the integrity attribute of the script or link tag.
    /// </summary>
    public string Hash { get; set; }
}