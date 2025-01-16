namespace GeeksCoreLibrary.Modules.Templates.Enums;

public enum ResourceInsertModes
{
    /// <summary>
    /// Load the script/css in the header as a link.
    /// </summary>
    Standard,

    /// <summary>
    /// Load the script/css in the header as inline css/scripts.
    /// </summary>
    InlineHead,

    /// <summary>
    /// Load the script/css at the end of the &lt;body&gt; with the async attribute.
    /// </summary>
    AsyncFooter,

    /// <summary>
    /// Load the script/css at the end of the &lt;body&gt; without the async attribute.
    /// </summary>
    SyncFooter
}