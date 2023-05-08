namespace GeeksCoreLibrary.Modules.Templates.Enums;

/// <summary>
/// An enum with all possible locations that a custom HTML snippet can be added to the page.
/// </summary>
public enum PageWidgetLocations
{
    /// <summary>
    /// Load the snippet at the top of the header element of the page.
    /// </summary>
    HeaderTop,
    
    /// <summary>
    /// Load the snippet at the bottom of the header element of the page.
    /// This is the default.
    /// </summary>
    HeaderBottom,
    
    /// <summary>
    /// Load the snippet at the top of the body element of the page.
    /// </summary>
    BodyTop,
    
    /// <summary>
    /// Load the snippet at the bottom of the body element of the page.
    /// </summary>
    BodyBottom
}