namespace GeeksCoreLibrary.Modules.Templates.Enums;

/// <summary>
/// The possible SQL trigger events.
/// </summary>
public enum TriggerEvents
{
    Unknown = 0,

    /// <summary>
    /// The trigger will fire when a new record is being inserted or is about to be inserted.
    /// </summary>
    Insert = 1,

    /// <summary>
    /// The trigger will fire when data of a row updated or is about to be updated.
    /// </summary>
    Update = 2,

    /// <summary>
    /// The trigger will fire when a record is deleted or is about to be deleted.
    /// </summary>
    Delete = 3
}