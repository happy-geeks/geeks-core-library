namespace GeeksCoreLibrary.Components.Configurator.Models;

/// <summary>
/// Model that represents a rendered sub step.
/// </summary>
public class SubStepHtmlModel
{
    /// <summary>
    /// Gets or sets the Wiser item ID of the sub step.
    /// </summary>
    public ulong Id { get; init; }

    /// <summary>
    /// Gets or sets the Wiser item title of the sub step.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets or sets the rendered HTML of the sub step.
    /// </summary>
    public string Html { get; init; }
}