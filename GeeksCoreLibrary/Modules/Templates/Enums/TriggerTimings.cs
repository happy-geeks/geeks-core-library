namespace GeeksCoreLibrary.Modules.Templates.Enums;

/// <summary>
/// The possible SQL trigger timings.
/// </summary>
public enum TriggerTimings
{
    Unknown = 0,

    /// <summary>
    /// The trigger will fire after the data has been manipulated.
    /// </summary>
    After = 1,

    /// <summary>
    /// The trigger will fire before the data has been manipulated.
    /// </summary>
    Before = 2
}